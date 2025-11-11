using Mirror;
using UnityEngine;

public class VotesMultiplied : RoleAbility
{
    [SerializeField] int multiplier;
    private TeamLeaderVote vote;

    [Server]
    public void RegisterPhase(GamePhase phase)
    {
        if (vote != null) return;
        if (phase is not TeamLeaderVote) return;
        vote = phase as TeamLeaderVote;
        vote.OnVoteChange += NumVotesChanged;
        if (isClient) return; //Prevent the host from registering twice
        RegisterPhaseClient(Owner.connectionToClient, vote);
    }

    [TargetRpc]
    private void RegisterPhaseClient(NetworkConnection conn, TeamLeaderVote phase)
    {
        phase.OnVoteChange += NumVotesChanged;
    }

    private void NumVotesChanged(ref int votes, HivePlayer ply)
    {
        //If the player is null, we assume whoever receives this is the right person
        if (ply != Owner && ply != null) return;
        votes *= multiplier;
    }
}
