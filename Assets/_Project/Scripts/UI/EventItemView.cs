using System;
using NeighborhoodManager.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NeighborhoodManager.UI
{
    public sealed class EventItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Image background;
        [SerializeField] private Color normalCountdownColor = Color.white;
        [SerializeField] private Color warningCountdownColor = new Color(1f, 0.82f, 0.2f);
        [SerializeField] private Color criticalCountdownColor = new Color(1f, 0.3f, 0.25f);
        [SerializeField] private Color normalBackgroundColor = new Color(0.12f, 0.16f, 0.22f, 0.9f);
        [SerializeField] private Color selectedBackgroundColor = new Color(0.25f, 0.55f, 0.85f, 0.9f);
        private string runtimeId;
        private Action<string> selected;

        public void Configure(TMP_Text title, TMP_Text detail, Button button, Image backgroundImage)
        {
            titleText = title;
            detailText = detail;
            selectButton = button;
            background = backgroundImage;
            if (Application.isPlaying)
            {
                BindButton();
            }
        }

        private void Awake() => BindButton();

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(Select);
            }
        }

        public void Bind(GameEventRuntime gameEvent, bool isSelected, Action<string> onSelected)
        {
            runtimeId = gameEvent.RuntimeId;
            selected = onSelected;
            float remaining = gameEvent.State == EventState.Handling
                ? gameEvent.HandlingRemainingTime : gameEvent.PendingRemainingTime;
            float timeLimit = gameEvent.State == EventState.Handling
                ? gameEvent.HandlingDuration : gameEvent.Config.PendingTimeLimit;
            CountdownLevel level = UiTextFormatter.GetCountdownLevel(remaining, timeLimit);
            Color countdownColor = level == CountdownLevel.Critical ? criticalCountdownColor
                : level == CountdownLevel.Warning ? warningCountdownColor : normalCountdownColor;
            string urgent = gameEvent.Config.Urgency == EventUrgency.Urgent ? "[紧急]" : string.Empty;
            string timeout = level == CountdownLevel.Critical ? " 即将超时" : string.Empty;
            string countdown = $"<color=#{ColorUtility.ToHtmlStringRGB(countdownColor)}>"
                + $"{Mathf.CeilToInt(Mathf.Max(0f, remaining))} 秒{timeout}</color>";
            titleText.text = $"{urgent}[{UiTextFormatter.FormatEventType(gameEvent.Config.EventType)}] "
                + $"{gameEvent.Config.DisplayName}    {countdown}";
            detailText.text = $"{UiTextFormatter.FormatEventState(gameEvent.State)} | 推荐："
                + $"{UiTextFormatter.FormatWorkerType(gameEvent.Config.RecommendedWorkerType)} | 成本：¥{gameEvent.Config.BudgetCost}\n"
                + $"成功：{UiTextFormatter.FormatEventImpact(gameEvent.Config, true)}\n"
                + $"失败：{UiTextFormatter.FormatEventImpact(gameEvent.Config, false)}";
            selectButton.interactable = gameEvent.State == EventState.Pending;
            background.color = isSelected ? selectedBackgroundColor : normalBackgroundColor;
        }

        private void Select() => selected?.Invoke(runtimeId);

        private void BindButton()
        {
            if (selectButton == null)
            {
                return;
            }

            selectButton.onClick.RemoveListener(Select);
            selectButton.onClick.AddListener(Select);
        }
    }
}
