using System;
using System.Collections.Generic;
using NeighborhoodManager.Configs;
using NeighborhoodManager.Models;
using NeighborhoodManager.Utilities;

namespace NeighborhoodManager.Systems
{
    public sealed class EventSystem
    {
        private readonly GameState state;
        private readonly IReadOnlyList<EventConfig> eventConfigs;
        private readonly GameBalanceConfig balance;
        private readonly ResourceSystem resourceSystem;
        private readonly WorkerSystem workerSystem;
        private readonly IRandomSource random;
        private float spawnRemainingTime;
        private int nextRuntimeId;

        public event Action Changed;
        public event Action<string> LogAdded;

        public int DailyCompletedCount { get; private set; }
        public int DailyFailedCount { get; private set; }

        public EventSystem(GameState state, IReadOnlyList<EventConfig> eventConfigs, GameBalanceConfig balance,
            ResourceSystem resourceSystem, WorkerSystem workerSystem, IRandomSource random)
        {
            this.state = state;
            this.eventConfigs = eventConfigs;
            this.balance = balance;
            this.resourceSystem = resourceSystem;
            this.workerSystem = workerSystem;
            this.random = random;
        }

        public void BeginDay()
        {
            state.ActiveEvents.RemoveAll(gameEvent => gameEvent.State == EventState.Completed || gameEvent.State == EventState.Failed);
            DailyCompletedCount = 0;
            DailyFailedCount = 0;
            spawnRemainingTime = Math.Min(2f, balance.MinEventSpawnInterval);
            Changed?.Invoke();
        }

        public GameEventRuntime GetById(string runtimeId)
        {
            return state.ActiveEvents.Find(gameEvent => gameEvent.RuntimeId == runtimeId);
        }

        public void Tick(float deltaTime)
        {
            UpdateExistingEvents(deltaTime);
            spawnRemainingTime -= deltaTime;
            if (spawnRemainingTime <= 0f)
            {
                TrySpawn();
                spawnRemainingTime = random.Range(balance.MinEventSpawnInterval, balance.MaxEventSpawnInterval);
            }
        }

        public void NotifyChanged() => Changed?.Invoke();

        private void UpdateExistingEvents(float deltaTime)
        {
            bool stateChanged = false;
            foreach (GameEventRuntime gameEvent in state.ActiveEvents)
            {
                if (gameEvent.State == EventState.Pending)
                {
                    gameEvent.PendingRemainingTime = Math.Max(0f, gameEvent.PendingRemainingTime - deltaTime);
                    if (gameEvent.PendingRemainingTime <= 0f)
                    {
                        Fail(gameEvent);
                        stateChanged = true;
                    }
                }
                else if (gameEvent.State == EventState.Handling)
                {
                    gameEvent.HandlingRemainingTime = Math.Max(0f, gameEvent.HandlingRemainingTime - deltaTime);
                    if (gameEvent.HandlingRemainingTime <= 0f)
                    {
                        Complete(gameEvent);
                        stateChanged = true;
                    }
                }
            }

            if (stateChanged)
            {
                Changed?.Invoke();
            }
        }

        private void TrySpawn()
        {
            int activeCount = state.ActiveEvents.FindAll(gameEvent =>
                gameEvent.State == EventState.Pending || gameEvent.State == EventState.Handling).Count;
            if (eventConfigs.Count == 0 || activeCount >= balance.MaxActiveEventCount)
            {
                return;
            }

            EventConfig config = eventConfigs[random.Range(0, eventConfigs.Count)];
            if (config == null)
            {
                return;
            }

            GameEventRuntime gameEvent = new GameEventRuntime
            {
                RuntimeId = $"event_{++nextRuntimeId:0000}",
                EventConfigId = config.EventId,
                Config = config,
                State = EventState.Pending,
                PendingRemainingTime = config.PendingTimeLimit,
                CreatedDay = state.CurrentDay,
                AssignedWorkerId = string.Empty
            };
            state.ActiveEvents.Add(gameEvent);
            Changed?.Invoke();
            LogAdded?.Invoke($"出现事件：{config.DisplayName}。");
        }

        private void Complete(GameEventRuntime gameEvent)
        {
            gameEvent.State = EventState.Completed;
            WorkerRuntime worker = workerSystem.GetById(gameEvent.AssignedWorkerId);
            ResourceDelta delta = resourceSystem.ApplyEventImpact(gameEvent.Config, true);
            workerSystem.Release(gameEvent.AssignedWorkerId);
            DailyCompletedCount++;
            string actor = worker == null ? string.Empty : worker.WorkerName;
            LogAdded?.Invoke($"{actor}完成{gameEvent.Config.DisplayName}：{ResourceDeltaFormatter.Format(delta)}");
        }

        private void Fail(GameEventRuntime gameEvent)
        {
            gameEvent.State = EventState.Failed;
            ResourceDelta delta = resourceSystem.ApplyEventImpact(gameEvent.Config, false);
            workerSystem.Release(gameEvent.AssignedWorkerId);
            DailyFailedCount++;
            LogAdded?.Invoke($"{gameEvent.Config.DisplayName}超时失败：{ResourceDeltaFormatter.Format(delta)}");
        }
    }
}
