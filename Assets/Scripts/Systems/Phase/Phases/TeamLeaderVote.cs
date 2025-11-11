using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using Steamworks;
using System;
using System.Linq;

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

    public delegate void ModifyVoteCost(ref int cost, HivePlayer ply);
    public event ModifyVoteCost OnCalculateVoteCost;

    public delegate void ModifyVotes(ref int votes, HivePlayer ply);
    public event ModifyVotes OnVoteChange;

    [Tooltip("The UI associated with this game phase")]
    [SerializeField] VoteUI UI;

    private Dictionary<HivePlayer, int> spentFavour = new();

    private void Start()
    {
        //If a wasp stings incorrectly without voting
        playerCount.AfterVariableChanged += (val) => { if (Active && allVotes.Value.Count >= playerCount) AllVotesReceived(); };  
    }

    public override void OnStartClient()
    {
        UI.SetVoteCostCalculation(NextVoteCost);
        UI.OnLockInVote += (votes) => VoteLockedIn(votes);
        UI.OnVoteChange += (ref int votes) => OnVoteChange?.Invoke(ref votes, null);
    }

    public override void Begin()
    {
        allVotes.Clear();
        voteTotal.Value = 0;
        spentFavour = new();
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

        //UI.TargetEnableUI(conn);
    }

    /// <summary>
    /// Call when a player locks in their vote
    /// </summary>
    /// <param name="ply">The player that voted</param>
    /// <param name="vote">How many votes the player sent</param>
    [Command(requiresAuthority = false)]
    public void VoteLockedIn(int votes, NetworkConnectionToClient conn = null)
    {
        if (!Active) return;
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;
        if (allVotes.Value.Any(vote => vote.ply == ply)) return;

        int voteCost = 0;
        bool upvote = votes >= 0;
        //Calculate cost based on unmodified votes first (clicks of the vote button)
        for (int i = 1; i < Math.Abs(votes); i++)
        {
            int cost = voteCost + NextVoteCost(upvote, i - 1);
            //Only cast as many votes as you are able to afford
            if (cost > ply.Favour)
            {
                votes = upvote ? i - 1 : 1 - i;
                break;
            }
            voteCost = cost;
        }
        ply.Favour.Value -= voteCost;
        spentFavour.Add(ply, voteCost);

        //Modify vote total before adding
        OnVoteChange?.Invoke(ref votes, ply);
        voteTotal.Value += votes;
        allVotes.Add(new PlayerVote()
        {
            ply = ply,
            votes = votes,
        });

        onPlayerVoted?.Invoke();

        //If we have received a vote from everyone
        if (allVotes.Value.Count >= playerCount) AllVotesReceived();
    }

    [Server]
    void AllVotesReceived()
    {
        //Invoke the all players voted event
        onAllPlayersVoted?.Invoke();
        bool success = voteTotal > 0;

        foreach (PlayerVote vote in allVotes)
        {
            //Refund all votes opposite to the result
            if ((vote.votes > 0) == success) continue;
            RefundVotes(vote.ply);
        }

        if (success) End();
        else voteFailed?.Invoke();
    }

    void RefundVotes(HivePlayer ply)
    {
        if (!spentFavour.TryGetValue(ply, out int cost)) return;
        ply.Favour.Value += cost;
    }

    public int NextVoteCost(bool upvote, int numVotes)
    {
        return NextVoteCost(upvote, numVotes, null);
    }

    public int NextVoteCost(bool upvote, int numVotes, HivePlayer ply)
    {
        int cost;
        if (upvote)
        {
            cost = numVotes >= 0 ? numVotes : (numVotes + 1);
        }
        else
        {
            cost = numVotes <= 0 ? -numVotes : (1 - numVotes);
        }

        OnCalculateVoteCost?.Invoke(ref cost, ply);
        return cost;
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