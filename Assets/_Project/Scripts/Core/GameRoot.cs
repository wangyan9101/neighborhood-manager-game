using System.Collections.Generic;
using NeighborhoodManager.Configs;
using NeighborhoodManager.Models;
using NeighborhoodManager.UI;
using UnityEngine;

namespace NeighborhoodManager.Core
{
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig balanceConfig;
        [SerializeField] private List<EventConfig> eventConfigs = new List<EventConfig>();
        [SerializeField] private List<WorkerConfig> workerConfigs = new List<WorkerConfig>();
        [SerializeField] private GameUIController gameUI;

        public GameSession Session { get; private set; }
        public GameBalanceConfig BalanceConfig => balanceConfig;
        public IReadOnlyList<EventConfig> EventConfigs => eventConfigs;
        public IReadOnlyList<WorkerConfig> WorkerConfigs => workerConfigs;
        public GameUIController GameUI => gameUI;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            Session = new GameSession();
            Session.Initialize(new GameState(), balanceConfig, eventConfigs, workerConfigs);
            gameUI.Bind(Session);
            Session.StartNewGame();
        }

        private void Update()
        {
            Session?.Tick(Time.deltaTime);
        }

        public void RestartGame()
        {
            Session?.Restart();
        }

        public void Configure(GameBalanceConfig balance, List<EventConfig> events,
            List<WorkerConfig> workers, GameUIController ui)
        {
            balanceConfig = balance;
            eventConfigs = events ?? new List<EventConfig>();
            workerConfigs = workers ?? new List<WorkerConfig>();
            gameUI = ui;
        }

        public bool ValidateReferences()
        {
            bool valid = balanceConfig != null && gameUI != null
                && eventConfigs.Count > 0 && workerConfigs.Count > 0;
            if (!valid)
            {
                Debug.LogError("GameRoot 配置不完整。请运行 Tools/Community Manager/Setup MVP Project。", this);
            }

            return valid;
        }
    }
}
