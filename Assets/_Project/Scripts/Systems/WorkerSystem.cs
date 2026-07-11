using System;
using System.Collections.Generic;
using NeighborhoodManager.Configs;
using NeighborhoodManager.Models;

namespace NeighborhoodManager.Systems
{
    public sealed class WorkerSystem
    {
        private readonly List<WorkerRuntime> workers;
        private readonly Dictionary<string, WorkerConfig> configs = new Dictionary<string, WorkerConfig>();

        public event Action Changed;

        public WorkerSystem(List<WorkerRuntime> workers)
        {
            this.workers = workers ?? throw new ArgumentNullException(nameof(workers));
        }

        public IReadOnlyList<WorkerRuntime> Workers => workers;

        public void Initialize(IReadOnlyList<WorkerConfig> workerConfigs)
        {
            workers.Clear();
            configs.Clear();

            foreach (WorkerConfig config in workerConfigs)
            {
                if (config == null || string.IsNullOrWhiteSpace(config.WorkerId))
                {
                    continue;
                }

                configs[config.WorkerId] = config;
                workers.Add(new WorkerRuntime
                {
                    WorkerId = config.WorkerId,
                    WorkerName = config.DisplayName,
                    WorkerType = config.WorkerType,
                    State = WorkerState.Idle,
                    CurrentEventRuntimeId = string.Empty
                });
            }

            Changed?.Invoke();
        }

        public WorkerRuntime GetById(string workerId) => workers.Find(worker => worker.WorkerId == workerId);

        public WorkerConfig GetConfig(string workerId)
        {
            configs.TryGetValue(workerId, out WorkerConfig config);
            return config;
        }

        public bool IsIdle(string workerId)
        {
            WorkerRuntime worker = GetById(workerId);
            return worker != null && worker.State == WorkerState.Idle;
        }

        public bool Occupy(string workerId, string eventRuntimeId, float duration)
        {
            WorkerRuntime worker = GetById(workerId);
            if (worker == null || worker.State != WorkerState.Idle)
            {
                return false;
            }

            worker.State = WorkerState.Working;
            worker.CurrentEventRuntimeId = eventRuntimeId;
            worker.WorkRemainingTime = duration;
            Changed?.Invoke();
            return true;
        }

        public void Tick(float deltaTime)
        {
            foreach (WorkerRuntime worker in workers)
            {
                if (worker.State != WorkerState.Working)
                {
                    continue;
                }

                worker.WorkRemainingTime = Math.Max(0f, worker.WorkRemainingTime - deltaTime);
            }
        }

        public void Release(string workerId)
        {
            WorkerRuntime worker = GetById(workerId);
            if (worker == null)
            {
                return;
            }

            worker.State = WorkerState.Idle;
            worker.CurrentEventRuntimeId = string.Empty;
            worker.WorkRemainingTime = 0f;
            Changed?.Invoke();
        }
    }
}
