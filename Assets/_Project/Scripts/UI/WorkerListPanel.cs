using System;
using System.Collections.Generic;
using NeighborhoodManager.Models;
using UnityEngine;

namespace NeighborhoodManager.UI
{
    public sealed class WorkerListPanel : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private WorkerItemView itemPrefab;
        private readonly List<WorkerItemView> items = new List<WorkerItemView>();

        public void Configure(Transform contentRoot, WorkerItemView prefab)
        {
            content = contentRoot;
            itemPrefab = prefab;
            if (prefab != null) prefab.gameObject.SetActive(false);
        }

        public void Refresh(IReadOnlyList<WorkerRuntime> workers, GameEventRuntime selectedEvent,
            IReadOnlyList<GameEventRuntime> events, Func<string, float?> getExpectedDuration, Action<string> onDispatch)
        {
            EnsureCapacity(workers.Count);
            for (int index = 0; index < items.Count; index++)
            {
                bool active = index < workers.Count;
                items[index].gameObject.SetActive(active);
                if (active)
                {
                    WorkerRuntime worker = workers[index];
                    GameEventRuntime currentEvent = string.IsNullOrEmpty(worker.CurrentEventRuntimeId) ? null
                        : FindEvent(events, worker.CurrentEventRuntimeId);
                    float? expectedDuration = selectedEvent == null ? null : getExpectedDuration(worker.WorkerId);
                    items[index].Bind(worker, selectedEvent, currentEvent, expectedDuration, onDispatch);
                }
            }
        }

        private static GameEventRuntime FindEvent(IReadOnlyList<GameEventRuntime> events, string runtimeId)
        {
            for (int index = 0; index < events.Count; index++)
            {
                if (events[index].RuntimeId == runtimeId) return events[index];
            }
            return null;
        }

        private void EnsureCapacity(int count)
        {
            while (items.Count < count)
            {
                WorkerItemView item = Instantiate(itemPrefab, content);
                item.gameObject.SetActive(true);
                items.Add(item);
            }
        }
    }
}
