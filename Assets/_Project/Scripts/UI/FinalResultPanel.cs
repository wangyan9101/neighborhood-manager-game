using System;
using NeighborhoodManager.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NeighborhoodManager.UI
{
    public sealed class FinalResultPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button restartButton;
        private Action restarted;

        public void Configure(TMP_Text text, Button button)
        {
            resultText = text;
            restartButton = button;
            if (Application.isPlaying)
            {
                BindButton();
            }
        }

        private void Awake() => BindButton();

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(Restart);
            }
        }

        public void Show(GameResult result, Action onRestart)
        {
            restarted = onRestart;
            resultText.text = $"{(result.IsVictory ? "胜利" : "失败")}\n{result.Message}\n预算：{result.FinalBudget}\n"
                + $"满意度：{result.FinalSatisfaction}  投诉：{result.FinalComplaintCount}\n设备健康：{result.FinalFacilityHealth}\n"
                + $"完成事件：{result.TotalCompletedEvents}  失败事件：{result.TotalFailedEvents}";
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Restart()
        {
            Hide();
            restarted?.Invoke();
        }

        private void BindButton()
        {
            if (restartButton == null)
            {
                return;
            }

            restartButton.onClick.RemoveListener(Restart);
            restartButton.onClick.AddListener(Restart);
        }
    }
}
