using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifyDrawCost : RoleAbility
{
    [SerializeField] float drawCostMod;
    [SerializeField] BuyRedraws buyRedraws;

    public void RegisterPhase(GamePhase phase)
    {
        if (buyRedraws != null) return;
        if (phase is not BuyRedraws) return;
        buyRedraws = phase as BuyRedraws;
        buyRedraws.onCalculateCost += ModifyCalculation;
        if (!isClient) RegisterPhaseClient(buyRedraws);
    }
    
    private void RegisterPhaseClient(BuyRedraws phase)
    {
        phase.onCalculateCost += ModifyCalculation;
    }

    private void ModifyCalculation(HivePlayer ply, int numDraws, ref int cost)
    {
        if (isClient || ply == Owner)
        {
            cost = Mathf.FloorToInt(cost * drawCostMod);
        }
    }
}
