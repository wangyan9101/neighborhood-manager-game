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
    public sealed class CoreSystemsTests
    {
        private readonly List<ScriptableObject> createdObjects = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject createdObject in createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }
            createdObjects.Clear();
        }

        [Test]
        public void DispatchFailsWhenBudgetIsInsufficient()
        {
            TestContextData context = CreateContext(WorkerType.Repairman, 100);
            context.State.Resources.Budget = 99;

            DispatchResult result = context.Dispatch.TryDispatch("event_1", "worker_1");

            Assert.That(result.Success, Is.False);
            StringAssert.Contains("预算不足", result.Message);
        }

        [Test]
        public void BusyWorkerCannotBeDispatchedAgain()
        {
            TestContextData context = CreateContext(WorkerType.Repairman, 100);
            context.Workers.Occupy("worker_1", "other_event", 5f);

            DispatchResult result = context.Dispatch.TryDispatch("event_1", "worker_1");

            Assert.That(result.Success, Is.False);
            StringAssert.Contains("正在处理", result.Message);
        }

        [Test]
        public void SuccessfulDispatchReducesBudget()
        {
            TestContextData context = CreateContext(WorkerType.Repairman, 100);
            int before = context.State.Resources.Budget;

            DispatchResult result = context.Dispatch.TryDispatch("event_1", "worker_1");

            Assert.That(result.Success, Is.True);
            Assert.That(context.State.Resources.Budget, Is.EqualTo(before - 100));
        }

        [Test]
        public void MatchingWorkerUsesMatchDuration()
        {
            TestContextData context = CreateContext(WorkerType.Repairman, 100);

            context.Dispatch.TryDispatch("event_1", "worker_1");

            Assert.That(context.Event.HandlingRemainingTime, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void MismatchedWorkerUsesOnePointSixDuration()
        {
            TestContextData context = CreateContext(WorkerType.Security, 100);

            context.Dispatch.TryDispatch("event_1", "worker_1");

            Assert.That(context.Event.HandlingRemainingTime, Is.EqualTo(16f).Within(0.001f));
        }

        [Test]
        public void PendingEventFailsAfterTimeout()
        {
            TestContextData context = CreateContext(WorkerType.Repairman, 100);
            context.Event.PendingRemainingTime = 0.5f;

            context.Events.Tick(0.6f);

            Assert.That(context.Event.State, Is.EqualTo(EventState.Failed));
            Assert.That(context.Events.DailyFailedCount, Is.EqualTo(1));
        }

        [Test]
        public void CompletedHandlingEventReleasesWorker()
        {
            TestContextData context = CreateContext(WorkerType.Repairman, 100);
            context.Dispatch.TryDispatch("event_1", "worker_1");

            context.Events.Tick(10.1f);

            Assert.That(context.Event.State, Is.EqualTo(EventState.Completed));
            Assert.That(context.Workers.GetById("worker_1").State, Is.EqualTo(WorkerState.Idle));
        }

        [Test]
        public void SatisfactionAndFacilityHealthAreClamped()
        {
            GameState state = new GameState();
            ResourceSystem resources = new ResourceSystem(state.Resources);
            resources.Initialize(CreateBalance());

            resources.ChangeSatisfaction(500);
            resources.ChangeFacilityHealth(-500);

            Assert.That(state.Resources.Satisfaction, Is.EqualTo(100));
            Assert.That(state.Resources.FacilityHealth, Is.EqualTo(0));
        }

        [TestCase(0, 85, ReportGrade.S)]
        [TestCase(1, 75, ReportGrade.A)]
        [TestCase(2, 60, ReportGrade.B)]
        [TestCase(3, 40, ReportGrade.C)]
        [TestCase(0, 39, ReportGrade.D)]
        public void DailyGradeRulesAreCorrect(int failed, int satisfaction, ReportGrade expected)
        {
            GameState state = new GameState();
            GameBalanceConfig balance = CreateBalance();
            ReportSystem reports = new ReportSystem(state, balance, new ResourceSystem(state.Resources));

            Assert.That(reports.CalculateGrade(failed, satisfaction), Is.EqualTo(expected));
        }

        [Test]
        public void FifthDayProducesFinalResult()
        {
            GameBalanceConfig balance = CreateBalance();
            balance.DayLengthSeconds = 0.1f;
            balance.MaxDayCount = 5;
            WorkerConfig worker = CreateWorker(WorkerType.Repairman);
            GameSession session = new GameSession();
            GameResult finalResult = null;
            session.Initialize(new GameState(), balance, new List<EventConfig>(), new List<WorkerConfig> { worker }, new FixedRandomSource());
            session.GameEnded += result => finalResult = result;
            session.StartNewGame();

            for (int day = 1; day <= 5; day++)
            {
                session.Tick(0.2f);
                if (day < 5) session.ContinueAfterDayReport();
            }

            Assert.That(finalResult, Is.Not.Null);
            Assert.That(session.State.Reports.Count, Is.EqualTo(5));
            Assert.That(session.State.Phase, Is.EqualTo(GamePhase.Victory));
        }

        private TestContextData CreateContext(WorkerType actualWorkerType, int eventCost)
        {
            GameBalanceConfig balance = CreateBalance();
            EventConfig eventConfig = CreateEvent(eventCost);
            WorkerConfig workerConfig = CreateWorker(actualWorkerType);
            GameState state = new GameState { Phase = GamePhase.Playing, CurrentDay = 1 };
            ResourceSystem resources = new ResourceSystem(state.Resources);
            resources.Initialize(balance);
            WorkerSystem workers = new WorkerSystem(state.Workers);
            workers.Initialize(new List<WorkerConfig> { workerConfig });
            EventSystem events = new EventSystem(state, new List<EventConfig>(), balance, resources, workers, new FixedRandomSource());
            GameEventRuntime gameEvent = new GameEventRuntime
            {
                RuntimeId = "event_1",
                EventConfigId = eventConfig.EventId,
                Config = eventConfig,
                State = EventState.Pending,
                PendingRemainingTime = eventConfig.PendingTimeLimit,
                CreatedDay = 1
            };
            state.ActiveEvents.Add(gameEvent);
            return new TestContextData
            {
                State = state,
                Event = gameEvent,
                Workers = workers,
                Events = events,
                Dispatch = new DispatchSystem(state, events, workers, resources)
            };
        }

        private GameBalanceConfig CreateBalance()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            createdObjects.Add(config);
            return config;
        }

        private EventConfig CreateEvent(int cost)
        {
            EventConfig config = ScriptableObject.CreateInstance<EventConfig>();
            config.EventId = "TEST_EVENT";
            config.DisplayName = "测试事件";
            config.BudgetCost = cost;
            config.BaseHandleDuration = 10f;
            config.PendingTimeLimit = 10f;
            config.RecommendedWorkerType = WorkerType.Repairman;
            createdObjects.Add(config);
            return config;
        }

        private WorkerConfig CreateWorker(WorkerType type)
        {
            WorkerConfig config = ScriptableObject.CreateInstance<WorkerConfig>();
            config.WorkerId = "worker_1";
            config.DisplayName = "测试员工";
            config.WorkerType = type;
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

        private sealed class TestContextData
        {
            public GameState State;
            public GameEventRuntime Event;
            public WorkerSystem Workers;
            public EventSystem Events;
            public DispatchSystem Dispatch;
        }
    }
}
