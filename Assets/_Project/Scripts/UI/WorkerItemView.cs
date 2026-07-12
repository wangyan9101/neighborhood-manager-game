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

        public void Bind(WorkerRuntime worker, GameEventRuntime selectedEvent, GameEventRuntime currentEvent,
            float? expectedDuration, Action<string> onDispatch)
        {
            workerId = worker.WorkerId;
            dispatched = onDispatch;
            titleText.text = $"{worker.WorkerName} · {UiTextFormatter.FormatWorkerType(worker.WorkerType)}";
            if (worker.State == WorkerState.Working)
            {
                string eventName = currentEvent?.Config?.DisplayName ?? worker.CurrentEventRuntimeId;
                detailText.text = $"处理中：{eventName}\n剩余：{Mathf.CeilToInt(Mathf.Max(0f, worker.WorkRemainingTime))} 秒 | 当前不可派工";
            }
            else if (selectedEvent != null)
            {
                string match = worker.WorkerType == selectedEvent.Config.RecommendedWorkerType ? "推荐" : "不匹配";
                string duration = expectedDuration.HasValue ? $"{expectedDuration.Value:0.#} 秒" : "--";
                detailText.text = $"状态：空闲 | {match}\n预计处理时间：{duration}";
            }
            else
            {
                detailText.text = "状态：空闲";
            }

            dispatchButton.interactable = selectedEvent != null && worker.State == WorkerState.Idle;
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
