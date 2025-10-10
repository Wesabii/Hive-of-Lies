using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HivePlayer : NetworkBehaviour
{
    #region Meta
    /// <summary>
    /// The ID associated with the player. This is currently derived from their CSteamID.
    /// </summary>
    public ulong PlayerID;

    /// <summary>
    /// The display name of this player
    /// </summary>
    public string DisplayName;

    /// <summary>
    /// Returns true on the client that is represented by this object.
    /// </summary>
    public bool LocalPlayer => hasAuthority && NetworkClient.active;
    #endregion

    #region Gameplay
    /// <summary>
    /// The team of this player. Whether they are a bee or a wasp.
    /// </summary>
    public TeamVariable Team;

    /// <summary>
    /// The game's currency. Used for redrawing cards, placing extra votes, and running for team leader.
    /// </summary>
    public IntVariable Favour;

    /// <summary>
    /// The role that the player has. This determines their deck of cards, starting favour, and special ability.
    /// </summary>
    public RoleVariable Role;

    /// <summary>
    /// How much favour it costs to draw another card
    /// </summary>
    public IntVariable NextDrawCost;

    /// <summary>
    /// How much favour it costs to place another upvote
    /// </summary>
    public IntVariable NextUpvoteCost;

    /// <summary>
    /// How much favour it costs to place another upvote
    /// </summary>
    public IntVariable NextDownvoteCost;

    /// <summary>
    /// How much favour this player has spent on votes this round
    /// </summary>
    public int FavourSpentVoting;

    /// <summary>
    /// How much favour it costs to stand for Team Leader
    /// </summary>
    public IntVariable NextStandCost;

    /// <summary>
    /// How many votes this player is currently deciding to give
    /// </summary>
    public IntVariable NumVotes;

    /// <summary>
    /// The players deck of cards
    /// </summary>
    public DeckVariable Deck;

    /// <summary>
    /// How many times this player has drawn cards
    /// </summary>
    public IntVariable NumDraws;

    /// <summary>
    /// The maximum draws the player is allowed on the mission
    /// </summary>
    public IntVariable RedrawsLeft;

    /// <summary>
    /// Whether this player is alive or not
    /// </summary>
    public BoolVariable IsAlive;

    /// <summary>
    /// The target of this player. Always null if the player is a bee.
    /// </summary>
    public HivePlayerVariable Target;

    /// <summary>
    /// How much favour it costs to sting. Only relevant on wasps.
    /// </summary>
    public IntVariable StingCost;

    /// <summary>
    /// List of the choices of roles the player will have at the start of the game
    /// </summary>
    public List<RoleData> RoleChoices = new();

    /// <summary>
    /// The on-screen button associated with this player
    /// </summary>
    public PlayerButton Button;
    #endregion

    public event TeamDelegate OnGetTeam;
    public delegate void TeamDelegate(ref ETeam team);

    private void Awake()
    {
        ResetValues();
    }

    public void ResetValues()
    {
        Team = ScriptableObject.CreateInstance<TeamVariable>();
        Favour = ScriptableObject.CreateInstance<IntVariable>();
        Role = ScriptableObject.CreateInstance<RoleVariable>();
        NextDrawCost = ScriptableObject.CreateInstance<IntVariable>();
        NextUpvoteCost = ScriptableObject.CreateInstance<IntVariable>();
        NextDownvoteCost = ScriptableObject.CreateInstance<IntVariable>();
        NextStandCost = ScriptableObject.CreateInstance<IntVariable>();
        NumVotes = ScriptableObject.CreateInstance<IntVariable>();
        Deck = ScriptableObject.CreateInstance<DeckVariable>();
        NumDraws = ScriptableObject.CreateInstance<IntVariable>();
        RedrawsLeft = ScriptableObject.CreateInstance<IntVariable>();
        IsAlive = ScriptableObject.CreateInstance<BoolVariable>();
        Target = ScriptableObject.CreateInstance<HivePlayerVariable>();
        StingCost = ScriptableObject.CreateInstance<IntVariable>();
        StingCost.Value = 10;
        IsAlive.Value = true;
        Deck.Value = new();

        //Favour cannot be negative
        Favour.OnVariableChanged += (int oldVal, ref int newVal) => newVal = Mathf.Max(0, newVal);
    }

    /// <summary>
    /// Get the team of this player, accounting for role abilities
    /// </summary>
    /// <returns></returns>
    public ETeam GetTeam()
    {
        ETeam returnVal = Team;
        OnGetTeam?.Invoke(ref returnVal);
        return returnVal;
    }

    [ClientRpc]
    public void SetReady(bool ready)
    {
        Button.SetReady(ready);
    }
}

public enum Team
{
    None,
    Bee,
    Wasp,
}