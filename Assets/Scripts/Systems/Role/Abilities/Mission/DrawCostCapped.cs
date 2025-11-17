using UnityEngine;
using Mirror;

public class DrawCostCapped : RoleAbility
{
    [SerializeField] int maxCost;
    private BuyRedraws buyRedraws;

    [Server]
    public void RegisterPhase(GamePhase phase)
    {
        if (buyRedraws != null) return;
        if (phase is not BuyRedraws) return;
        buyRedraws = phase as BuyRedraws;
        buyRedraws.onCalculateCost += ModifyCalculation;
        if (isClient) return; //Prevent the host from registering twice
        RegisterPhaseClient(Owner.connectionToClient, phase as BuyRedraws);
    }

    [TargetRpc]
    private void RegisterPhaseClient(NetworkConnection conn, BuyRedraws phase)
    {
        phase.onCalculateCost += ModifyCalculation;
    }

    private void ModifyCalculation(HivePlayer ply, int numDraws, ref int cost)
    {
        if (!IsOwner(ply)) return;
        cost = Mathf.Min(maxCost, cost);
    }
}
