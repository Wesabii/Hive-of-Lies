using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Steamworks;
using Mirror;

public class Setup : GamePhase
{
    [Tooltip("The number of role choices that bees get")]
    [SerializeField] IntVariable beeChoices;

    [Tooltip("The number of role choices that wasps get")]
    [SerializeField] IntVariable waspChoices;

    [Tooltip("The ratio of traitors to innocents")]
    [SerializeField] FloatVariable traitorRatio;

    [Tooltip("All the roles that can appear in the game")]
    [SerializeField] RoleDataSet roles;

    [Tooltip("Runtime set of all the players in the game")]
    [SerializeField] HivePlayerSet allPlayers;

    [Tooltip("Runtime set of all the wasp players in the game")]
    [SerializeField] HivePlayerSet waspPlayers;

    [Tooltip("Runtime set of all the bee players in the game")]
    [SerializeField] HivePlayerSet beePlayers;

    [Tooltip("The player count of the game")]
    [SerializeField] IntVariable playerCount;

    [Tooltip("Dictionary of Players and their respective NetworkConnections")]
    [SerializeField] HivePlayerDictionary playersByNetworkConnection;

    [Tooltip("Invoked when all the setup logic is completed")]
    [SerializeField] GameEvent setupFinished;

    [SerializeField] ETeam beeTeam;
    [SerializeField] ETeam waspTeam;

    [SerializeField] GameObject teamPopup;

    [SerializeField] GameObject playerButton;

    [SerializeField] FloatVariable popupX;
    [SerializeField] FloatVariable popupY;

    [SerializeField] Transform topPlayerRow;
    [SerializeField] Transform rightPlayerRow;
    [SerializeField] Transform bottomPlayerRow;
    [SerializeField] Transform leftPlayerRow;

    [Tooltip("The localised text for the bee popup")]
    [SerializeField] LocalizedString beePopupText;
    [Tooltip("The localised text for the wasp popup")]
    [SerializeField] LocalizedString waspPopupText;

    [Tooltip("The localised text for the bee popup if there are multiple wasps")]
    [SerializeField] LocalizedString beePopupText_pl;
    [Tooltip("The localised text for the wasp popup if you have multiple (or no) allies")]
    [SerializeField] LocalizedString waspPopupText_pl;

    /// <summary>
    /// Run the game setup. This includes handing out roles and selecting teams.
    /// </summary>
    [Server]
    public void BeginSetup()
    {
        Debug.Log("All players have entered the game. Beginning setup.");

        roles.Shuffle();
        //Shuffle the players so we can randomly assign teams
        allPlayers.Shuffle();
        //Shuffle the roles so we can randomly dish them out to players
        //Roles.Shuffle();
        CreateButtons(allPlayers);

        AssignTeams(allPlayers);

        GiveRoleChoices(allPlayers);

        setupFinished?.Invoke();
        Debug.Log("Setup Finished");
    }

    void CreateButtons(HivePlayerSet plys)
    {
        for (int i = 0; i < plys.Count; i++)
        {
            HivePlayer ply = plys[i];
            GameObject button = Instantiate(playerButton);
            NetworkServer.Spawn(button);
            SetupButtonOnClient(button, i, plys.Count);
            PlayerButton plButton = button.GetComponent<PlayerButton>();
            plButton.Owner = ply;
            ply.Button = plButton;
        }
    }

