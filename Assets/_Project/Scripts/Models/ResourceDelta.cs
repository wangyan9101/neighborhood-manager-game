using System;

namespace NeighborhoodManager.Models
{
    [Serializable]
    public struct ResourceDelta
    {
        public int Budget;
        public int Satisfaction;
        public int Complaint;
        public int FacilityHealth;

        public ResourceDelta(int budget, int satisfaction, int complaint, int facilityHealth)
        {
            Budget = budget;
            Satisfaction = satisfaction;
            Complaint = complaint;
            FacilityHealth = facilityHealth;
        }
    }
}
