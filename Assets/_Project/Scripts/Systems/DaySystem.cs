using NeighborhoodManager.Configs;
using NeighborhoodManager.Models;

namespace NeighborhoodManager.Systems
{
    public sealed class DaySystem
    {
        private readonly GameState state;
        private readonly GameBalanceConfig balance;

        public DaySystem(GameState state, GameBalanceConfig balance)
        {
            this.state = state;
            this.balance = balance;
        }

        public void Reset()
        {
            state.CurrentDay = 0;
            state.DayRemainingTime = 0f;
        }

        public void StartNextDay()
        {
            state.CurrentDay++;
            state.DayRemainingTime = balance.DayLengthSeconds;
            state.DayStartSnapshot = DayResourceSnapshot.From(state.Resources);
            state.Phase = GamePhase.Playing;
        }

        public bool Tick(float deltaTime)
        {
            state.DayRemainingTime = System.Math.Max(0f, state.DayRemainingTime - deltaTime);
            return state.DayRemainingTime <= 0f;
        }

        public bool IsFinalDay => state.CurrentDay >= balance.MaxDayCount;
    }
}
