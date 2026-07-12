using System.Collections.Generic;
using System.Linq;
using NeighborhoodManager.Configs;
using NeighborhoodManager.Models;
using NUnit.Framework;
using UnityEditor;

namespace NeighborhoodManager.Tests
{
    public sealed class Demo02ConfigurationTests
    {
        private const string BalancePath = "Assets/_Project/Configs/Balance/GameBalanceConfig.asset";
        private const string EventFolder = "Assets/_Project/Configs/Events";
        private const string WorkerFolder = "Assets/_Project/Configs/Workers";

        [Test]
        public void BalanceAssetUsesDemo02Values()
        {
            GameBalanceConfig balance = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(BalancePath);

            Assert.That(balance, Is.Not.Null);
            Assert.That(balance.InitialBudget, Is.EqualTo(2800));
            Assert.That(balance.DailyBaseIncome, Is.EqualTo(600));
            Assert.That(balance.HighSatisfactionThreshold, Is.EqualTo(80));
            Assert.That(balance.HighSatisfactionBonus, Is.EqualTo(200));
            Assert.That(balance.LowSatisfactionThreshold, Is.EqualTo(45));
            Assert.That(balance.LowSatisfactionPenalty, Is.EqualTo(200));
            Assert.That(balance.MinEventSpawnInterval, Is.EqualTo(12f));
            Assert.That(balance.MaxEventSpawnInterval, Is.EqualTo(22f));
            Assert.That(balance.MinEventSpawnInterval, Is.LessThanOrEqualTo(balance.MaxEventSpawnInterval));
            Assert.That(balance.MaxActiveEventCount, Is.EqualTo(4));
            Assert.That(balance.DayLengthSeconds, Is.EqualTo(60f));
            Assert.That(balance.MaxDayCount, Is.EqualTo(5));
        }

        [Test]
        public void WorkerAssetsUseDemo02DurationMultipliers()
        {
            List<WorkerConfig> workers = LoadAssets<WorkerConfig>(WorkerFolder);

            Assert.That(workers, Has.Count.EqualTo(3));
            foreach (WorkerConfig worker in workers)
            {
                Assert.That(worker.MatchDurationMultiplier, Is.EqualTo(1f), worker.WorkerId);
                Assert.That(worker.MismatchDurationMultiplier, Is.EqualTo(1.6f), worker.WorkerId);
            }
        }

        [Test]
        public void ExistingEventsUseDemo02Values()
        {
            AssertEvent("ELEVATOR_BROKEN", WorkerType.Repairman, EventUrgency.Urgent,
                350, 28f, 42f, 6, -2, 8, -10, 3, -8);
            AssertEvent("PARKING_OCCUPIED", WorkerType.Security, EventUrgency.Normal,
                120, 18f, 50f, 3, -2, 0, -5, 3, 0);
            AssertEvent("NOISE_COMPLAINT", WorkerType.CustomerService, EventUrgency.Normal,
                80, 16f, 55f, 2, -2, 0, -4, 2, 0);
            AssertEvent("LOCKER_STUCK", WorkerType.Repairman, EventUrgency.Normal,
                180, 22f, 48f, 3, -1, 3, -5, 3, -2);
            AssertEvent("CAMERA_OFFLINE", WorkerType.Security, EventUrgency.Urgent,
                220, 24f, 65f, 2, -1, 7, -4, 1, -8);
        }

        [Test]
        public void EventAssetsAreValidAndIdsAreUnique()
        {
            List<EventConfig> events = LoadAssets<EventConfig>(EventFolder);
            string[] requiredNewIds = { "ELEV_002", "PARK_002", "CHARGE_002", "GEN_002" };

            Assert.That(events, Has.Count.EqualTo(9));
            Assert.That(events.Select(item => item.EventId).Distinct().Count(), Is.EqualTo(events.Count));
            Assert.That(events.Select(item => item.EventId), Is.SupersetOf(requiredNewIds));
            foreach (EventConfig eventConfig in events)
            {
                Assert.That(eventConfig.EventId, Is.Not.Null.And.Not.Empty, eventConfig.name);
                Assert.That(eventConfig.DisplayName, Is.Not.Null.And.Not.Empty, eventConfig.EventId);
                Assert.That(eventConfig.BaseHandleDuration, Is.GreaterThan(0f), eventConfig.EventId);
                Assert.That(eventConfig.PendingTimeLimit, Is.GreaterThan(0f), eventConfig.EventId);
                Assert.That(eventConfig.BudgetCost, Is.GreaterThanOrEqualTo(0), eventConfig.EventId);
                Assert.That(eventConfig.SuccessBudgetDelta, Is.EqualTo(0), eventConfig.EventId);
                Assert.That(eventConfig.FailureBudgetDelta, Is.EqualTo(0), eventConfig.EventId);
            }
        }

