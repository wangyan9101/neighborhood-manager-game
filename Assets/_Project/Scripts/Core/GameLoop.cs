using System;
using NeighborhoodManager.Models;
using NeighborhoodManager.Systems;

namespace NeighborhoodManager.Core
{
    public sealed class GameLoop
    {
        private readonly GameState state;
        private readonly WorkerSystem workers;
        private readonly EventSystem events;
        private readonly DaySystem days;

        public event Action DayExpired;

        public GameLoop(GameState state, WorkerSystem workers, EventSystem events, DaySystem days)
        {
            this.state = state;
            this.workers = workers;
            this.events = events;
            this.days = days;
        }

        public void Tick(float deltaTime)
        {
            if (state.Phase != GamePhase.Playing || deltaTime <= 0f)
            {
                return;
            }

            state.TotalGameTime += deltaTime;
            workers.Tick(deltaTime);
            events.Tick(deltaTime);
            if (days.Tick(deltaTime))
            {
                DayExpired?.Invoke();
            }
        }
    }
}
