using System;
using System.Collections.Generic;

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
        public List<string> Reasons = new List<string>();
        public List<string> TomorrowSuggestions = new List<string>();
    }
}
