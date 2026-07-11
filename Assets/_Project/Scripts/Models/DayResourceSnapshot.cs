using System;

namespace NeighborhoodManager.Models
{
    [Serializable]
    public sealed class DayResourceSnapshot
    {
        public int Budget;
        public int Satisfaction;
        public int ComplaintCount;
        public int FacilityHealth;

        public static DayResourceSnapshot From(ResourceModel resources)
        {
            return new DayResourceSnapshot
            {
                Budget = resources.Budget,
                Satisfaction = resources.Satisfaction,
                ComplaintCount = resources.ComplaintCount,
                FacilityHealth = resources.FacilityHealth
            };
        }
    }
}
