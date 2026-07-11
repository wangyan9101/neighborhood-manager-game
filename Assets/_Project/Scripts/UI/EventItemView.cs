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
            titleText.text = gameEvent.Config.DisplayName;
            float remaining = gameEvent.State == EventState.Handling
                ? gameEvent.HandlingRemainingTime : gameEvent.PendingRemainingTime;
            detailText.text = $"{gameEvent.Config.FacilityType} | {gameEvent.Config.Urgency} | {gameEvent.State}\n"
                + $"剩余 {Mathf.CeilToInt(remaining)} 秒 | 成本 ¥{gameEvent.Config.BudgetCost} | 推荐 {gameEvent.Config.RecommendedWorkerType}";
            selectButton.interactable = gameEvent.State == EventState.Pending;
            background.color = isSelected ? new Color(0.25f, 0.55f, 0.85f, 0.9f) : new Color(0.12f, 0.16f, 0.22f, 0.9f);
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
