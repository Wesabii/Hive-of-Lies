using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using Steamworks;

public class TeamLeaderVote : GamePhase
{
    [Tooltip("Set of all player votes")]
    [SerializeField] VoteSet allVotes;

    [Tooltip("Running total for the vote. If > 0, the team leader is voted in.")]
    [SerializeField] IntVariable voteTotal;

    [Tooltip("All players by their NetworkConnections")]
    [SerializeField] HivePlayerDictionary playersByConnection;

    [Tooltip("The number of players in the game.")]
    [SerializeField] IntVariable playerCount;



    [Tooltip("Invoked when the vote begins")]
    [SerializeField] GameEvent voteBegin;

    [Tooltip("Invoked when a player votes")]
    [SerializeField] GameEvent onPlayerVoted;

    [Tooltip("Invoked when all players have voted")]
    [SerializeField] GameEvent onAllPlayersVoted;

    [Tooltip("Invoked when there are more downvotes than upvotes")]
    [SerializeField] GameEvent voteFailed;

    [Tooltip("The UI associated with this game phase")]
    [SerializeField] VoteUI UI;

    private void Start()
    {
        playerCount.AfterVariableChanged += (val) => { if (Active && allVotes.Value.Count == playerCount) AllVotesReceived(); };
    }

    public override void Begin()
    {
        allVotes.Clear();
        voteTotal.Value = 0;
        voteBegin?.Invoke();
    }

    public void OnServerConnected(NetworkConnection conn)
    {
        if (!Active) return;
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;

        foreach (PlayerVote vote in allVotes.Value)
        {
            //If the player has already voted, we don't need to enable the UI.
            if (vote.ply == ply) return;
        }

        UI.TargetEnableUI(conn);
    }

    [Server]
    public void PlayerIncreasedVote(NetworkConnection conn)
    {
        if (!Active) return;

        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;

        int cost = ply.NextUpvoteCost;        

        if (ply.Favour < cost && cost > 0) return;

        ply.NumVotes++;

        ply.Favour.Value -= cost;
        ply.FavourSpentVoting += cost;

        ply.NextDownvoteCost.Value = CalculateDownvoteCost(ply.NumVotes);

        ply.NextUpvoteCost.Value = CalculateUpvoteCost(ply.NumVotes);
    }

    [Server]
    public void PlayerDecreasedVote(NetworkConnection conn)
    {
        if (!Active) return;

        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;

        int cost = ply.NextDownvoteCost;

        if (ply.Favour < cost && cost > 0) return;

        ply.NumVotes--;

        ply.Favour.Value -= cost;
        ply.FavourSpentVoting += cost;

        ply.NextUpvoteCost.Value = CalculateUpvoteCost(ply.NumVotes);

        ply.NextDownvoteCost.Value = CalculateDownvoteCost(ply.NumVotes);
    }

    /// <summary>
    /// Call when a player locks in their vote
    /// </summary>
    /// <param name="ply">The player that voted</param>
    /// <param name="vote">How many votes the player sent</param>
    [Server]
    public void VoteLockedIn(NetworkConnection conn)
    {
        if (!Active) return;

        playersByConnection.Value.TryGetValue(conn, out HivePlayer ply);

        voteTotal.Value += ply.NumVotes;
        allVotes.Add(new PlayerVote()
        {
            ply = ply,
            votes = ply.NumVotes
        });

        //Invoke the player voted event
        onPlayerVoted?.Invoke();

        ply.NumVotes.Value = 0;
        ply.NextDownvoteCost.Value = 0;
        ply.NextUpvoteCost.Value = 0;

        //If we have received a vote from everyone
        if (allVotes.Value.Count == playerCount) AllVotesReceived();
    }

    [Server]
    void AllVotesReceived()
    {
        //Invoke the all players voted event
        onAllPlayersVoted?.Invoke();

        //If the vote was successful
        if (voteTotal > 0)
        {
            foreach (PlayerVote vote in allVotes)
            {
                //If the player spent favour voting no, refund their downvotes
                RefundVotes(vote.ply, vote.votes < 0);
            }

            End();
        }
        else
        {
            foreach (PlayerVote vote in allVotes)
            {
                //If the player spent favour voting yes, refund their downvotes
                RefundVotes(vote.ply, vote.votes > 0);
            }

            //Back to standing for TeamLeader
            voteFailed?.Invoke();
        }
    }

    void RefundVotes(HivePlayer ply, bool shouldRefund)
    {
        if (shouldRefund) ply.Favour.Value += ply.FavourSpentVoting;
        ply.FavourSpentVoting = 0;
    }

    /// <summary>
    /// Calculate the costs of the next up vote
    /// </summary>
    /// <param name="numVotes"></param>
    /// <returns></returns>
    public static int CalculateUpvoteCost(int numVotes)
    {
        return numVotes >= 0 ? 1 * numVotes : 1 * (numVotes + 1);
    }

    /// <summary>
    /// Calculate the costs of the next down vote
    /// </summary>
    /// <param name="numVotes"></param>
    /// <returns></returns>
    public static int CalculateDownvoteCost(int numVotes)
    {
        return numVotes > 0 ? -1 * (numVotes-1) : -1 * numVotes;
    }
}

/// <summary>
/// Represents the vote of a player
/// </summary>
[System.Serializable]
public struct PlayerVote
{
    /// <summary>
    /// The player that this vote is from
    /// </summary>
    public HivePlayer ply;
    /// <summary>
    /// How many votes the player sent
    /// </summary>
    public int votes;
}