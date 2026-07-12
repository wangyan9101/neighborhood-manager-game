using System.Collections.Generic;
using NeighborhoodManager.Configs;
using NeighborhoodManager.Models;
using NeighborhoodManager.UI;
using NUnit.Framework;
using UnityEngine;

namespace NeighborhoodManager.Tests
{
    public sealed class Task3UiFormattingTests
    {
        private readonly List<ScriptableObject> createdObjects = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject createdObject in createdObjects)
                Object.DestroyImmediate(createdObject);
            createdObjects.Clear();
        }

        [TestCase(10f, 10f, CountdownLevel.Normal)]
        [TestCase(5.1f, 10f, CountdownLevel.Normal)]
        [TestCase(5f, 10f, CountdownLevel.Warning)]
        [TestCase(2.6f, 10f, CountdownLevel.Warning)]
        [TestCase(2.5f, 10f, CountdownLevel.Critical)]
        [TestCase(0f, 10f, CountdownLevel.Critical)]
        [TestCase(-1f, 10f, CountdownLevel.Critical)]
        [TestCase(5f, 0f, CountdownLevel.Critical)]
        public void CountdownLevelUsesSafeBoundaries(float remaining, float limit, CountdownLevel expected)
        {
            Assert.That(UiTextFormatter.GetCountdownLevel(remaining, limit), Is.EqualTo(expected));
        }

        [Test]
        public void EventImpactSummaryUsesConfigValuesAndOmitsZero()
        {
            EventConfig config = CreateEvent();
            config.SuccessSatisfactionDelta = 6;
            config.SuccessComplaintDelta = -2;
            config.FailureSatisfactionDelta = -10;
            config.FailureComplaintDelta = 3;
            config.FailureFacilityHealthDelta = -8;

            string success = UiTextFormatter.FormatEventImpact(config, true);
            string failure = UiTextFormatter.FormatEventImpact(config, false);

            Assert.That(success, Is.EqualTo("满意度 +6，投诉 -2"));
            Assert.That(failure, Is.EqualTo("满意度 -10，投诉 +3，设备健康 -8"));
        }

        [TestCase(EventState.Pending, "待处理")]
        [TestCase(EventState.Handling, "处理中")]
        [TestCase(EventState.Completed, "已完成")]
        [TestCase(EventState.Failed, "已失败")]
        public void EventStatesHaveReadableChineseLabels(EventState state, string expected)
        {
            Assert.That(UiTextFormatter.FormatEventState(state), Is.EqualTo(expected));
        }

        [TestCase(WorkerType.Repairman, "维修工")]
        [TestCase(WorkerType.Security, "保安")]
        [TestCase(WorkerType.CustomerService, "客服")]
        public void WorkerTypesHaveReadableChineseLabels(WorkerType type, string expected)
        {
            Assert.That(UiTextFormatter.FormatWorkerType(type), Is.EqualTo(expected));
        }

        [Test]
        public void DayReportTextPreservesReasonsSuggestionsAndSigns()
        {
            var report = new DayReportModel
            {
                DayIndex = 2,
                Income = 800,
                Expense = 350,
                BudgetDelta = 450,
                SatisfactionDelta = -4,
                ComplaintDelta = 2,
                FacilityHealthDelta = -8,
                Grade = ReportGrade.B,
                Reasons = new List<string> { "基础物业收入：+600", "满意度达到 80：收入奖励 +200" },
                TomorrowSuggestions = new List<string> { "第一条", "第二条", "第三条" }
            };

            string text = UiTextFormatter.FormatDayReport(report);

            StringAssert.Contains("预算：+450", text);
            StringAssert.Contains("满意度：-4", text);
            Assert.That(text.IndexOf("第一条", System.StringComparison.Ordinal),
                Is.LessThan(text.IndexOf("第二条", System.StringComparison.Ordinal)));
            Assert.That(text.IndexOf("第二条", System.StringComparison.Ordinal),
                Is.LessThan(text.IndexOf("第三条", System.StringComparison.Ordinal)));
            StringAssert.Contains("收入奖励 +200", text);
        }

        [Test]
        public void EmptySuggestionsUseStableFallback()
        {
            Assert.That(UiTextFormatter.FormatSuggestions(new List<string>()),
                Does.Contain("当前运营状态稳定"));
        }

        [Test]
        public void FinalResultTextUsesFinalModelStatisticsAndGrade()
        {
            var result = new GameResult
            {
                IsVictory = true,
                Message = "五天运营目标达成！",
                Grade = ReportGrade.A,
                TotalCompletedEvents = 16,
                TotalFailedEvents = 5,
                FinalBudget = 420,
                FinalSatisfaction = 61,
                FinalComplaintCount = 9,
                FinalFacilityHealth = 54
            };

            string text = UiTextFormatter.FormatFinalResult(result);

            StringAssert.Contains("评级：A", text);
            StringAssert.Contains("完成事件：16", text);
            StringAssert.Contains("失败事件：5", text);
            StringAssert.Contains("最终预算：420", text);
            StringAssert.Contains("最终设备健康：54", text);
        }

        [TestCase("已派维修工处理电梯故障，预算 -350。", "[派工]")]
        [TestCase("维修工完成电梯故障：满意度 +6", "[完成]")]
        [TestCase("电梯故障超时失败：满意度 -10", "[失败]")]
        public void ResultLogsReceiveReadablePrefixes(string message, string prefix)
        {
            StringAssert.StartsWith(prefix, UiTextFormatter.FormatLogEntry(message));
        }

        private EventConfig CreateEvent()
        {
            EventConfig config = ScriptableObject.CreateInstance<EventConfig>();
            createdObjects.Add(config);
            return config;
        }
    }
}