    /// <summary>
    /// Place the player buttons in the correct positions around the table
    /// </summary>
    [ClientRpc]
    void SetupButtonOnClient(GameObject button, int currentPlayer, int maxPlayers)
    {
        // Think about: Should the clients player button always be in the top-middle position if possible?

        // Dictionary<Player Count, Num Seats (top, right, bottom, left)>
        Dictionary<int, (int, int, int, int)> seats = new()
        {
            { 1, (1,0,0,0) },
            { 2, (1,0,1,0) },
            { 3, (1,0,2,01) },
            { 4, (2,0,2,0) },
            { 5, (2,0,3,0) },
            { 6, (3,0,3,0) },
            { 7, (2,1,3,1) },
            { 8, (3,1,3,1) },
            { 9, (2,2,3,2) },
            {10, (3,2,3,2) },
            {11, (3,2,4,2) },
            {12, (4,2,4,2) },
        };

        if (!seats.TryGetValue(maxPlayers, out (int, int, int, int) seatingCounts)) return;

        int i = 0;
        int seatNum = 0;

        i += seatingCounts.Item1;
        if (currentPlayer < i) 
        {
            seatNum = currentPlayer;

            if (seatNum < seatingCounts.Item1 / 2)
            {
                Transform imageTransform = button.GetComponentInChildren<UnityEngine.UI.Image>().transform;
                imageTransform.localScale = new Vector3(-imageTransform.localScale.x, imageTransform.localScale.y, imageTransform.localScale.z);
            }
            button.transform.SetParent(topPlayerRow);
            return;
        }

        i += seatingCounts.Item2;
        if (currentPlayer < i)
        {
            button.transform.SetParent(rightPlayerRow);
            return;
        }

        i += seatingCounts.Item3;
        if (currentPlayer < i)
        {
            seatNum = seatingCounts.Item3 + currentPlayer - i;

            if (seatNum < seatingCounts.Item3/2)
            {
                Transform imageTransform = button.GetComponentInChildren<UnityEngine.UI.Image>().transform;
                imageTransform.localScale = new Vector3(-imageTransform.localScale.x, imageTransform.localScale.y, imageTransform.localScale.z);
            }
            button.transform.SetParent(bottomPlayerRow);
            return;
        }

        i += seatingCounts.Item4;
        if (currentPlayer < i)
        {
            button.transform.SetParent(leftPlayerRow);

            Transform imageTransform = button.GetComponentInChildren<UnityEngine.UI.Image>().transform;
            imageTransform.localScale = new Vector3(-imageTransform.localScale.x, imageTransform.localScale.y, imageTransform.localScale.z);
            return;
        }

        Debug.LogError($"Not enough seats for the current player count ({maxPlayers})");
    }

    /// <summary>
    /// Assign a team to each player
    /// </summary>
    [Server]
    void AssignTeams(HivePlayerSet plys)
    {
        beePlayers.Clear();
        waspPlayers.Clear();
        //Increments to determine whether a player should be an innocent or a traitor
        float teamCounter = 0;
        foreach (HivePlayer ply in plys)
        {
            ply.IsAlive.Value = true;
            teamCounter += traitorRatio;
            if (teamCounter >= 1)
            {
                teamCounter--;
                ply.Team = ScriptableObject.CreateInstance<TeamVariable>();
                ply.Team.Value = waspTeam;
                waspPlayers.Add(ply);
            }
            else
            {
                ply.Team = ScriptableObject.CreateInstance<TeamVariable>();
                ply.Team.Value = beeTeam;
                beePlayers.Add(ply);
            }

            int waspTotal = Mathf.FloorToInt(playerCount * traitorRatio);
            DisplayTeamPopup(ply.connectionToClient, ply.Team.Value.Team, waspTotal);
        }
    }

    [TargetRpc]
    void DisplayTeamPopup(NetworkConnection conn, Team team, int waspTotal)
    {
        teamPopup = Instantiate(teamPopup);
        

        if (team == Team.Wasp)
        {
            string text;
            if (waspTotal == 2) text = string.Format(waspPopupText.GetLocalizedString(), waspTotal - 1);
            else text = string.Format(waspPopupText_pl.GetLocalizedString(), waspTotal - 1);

            teamPopup.GetComponent<Notification>().SetText(text);
        }
        else
        {
            string text;
            if (waspTotal == 1) text = string.Format(beePopupText.GetLocalizedString(), waspTotal);
            else text = string.Format(beePopupText_pl.GetLocalizedString(), waspTotal);

            teamPopup.GetComponent<Notification>().SetText(text);
        }
        
        float x = popupX.Value > 0 ? popupX : Screen.width/2;
        float y = popupY.Value > 0 ? popupY : Screen.height/2;
        teamPopup.transform.GetChild(0).position = new Vector3(x, y, 1);
    }

    /// <summary>
    /// Give players a selection of roles to choose from
    /// </summary>
    [Server]
    void GiveRoleChoices(HivePlayerSet plys)
    {
        foreach (HivePlayer ply in plys)
        {
            ply.RoleChoices = new();
            ply.Role.Value = null;
            int choices = ply.Team.Value.Team == Team.Bee ? beeChoices : waspChoices;
            for (int i = 0; i < roles.Value.Count; i++)
            {
                RoleData role = roles.Value[i];
                if (role.Team != ply.Team.Value.Team) continue;
                if (!role.Enabled) continue;
                if (role.PlayersRequired > playerCount) continue;

                ply.RoleChoices.Add(role);
                roles.Remove(role);
                i--;
                if (ply.RoleChoices.Count == choices) break;
            }
        }
    }

    /// <summary>
    /// We don't actually need the begin for this, since it begins under different circumstances.
    /// </summary>
    public override void Begin()
    {
    }
}