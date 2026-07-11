using System;

namespace NeighborhoodManager.Models
{
    [Serializable]
    public sealed class GameResult
    {
        public bool IsVictory;
        public string Message;
        public int FinalBudget;
        public int FinalSatisfaction;
        public int FinalComplaintCount;
        public int FinalFacilityHealth;
        public int TotalCompletedEvents;
        public int TotalFailedEvents;
    }
}
