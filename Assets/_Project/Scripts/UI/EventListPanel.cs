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
            EnsureCapacity(events.Count);
            for (int index = 0; index < items.Count; index++)
            {
                bool active = index < events.Count;
                items[index].gameObject.SetActive(active);
                if (active)
                {
                    GameEventRuntime gameEvent = events[index];
                    items[index].Bind(gameEvent, gameEvent.RuntimeId == selectedId, onSelected);
                }
            }
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
