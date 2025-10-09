using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CardsMission : MissionType
{
    [Tooltip("Total value of all played cards")]
    [SerializeField] IntVariable playedTotal;

    /// <summary>
    /// List of all players who have played cards
    /// </summary>
    private List<HivePlayer> playersPlayed;

    [Tooltip("All the roles in the game")]
    [SerializeField] RoleSet allRoles;

    [Tooltip("All the players in the game")]
    [SerializeField] HivePlayerSet allPlayers;

    [Tooltip("All players by their NetworkConnection")]
    [SerializeField] HivePlayerDictionary playersByConnection;

    [Tooltip("Set of all players on the mission")]
    [SerializeField] HivePlayerSet playersOnMission;

    [Tooltip("Set of all played cards")]
    [SerializeField] CardSet playedCards;

    [Tooltip("Number of draws the player gets to make for free. Should always be at least 1.")]
    [SerializeField] IntVariable freeDraws;

    [Tooltip("Cost of the first paid draw.")]
    [SerializeField] IntVariable firstDrawCost;

    [Tooltip("How much to increase all draw costs by")]
    [SerializeField] IntVariable globalDrawCostMod;

    [Tooltip("Invoked when all players have played their cards")]
    [SerializeField] GameEvent allPlayersPlayed;

    [Tooltip("Invoked when all players decks have been created")]
    [SerializeField] GameEvent afterDeckCreated;

    [Tooltip("The UI for this game phase")]
    [SerializeField] CardMissionUI UI;

    [Server]
    public void AfterRolesChosen()
    {
        //Delay by a frame so the Two Bees roles have a chance to listen to this event
        StartCoroutine(Coroutines.Delay(0.01f, () =>
        {
            foreach (HivePlayer ply in allPlayers.Value)
            {
                Role role = ply.Role.Value;
                foreach (Card card in role.Data.StartingDeck) ply.Deck.Value.DrawPile.Add(Instantiate(card));

                ply.Deck.Value.Shuffle();
            }

            afterDeckCreated?.Invoke();
        }));

    }

    [Client]
    public override void OnStartClient()
    {
        UI.onDraw += () => PlayerClickedDraw();
        UI.onPlay += () => PlayerClickedSubmit();
    }

    public void OnServerConnected(NetworkConnection conn)
    {
        if (!Active) return;
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;
        if (!playersOnMission.Value.Contains(ply)) return;
        if (playersPlayed.Contains(ply)) return;

        if (ply.Deck.Value.Hand.Count == 0) ply.Deck.Value.Draw();

        //UI.TargetActivateMissionUI(conn, ply.Deck.Value.Hand[0].Sprite, ply.NextDrawCost);
    }

    [Server]
    public override void StartMission()
    {
        playedTotal.Value = 0;
        playersPlayed = new();
        playedCards.Value = new();
        foreach (HivePlayer ply in playersOnMission)
        {
            EnableUI(ply.connectionToClient);
            ply.Deck.Value.Draw();
            SendClientDrawInfo(ply.connectionToClient, ply.Deck.Value.Hand[0].Sprite, ply.NextDrawCost);
        }
    }

    [TargetRpc]
    private void EnableUI(NetworkConnection conn)
    {
        UI.ShowUI();
    }

    [Command(requiresAuthority = false)]
    private void PlayerClickedDraw(NetworkConnectionToClient conn = null)
    {
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;
        //If the player isn't on the mission
        if (!playersOnMission.Value.Contains(ply)) return;

        if (ply.Favour < ply.NextDrawCost && ply.NextDrawCost > 0) return;

        Deck deck = ply.Deck;

        if (deck.Hand.Count > 0)
        {
            Card handCard = deck.Hand[0];
            deck.Discard(handCard);
        }

        ply.Favour.Value -= ply.NextDrawCost;

        ply.NextDrawCost.Value = CalculateDrawCost(ply.NumDraws);

        deck.Draw();

        ply.NumDraws++;

        SendClientDrawInfo(conn, deck.Hand[0].Sprite, ply.NextDrawCost);
    }

    [TargetRpc]
    private void SendClientDrawInfo(NetworkConnection conn, Sprite cardSprite, int nextDrawCost)
    {
        UI.ChangeDrawCost(nextDrawCost);
        UI.ChangeHandCard(cardSprite);
    }

    [Command(requiresAuthority = false)]
    private void PlayerClickedSubmit(NetworkConnectionToClient conn = null)
    {
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;
        //If the player isn't on the mission
        if (!playersOnMission.Value.Contains(ply)) return;
        //If the player has already played a card
        if (playersPlayed.Contains(ply)) return;
        Deck deck = ply.Deck;

        Card card = deck.Play();

        playedTotal.Value += card.Value;

        playersPlayed.Add(ply);

        playedCards.Add(card);

        ply.NumDraws.Value = 0;
        ply.NextDrawCost.Value = 0;

        ply.Deck.Value.Reshuffle();

        if (playersPlayed.Count >= playersOnMission.Value.Count)
        {
            allPlayersPlayed?.Invoke();
        }
    }

    /// <summary>
    /// Calculates the cost of the draw
    /// </summary>
    /// <param name="ply">The player drawing a card</param>
    /// <param name="numRerolls">The number of cards they have drawn so far</param>
    /// <returns></returns>
    [Server]
    public int CalculateDrawCost(int numDraws)
    {
        //Make sure the correct number of rerolls are free
        if (numDraws < freeDraws) return 0;

        //Calculate how many rerolls we have used that aren't free.
        numDraws -= freeDraws;

        //Formula. Can edit this however we like for balance.
        int cost = firstDrawCost * (numDraws + 1);

        //Currently deciding to put this before roles tinker with it. Might change later, who knows.
        cost += globalDrawCostMod;

        return Mathf.Max(cost, 0);
    }
}