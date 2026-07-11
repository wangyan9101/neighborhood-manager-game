using NeighborhoodManager.Models;
using UnityEngine;

namespace NeighborhoodManager.Configs
{
    [CreateAssetMenu(menuName = "Neighborhood Manager/Event", fileName = "EventConfig")]
    public sealed class EventConfig : ScriptableObject
    {
        public string EventId;
        public string DisplayName;
        [TextArea] public string Description;
        public GameEventType EventType;
        public FacilityType FacilityType;
        public EventUrgency Urgency;
        [Min(1f)] public float PendingTimeLimit = 30f;
        [Min(0.1f)] public float BaseHandleDuration = 12f;
        [Min(0)] public int BudgetCost;
        public WorkerType RecommendedWorkerType;
        public int SuccessBudgetDelta;
        public int SuccessSatisfactionDelta;
        public int SuccessComplaintDelta;
        public int SuccessFacilityHealthDelta;
        public int FailureBudgetDelta;
        public int FailureSatisfactionDelta;
        public int FailureComplaintDelta;
        public int FailureFacilityHealthDelta;
    }
}
