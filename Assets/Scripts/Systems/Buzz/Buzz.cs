using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Mirror;

public class Buzz : GamePhase
{
    #region CLIENT
    [SerializeField] GameObject lockInButton;
    [SerializeField] GameObject buzzButton;
    [SerializeField] TMPro.TMP_Text chosenText;
    [SerializeField] LocalizedString chosenTextString;

    [SerializeField] IntVariable favour;
    #endregion
    #region SERVER
    [SerializeField] GameEvent beesWin;
    [SerializeField] GameEvent waspsWin;
    [SerializeField] IntVariable lives;

    [SerializeField] HivePlayerDictionary playersByConnection;
    [SerializeField] HivePlayerSet alivePlayers;
    [SerializeField] HivePlayerSet waspPlayers;
    [SerializeField] MissionVariable currentMission;
    [SerializeField] VoteSet playerVotes;
    [SerializeField] IntVariable voteTotal;

    [SerializeField] GameObject pickPlayerButton;
    [SerializeField] GameObject removePlayerButton;

    [SerializeField] NetworkingEvent showPlayerVote;
    [SerializeField] GameEvent buzzStarted;

    [SerializeField] GameEvent allPlayersVoted;

    /// <summary>
    /// How many bees are selected. If > 0 the buzz will not be successful
    /// </summary>
    int beesInBuzz;

    /// <summary>
    /// Whether the buzz is a success (all selected players are wasps)
    /// </summary>
    bool successful => beesInBuzz == 0;

    /// <summary>
    /// How long the buzz phase lasts in seconds
    /// </summary>
    [SerializeField] IntTimer buzzTimer;

    /// <summary>
    /// Timer for how long players have to pick 
    /// </summary>
    [SerializeField] IntTimer pickTimer;
    Coroutine pickCoroutine;

    /// <summary>
    /// The IntVariable that keeps track of the pickTime
    /// </summary>
    [SerializeField] IntTimer voteTimer;
    Coroutine voteCoroutine;

    /// <summary>
    /// The player who buzzed in the current buzz
    /// </summary>
    [SerializeField] HivePlayerVariable currentBuzzer;

    List<HivePlayer> playersBuzzed = new();
    [SerializeField] HivePlayerSet playersSelected;

    [SerializeField] HivePlayerVariable teamLeader;
    [SerializeField] HivePlayerSet missionPlayersSelected;

    List<PlayerButtonDropdownItem> addButtons = new();
    #endregion

    public override void Begin()
    {
        currentMission.Value = null;
        waspPlayers.AfterItemRemoved += OnWaspDied;
        teamLeader.Value = null;
        for (int i = 0; i < missionPlayersSelected.Count; i = 0) missionPlayersSelected.Remove(missionPlayersSelected.Value[0]);

        currentBuzzer.OnVariableChanged += (HivePlayer oldPly, ref HivePlayer newPly) => { if (oldPly != null) oldPly.Button.ChangeTeamLeader(false); };
        currentBuzzer.AfterVariableChanged += ply => { if (ply != null) ply.Button.ChangeTeamLeader(true); };

        playersSelected.AfterItemAdded += ply => ply.Button.ChangeOnMission(true);
        playersSelected.AfterItemRemoved += ply => ply.Button.ChangeOnMission(false);
        playersSelected.AfterCleared += () =>
        {
            foreach (HivePlayer ply in alivePlayers.Value)
            {
                ply.Button.ChangeOnMission(false);
            };
        };

        buzzStarted.Invoke();

        buzzTimer.OnTimerEnd += waspsWin.Invoke;
        StartCoroutine(buzzTimer.StartTimer());

        pickTimer.OnTimerEnd += StartBuzz;
        voteTimer.OnTimerEnd += OnVoteEnd;

        StartBuzz();
    }

    void StartBuzz()
    {
        //Stop the pickTimer from continuing
        if (pickCoroutine != null) StopCoroutine(pickCoroutine);
        //If time ran out before players were selected for the buzz
        if (currentBuzzer.Value != null)
        {
            SetLockInActive(currentBuzzer.Value.connectionToClient, false);
            currentBuzzer.Value = null;
            foreach (PlayerButtonDropdownItem i in addButtons) Destroy(i.gameObject);
            addButtons = new();
            playersSelected.Clear();
        }

        //Once all players have buzzed, anyone can buzz again
        if (playersBuzzed.Count == alivePlayers.Count) playersBuzzed = new();

        foreach (HivePlayer pl in alivePlayers.Value)
        {
            //Only players that haven't buzzed yet get to buzz
            if (playersBuzzed.Contains(pl)) continue;
            SetBuzzActive(pl.connectionToClient, true);
        }
    }

