using NeighborhoodManager.Models;
using UnityEngine;

namespace NeighborhoodManager.Configs
{
    [CreateAssetMenu(menuName = "Neighborhood Manager/Worker", fileName = "WorkerConfig")]
    public sealed class WorkerConfig : ScriptableObject
    {
        public string WorkerId;
        public string DisplayName;
        public WorkerType WorkerType;
        [Min(0.1f)] public float MatchDurationMultiplier = 1f;
        [Min(0.1f)] public float MismatchDurationMultiplier = 1.6f;
    }
}
