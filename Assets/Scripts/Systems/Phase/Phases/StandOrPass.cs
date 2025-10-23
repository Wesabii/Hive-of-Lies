using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// All players have the choice to either stand for TeamLeader, or pass. Those who stand must have enough favour.
/// <para></para>
/// The player with the most favour becomes the TeamLeader.
/// </summary>
public class StandOrPass : GamePhase
{
    [Tooltip("The playercount")]
    [SerializeField] IntVariable playerCount;

    [Tooltip("The number of partners on the mission")]
    [SerializeField] IntVariable numPartners;

    [Tooltip("The Team Leader")]
    [SerializeField] HivePlayerVariable teamLeader;

    [Tooltip("The Current Mission")]
    [SerializeField] MissionVariable currentMission;

    [Tooltip("The players that chose to stand")]
    [SerializeField] HivePlayerSet standingPlayers;

    [Tooltip("The players that chose to pass")]
    [SerializeField] HivePlayerSet passedPlayers;

    [Tooltip("The players that are on the mission")]
    [SerializeField] HivePlayerSet playersOnMission;

    [Tooltip("All players by their NetworkConnection")]
    [SerializeField] HivePlayerDictionary players;

    [Tooltip("Invoked when this phase begins")]
    [SerializeField] GameEvent standOrPassBegin;

    /// <summary>
    /// The total amount of favour shared by the players standing for team leader. Used to determine probability of them being team leader
    /// </summary>
    int totalFavourOfStanding;

    /// <summary>
    /// This + the players remaining favour is how many draws they have of being team leader.
    /// <para></para>
    /// The higher this number, the less impactful their actual favour is.
    /// </summary>
    const int favourWeightMod = 3;

    #region Events
    [Tooltip("Invoked once all players have decided to stand or pass")]
    [SerializeField] GameEvent onAllPlayersStandOrPass;

    [Tooltip("Invoked once the team leader vote has been counted")]
    [SerializeField] GameEvent onTeamLeaderVoteCounted;

    [Tooltip("Invoked if nobody stands for the position of team leader.")]
    [SerializeField] GameEvent onNobodyStood;
    #endregion

    [Tooltip("The UI for this game phase")]
    [SerializeField] StandOrPassUI UI;

    private void Start()
    {
        teamLeader.AfterVariableChanged += ply =>
        {
            foreach (KeyValuePair<NetworkConnection, HivePlayer> pair in players.Value)
            {
                pair.Value.Button.ChangeTeamLeader(pair.Value == teamLeader.Value);
            }
        };

        playerCount.AfterVariableChanged += (val) => { if ( Active && standingPlayers.Value.Count + passedPlayers.Value.Count == playerCount) ReceiveResults(); };
    }

    public override void Begin()
    {
        standingPlayers.Reset();
        passedPlayers.Reset();
        playersOnMission.Reset();
        totalFavourOfStanding = 0;
        teamLeader.Value = null;

        standOrPassBegin?.Invoke();
    }

    [Server]
    public void OnPlayerReconnect(NetworkConnection conn)
    {
        //Ignore if this is not the current game phase
        if (!Active) return;
        if (!players.Value.TryGetValue(conn, out HivePlayer ply)) return;
        //If the player hasn't stood or passed yet, show them the UI again
        if (standingPlayers.Value.Contains(ply) || passedPlayers.Value.Contains(ply)) return;

        CreateUI(conn, ply.NextStandCost);
    }

    [TargetRpc]
    void CreateUI(NetworkConnection conn, int cost)
    {
        UI.CreateUI(cost);
    }

    /// <summary>
    /// Called when a player stands for team leader
    /// </summary>
    /// <param name="conn"></param>
    [Server]
    public void PlayerStood(NetworkConnection conn)
    {
        PlayerDecision(conn, true);
    }

    /// <summary>
    /// Called when a player passes the opportunity to stand for team leader
    /// </summary>
    /// <param name="conn"></param>
    [Server]
    public void PlayerPassed(NetworkConnection conn)
    {
        PlayerDecision(conn, false);
    }

    /// <summary>
    /// Called when a player decides to stand or pass
    /// </summary>
    /// <param name="ply">The player who made the decision</param>
    /// <param name="stood">True if they chose to stand</param>
    [Server]
    void PlayerDecision(NetworkConnection conn, bool isStanding)
    {
        Debug.Log("Player has stood or passed");
        if (!Active) return;
        if (!players.Value.TryGetValue(conn, out HivePlayer ply)) return;

        //Make sure they haven't already voted
        if (passedPlayers.Value.Contains(ply) || standingPlayers.Value.Contains(ply)) return;

        if (isStanding)
        {
            standingPlayers.Add(ply);
            //Lose favour when you stand
            ply.Favour.Value -= ply.NextStandCost;
            totalFavourOfStanding += ply.Favour.Value + favourWeightMod;
        }
        else passedPlayers.Add(ply);

        if (standingPlayers.Value.Count + passedPlayers.Value.Count == playerCount) ReceiveResults();
    }

    /// <summary>
    /// Called once all players have decided to stand or pass
    /// </summary>
    [Server]
    void ReceiveResults()
    {
        Debug.Log("All players have stood or passed");
        //Invoke event for all players having made a decision
        onAllPlayersStandOrPass?.Invoke();

        //Find the highest influence players who stood.
        SortStandingList();

        if (standingPlayers.Value.Count == 0)
        {
            //If nobody stood for the position of team leader, and we don't currently have a team leader
            if (teamLeader.Value == null)
            {
                currentMission.Value.AfterEffectTriggered += OnMissionEffectFinished;
                currentMission.Value.TriggerValidEffects(0);
                return;
            }
        }
        else
        {
            bool newTeamLeader = false;
            //Team leader is decided randomly, but heavily weighted towards players with more remaining favour
            for (int i = 0; i < standingPlayers.Value.Count; i++)
            {
                HivePlayer ply = standingPlayers.Value[i];

                if (Random.Range(0, 1f) <= (ply.Favour.Value + favourWeightMod) / (float) totalFavourOfStanding)
                {
                    teamLeader.Value = ply;
                    playersOnMission.Add(teamLeader);
                    newTeamLeader = true;
                    break;
                }

                totalFavourOfStanding -= ply.Favour.Value + favourWeightMod;
            }

            if (!newTeamLeader) Debug.LogError("No team leader was picked");
        }

        //Invoke event before determining the Team Leader
        onTeamLeaderVoteCounted?.Invoke();
        
        Debug.Log($"The team leader has been set to {teamLeader.Value.DisplayName}");

        //All unsuccessful players get their favour back
        foreach (HivePlayer ply in standingPlayers)
        {
            if (ply != teamLeader.Value)
            {
                ply.Favour.Value += ply.NextStandCost;
            }
        }

        End();
    }

    /// <summary>
    /// Sort the list of standing players
    /// </summary>
    [Server]
    void SortStandingList()
    {
        standingPlayers.Sort((a, b) =>
        {
            int result = a.Favour.Value.CompareTo(b.Favour);
            result *= -1;
            //If both players have the same amount of favour, we should randomly pick between them
            return result == 0 ? (2*Random.Range(0, 2))-1 : result;
        });
    }

    void OnMissionEffectFinished()
    {
        currentMission.Value.AfterEffectTriggered -= OnMissionEffectFinished;

        onNobodyStood?.Invoke();
    }
}