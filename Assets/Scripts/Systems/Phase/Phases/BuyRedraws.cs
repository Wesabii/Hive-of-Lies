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

    public event CalculateCostDelegate onCalculateCost;
    public delegate void CalculateCostDelegate(HivePlayer ply, int draws, ref int cost);
    #endregion

    #region Client
    [SerializeField] private BuyRedrawsUI ui;
    [SerializeField] private IntVariable clientRedrawCost;
    #endregion

    [Client]
    public override void OnStartClient()
    {
        ui.OnContinue += (draws) => OnPlayerContinue(draws);
        ui.OnAddDraw += (draws) => clientRedrawCost.Value = CalculateDrawCost(null, draws);
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
        int cost = CalculateTotalCost(ply, draws);
        if (cost > ply.Favour)
        {
            Debug.LogError("Player tried to buy more redraws than they can afford");
            return;
        }
        
        ply.Favour.Value -= cost;
        ply.RedrawsLeft.Value += draws;

        finishedPlayers.Add(ply);
        if (finishedPlayers.Count >= playersOnMission.Count)
        {
            End();
        }
    }

    private int CalculateDrawCost(HivePlayer ply, int drawNum)
    {
        int cost = drawNum * redrawCost.Value;
        onCalculateCost?.Invoke(ply, drawNum, ref cost);
        return cost;
    }

    private int CalculateTotalCost(HivePlayer ply, int numDraws)
    {
        int cost = 0;
        for (int i = 0; i < numDraws; i++)
        {
            cost += CalculateDrawCost(ply, i + 1);
        }
        return cost;
    }
}
