using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuyRedraws : GamePhase
{
    #region Server
    [SerializeField] private HivePlayerDictionary playersByConnection;
    [SerializeField] private HivePlayerSet playersOnMission;

    [SerializeField] private IntVariable redrawCost;
    
    private List<HivePlayer> finishedPlayers = new();
    #endregion

    #region Client
    [SerializeField] private BuyRedrawsUI ui;
    #endregion

    [Client]
    public override void OnStartClient()
    {
        ui.OnContinue += (draws) => OnPlayerContinue(draws);
    }

    [Server]
    public override void Begin()
    {
        finishedPlayers = new();
        playersOnMission.Value.ForEach(ply => EnableUI(ply.connectionToClient, true));
    }

    [TargetRpc]
    private void EnableUI(NetworkConnection conn, bool active)
    {
        ui.Setup(redrawCost);
    }

    [Command(requiresAuthority = false)]
    private void OnPlayerContinue(int draws, NetworkConnectionToClient conn = null)
    {
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;
        if (finishedPlayers.Contains(ply)) return;

        int allowedDraws = Mathf.Min(draws, Mathf.FloorToInt(ply.Favour.Value / (float) redrawCost.Value));
        int cost = allowedDraws * redrawCost.Value;

        ply.Favour.Value -= cost;
        ply.RedrawsLeft.Value += allowedDraws;

        finishedPlayers.Add(ply);
        if (finishedPlayers.Count >= playersOnMission.Count)
        {
            End();
        }
    }
}
