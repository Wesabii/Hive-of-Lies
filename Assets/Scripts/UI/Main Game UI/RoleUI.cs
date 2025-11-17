using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Analytics;

public class RoleUI : NetworkBehaviour
{
    #region CLIENT
    List<GameObject> cards;

    [SerializeField] TMP_Text RoleName;
    [SerializeField] TMP_Text RoleDescription;
    [SerializeField] GameObject RoleCard;
    [SerializeField] Image RoleSprite;
    [SerializeField] Image RoleNameBackground;
    [SerializeField] ColourVariable WaspColour;
    [SerializeField] ColourVariable BeeColour;

    [SerializeField] GameObject screenCover;
    [SerializeField] GameObject chooseRoleText;

    [SerializeField] FloatVariable cardXPosition;
    [SerializeField] FloatVariable cardYPosition;
    List<RoleData> choices = new();
    #endregion

    [Space]
    [Space]

    #region SERVER
    [Tooltip("All players by their network connection")]
    [SerializeField] HivePlayerDictionary playersByConnection;

    [Tooltip("Runtime set of all the roles in the game")]
    [SerializeField] RoleSet allRoles;

    [Tooltip("Runtime set of all rejected roles")]
    [SerializeField] RoleDataSet rejectedRoles;

    [Tooltip("The amount of players in the game")]
    [SerializeField] IntVariable playerCount;

    [Tooltip("The event to invoke when all players have chosen their roles")]
    [SerializeField] GameEvent allPlayersChosenRoles;
    #endregion

    public override void OnStartClient()
    {
        base.OnStartClient();
        cards = new List<GameObject>();
    }

    [Server]
    public void OnSetupFinished()
    {
        allRoles.Clear();
        foreach (KeyValuePair<NetworkConnection, HivePlayer> pair in playersByConnection.Value)
        {
            ReceiveRoleInfo(pair.Key, pair.Value.RoleChoices);
        }
    }

    [TargetRpc]
    public void ReceiveRoleInfo(NetworkConnection conn, List<RoleData> roleChoices)
    {
        if (screenCover != null) screenCover.SetActive(true);
        if (chooseRoleText != null) chooseRoleText.SetActive(true);

        if (roleChoices.Count == 0) Debug.LogError("Player has no role choices");
        if (roleChoices[0].Team == Team.Bee) RoleNameBackground.color = BeeColour;
        if (roleChoices[0].Team == Team.Wasp) RoleNameBackground.color = WaspColour;

        //Don't bother with the role cards if you have no role choices
        if (roleChoices.Count == 1)
        {
            RoleCardClicked(roleChoices[0]);
            return;
        }

        for (int i = 0; i < roleChoices.Count; i++)
        {
            GameObject card = Instantiate(RoleCard);
            RoleCard cardScript = card.GetComponent<RoleCard>();
            cardScript.SetPos(GetCardPositionOnScreen(i, roleChoices.Count));
            cardScript.SetData(roleChoices[i]);
            cardScript.OnRoleCardClicked += RoleCardClicked;
            if (i == 0) cardScript.Tutorial.SetActive(true);
            cards.Add(card);
            choices.Add(roleChoices[i]);
        }
    }

    [Client]
    void RoleCardClicked(RoleData data)
    {
        if (screenCover != null) screenCover.SetActive(false);
        if (chooseRoleText != null) chooseRoleText.SetActive(false);
        foreach (GameObject card in cards)
        {
            Destroy(card);
        }

        RoleName.text = data.RoleName;
        RoleDescription.text = data.Description;
        RoleSprite.sprite = data.Sprite;
        RoleSprite.gameObject.SetActive(true);

        AssignPlayerRole(data);
    }

    [Command(requiresAuthority = false)]
    void AssignPlayerRole(RoleData data, NetworkConnectionToClient conn = null)
    {

        //If the player doesn't exist
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;
        //If the player already has a role
        if (ply.Role.Value != null) return;
        //If the role selected isn't one of the players choices
        if (!ply.RoleChoices.Contains(data)) return;

        foreach (RoleData rl in ply.RoleChoices)
        {
            if (rl == data)
            {
                AnalyticsService.Instance.RecordEvent(new RolePickedEvent(rl, playerCount.Value));
            }
            else
            {
                AnalyticsService.Instance.RecordEvent(new RolePassedEvent(rl, playerCount.Value, data));
                rejectedRoles.Add(rl);
            }
        }

        List<RoleAbility> abilities = new();
        for (int i = 0; i < data.Abilities.Count; i++)
        {
            GameObject abilityObject = Instantiate(data.Abilities[i]);
            NetworkServer.Spawn(abilityObject, conn);

            RoleAbility[] scripts = abilityObject.GetComponents<RoleAbility>();

            foreach (RoleAbility ability in scripts)
            {
                ability.Owner = ply;
                abilities.Add(ability);
            }
        }

        Role role = new()
        {
            Abilities = abilities,
            Data = data
        };

        allRoles.Add(role);
        ply.Role.Value = role;
        ply.Favour.Value += data.StartingFavour;

        if (allRoles.Value.Count == playerCount) StartCoroutine(Coroutines.Delay(() => allPlayersChosenRoles?.Invoke()));
    }

    public void ChangeRoleDescription(string description)
    {
        RoleDescription.text = description;
    }

    Vector3 GetCardPositionOnScreen(int index, int cardsTotal)
    {
        //In case these aren't set properly
        float screenX = cardXPosition.Value > 0 ? cardXPosition.Value : Screen.height/2;
        float screenY = cardYPosition.Value > 0 ? cardYPosition.Value : Screen.width / 2;
        const float spacing = 400;

        float x = screenX;

        x += spacing * (1 - cardsTotal + (2*index))/2;

        return new Vector3(x, cardYPosition, 0);
    }
}

public class RolePickedEvent : Unity.Services.Analytics.Event
{
    public RolePickedEvent(RoleData role, int playerCount, string eventName = "rolePicked"): base(eventName)
    {
        RoleName = role.RoleName;
        Team = role.Team;
        Favour = role.StartingFavour;
        PlayerCount = playerCount;
    }
    public string RoleName { set { SetParameter("roleName", value); } }
    public Team Team { set { SetParameter("team", value.ToString()); } }
    public int Favour { set { SetParameter("favour", value); } }
    public int PlayerCount { set { SetParameter("playerCount", value); } }
}

public class RolePassedEvent : RolePickedEvent
{
    public RolePassedEvent(RoleData role, int playerCount, RoleData rolePicked) : base(role, playerCount, "rolePassed")
    {
        RolePicked = rolePicked.RoleName;
    }

    public string RolePicked { set { SetParameter("rolePicked", value); } }
}