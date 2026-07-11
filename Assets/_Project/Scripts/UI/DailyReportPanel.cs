using System;
using NeighborhoodManager.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NeighborhoodManager.UI
{
    public sealed class DailyReportPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text reportText;
        [SerializeField] private Button continueButton;
        private Action continued;

        public void Configure(TMP_Text text, Button button)
        {
            reportText = text;
            continueButton = button;
            if (Application.isPlaying)
            {
                BindButton();
            }
        }

        private void Awake() => BindButton();

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(Continue);
            }
        }

        public void Show(DayReportModel report, Action onContinue)
        {
            continued = onContinue;
            reportText.text = $"第 {report.DayIndex} 天结算\n评级：{report.Grade}\n收入：{report.Income}  支出：{report.Expense}\n"
                + $"完成：{report.CompletedEventCount}  失败：{report.FailedEventCount}\n预算变化：{report.BudgetDelta}\n"
                + $"满意度：{report.SatisfactionDelta}  投诉：{report.ComplaintDelta}  健康：{report.FacilityHealthDelta}";
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Continue()
        {
            Hide();
            continued?.Invoke();
        }

        private void BindButton()
        {
            if (continueButton == null)
            {
                return;
            }

            continueButton.onClick.RemoveListener(Continue);
            continueButton.onClick.AddListener(Continue);
        }
    }
}
