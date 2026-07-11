using System;
using System.Collections.Generic;

namespace NeighborhoodManager.Models
{
    [Serializable]
    public sealed class GameState
    {
        public GamePhase Phase;
        public int CurrentDay;
        public float DayRemainingTime;
        public float TotalGameTime;
        public ResourceModel Resources = new ResourceModel();
        public List<GameEventRuntime> ActiveEvents = new List<GameEventRuntime>();
        public List<WorkerRuntime> Workers = new List<WorkerRuntime>();
        public List<DayReportModel> Reports = new List<DayReportModel>();
        public DayResourceSnapshot DayStartSnapshot;
    }
}
