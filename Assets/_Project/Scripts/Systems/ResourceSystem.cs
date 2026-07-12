using System;
using NeighborhoodManager.Configs;
using NeighborhoodManager.Models;
using UnityEngine;

namespace NeighborhoodManager.Systems
{
    public sealed class ResourceSystem
    {
        private readonly ResourceModel resources;

        public event Action Changed;

        public int DailyIncome { get; private set; }
        public int DailyExpense { get; private set; }

        public ResourceSystem(ResourceModel resources)
        {
            this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
        }

        public void Initialize(GameBalanceConfig balance)
        {
            resources.Budget = Mathf.Max(0, balance.InitialBudget);
            resources.Satisfaction = Mathf.Clamp(balance.InitialSatisfaction, 0, 100);
            resources.ComplaintCount = Mathf.Max(0, balance.InitialComplaintCount);
            resources.FacilityHealth = Mathf.Clamp(balance.InitialFacilityHealth, 0, 100);
            ResetDailyAccounting();
            Changed?.Invoke();
        }

        public void ResetDailyAccounting()
        {
            DailyIncome = 0;
            DailyExpense = 0;
        }

        public bool HasBudget(int amount) => amount >= 0 && resources.Budget >= amount;

        public bool TrySpend(int amount)
        {
            if (!HasBudget(amount))
            {
                return false;
            }

            resources.Budget -= amount;
            DailyExpense += amount;
            Changed?.Invoke();
            return true;
        }

        public void ChangeBudget(int delta)
        {
            if (delta >= 0)
            {
                resources.Budget += delta;
                DailyIncome += delta;
            }
            else
            {
                int expense = -delta;
                resources.Budget = Mathf.Max(0, resources.Budget - expense);
                DailyExpense += expense;
            }

            Changed?.Invoke();
        }

        public void ChangeSatisfaction(int delta)
        {
            resources.Satisfaction = Mathf.Clamp(resources.Satisfaction + delta, 0, 100);
            Changed?.Invoke();
        }

        public void ChangeComplaintCount(int delta)
        {
            resources.ComplaintCount = Mathf.Max(0, resources.ComplaintCount + delta);
            Changed?.Invoke();
        }

        public void ChangeFacilityHealth(int delta)
        {
            resources.FacilityHealth = Mathf.Clamp(resources.FacilityHealth + delta, 0, 100);
            Changed?.Invoke();
        }

        public ResourceDelta ApplyEventImpact(EventConfig config, bool succeeded)
        {
            int budgetBefore = resources.Budget;
            int satisfactionBefore = resources.Satisfaction;
            int complaintBefore = resources.ComplaintCount;
            int healthBefore = resources.FacilityHealth;

            if (succeeded)
            {
                ChangeBudget(config.SuccessBudgetDelta);
                ChangeSatisfaction(config.SuccessSatisfactionDelta);
                ChangeComplaintCount(config.SuccessComplaintDelta);
                ChangeFacilityHealth(config.SuccessFacilityHealthDelta);
            }
            else
            {
                ChangeBudget(config.FailureBudgetDelta);
                ChangeSatisfaction(config.FailureSatisfactionDelta);
                ChangeComplaintCount(config.FailureComplaintDelta);
                ChangeFacilityHealth(config.FailureFacilityHealthDelta);
            }

            return new ResourceDelta(
                resources.Budget - budgetBefore,
                resources.Satisfaction - satisfactionBefore,
                resources.ComplaintCount - complaintBefore,
                resources.FacilityHealth - healthBefore);
        }
    }
}
