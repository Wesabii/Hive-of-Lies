using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Mirror;
using Unity.Services.Analytics;

public class GameEnd : NetworkBehaviour
{
    #region SERVER
    /// <summary>
    /// Amount of research needed for the Bees to win the game
    /// </summary>
    [SerializeField] IntVariable ResearchNeededForWin;

    /// <summary>
    /// How much honey has to be stolen before the wasps win
    /// </summary>
    [SerializeField] IntVariable HoneyNeededForWin;

    [SerializeField] IntVariable HoneyStolen;

    [SerializeField] IntVariable ResearchProgress;

    [SerializeField] HivePlayerDictionary playersByConnection;

    [SerializeField] HivePlayerSet beePlayers;

    [SerializeField] HivePlayerSet waspPlayers;

    [SerializeField] GameObject gameEndScreen;

    [SerializeField] IntVariable roundNum;

    bool hasWon;
    #endregion


    [Tooltip("The text to display when the bees wins")]
    [SerializeField] LocalizedString beesWinText;

    [Tooltip("The text to display when the wasps wins")]
    [SerializeField] LocalizedString waspsWinText;

    [Tooltip("The text to display on a solo win")]
    [SerializeField] LocalizedString soloWinText;

    public override void OnStartServer()
    {
        HoneyStolen.AfterVariableChanged += change =>
        {
            if (change >= HoneyNeededForWin) StartCoroutine(Coroutines.Delay(WaspsWin));
        };

        ResearchProgress.AfterVariableChanged += change =>
        {
            if (change >= ResearchNeededForWin) StartCoroutine(Coroutines.Delay(BeesWin));
        };

        waspPlayers.AfterItemRemoved += (item) =>
        {
            if (waspPlayers.Value.Count == 0) BeesWin();
        };
    }

    public void OnSetupFinished()
    {
        ResearchNeededForWin.Value = beePlayers.Count + 2;
        HoneyNeededForWin.Value = waspPlayers.Count + 3;
    }

    [Server]
    public void BeesWin()
    {
        if (hasWon) return;
        hasWon = true;
        //If 3 honey is stolen at the same time as 3 wasp facts are learned, the bees don't win
        if (HoneyStolen >= HoneyNeededForWin) return;
        GameObject screen = Instantiate(gameEndScreen);
        screen.GetComponent<PlayAgainButton>().SetText(beesWinText.GetLocalizedString());
        NetworkServer.Spawn(screen);

        HiveGameEnded waspsWinEvent = CreateGameEndEvent(Team.Bee);
        AnalyticsService.Instance.RecordEvent(waspsWinEvent);
    }

    [Server]
    public void WaspsWin()
    {
        if (hasWon) return;
        hasWon = true;
        GameObject screen = Instantiate(gameEndScreen);
        screen.GetComponent<PlayAgainButton>().SetText(waspsWinText.GetLocalizedString());
        NetworkServer.Spawn(screen);

        HiveGameEnded waspsWinEvent = CreateGameEndEvent(Team.Wasp);
        AnalyticsService.Instance.RecordEvent(waspsWinEvent);
    }

    [Server]
    public void PlayerWins(NetworkConnection conn)
    {
        if (!playersByConnection.Value.TryGetValue(conn, out HivePlayer ply)) return;

        GameObject screen = Instantiate(gameEndScreen);
        screen.GetComponent<PlayAgainButton>().SetText(string.Format(soloWinText.GetLocalizedString(), ply.DisplayName));
        NetworkServer.Spawn(screen);

        HiveGameEnded waspsWinEvent = CreateGameEndEvent(Team.None);
        AnalyticsService.Instance.RecordEvent(waspsWinEvent);
    }

    private HiveGameEnded CreateGameEndEvent(Team team)
    {
        HiveGameEnded ev = new();
        ev.BeePoints = ResearchProgress.Value;
        ev.WaspPoints = HoneyStolen.Value;
        ev.WaspsAlive = waspPlayers.Value.Count;
        ev.Team = team;
        ev.PlayerCount = playersByConnection.Value.Count;
        ev.RoundNum = roundNum.Value;

        return ev;
    }
}

public class HiveGameEnded : Unity.Services.Analytics.Event
{
    public HiveGameEnded() : base("hiveGameEnded")
    {
    }

    public int BeePoints { set { SetParameter("beePoints", value); } }
    public int WaspPoints { set { SetParameter("waspPoints", value); } }
    public int WaspsAlive { set { SetParameter("waspsAlive", value); } }
    public Team Team { set { SetParameter("team", value.ToString()); } }
    public int PlayerCount { set { SetParameter("playerCount", value); } }
    public int RoundNum { set { SetParameter("roundNum", value); } }
}