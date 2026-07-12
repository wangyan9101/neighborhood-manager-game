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
            resultText.text = UiTextFormatter.FormatFinalResult(result);
            restartButton.interactable = true;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Restart()
        {
            if (!restartButton.interactable) return;
            restartButton.interactable = false;
            Hide();
            Action callback = restarted;
            restarted = null;
            callback?.Invoke();
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
