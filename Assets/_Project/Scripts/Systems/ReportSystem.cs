using System.Collections.Generic;
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
            int income = CalculateDailyIncome(state.Resources.Satisfaction);
            var reasons = new List<string> { $"基础物业收入：+{balance.DailyBaseIncome}" };
            if (state.Resources.Satisfaction >= balance.HighSatisfactionThreshold)
            {
                reasons.Add($"满意度达到 {balance.HighSatisfactionThreshold}：收入奖励 +{balance.HighSatisfactionBonus}");
            }
            else if (state.Resources.Satisfaction <= balance.LowSatisfactionThreshold)
            {
                reasons.Add($"满意度不高于 {balance.LowSatisfactionThreshold}：收入减少 -{balance.LowSatisfactionPenalty}");
            }
            resources.ChangeBudget(income);

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
                Grade = CalculateGrade(failedCount, state.Resources.Satisfaction),
                Reasons = reasons,
                TomorrowSuggestions = CreateTomorrowSuggestions(state.Resources)
            };
        }

        public int CalculateDailyIncome(int satisfaction)
        {
            if (satisfaction >= balance.HighSatisfactionThreshold)
            {
                return balance.DailyBaseIncome + balance.HighSatisfactionBonus;
            }

            if (satisfaction <= balance.LowSatisfactionThreshold)
            {
                return balance.DailyBaseIncome - balance.LowSatisfactionPenalty;
            }

            return balance.DailyBaseIncome;
        }

        public List<string> CreateTomorrowSuggestions(ResourceModel currentResources)
        {
            var suggestions = new List<string>(3);
            if (currentResources.Budget < 1000)
                suggestions.Add("预算紧张，优先处理低成本高收益事件。");
            if (currentResources.ComplaintCount >= 8)
                suggestions.Add("投诉压力较高，优先安排客服和保安处理投诉事件。");
            if (currentResources.FacilityHealth <= 50)
                suggestions.Add("设备状态较差，优先处理设施故障。");
            if (suggestions.Count == 0)
                suggestions.Add("当前运营状态稳定，继续关注紧急事件和员工占用。");
            return suggestions;
        }

        public ReportGrade CalculateGrade(int failedCount, int satisfaction)
        {
            if (failedCount == 0 && satisfaction >= 85) return ReportGrade.S;
            if (failedCount <= 1 && satisfaction >= 75) return ReportGrade.A;
            if (failedCount <= 2 && satisfaction >= 60) return ReportGrade.B;
            return satisfaction >= 40 ? ReportGrade.C : ReportGrade.D;
        }

        public GameResult CreateFinalResult(int unreportedCompletedCount = 0,
            int unreportedFailedCount = 0, bool budgetFailure = false)
        {
            bool failed = budgetFailure
                || state.Resources.Satisfaction <= balance.FailureSatisfactionLimit
                || state.Resources.ComplaintCount >= balance.FailureComplaintLimit
                || state.Resources.FacilityHealth <= balance.FailureFacilityHealthLimit;
            int totalCompleted = state.Reports.Sum(report => report.CompletedEventCount) + unreportedCompletedCount;
            int totalFailed = state.Reports.Sum(report => report.FailedEventCount) + unreportedFailedCount;
            return new GameResult
            {
                IsVictory = !failed,
                Message = budgetFailure ? "预算耗尽且仍有待处理事件。"
                    : failed ? "小区运营未达到要求。" : "五天运营目标达成！",
                FinalBudget = state.Resources.Budget,
                FinalSatisfaction = state.Resources.Satisfaction,
                FinalComplaintCount = state.Resources.ComplaintCount,
                FinalFacilityHealth = state.Resources.FacilityHealth,
                TotalCompletedEvents = totalCompleted,
                TotalFailedEvents = totalFailed,
                Grade = CalculateGrade(totalFailed, state.Resources.Satisfaction)
            };
        }
    }
}
