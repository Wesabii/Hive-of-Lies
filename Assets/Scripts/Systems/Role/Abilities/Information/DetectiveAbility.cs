using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class DetectiveAbility : RoleAbility
{
    /// <summary>
    /// The round on which the ability triggers
    /// </summary>
    [SerializeField] int abilityTriggerRound = 4;

    /// <summary>
    /// Reference to the information popup
    /// </summary>
    [SerializeField] GameObject popup;

    [SerializeField] IntVariable roundNum;

    [SerializeField] HivePlayerSet beePlayers;

    [Server]
    public void OnMissionEnd()
    {
        if (roundNum != abilityTriggerRound) return;

        string txt = "";
        beePlayers.Shuffle();
        foreach (HivePlayer ply in beePlayers.Value)
        {
            if (ply == Owner) continue;
            txt = $"{ply.DisplayName} is the {ply.Role.Value.Data.RoleName}";
            break;
        }
        SpawnPopup(txt);
    }

    [TargetRpc]
    void SpawnPopup(string txt)
    {
        popup.GetComponent<Notification>().SetText(txt);
        Instantiate(popup);
    }
}
