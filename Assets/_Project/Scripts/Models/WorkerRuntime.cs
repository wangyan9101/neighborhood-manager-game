using System;

namespace NeighborhoodManager.Models
{
    [Serializable]
    public sealed class WorkerRuntime
    {
        public string WorkerId;
        public string WorkerName;
        public WorkerType WorkerType;
        public WorkerState State;
        public string CurrentEventRuntimeId;
        public float WorkRemainingTime;
    }
}
