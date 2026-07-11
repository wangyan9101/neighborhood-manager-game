using System.Linq;
using NeighborhoodManager.Configs;
using NeighborhoodManager.Models;

namespace NeighborhoodManager.Systems
{
    public sealed class ReportSystem
    {
        private readonly GameState state;
        private readonly GameBalanceConfig balance;
        private readonly ResourceSystem resources;

        public ReportSystem(GameState state, GameBalanceConfig balance, ResourceSystem resources)
        {
            this.state = state;
            this.balance = balance;
            this.resources = resources;
        }

        public DayReportModel CreateDayReport(int completedCount, int failedCount)
        {
            resources.ChangeBudget(balance.DailyBaseIncome);
            if (state.Resources.Satisfaction >= balance.HighSatisfactionThreshold)
            {
                resources.ChangeBudget(balance.HighSatisfactionBonus);
            }
            else if (state.Resources.Satisfaction < balance.LowSatisfactionThreshold)
            {
                resources.ChangeBudget(-balance.LowSatisfactionPenalty);
            }

            DayResourceSnapshot start = state.DayStartSnapshot;
            return new DayReportModel
            {
                DayIndex = state.CurrentDay,
                Income = resources.DailyIncome,
                Expense = resources.DailyExpense,
                CompletedEventCount = completedCount,
                FailedEventCount = failedCount,
                BudgetDelta = state.Resources.Budget - start.Budget,
                SatisfactionDelta = state.Resources.Satisfaction - start.Satisfaction,
                ComplaintDelta = state.Resources.ComplaintCount - start.ComplaintCount,
                FacilityHealthDelta = state.Resources.FacilityHealth - start.FacilityHealth,
                Grade = CalculateGrade(failedCount, state.Resources.Satisfaction)
            };
        }

        public ReportGrade CalculateGrade(int failedCount, int satisfaction)
        {
            if (failedCount == 0 && satisfaction >= 85) return ReportGrade.S;
            if (failedCount <= 1 && satisfaction >= 75) return ReportGrade.A;
            if (failedCount <= 2 && satisfaction >= 60) return ReportGrade.B;
            return satisfaction >= 40 ? ReportGrade.C : ReportGrade.D;
        }

        public GameResult CreateFinalResult()
        {
            bool failed = state.Resources.Satisfaction <= balance.FailureSatisfactionLimit
                || state.Resources.ComplaintCount >= balance.FailureComplaintLimit
                || state.Resources.FacilityHealth <= balance.FailureFacilityHealthLimit;
            return new GameResult
            {
                IsVictory = !failed,
                Message = failed ? "小区运营未达到要求。" : "五天运营目标达成！",
                FinalBudget = state.Resources.Budget,
                FinalSatisfaction = state.Resources.Satisfaction,
                FinalComplaintCount = state.Resources.ComplaintCount,
                FinalFacilityHealth = state.Resources.FacilityHealth,
                TotalCompletedEvents = state.Reports.Sum(report => report.CompletedEventCount),
                TotalFailedEvents = state.Reports.Sum(report => report.FailedEventCount)
            };
        }
    }
}
