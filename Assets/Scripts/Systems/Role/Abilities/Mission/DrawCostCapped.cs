using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;

public class DrawCostCapped : RoleAbility
{
    [SerializeField] int maxCost;
    [SerializeField] BuyRedraws buyRedraws;

    public void RegisterPhase(GamePhase phase)
    {
        if (buyRedraws != null) return;
        if (phase is not BuyRedraws) return;
        if (isClient) return;
        buyRedraws = phase as BuyRedraws;
        buyRedraws.onCalculateCost += ModifyCalculation;
        RegisterPhaseClient(Owner.connectionToClient, phase as BuyRedraws);
    }

    [TargetRpc]
    private void RegisterPhaseClient(NetworkConnection conn, BuyRedraws phase)
    {
        phase.onCalculateCost += ModifyCalculation;
    }

    private void ModifyCalculation(HivePlayer ply, int numDraws, ref int cost)
    {
        if (isClient || ply == Owner)
        {
            cost = Mathf.Min(maxCost, cost);
        }
    }
}
