using System;

namespace NeighborhoodManager.Models
{
    [Serializable]
    public sealed class DayReportModel
    {
        public int DayIndex;
        public int Income;
        public int Expense;
        public int CompletedEventCount;
        public int FailedEventCount;
        public int BudgetDelta;
        public int SatisfactionDelta;
        public int ComplaintDelta;
        public int FacilityHealthDelta;
        public ReportGrade Grade;
    }
}
