using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Mirror;

public class ModifyVoteCost : RoleAbility
{
    [SerializeField] float multiplier = 1;
    [SerializeField] int modifier = 0;
    private TeamLeaderVote vote;

    public void RegisterPhase(GamePhase phase)
    {
        if (vote != null) return;
        if (phase is not TeamLeaderVote) return;
        vote = phase as TeamLeaderVote;
        vote.OnCalculateVoteCost += CostChanged;
        if (isClient) return; //Prevent the host from registering twice
        RegisterPhaseClient(Owner.connectionToClient, phase as TeamLeaderVote);
    }

    [TargetRpc]
    private void RegisterPhaseClient(NetworkConnection conn, TeamLeaderVote phase)
    {
        phase.OnCalculateVoteCost += CostChanged;
    }

    private void CostChanged(ref int cost, HivePlayer ply)
    {
        //If the player is null, we assume whoever receives this is the right person
        if (ply != Owner && ply != null) return;
        cost = Mathf.FloorToInt(cost * multiplier) + modifier;
    }
}
