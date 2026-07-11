using NeighborhoodManager.Models;

namespace NeighborhoodManager.Systems
{
    public sealed class DispatchSystem
    {
        private readonly GameState state;
        private readonly EventSystem eventSystem;
        private readonly WorkerSystem workerSystem;
        private readonly ResourceSystem resourceSystem;

        public DispatchSystem(GameState state, EventSystem eventSystem, WorkerSystem workerSystem, ResourceSystem resourceSystem)
        {
            this.state = state;
            this.eventSystem = eventSystem;
            this.workerSystem = workerSystem;
            this.resourceSystem = resourceSystem;
        }

        public DispatchResult TryDispatch(string eventRuntimeId, string workerId)
        {
            if (state.Phase != GamePhase.Playing)
            {
                return DispatchResult.Failed("当前不在游戏进行阶段。");
            }

            GameEventRuntime gameEvent = eventSystem.GetById(eventRuntimeId);
            if (gameEvent == null)
            {
                return DispatchResult.Failed("事件不存在。");
            }

            if (gameEvent.State != EventState.Pending)
            {
                return DispatchResult.Failed("事件已不处于待处理状态。");
            }

            WorkerRuntime worker = workerSystem.GetById(workerId);
            if (worker == null)
            {
                return DispatchResult.Failed("员工不存在。");
            }

            if (worker.State != WorkerState.Idle)
            {
                return DispatchResult.Failed("员工正在处理其他事件。");
            }

            if (!resourceSystem.HasBudget(gameEvent.Config.BudgetCost))
            {
                return DispatchResult.Failed("预算不足。");
            }

            var workerConfig = workerSystem.GetConfig(workerId);
            if (workerConfig == null)
            {
                return DispatchResult.Failed("员工配置不存在。");
            }

            resourceSystem.TrySpend(gameEvent.Config.BudgetCost);
            float multiplier = worker.WorkerType == gameEvent.Config.RecommendedWorkerType
                ? workerConfig.MatchDurationMultiplier
                : workerConfig.MismatchDurationMultiplier;
            float duration = gameEvent.Config.BaseHandleDuration * multiplier;

            gameEvent.State = EventState.Handling;
            gameEvent.HandlingRemainingTime = duration;
            gameEvent.AssignedWorkerId = workerId;
            workerSystem.Occupy(workerId, eventRuntimeId, duration);
            eventSystem.NotifyChanged();
            return DispatchResult.Succeeded($"已派{worker.WorkerName}处理{gameEvent.Config.DisplayName}，预算 -{gameEvent.Config.BudgetCost}。");
        }
    }
}
