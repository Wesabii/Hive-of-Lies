using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Decides what the next mission will be
/// </summary>
public class DecideMission : GamePhase
{
    #region SERVER
    /// <summary>
    /// List of all possible mission lists for all possible player counts.
    /// </summary>
    [SerializeField] GameModeVariable gameMode;

    /// <summary>
    /// Which players have voted
    /// </summary>
    List<HivePlayer> TotalVotes;

    /// <summary>
    /// The mission that was voted in
    /// </summary>
    Mission DecidedMission;

    [Tooltip("The playercount of the game")]
    [SerializeField] IntVariable playerCount;

    [Tooltip("The current round number of the game")]
    [SerializeField] IntVariable roundNum;

    [Tooltip("The number of mission choices the players have")]
    [SerializeField] IntVariable numMissionChoices;

    [Tooltip("How much more difficult the missions are")]
    [SerializeField] IntVariable difficutlyMod;

    [Tooltip("The mission list that will be used this game")]
    [SerializeField] MissionListVariable decidedMissionList;

    [Tooltip("All players by their connection")]
    [SerializeField] HivePlayerDictionary players;

    [Tooltip("The currently active mission")]
    [SerializeField] MissionVariable currentMission;

    [Tooltip("The set of all completed missions")]
    [SerializeField] MissionSet completedMissions;

    [Tooltip("The missions that players are choosing between")]
    [SerializeField] MissionSet decidedMissionChoices;

    [Tooltip("Event to invoke when the mission choices have been picked")]
    [SerializeField] GameEvent onMissionChoicesDecided;

    [Tooltip("The event invoked to begin the final phase of the game")]
    [SerializeField] GameEvent startFinalPhase;

    int missionListIndex;
    #endregion

    #region CLIENT
    [SerializeField] GameObject missionCard;
    List<GameObject> cards = new List<GameObject>();
    #endregion

    /// <summary>
    /// Who has voted for which mission, and how many votes in total that mission has
    /// </summary>
    public Dictionary<Mission, (List<HivePlayer>, int)> MissionVotes;

    void Start()
    {
        //Keep track of all the lists we find that are valid for this game
        List<MissionList> possibleLists = new List<MissionList>();

        foreach (MissionList l in gameMode.Value.MissionLists)
        {
            if (l.MinPlayers > playerCount) continue;
            if (l.MaxPlayers < playerCount) continue;
            possibleLists.Add(l);
        }

        MissionList decided = possibleLists.GetRandom();
        //Create a copy of the decided list so we can make changes to it
        MissionList list = Instantiate(decided);

        if (decided == null) throw new System.Exception("Decided mission list is null");

        list.IncludedThreads.Add(decided);
        list.name = "Mission List";
        list.AddThreads(ignoreOriginSize: true);

        decidedMissionList.Value = list;

        playerCount.AfterVariableChanged += (val) => { if (Active && TotalVotes.Count >= playerCount) DetermineMission(); };
    }

    /// <summary>
    /// Determine what the mission choices given to the players should be
    /// </summary>
    [Server]
    public override void Begin()
    {
        TotalVotes = new List<HivePlayer>();
        MissionVotes = new Dictionary<Mission, (List<HivePlayer>, int)>();

        if (roundNum == decidedMissionList.Value.List.Count && !decidedMissionList.Value.LoopFinalMission)
        {
            startFinalPhase.Invoke();
            return;
        }

        //List of missions and weights.
        missionListIndex = Mathf.Min(roundNum, decidedMissionList.Value.List.Count - 1);
        List<MissionListEntryEntry> missionDataChoices = decidedMissionList.Value.List[missionListIndex].Missions;

        //Essentially the same as above, but we can edit it as much as we like since it's non-static.
        List<(Mission,float)> missionChoices = new List<(Mission,float)>();

        //Pure list of missions to send to clients
        List<Mission> choices = new List<Mission>();

        /*
        * Find {MissionChoices} random missions from the list of missions given their particular weightings.
        * Useful to imagine as taking balls out of a bag and each mission is a different colour of ball.
        * The number of balls in the bag of any given colour is equal to the weighting of the mission.
        */

        //Total Weight
        float total = 0;

        //Find the total weight of all objects so we know how many balls are in the bag
        missionDataChoices.ForEach(miss =>
        {
            //Only add missions if we meet the condition
            if (miss.Mission.Condition == null || miss.Mission.Condition.Condition())
            {
                total += miss.Weight;
                missionChoices.Add((miss.Mission, total)); //Set each weight to be cumulative so we can find actual probabilities later.
            }
        });

        //If there's only one option, we don't vote on the mission, and just go forward to the next stage.
        if (missionChoices.Count == 1 || numMissionChoices.Value == 1)
        {
            StartCoroutine(Coroutines.Delay(DetermineMission));
            return;
        }

        //Draw a ball from the bag and then remove all instances of that colour of ball so we don't draw the same mission twice.
        for (int i = 0; i < numMissionChoices; i++)
        {
            float rand = 1;
            while (rand == 1) rand = Random.Range(0f, 1f); //Get a random float in the range [0,1).

            // Weight of the selected mission. Used to reduce weight of missions above it in the list to compensate for removal.
            float selectedWeight = 0;

            //Run for each mission choice
            for (int j = 0; j < missionChoices.Count; j++)
            {
                (Mission,float) miss = missionChoices[j];
                //Make sure we only select one item per pass
                if (selectedWeight == 0)
                {
                    //Check probabilities of each item
                    if (rand < miss.Item2 / total)
                    {
                        //Add the mission to the selection
                        MissionVotes.Add(miss.Item1, (new List<HivePlayer>(), 0));
                        choices.Add(miss.Item1);
                        //Remove all instances of this mission, thus reducing the total number of balls in the bag.
                        total -= miss.Item2;
                        //Store the weight of this mission so we can adjust the weight of the other missions to compensate for this missions removal.
                        selectedWeight = miss.Item2;
                        //Make sure this item can't come up again
                        missionChoices.RemoveAt(j);
                        j--;
                    }
                }  
                else
                {
                    //Reduce weight to compensate for a mission below this in the list being picked.
                    miss.Item2 -= selectedWeight;
                    missionChoices[j] = miss;
                }
            }
        }

        decidedMissionChoices.AddRange(choices);
        onMissionChoicesDecided?.Invoke();
        CreateMissionCards(choices, difficutlyMod);
    }

