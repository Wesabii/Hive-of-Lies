using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
using UnityEngine.UI;

public class CardMissionUI : NetworkBehaviour
{
    #region CLIENT

    [SerializeField] GameObject UI;
    [SerializeField] Image drawResult;
    [SerializeField] Image nextCard;
    [SerializeField] TMP_Text drawCost;
    [SerializeField] GameObject drawButton;
    [SerializeField] GameObject submitButton;
    [SerializeField] IntVariable favour;

    [Tooltip("Returns true if the player is on the mission")]
    [SerializeField] BoolVariable isOnMission;
    #endregion
    #region SERVER

    [Tooltip("The current round of the game")]
    [SerializeField] IntVariable roundNum;

    [Tooltip("What draws are required to succeed the mission")]
    [SerializeField] IntVariable missionDifficulty;

    [Tooltip("Invoked when a player draws a card")]
    [SerializeField] NetworkingEvent playerDrew;

    [Tooltip("Invoked when a player plays a card")]
    [SerializeField] NetworkingEvent playerPlayed;

    [Tooltip("All players in the game")]
    [SerializeField] HivePlayerSet allPlayers;

    [Tooltip("All players by their NetworkConnection")]
    [SerializeField] HivePlayerDictionary playersByConnection;

    #endregion

    private void Start()
    {
        allPlayers.AfterItemRemoved += (ply) => 
        {
            playerPlayed?.Invoke(ply.connectionToClient);
            ForceCloseUI(ply.connectionToClient);
        };
    }

    [Server]
    public void AfterDeckCreated()
    {
        foreach (HivePlayer ply in allPlayers.Value) 
        {
            ply.Deck.Value.Hand.AfterItemAdded += card => ReceiveDrawResultFromServer(ply.connectionToClient, card.Sprite);
            ply.NextDrawCost.AfterVariableChanged += val => OnDrawCostChanged(ply.connectionToClient, val);
        };
    }

    [ClientRpc]
    public void ShowUI()
    {
        if (!isOnMission) return;

        UI.SetActive(true);
        PlayerDrewCard();
    }

    [TargetRpc]
    public void TargetActivateMissionUI(NetworkConnection conn, Sprite sprite, int cost)
    {
        UI.SetActive(true);
        drawResult.sprite = sprite;
        drawCost.text = cost.ToString();
    }

    /// <summary>
    /// Called when the draw button is clicked on this client
    /// </summary>
    [Client]
    public void DrawNewCard()
    {
        PlayerDrewCard();
        nextCard.gameObject.SetActive(false);
    }

    [Command(requiresAuthority = false)]
    public void PlayerDrewCard(NetworkConnectionToClient conn = null)
    {
        playerDrew?.Invoke(conn);
    }

    /// <summary>
    /// The server tells us what we drew
    /// </summary>
    [TargetRpc]
    void ReceiveDrawResultFromServer(NetworkConnection conn, Sprite sprite)
    {
        drawResult.color = new Color(drawResult.color.r, drawResult.color.g, drawResult.color.b, 1);
        drawResult.sprite = sprite;
    }

    [Server]
    public void ShowTopCard(NetworkConnection conn)
    {
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;

        SeeTopCard(conn, ply.Deck.Value.GetTopCard().Sprite);
    }

    [TargetRpc]
    void SeeTopCard(NetworkConnection conn, Sprite sprite)
    {
        nextCard.gameObject.SetActive(true);
        nextCard.sprite = sprite;
    }

    [TargetRpc]
    void OnDrawCostChanged(NetworkConnection conn, int val)
    {
        drawCost.text = val.ToString();
        drawButton.GetComponent<Button>().interactable = val <= favour;
    }

    /// <summary>
    /// Called when the submit button is clicked on this client
    /// </summary>
    [Client]
    public void PlayCard()
    {
        UI.SetActive(false);
        drawResult.color = new Color(drawResult.color.r, drawResult.color.g, drawResult.color.b, 0);
        PlayerPlayedCard();
    }

    [ClientRpc]
    public void CloseUI()
    {
        UI.SetActive(false);
    }

    [TargetRpc]
    void ForceCloseUI(NetworkConnection conn)
    {
        UI.SetActive(false);
    }

    [Command(requiresAuthority = false)]
    void PlayerPlayedCard(NetworkConnectionToClient conn = null)
    {
        playerPlayed?.Invoke(conn);
    }
}