    /// <summary>
    /// Called when a player clicks the buzz button
    /// </summary>
    [Client]
    public void BuzzClicked()
    {
        OnBuzz();
    }
    
    /// <summary>
    /// Called on the server when a player clicks the buzz button
    /// </summary>
    [Command(requiresAuthority = false)]
    void OnBuzz(NetworkConnectionToClient conn = null)
    {
        playerVotes.Clear();
        playersSelected.Clear();
        //If someone is currently buzzing
        if (currentBuzzer.Value != null) return;
        //If the player is dead / not in the game
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;
        //If they've buzzed before
        if (playersBuzzed.Contains(ply)) return;
        playersBuzzed.Add(ply);
        //Set this player as the current buzzer
        currentBuzzer.Value = ply; 

        beesInBuzz = 0;

        foreach (HivePlayer pl in alivePlayers.Value)
        {
            SetBuzzActive(pl.connectionToClient, false);
            //Can't pick yourself except for testing purposes
#if !UNITY_EDITOR
            if (pl == currentBuzzer.Value) continue;
#endif
            PlayerButtonDropdownItem item = pl.Button.AddDropdownItem(pickPlayerButton, currentBuzzer.Value);
            item.OnItemClicked += (ply) => AddPlayer(ply, item);
            addButtons.Add(item);
        }
        SetLockInActive(ply.connectionToClient, true);
        SetLockInInteractable(ply.connectionToClient, false);

        pickTimer.ResetTimer();
        pickCoroutine = StartCoroutine(pickTimer.StartTimer());
    }

    [Server]
    void AddPlayer(HivePlayer ply, PlayerButtonDropdownItem item)
    {
        //You should always be able to select at least one player, but the "playersSelected.Value.Count != 0" should only have an impact in the case of testing (with 0 wasps)
        if (playersSelected.Count >= waspPlayers.Count && playersSelected.Count != 0) return;
        if (playersSelected.Contains(ply)) return;
#if !UNITY_EDITOR
        if (ply == currentBuzzer.Value) return;
#endif
        addButtons.Remove(item);
        Destroy(item.gameObject);

        Debug.Log($"{currentBuzzer.Value.DisplayName} has selected {ply.DisplayName}");
        playersSelected.Add(ply);
        if (ply.Team.Value.Team == Team.Bee) beesInBuzz++;

        SetPlayersChosen(currentBuzzer.Value.connectionToClient, playersSelected.Count, waspPlayers.Count);

        if (playersSelected.Count < waspPlayers.Count) return;
        
        OnMaxPlayersAdded();
    }

    [Server]
    void OnMaxPlayersAdded()
    {
        SetLockInInteractable(currentBuzzer.Value.connectionToClient, true);
        foreach (PlayerButtonDropdownItem i in addButtons) Destroy(i.gameObject);
        addButtons = new();
    }

    [TargetRpc]
    void SetLockInActive(NetworkConnection conn, bool active)
    {
        lockInButton.SetActive(active);
    }

    [TargetRpc]
    void SetLockInInteractable(NetworkConnection conn, bool interactable)
    {
        lockInButton.GetComponent<UnityEngine.UI.Button>().interactable = interactable;
    }

    [TargetRpc]
    void SetBuzzActive(NetworkConnection conn, bool active)
    {
        buzzButton.SetActive(active);
    }

    [TargetRpc]
    void SetPlayersChosen(NetworkConnection conn, int currentChosen, int max)
    {
        chosenText.GetComponent<TMPro.TMP_Text>().text = string.Format(chosenTextString.GetLocalizedString(), currentChosen, max);
    }

    [Client]
    public void BuzzSubmitted()
    {
        ServerBuzzSubmitted();
        lockInButton.SetActive(false);
    }

    [Command(requiresAuthority = false)]
    void ServerBuzzSubmitted(NetworkConnectionToClient conn = null)
    {
        if (!Active) return;
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;
        if (ply != currentBuzzer.Value) return;

        //Stop the pick timer, start the vote timer
        StopCoroutine(pickCoroutine);
 
        voteCoroutine = StartCoroutine(voteTimer.StartTimer());

        foreach (PlayerButtonDropdownItem i in addButtons) Destroy(i.gameObject);
        addButtons = new();

        foreach (HivePlayer pl in alivePlayers)
        {
            if (!CanVote(pl)) continue;
            showPlayerVote.Invoke(pl.connectionToClient);
        }
    }