    /// <summary>
    /// Creates the mission cards on the screen
    /// </summary>
    /// <param name="choices"></param>
    [ClientRpc]
    void CreateMissionCards(List<Mission> choices, int difficultyMod)
    {
        cards = new List<GameObject>();
        for (int i = 0; i < choices.Count; i++)
        {
            GameObject card = Instantiate(missionCard);

            MissionCard cardScript = card.GetComponent<MissionCard>();
            cardScript.SetPos(GetCardPositionOnScreen(i, choices.Count));
            cardScript.SetData(choices[i], difficultyMod);
            cardScript.OnMissionCardClicked += MissionCardClicked;
            cards.Add(card);
        }
    }

    static Vector3 GetCardPositionOnScreen(int index, int cardsTotal)
    {
        const float margin = 600;

        float adjustedWidth = Screen.width - (2 * margin);

        float x = Screen.width / 2;
        if (cardsTotal > 1)
        {
            x = margin + adjustedWidth * (index / (float)(cardsTotal - 1));
        }

        return new Vector3(x, Screen.height / 2, 0);
    }

    [Client]
    void MissionCardClicked(Mission data)
    {
        foreach (GameObject card in cards)
        {
            Destroy(card);
        }
        cards = new();

        PlayerVoted(data);
    }

    /// <summary>
    /// Call when a player votes to count it.
    /// </summary>
    /// <param name="ply">The player that voted</param>
    /// <param name="mission">The mission they voted for</param>
    [Command(requiresAuthority = false)]
    public void PlayerVoted(Mission vote, NetworkConnectionToClient conn = null)
    {
        if (!Active) return;
        //Make sure they're alive
        if (!players.Value.TryGetValue(conn, out HivePlayer ply)) return;
        //Make sure players don't vote twice.
        if (TotalVotes.Contains(ply)) return;

        //Make sure they're voting on a valid mission.
        if (!MissionVotes.TryGetValue(vote, out (List<HivePlayer>, int) Tuple)) return;

        Tuple.Item1.Add(ply);
        Tuple.Item2++;

        MissionVotes[vote] = Tuple;
        TotalVotes.Add(ply);

        if (TotalVotes.Count >= playerCount) DetermineMission();
    }

    /// <summary>
    /// Determine which mission won the vote. This becomes the active mission
    /// </summary>
    [Server]
    void DetermineMission()
    {
        int maxVotes = int.MinValue;
        foreach (KeyValuePair<Mission,(List<HivePlayer>,int)> item in MissionVotes)
        {
            if (item.Value.Item2 > maxVotes)
            {
                maxVotes = item.Value.Item2;

                DecidedMission = item.Key;
            }
        }

        currentMission.Value = DecidedMission ?? decidedMissionList.Value.List[missionListIndex].Missions[0].Mission;

        foreach (KeyValuePair<NetworkConnection,HivePlayer> pair in players.Value)
        {
            pair.Value.NextStandCost.Value = currentMission.Value.FavourCost;
        }

        DestroyRemnantCards();
        End();
    }

    /// <summary>
    /// Make sure to destroy any lingering cards.
    /// This is mostly important for dead players who can't click cards
    /// </summary>
    [ClientRpc]
    void DestroyRemnantCards()
    {
        foreach (GameObject card in cards)
        {
            Destroy(card);
        }
        cards = new();
    }
}