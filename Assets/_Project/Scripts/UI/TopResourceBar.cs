using NeighborhoodManager.Models;
using TMPro;
using UnityEngine;

namespace NeighborhoodManager.UI
{
    public sealed class TopResourceBar : MonoBehaviour
    {
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text budgetText;
        [SerializeField] private TMP_Text satisfactionText;
        [SerializeField] private TMP_Text complaintText;
        [SerializeField] private TMP_Text healthText;

        public void Configure(TMP_Text day, TMP_Text time, TMP_Text budget, TMP_Text satisfaction,
            TMP_Text complaint, TMP_Text health)
        {
            dayText = day;
            timeText = time;
            budgetText = budget;
            satisfactionText = satisfaction;
            complaintText = complaint;
            healthText = health;
        }

        public void Refresh(GameState state)
        {
            dayText.text = $"第 {state.CurrentDay} 天";
            timeText.text = $"剩余 {Mathf.CeilToInt(state.DayRemainingTime)} 秒";
            budgetText.text = $"预算：¥{state.Resources.Budget}{Warning(state.Resources.Budget <= 500, "紧张")}";
            satisfactionText.text = $"满意度：{state.Resources.Satisfaction}{Warning(state.Resources.Satisfaction <= 30, "危险")}";
            complaintText.text = $"投诉：{state.Resources.ComplaintCount}{Warning(state.Resources.ComplaintCount >= 10, "偏高")}";
            healthText.text = $"设备健康：{state.Resources.FacilityHealth}{Warning(state.Resources.FacilityHealth <= 30, "危险")}";
        }

        private static string Warning(bool active, string label) => active ? $" [{label}]" : string.Empty;
    }
}