    /// <summary>
    /// Returns true if a player is allowed to vote on the buzz
    /// </summary>
    /// <param name="ply"></param>
    /// <returns></returns>
    [Server]
    bool CanVote(HivePlayer ply)
    {
#if UNITY_EDITOR
        //Purely for debugging purposes
        if (alivePlayers.Count == 1) return true;
#endif

        //Players who have been selected do not get to vote
        if (playersSelected.Contains(ply)) return false;
        return true;
    }

    [Server]
    public void PlayerIncreasedVote(NetworkConnection conn)
    {
        if (!Active) return;

        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;

        //int cost = ply.NextUpvoteCost;

        //if (ply.Favour < cost && cost > 0) return;

        //ply.NumVotes++;

        //ply.Favour.Value -= cost;

        //ply.NextDownvoteCost.Value = TeamLeaderVote.CalculateDownvoteCost(ply.NumVotes);

        //ply.NextUpvoteCost.Value = TeamLeaderVote.CalculateUpvoteCost(ply.NumVotes);
    }

    [Server]
    public void PlayerDecreasedVote(NetworkConnection conn)
    {
        if (!Active) return;

        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;

        //int cost = ply.NextDownvoteCost;

        //if (ply.Favour < cost && cost > 0) return;

        //ply.NumVotes--;

        //ply.Favour.Value -= cost;

        //ply.NextUpvoteCost.Value = TeamLeaderVote.CalculateUpvoteCost(ply.NumVotes);

        //ply.NextDownvoteCost.Value = TeamLeaderVote.CalculateDownvoteCost(ply.NumVotes);
    }

    [Server]
    public void OnPlayerVoted(NetworkConnection conn)
    {
        if (!Active) return;
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;
        //If the player shouldn't be able to vote, don't let them.
        if (!CanVote(ply)) return;
        
        //voteTotal.Value += ply.NumVotes;
        //playerVotes.Add(new PlayerVote
        //{
        //    ply = ply,
        //    votes = ply.NumVotes
        //});

        //ply.NumVotes.Value = 0;
        //ply.NextUpvoteCost.Value = 0;
        //ply.NextDownvoteCost.Value = 0;

        if (playerVotes.Count >= alivePlayers.Count - playersSelected.Count) OnVoteEnd();
    }

    /// <summary>
    /// Called when the final player places their vote
    /// </summary>
    [Server]
    void OnVoteEnd()
    {
        //If not everyone voted;
        if (playerVotes.Value.Count < alivePlayers.Count - playersSelected.Count)
        {
            foreach (HivePlayer ply in alivePlayers.Value)
            {
                //Skip players that couldn't vote
                if (!CanVote(ply)) continue;
                bool isInSet = false;
                for (int i = 0; i < playerVotes.Count; i++)
                {
                    if (playerVotes.Value[i].ply == ply)
                    {
                        isInSet = true;
                        break;
                    }
                }
                if (isInSet) continue;

                playerVotes.Add(new PlayerVote { ply = ply, votes = 1 });
                voteTotal.Value++;
            }
        }
        allPlayersVoted.Invoke();

        //Reset the vote timer
        voteTimer.ResetTimer();
        StopCoroutine(voteCoroutine);

        if (voteTotal.Value > 0)
        {
            if (successful) beesWin?.Invoke();
            else OnBuzzIncorrect();
        }

        voteTotal.Value = 0;
        playerVotes.Clear();
        playersSelected.Clear();
        currentBuzzer.Value = null;
        StartBuzz();
    }

    [Server]
    void OnBuzzIncorrect()
    {
        lives.Value -= beesInBuzz;
        if (lives.Value <= 0)
        {
            waspsWin?.Invoke();
            return;
        }
    }

    /// <summary>
    /// Logic to make sure everything works fine when a wasp dies mid buzz phase
    /// </summary>
    /// <param name="ply"></param>
    void OnWaspDied(HivePlayer ply)
    {
        //If the wasp was buzzed, everything should just work out fine
        if (playersSelected.Contains(ply)) return;

        if (playersBuzzed.Contains(ply)) playersBuzzed.Remove(ply);
        //Otherwise the player casts a yes vote, and dies.
        //ply.NumVotes.Value = 1;
        OnPlayerVoted(new NetworkConnectionToClient((int) ply.netId));
    }
}
