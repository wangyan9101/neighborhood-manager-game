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
            reportText.text = UiTextFormatter.FormatDayReport(report);
            continueButton.interactable = true;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Continue()
        {
            if (!continueButton.interactable) return;
            continueButton.interactable = false;
            Hide();
            Action callback = continued;
            continued = null;
            callback?.Invoke();
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
