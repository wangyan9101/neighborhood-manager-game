using System.Collections.Generic;
using NeighborhoodManager.Configs;
using NeighborhoodManager.Core;
using NeighborhoodManager.Models;
using NeighborhoodManager.Systems;
using NeighborhoodManager.Utilities;
using NUnit.Framework;
using UnityEngine;

namespace NeighborhoodManager.Tests
{
    public sealed class Task2CoreRulesTests
    {
        private readonly List<ScriptableObject> createdObjects = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject createdObject in createdObjects)
                Object.DestroyImmediate(createdObject);
            createdObjects.Clear();
        }

        [TestCase(100, 800)]
        [TestCase(80, 800)]
        [TestCase(79, 600)]
        [TestCase(46, 600)]
        [TestCase(45, 400)]
        [TestCase(0, 400)]
        public void DailyIncomeUsesConfiguredBoundaries(int satisfaction, int expectedIncome)
        {
            GameBalanceConfig balance = CreateBalance();
            GameState state = CreateInitializedState(balance);
            state.Resources.Satisfaction = satisfaction;
            state.DayStartSnapshot = DayResourceSnapshot.From(state.Resources);
            var resources = new ResourceSystem(state.Resources);
            resources.ResetDailyAccounting();
            var reports = new ReportSystem(state, balance, resources);
            int budgetBefore = state.Resources.Budget;

            DayReportModel report = reports.CreateDayReport(0, 0);

            Assert.That(report.Income, Is.EqualTo(expectedIncome));
            Assert.That(state.Resources.Budget, Is.EqualTo(budgetBefore + expectedIncome));
        }

        [Test]
        public void DailyIncomeIsAppliedOnlyOnceWhenSettlementIsTickedAgain()
        {
            GameBalanceConfig balance = CreateBalance();
            balance.DayLengthSeconds = 0.1f;
            GameSession session = CreateSession(balance);
            session.StartNewGame();
            session.State.Resources.Satisfaction = 80;

            session.Tick(0.2f);
            int settledBudget = session.State.Resources.Budget;
            session.Tick(0.2f);

            Assert.That(session.State.Phase, Is.EqualTo(GamePhase.DaySettlement));
            Assert.That(settledBudget, Is.EqualTo(balance.InitialBudget + 800));
            Assert.That(session.State.Resources.Budget, Is.EqualTo(settledBudget));
            Assert.That(session.State.Reports, Has.Count.EqualTo(1));
        }

        [TestCase(1, EventState.Pending, false)]
        [TestCase(0, EventState.Pending, true)]
        [TestCase(-1, EventState.Pending, true)]
        [TestCase(0, EventState.Handling, false)]
        [TestCase(0, EventState.Completed, false)]
        public void BudgetFailureRequiresNonPositiveBudgetAndPendingEvent(
            int budget, EventState eventState, bool expectedFailure)
        {
            GameSession session = CreateStartedSession();
            session.State.Resources.Budget = budget;
            session.State.ActiveEvents.Add(CreateRuntimeEvent(eventState));

            session.Tick(0.01f);

            Assert.That(session.State.Phase == GamePhase.Failed, Is.EqualTo(expectedFailure));
        }

        [Test]
        public void ZeroBudgetWithoutEventsDoesNotFail()
        {
            GameSession session = CreateStartedSession();
            session.State.Resources.Budget = 0;

            session.Tick(0.01f);

            Assert.That(session.State.Phase, Is.EqualTo(GamePhase.Playing));
        }

        [Test]
        public void BudgetFailureDoesNotRepeatAfterGameHasEnded()
        {
            GameSession session = CreateStartedSession();
            session.State.Resources.Budget = 0;
            session.State.ActiveEvents.Add(CreateRuntimeEvent(EventState.Pending));
            int endedCount = 0;
            session.GameEnded += _ => endedCount++;

            session.Tick(0.01f);
            session.Tick(0.01f);

            Assert.That(endedCount, Is.EqualTo(1));
        }

        [Test]
        public void SettlementPhaseDoesNotRunBudgetFailureCheck()
        {
            GameSession session = CreateStartedSession();
            session.State.Resources.Budget = 0;
            session.State.ActiveEvents.Add(CreateRuntimeEvent(EventState.Pending));
            session.State.Phase = GamePhase.DaySettlement;

            session.Tick(0.01f);

            Assert.That(session.State.Phase, Is.EqualTo(GamePhase.DaySettlement));
        }

        [TestCase(WorkerType.Repairman, 20f)]
        [TestCase(WorkerType.Security, 32f)]
        public void HandleDurationHasOneAuthoritativeMultiplierApplication(WorkerType workerType, float expectedDuration)
        {
            GameBalanceConfig balance = CreateBalance();
            EventConfig eventConfig = CreateEvent();
            eventConfig.BaseHandleDuration = 20f;
            WorkerConfig workerConfig = CreateWorker(workerType);
            GameState state = CreateInitializedState(balance);
            var resources = new ResourceSystem(state.Resources);
            var workers = new WorkerSystem(state.Workers);
            workers.Initialize(new List<WorkerConfig> { workerConfig });
            var events = new EventSystem(state, new List<EventConfig>(), balance, resources, workers, new FixedRandomSource());
            var dispatch = new DispatchSystem(state, events, workers, resources);

            float duration = dispatch.CalculateHandleDuration(eventConfig, workers.GetById(workerConfig.WorkerId));

            Assert.That(duration, Is.EqualTo(expectedDuration).Within(0.001f));
        }

        [Test]
        public void ResourceDeltaFormattingUsesSignsOrderAndOmitsZero()
        {
            var delta = new ResourceDelta(-350, 6, -2, 8);

            string text = ResourceDeltaFormatter.Format(delta);

            Assert.That(text, Is.EqualTo("预算 -350，满意度 +6，投诉 -2，设备健康 +8"));
            Assert.That(ResourceDeltaFormatter.Format(new ResourceDelta(0, 0, 0, 0)), Is.EqualTo("无资源变化"));
        }

        [Test]
        public void AppliedEventDeltaReportsActualClampedChange()
        {
            GameBalanceConfig balance = CreateBalance();
            GameState state = CreateInitializedState(balance);
            state.Resources.Satisfaction = 98;
            EventConfig eventConfig = CreateEvent();
            eventConfig.SuccessSatisfactionDelta = 6;
            var resources = new ResourceSystem(state.Resources);

            ResourceDelta delta = resources.ApplyEventImpact(eventConfig, true);

            Assert.That(state.Resources.Satisfaction, Is.EqualTo(100));
            Assert.That(delta.Satisfaction, Is.EqualTo(2));
        }

        [Test]
        public void EventCompletionAppliesImpactAndLogsOnlyOnce()
        {
            EventTestContext context = CreateEventContext(EventState.Handling);
            context.Event.HandlingRemainingTime = 0.1f;
            context.Event.Config.SuccessSatisfactionDelta = 6;
            var logs = new List<string>();
            context.Events.LogAdded += logs.Add;
            int before = context.State.Resources.Satisfaction;

            context.Events.Tick(0.2f);
            context.Events.Tick(0.2f);

            Assert.That(context.State.Resources.Satisfaction, Is.EqualTo(before + 6));
            Assert.That(context.Events.DailyCompletedCount, Is.EqualTo(1));
            Assert.That(logs, Has.Count.EqualTo(1));
            StringAssert.Contains("满意度 +6", logs[0]);
        }

        [Test]
        public void EventFailureAppliesImpactAndLogsOnlyOnce()
        {
            EventTestContext context = CreateEventContext(EventState.Pending);
            context.Event.PendingRemainingTime = 0.1f;
            context.Event.Config.FailureComplaintDelta = 3;
            var logs = new List<string>();
            context.Events.LogAdded += logs.Add;
            int before = context.State.Resources.ComplaintCount;

            context.Events.Tick(0.2f);
            context.Events.Tick(0.2f);

            Assert.That(context.State.Resources.ComplaintCount, Is.EqualTo(before + 3));
            Assert.That(context.Events.DailyFailedCount, Is.EqualTo(1));
            Assert.That(logs, Has.Count.EqualTo(1));
            StringAssert.Contains("投诉 +3", logs[0]);
        }

        [Test]
        public void DayReportContainsOnlyTriggeredIncomeReasons()
        {
            GameBalanceConfig balance = CreateBalance();
            GameState state = CreateInitializedState(balance);
            state.Resources.Satisfaction = 80;
            state.DayStartSnapshot = DayResourceSnapshot.From(state.Resources);
            var reports = new ReportSystem(state, balance, new ResourceSystem(state.Resources));

            DayReportModel report = reports.CreateDayReport(0, 0);

            Assert.That(report.Reasons, Has.Count.EqualTo(2));
            StringAssert.Contains("基础物业收入", report.Reasons[0]);
            StringAssert.Contains("收入奖励", report.Reasons[1]);
        }

        [Test]
        public void SuggestionsUseBoundaryValuesAndFixedOrder()
        {
            ReportSystem reports = CreateReportSystem(out GameState state);
            state.Resources.Budget = 999;
            state.Resources.ComplaintCount = 8;
            state.Resources.FacilityHealth = 50;

            List<string> suggestions = reports.CreateTomorrowSuggestions(state.Resources);

            Assert.That(suggestions, Has.Count.EqualTo(3));
            StringAssert.StartsWith("预算", suggestions[0]);
            StringAssert.StartsWith("投诉", suggestions[1]);
            StringAssert.StartsWith("设备", suggestions[2]);
        }

        [Test]
        public void SuggestionThresholdsAreExclusiveOrInclusiveAsSpecified()
        {
            ReportSystem reports = CreateReportSystem(out GameState state);
            state.Resources.Budget = 1000;
            state.Resources.ComplaintCount = 7;
            state.Resources.FacilityHealth = 51;

            List<string> suggestions = reports.CreateTomorrowSuggestions(state.Resources);

            Assert.That(suggestions, Has.Count.EqualTo(1));
            StringAssert.StartsWith("当前运营状态稳定", suggestions[0]);
        }

        [Test]
        public void FinalResultCombinesReportsAndUnreportedCurrentDayCounts()
        {
            ReportSystem reports = CreateReportSystem(out GameState state);
            state.Resources.Budget = 1234;
            state.Resources.Satisfaction = 75;
            state.Resources.ComplaintCount = 4;
            state.Resources.FacilityHealth = 66;
            state.Reports.Add(new DayReportModel { CompletedEventCount = 3, FailedEventCount = 2 });
            state.ActiveEvents.Add(CreateRuntimeEvent(EventState.Pending));

            GameResult result = reports.CreateFinalResult(2, 1);

            Assert.That(result.TotalCompletedEvents, Is.EqualTo(5));
            Assert.That(result.TotalFailedEvents, Is.EqualTo(3));
            Assert.That(result.FinalBudget, Is.EqualTo(1234));
            Assert.That(result.FinalSatisfaction, Is.EqualTo(75));
            Assert.That(result.FinalComplaintCount, Is.EqualTo(4));
            Assert.That(result.FinalFacilityHealth, Is.EqualTo(66));
            Assert.That(result.Grade, Is.EqualTo(ReportGrade.C));
        }

        private ReportSystem CreateReportSystem(out GameState state)
        {
            GameBalanceConfig balance = CreateBalance();
            state = CreateInitializedState(balance);
            return new ReportSystem(state, balance, new ResourceSystem(state.Resources));
        }

        private GameSession CreateStartedSession()
        {
            GameSession session = CreateSession(CreateBalance());
            session.StartNewGame();
            return session;
        }

        private GameSession CreateSession(GameBalanceConfig balance)
        {
            var session = new GameSession();
            session.Initialize(new GameState(), balance, new List<EventConfig>(),
                new List<WorkerConfig> { CreateWorker(WorkerType.Repairman) }, new FixedRandomSource());
            return session;
        }

        private EventTestContext CreateEventContext(EventState eventState)
        {
            GameBalanceConfig balance = CreateBalance();
            GameState state = CreateInitializedState(balance);
            EventConfig eventConfig = CreateEvent();
            WorkerConfig workerConfig = CreateWorker(WorkerType.Repairman);
            var resources = new ResourceSystem(state.Resources);
            var workers = new WorkerSystem(state.Workers);
            workers.Initialize(new List<WorkerConfig> { workerConfig });
            var events = new EventSystem(state, new List<EventConfig>(), balance, resources, workers, new FixedRandomSource());
            GameEventRuntime runtimeEvent = CreateRuntimeEvent(eventState);
            runtimeEvent.Config = eventConfig;
            runtimeEvent.EventConfigId = eventConfig.EventId;
            if (eventState == EventState.Handling)
            {
                runtimeEvent.AssignedWorkerId = workerConfig.WorkerId;
                workers.Occupy(workerConfig.WorkerId, runtimeEvent.RuntimeId, runtimeEvent.HandlingRemainingTime);
            }
            state.ActiveEvents.Add(runtimeEvent);
            return new EventTestContext { State = state, Event = runtimeEvent, Events = events };
        }

        private GameState CreateInitializedState(GameBalanceConfig balance)
        {
            var state = new GameState { Phase = GamePhase.Playing, CurrentDay = 1 };
            new ResourceSystem(state.Resources).Initialize(balance);
            state.DayStartSnapshot = DayResourceSnapshot.From(state.Resources);
            return state;
        }

        private GameEventRuntime CreateRuntimeEvent(EventState state)
        {
            return new GameEventRuntime
            {
                RuntimeId = "runtime_test",
                EventConfigId = "TASK2_TEST",
                Config = CreateEvent(),
                State = state,
                PendingRemainingTime = 100f,
                HandlingRemainingTime = 100f,
                AssignedWorkerId = string.Empty,
                CreatedDay = 1
            };
        }

        private GameBalanceConfig CreateBalance()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            createdObjects.Add(config);
            return config;
        }

        private EventConfig CreateEvent()
        {
            EventConfig config = ScriptableObject.CreateInstance<EventConfig>();
            config.EventId = "TASK2_TEST";
            config.DisplayName = "测试事件";
            config.BaseHandleDuration = 10f;
            config.PendingTimeLimit = 100f;
            config.RecommendedWorkerType = WorkerType.Repairman;
            createdObjects.Add(config);
            return config;
        }

        private WorkerConfig CreateWorker(WorkerType workerType)
        {
            WorkerConfig config = ScriptableObject.CreateInstance<WorkerConfig>();
            config.WorkerId = "worker_test";
            config.DisplayName = "测试员工";
            config.WorkerType = workerType;
            config.MatchDurationMultiplier = 1f;
            config.MismatchDurationMultiplier = 1.6f;
            createdObjects.Add(config);
            return config;
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            public int Range(int minimumInclusive, int maximumExclusive) => minimumInclusive;
            public float Range(float minimumInclusive, float maximumInclusive) => maximumInclusive;
        }

        private sealed class EventTestContext
        {
            public GameState State;
            public GameEventRuntime Event;
            public EventSystem Events;
        }
    }
}
