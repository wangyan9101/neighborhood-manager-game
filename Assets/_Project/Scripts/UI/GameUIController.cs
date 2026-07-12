using NeighborhoodManager.Core;
using NeighborhoodManager.Models;
using UnityEngine;

namespace NeighborhoodManager.UI
{
    public sealed class GameUIController : MonoBehaviour
    {
        [SerializeField] private TopResourceBar topResourceBar;
        [SerializeField] private EventListPanel eventListPanel;
        [SerializeField] private WorkerListPanel workerListPanel;
        [SerializeField] private LogPanel logPanel;
        [SerializeField] private DailyReportPanel dailyReportPanel;
        [SerializeField] private FinalResultPanel finalResultPanel;

        private GameSession session;
        private string selectedEventRuntimeId;
        private float refreshTimer;

        public bool HasRequiredReferences => topResourceBar != null && eventListPanel != null
            && workerListPanel != null && logPanel != null && dailyReportPanel != null && finalResultPanel != null;

        public void Bind(GameSession gameSession)
        {
            session = gameSession;
            session.ResourcesChanged += RefreshResources;
            session.EventsChanged += RefreshEvents;
            session.WorkersChanged += RefreshWorkers;
            session.LogAdded += logPanel.Add;
            session.DayEnded += ShowDayReport;
            session.GameEnded += ShowFinalResult;
            dailyReportPanel.Hide();
            finalResultPanel.Hide();
        }

        public void Configure(TopResourceBar resources, EventListPanel events, WorkerListPanel workers,
            LogPanel log, DailyReportPanel dailyReport, FinalResultPanel finalResult)
        {
            topResourceBar = resources;
            eventListPanel = events;
            workerListPanel = workers;
            logPanel = log;
            dailyReportPanel = dailyReport;
            finalResultPanel = finalResult;
        }

        private void Update()
        {
            if (session == null || session.State.Phase != GamePhase.Playing)
            {
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = 0.25f;
            RefreshResources();
            eventListPanel.Refresh(session.State.ActiveEvents, selectedEventRuntimeId, SelectEvent);
            RefreshWorkers();
        }

        private void RefreshResources()
        {
            topResourceBar.Refresh(session.State);
        }

        private void RefreshEvents()
        {
            if (session.State.ActiveEvents.Find(item => item.RuntimeId == selectedEventRuntimeId)?.State != EventState.Pending)
            {
                selectedEventRuntimeId = null;
            }

            eventListPanel.Refresh(session.State.ActiveEvents, selectedEventRuntimeId, SelectEvent);
            RefreshWorkers();
        }

        private void RefreshWorkers()
        {
            GameEventRuntime selectedEvent = session.State.ActiveEvents.Find(item =>
                item.RuntimeId == selectedEventRuntimeId && item.State == EventState.Pending);
            workerListPanel.Refresh(session.State.Workers, selectedEvent, session.State.ActiveEvents,
                workerId => selectedEvent == null ? null
                    : session.GetExpectedHandleDuration(selectedEvent.RuntimeId, workerId), DispatchWorker);
        }

        private void SelectEvent(string runtimeId)
        {
            selectedEventRuntimeId = runtimeId;
            RefreshEvents();
        }

        private void DispatchWorker(string workerId)
        {
            DispatchResult result = session.TryDispatch(selectedEventRuntimeId, workerId);
            if (result.Success)
            {
                selectedEventRuntimeId = null;
            }

            RefreshEvents();
        }

        private void ShowDayReport(DayReportModel report)
        {
            dailyReportPanel.Show(report, session.ContinueAfterDayReport);
        }

        private void ShowFinalResult(GameResult result)
        {
            dailyReportPanel.Hide();
            finalResultPanel.Show(result, session.Restart);
        }
    }
}
