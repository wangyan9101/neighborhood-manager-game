using UnityEngine;

namespace NeighborhoodManager.Configs
{
    [CreateAssetMenu(menuName = "Neighborhood Manager/Game Balance", fileName = "GameBalanceConfig")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [Header("Initial Resources")]
        public int InitialBudget = 5000;
        public int InitialSatisfaction = 70;
        public int InitialComplaintCount = 3;
        public int InitialFacilityHealth = 80;

        [Header("Game Length")]
        [Min(1f)] public float DayLengthSeconds = 60f;
        [Min(1)] public int MaxDayCount = 5;

        [Header("Events")]
        [Min(1)] public int MaxActiveEventCount = 5;
        [Min(0.1f)] public float MinEventSpawnInterval = 20f;
        [Min(0.1f)] public float MaxEventSpawnInterval = 40f;

        [Header("Daily Economy")]
        public int DailyBaseIncome = 1000;
        public int HighSatisfactionThreshold = 80;
        public int HighSatisfactionBonus = 300;
        public int LowSatisfactionThreshold = 40;
        public int LowSatisfactionPenalty = 300;

        [Header("Failure Limits")]
        public int FailureSatisfactionLimit = 20;
        public int FailureComplaintLimit = 20;
        public int FailureFacilityHealthLimit = 10;

        private void OnValidate()
        {
            InitialSatisfaction = Mathf.Clamp(InitialSatisfaction, 0, 100);
            InitialComplaintCount = Mathf.Max(0, InitialComplaintCount);
            InitialFacilityHealth = Mathf.Clamp(InitialFacilityHealth, 0, 100);
            MaxEventSpawnInterval = Mathf.Max(MinEventSpawnInterval, MaxEventSpawnInterval);
        }
    }
}
