using System.Collections.Generic;
using System.Linq;
using NeighborhoodManager.Configs;
using NeighborhoodManager.Models;
using NeighborhoodManager.Utilities;

namespace NeighborhoodManager.UI
{
    public enum CountdownLevel { Normal, Warning, Critical }

    public static class UiTextFormatter
    {
        public static CountdownLevel GetCountdownLevel(float remainingTime, float timeLimit)
        {
            if (timeLimit <= 0f)
                return CountdownLevel.Critical;

            float ratio = UnityEngine.Mathf.Clamp01(remainingTime / timeLimit);
            if (ratio > 0.5f) return CountdownLevel.Normal;
            return ratio > 0.25f ? CountdownLevel.Warning : CountdownLevel.Critical;
        }

        public static string FormatEventImpact(EventConfig config, bool succeeded)
        {
            ResourceDelta delta = succeeded
                ? new ResourceDelta(config.SuccessBudgetDelta, config.SuccessSatisfactionDelta,
                    config.SuccessComplaintDelta, config.SuccessFacilityHealthDelta)
                : new ResourceDelta(config.FailureBudgetDelta, config.FailureSatisfactionDelta,
                    config.FailureComplaintDelta, config.FailureFacilityHealthDelta);
            return ResourceDeltaFormatter.Format(delta);
        }

        public static string FormatEventType(GameEventType type)
        {
            switch (type)
            {
                case GameEventType.Complaint: return "投诉";
                case GameEventType.Fault: return "故障";
                case GameEventType.Security: return "安保";
                case GameEventType.Environment: return "环境";
                default: return type.ToString();
            }
        }

        public static string FormatEventState(EventState state)
        {
            switch (state)
            {
                case EventState.Pending: return "待处理";
                case EventState.Handling: return "处理中";
                case EventState.Completed: return "已完成";
                case EventState.Failed: return "已失败";
                default: return state.ToString();
            }
        }

        public static string FormatWorkerType(WorkerType type)
        {
            switch (type)
            {
                case WorkerType.Repairman: return "维修工";
                case WorkerType.Security: return "保安";
                case WorkerType.CustomerService: return "客服";
                default: return type.ToString();
            }
        }

        public static string FormatSigned(int value) => value > 0 ? $"+{value}" : value.ToString();

        public static string FormatReasons(IReadOnlyList<string> reasons)
        {
            if (reasons == null || reasons.Count == 0) return "- 无额外结算原因";
            return string.Join("\n", reasons.Select(reason => "- " + reason));
        }

        public static string FormatSuggestions(IReadOnlyList<string> suggestions)
        {
            if (suggestions == null || suggestions.Count == 0)
                return "1. 当前运营状态稳定，继续关注紧急事件和员工占用。";

            return string.Join("\n", suggestions.Take(3)
                .Select((suggestion, index) => $"{index + 1}. {suggestion}"));
        }

        public static string FormatDayReport(DayReportModel report)
        {
            return $"第 {report.DayIndex} 天结算    评级：{report.Grade}\n"
                + $"今日收入：{report.Income}    今日支出：{report.Expense}\n"
                + $"完成事件：{report.CompletedEventCount}    失败事件：{report.FailedEventCount}\n"
                + $"预算：{FormatSigned(report.BudgetDelta)}    满意度：{FormatSigned(report.SatisfactionDelta)}\n"
                + $"投诉：{FormatSigned(report.ComplaintDelta)}    设备健康：{FormatSigned(report.FacilityHealthDelta)}\n\n"
                + $"结算原因\n{FormatReasons(report.Reasons)}\n\n"
                + $"明日建议\n{FormatSuggestions(report.TomorrowSuggestions)}";
        }

        public static string FormatFinalResult(GameResult result)
        {
            return $"运营结束 - {(result.IsVictory ? "胜利" : "失败")}\n"
                + $"评级：{result.Grade}\n{result.Message}\n\n"
                + $"完成事件：{result.TotalCompletedEvents}    失败事件：{result.TotalFailedEvents}\n"
                + $"最终预算：{result.FinalBudget}\n最终满意度：{result.FinalSatisfaction}\n"
                + $"最终投诉：{result.FinalComplaintCount}\n最终设备健康：{result.FinalFacilityHealth}";
        }

        public static string FormatLogEntry(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return string.Empty;
            if (message.Contains("超时失败")) return "[失败] " + message;
            if (message.Contains("完成")) return "[完成] " + message;
            if (message.StartsWith("已派")) return "[派工] " + message;
            return message;
        }
    }
}
