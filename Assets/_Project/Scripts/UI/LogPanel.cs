using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NeighborhoodManager.UI
{
    public sealed class LogPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text logText;
        private readonly Queue<string> messages = new Queue<string>();

        public void Configure(TMP_Text text) => logText = text;

        public void Add(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            messages.Enqueue(UiTextFormatter.FormatLogEntry(message));
            while (messages.Count > 20) messages.Dequeue();
            logText.text = string.Join("\n", messages);
        }
    }
}
