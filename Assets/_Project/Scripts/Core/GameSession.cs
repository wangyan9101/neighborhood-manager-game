using System;
using System.Collections.Generic;
using NeighborhoodManager.Configs;
using NeighborhoodManager.Models;
using NeighborhoodManager.Systems;
using NeighborhoodManager.Utilities;

namespace NeighborhoodManager.Core
{
    public sealed class GameSession
    {
        private GameBalanceConfig balance;
        private IReadOnlyList<EventConfig> eventConfigs;
        private IReadOnlyList<WorkerConfig> workerConfigs;
        private IRandomSource random;
        private ResourceSystem resourceSystem;
        private WorkerSystem workerSystem;
        private EventSystem eventSystem;
        private DispatchSystem dispatchSystem;
        private DaySystem daySystem;
        private ReportSystem reportSystem;
        private GameLoop gameLoop;

        public event Action ResourcesChanged;
        public event Action EventsChanged;
        public event Action WorkersChanged;
        public event Action<string> LogAdded;
        public event Action<DayReportModel> DayEnded;
        public event Action<GameResult> GameEnded;

        public GameState State { get; private set; }

        public void Initialize(GameState state, GameBalanceConfig balanceConfig,
            IReadOnlyList<EventConfig> events, IReadOnlyList<WorkerConfig> workers, IRandomSource randomSource = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            balance = balanceConfig ?? throw new ArgumentNullException(nameof(balanceConfig));
            eventConfigs = events ?? throw new ArgumentNullException(nameof(events));
            workerConfigs = workers ?? throw new ArgumentNullException(nameof(workers));
            random = randomSource ?? new SystemRandomSource();
            BuildSystems();
            State.Phase = GamePhase.Initializing;
        }

        public void StartNewGame()
        {
            EnsureInitialized();
            ResetStateCollections();
            resourceSystem.Initialize(balance);
            workerSystem.Initialize(workerConfigs);
            daySystem.Reset();
            StartNextDay();
        }

        public void Tick(float deltaTime)
        {
            EnsureInitialized();
            gameLoop.Tick(deltaTime);
            TryEndForBudgetFailure();
        }

        public DispatchResult TryDispatch(string eventRuntimeId, string workerId)
        {
            EnsureInitialized();
            DispatchResult result = dispatchSystem.TryDispatch(eventRuntimeId, workerId);
            LogAdded?.Invoke(result.Message);
            return result;
        }

        public float? GetExpectedHandleDuration(string eventRuntimeId, string workerId)
        {
            EnsureInitialized();
            GameEventRuntime gameEvent = eventSystem.GetById(eventRuntimeId);
            WorkerRuntime worker = workerSystem.GetById(workerId);
            if (gameEvent == null || worker == null)
            {
                return null;
            }

            return dispatchSystem.CalculateHandleDuration(gameEvent.Config, worker);
        }

        public void ContinueAfterDayReport()
        {
            EnsureInitialized();
            if (State.Phase != GamePhase.DaySettlement)
            {
                return;
            }

            StartNextDay();
        }

        public void Restart()
        {
            EnsureInitialized();
            BuildSystems();
            StartNewGame();
        }

        private void BuildSystems()
        {
            resourceSystem = new ResourceSystem(State.Resources);
            workerSystem = new WorkerSystem(State.Workers);
            eventSystem = new EventSystem(State, eventConfigs, balance, resourceSystem, workerSystem, random);
            dispatchSystem = new DispatchSystem(State, eventSystem, workerSystem, resourceSystem);
            daySystem = new DaySystem(State, balance);
            reportSystem = new ReportSystem(State, balance, resourceSystem);
            gameLoop = new GameLoop(State, workerSystem, eventSystem, daySystem);

            resourceSystem.Changed += () => ResourcesChanged?.Invoke();
            workerSystem.Changed += () => WorkersChanged?.Invoke();
            eventSystem.Changed += () => EventsChanged?.Invoke();
            eventSystem.LogAdded += message => LogAdded?.Invoke(message);
            gameLoop.DayExpired += EndCurrentDay;
        }

        private void ResetStateCollections()
        {
            State.ActiveEvents.Clear();
            State.Workers.Clear();
            State.Reports.Clear();
            State.TotalGameTime = 0f;
            State.DayStartSnapshot = null;
        }

        private void StartNextDay()
        {
            resourceSystem.ResetDailyAccounting();
            daySystem.StartNextDay();
            eventSystem.BeginDay();
            LogAdded?.Invoke($"第 {State.CurrentDay} 天开始。");
            ResourcesChanged?.Invoke();
            WorkersChanged?.Invoke();
            EventsChanged?.Invoke();
        }

        private void EndCurrentDay()
        {
            State.Phase = GamePhase.DaySettlement;
            DayReportModel report = reportSystem.CreateDayReport(
                eventSystem.DailyCompletedCount, eventSystem.DailyFailedCount);
            State.Reports.Add(report);
            DayEnded?.Invoke(report);
            LogAdded?.Invoke($"第 {State.CurrentDay} 天结算，评级 {report.Grade}。");

            if (daySystem.IsFinalDay)
            {
                GameResult result = reportSystem.CreateFinalResult();
                State.Phase = result.IsVictory ? GamePhase.Victory : GamePhase.Failed;
                GameEnded?.Invoke(result);
            }
        }

        private void TryEndForBudgetFailure()
        {
            if (State.Phase != GamePhase.Playing || State.Resources.Budget > 0
                || !State.ActiveEvents.Exists(gameEvent => gameEvent.State == EventState.Pending))
            {
                return;
            }

            GameResult result = reportSystem.CreateFinalResult(
                eventSystem.DailyCompletedCount, eventSystem.DailyFailedCount, true);
            State.Phase = GamePhase.Failed;
            LogAdded?.Invoke(result.Message);
            GameEnded?.Invoke(result);
        }

        private void EnsureInitialized()
        {
            if (State == null || balance == null)
            {
                throw new InvalidOperationException("GameSession 尚未初始化。");
            }
        }
    }
}
