using System;
using NeighborhoodManager.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NeighborhoodManager.UI
{
    public sealed class WorkerItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Button dispatchButton;
        private string workerId;
        private Action<string> dispatched;

        public void Configure(TMP_Text title, TMP_Text detail, Button button)
        {
            titleText = title;
            detailText = detail;
            dispatchButton = button;
            if (Application.isPlaying)
            {
                BindButton();
            }
        }

        private void Awake() => BindButton();

        private void OnDestroy()
        {
            if (dispatchButton != null)
            {
                dispatchButton.onClick.RemoveListener(Dispatch);
            }
        }

        public void Bind(WorkerRuntime worker, bool hasSelectedEvent, Action<string> onDispatch)
        {
            workerId = worker.WorkerId;
            dispatched = onDispatch;
            titleText.text = $"{worker.WorkerName} · {worker.WorkerType}";
            detailText.text = worker.State == WorkerState.Idle
                ? "空闲"
                : $"处理中：{worker.CurrentEventRuntimeId}（{Mathf.CeilToInt(worker.WorkRemainingTime)} 秒）";
            dispatchButton.interactable = hasSelectedEvent && worker.State == WorkerState.Idle;
        }

        private void Dispatch() => dispatched?.Invoke(workerId);

        private void BindButton()
        {
            if (dispatchButton == null)
            {
                return;
            }

            dispatchButton.onClick.RemoveListener(Dispatch);
            dispatchButton.onClick.AddListener(Dispatch);
        }
    }
}
