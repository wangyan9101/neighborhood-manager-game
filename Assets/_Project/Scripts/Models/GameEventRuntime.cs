using System;
using NeighborhoodManager.Configs;

namespace NeighborhoodManager.Models
{
    [Serializable]
    public sealed class GameEventRuntime
    {
        public string RuntimeId;
        public string EventConfigId;
        public EventConfig Config;
        public EventState State;
        public float PendingRemainingTime;
        public float HandlingRemainingTime;
        public string AssignedWorkerId;
        public int CreatedDay;
    }
}
