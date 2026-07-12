using System;
using System.Collections.Generic;
using NeighborhoodManager.Models;
using UnityEngine;

namespace NeighborhoodManager.UI
{
    public sealed class EventListPanel : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private EventItemView itemPrefab;
        private readonly List<EventItemView> items = new List<EventItemView>();

        public void Configure(Transform contentRoot, EventItemView prefab)
        {
            content = contentRoot;
            itemPrefab = prefab;
            if (prefab != null) prefab.gameObject.SetActive(false);
        }

        public void Refresh(IReadOnlyList<GameEventRuntime> events, string selectedId, Action<string> onSelected)
        {
            int visibleCount = 0;
            for (int index = 0; index < events.Count; index++)
            {
                if (IsVisible(events[index])) visibleCount++;
            }

            EnsureCapacity(visibleCount);
            int eventIndex = 0;
            for (int index = 0; index < items.Count; index++)
            {
                while (eventIndex < events.Count && !IsVisible(events[eventIndex])) eventIndex++;
                bool active = eventIndex < events.Count;
                items[index].gameObject.SetActive(active);
                if (active)
                {
                    GameEventRuntime gameEvent = events[eventIndex++];
                    items[index].Bind(gameEvent, gameEvent.RuntimeId == selectedId, onSelected);
                }
            }
        }

        private static bool IsVisible(GameEventRuntime gameEvent)
        {
            return gameEvent.State == EventState.Pending || gameEvent.State == EventState.Handling;
        }

        private void EnsureCapacity(int count)
        {
            while (items.Count < count)
            {
                EventItemView item = Instantiate(itemPrefab, content);
                item.gameObject.SetActive(true);
                items.Add(item);
            }
        }
    }
}
