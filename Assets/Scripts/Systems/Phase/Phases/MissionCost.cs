using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionPayment : GamePhase
{
    #region Server
    [SerializeField] private HivePlayerDictionary playersByConnection;
    [SerializeField] private HivePlayerSet playersOnMission;

    [SerializeField] private GameEvent onPaymentFailed;
    
    private List<HivePlayer> paidPlayers = new();
    private int totalContributions;
    #endregion

    #region Client
    [SerializeField] private MissionPaymentUI missionCostUI;
    #endregion

    [Client]
    public override void OnStartClient()
    {
        missionCostUI.OnPaid += (cost) => OnPlayerPaid(cost);
    }

    [Server]
    public override void Begin()
    {
        foreach (HivePlayer ply in playersOnMission)
        {
            EnableUI(ply.connectionToClient, true);
        }
    }

    [TargetRpc]
    private void EnableUI(NetworkConnection conn, bool active)
    {
        missionCostUI.Setup(2);
    }

    [Command(requiresAuthority = false)]
    private void OnPlayerPaid(int cost, NetworkConnectionToClient conn = null)
    {
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;
        if (paidPlayers.Contains(ply)) return;

        paidPlayers.Add(ply);
        cost = Mathf.Clamp(cost, 0, ply.Favour);
        ply.Favour.Value -= cost;

        //Edit this later to allow role tinkering
        totalContributions += cost;
        if (paidPlayers.Count >= playersOnMission.Count)
        {
            OnAllPlayersPaid();
        }
    }

    [Server]
    private void OnAllPlayersPaid()
    {
        if (totalContributions > 0 /* the mission cost */)
        {
            Debug.Log("End payment");
            End();
        }
        else
        {
            onPaymentFailed?.Invoke();
        }
    }
}
