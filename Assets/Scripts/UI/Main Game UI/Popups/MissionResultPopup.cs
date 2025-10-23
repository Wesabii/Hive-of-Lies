using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using TMPro;

public class MissionResultPopup : NetworkBehaviour
{
    #region CLIENT
    [SerializeField] GameObject popup;

    [SerializeField] GameObject effectPrefab;
    [SerializeField] Transform effectParent;

    [Tooltip("Whether the local player is on the mission")]
    [SerializeField] BoolVariable isOnMission;

    [Tooltip("Invoked when the client closes the mission popup")]
    [SerializeField] GameEvent popupClosed;
    #endregion

    #region SERVER
    [Tooltip("Set of all players")]
    [SerializeField] HivePlayerSet allPlayers;

    [Tooltip("Set of all players on the mission")]
    [SerializeField] HivePlayerSet playersOnMission;

    [Tooltip("The team leader of the current mission")]
    [SerializeField] HivePlayerVariable teamLeader;

    [Tooltip("Set of all played cards this mission")]
    [SerializeField] CardSet playedCards;

    [Tooltip("The total value of played cards")]
    [SerializeField] IntVariable cardsTotal;

    [Tooltip("How much more difficult all missions are")]
    [SerializeField] IntVariable missionDifficulty;

    [Tooltip("The current mission")]
    [SerializeField] MissionVariable currentMission;

    [Tooltip("Invoked when all players have closed the popup")]
    [SerializeField] GameEvent allPlayersClosed;

    List<GameObject> effectTiers = new();
    #endregion

    [Server]
    public void OnMissionEnd()
    {
        foreach (HivePlayer ply in allPlayers.Value)
        {
            List<Card> played = new();

            //Show players on the mission what everyone played. Nobody else gets to see.
            if (ShouldShowContributions(ply)) played.AddRange(playedCards.Value);

            CreatePopup(ply.connectionToClient, "", played, currentMission, cardsTotal, missionDifficulty);
        }
    }

    /// <summary>
    /// Returns true if a the specified player should be able to see the cards that were played
    /// </summary>
    /// <param name="ply"></param>
    /// <returns></returns>
    bool ShouldShowContributions(HivePlayer ply)
    {
        return false;
    }

    [TargetRpc]
    public void CreatePopup(NetworkConnection conn, string flavour, List<Card> contributions, Mission currentMission, int cardsTotal, int difficulty) 
    {
        foreach (GameObject obj in effectTiers) Destroy(obj);
        effectTiers = new();


        MissionEffectTier tier = currentMission.GetValidEffect(cardsTotal - difficulty);

        foreach (MissionEffect effect in tier.effects)
        {
            MissionEffectIcon effectIcon = Instantiate(effectPrefab).GetComponent<MissionEffectIcon>();
            effectTiers.Add(effectIcon.gameObject);
            effectIcon.transform.SetParent(effectParent);
            effectIcon.transform.localScale = new Vector3(2f, 2f, 2f);

            effectIcon.CreateIcon(effect);
        }
        

        popup.SetActive(true);
    }

    [Client]
    public void OnClose()
    {
        popup.SetActive(false);
        popupClosed?.Invoke();
    }
}