        [Test]
        public void NewEventsUseDemo02ValuesAndExistingEnums()
        {
            AssertEvent("ELEV_002", WorkerType.Repairman, EventUrgency.Normal,
                220, 22f, 55f, 4, -1, 6, -6, 2, -5);
            AssertEvent("PARK_002", WorkerType.Repairman, EventUrgency.Normal,
                240, 25f, 60f, 4, -2, 5, -5, 2, -4);
            AssertEvent("CHARGE_002", WorkerType.Security, EventUrgency.Normal,
                70, 17f, 45f, 3, -2, 0, -4, 2, 0);
            AssertEvent("GEN_002", WorkerType.CustomerService, EventUrgency.Normal,
                100, 18f, 50f, 3, -2, 0, -3, 2, 0);

            Assert.That(LoadEvent("ELEV_002").EventType, Is.EqualTo(GameEventType.Fault));
            Assert.That(LoadEvent("ELEV_002").FacilityType, Is.EqualTo(FacilityType.Elevator));
            Assert.That(LoadEvent("PARK_002").EventType, Is.EqualTo(GameEventType.Fault));
            Assert.That(LoadEvent("PARK_002").FacilityType, Is.EqualTo(FacilityType.ParkingLot));
            Assert.That(LoadEvent("CHARGE_002").EventType, Is.EqualTo(GameEventType.Security));
            Assert.That(LoadEvent("CHARGE_002").FacilityType, Is.EqualTo(FacilityType.ChargingPile));
            Assert.That(LoadEvent("GEN_002").EventType, Is.EqualTo(GameEventType.Environment));
            Assert.That(LoadEvent("GEN_002").FacilityType, Is.EqualTo(FacilityType.General));
        }

        private static void AssertEvent(string id, WorkerType workerType, EventUrgency urgency,
            int cost, float duration, float timeout, int successSatisfaction, int successComplaint, int successHealth,
            int failureSatisfaction, int failureComplaint, int failureHealth)
        {
            EventConfig eventConfig = LoadEvent(id);

            Assert.That(eventConfig, Is.Not.Null, id);
            Assert.That(eventConfig.RecommendedWorkerType, Is.EqualTo(workerType), id);
            Assert.That(eventConfig.Urgency, Is.EqualTo(urgency), id);
            Assert.That(eventConfig.BudgetCost, Is.EqualTo(cost), id);
            Assert.That(eventConfig.BaseHandleDuration, Is.EqualTo(duration), id);
            Assert.That(eventConfig.PendingTimeLimit, Is.EqualTo(timeout), id);
            Assert.That(eventConfig.SuccessBudgetDelta, Is.EqualTo(0), id);
            Assert.That(eventConfig.SuccessSatisfactionDelta, Is.EqualTo(successSatisfaction), id);
            Assert.That(eventConfig.SuccessComplaintDelta, Is.EqualTo(successComplaint), id);
            Assert.That(eventConfig.SuccessFacilityHealthDelta, Is.EqualTo(successHealth), id);
            Assert.That(eventConfig.FailureBudgetDelta, Is.EqualTo(0), id);
            Assert.That(eventConfig.FailureSatisfactionDelta, Is.EqualTo(failureSatisfaction), id);
            Assert.That(eventConfig.FailureComplaintDelta, Is.EqualTo(failureComplaint), id);
            Assert.That(eventConfig.FailureFacilityHealthDelta, Is.EqualTo(failureHealth), id);
        }

        private static EventConfig LoadEvent(string id)
        {
            return AssetDatabase.LoadAssetAtPath<EventConfig>($"{EventFolder}/{id}.asset");
        }

        private static List<T> LoadAssets<T>(string folder) where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(item => item != null)
                .ToList();
        }
    }
}
