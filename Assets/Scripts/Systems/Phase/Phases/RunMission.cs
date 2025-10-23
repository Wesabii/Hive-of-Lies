using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Unity.Services.Analytics;
using System.Linq;

public class RunMission : GamePhase
{
    /// <summary>
    /// The specific type of mission we want to run (e.g. cards, dice, etc.)
    /// </summary>
    public MissionType mission;

    [Tooltip("The currently active mission")]
    [SerializeField] MissionVariable currentMission;

    [Tooltip("Set of all completed missions")]
    [SerializeField] MissionSet completedMissions;

    [Tooltip("Set of all players on the mission")]
    [SerializeField] HivePlayerSet playersOnMission;

    [Tooltip("Set of all players")]
    [SerializeField] HivePlayerSet players;

    [SerializeField] HivePlayerVariable teamLeader;

    [Tooltip("The set to add a plot point to when it is traversed")]
    [SerializeField] MissionPlotPointSet traversedPlotPoints;

    [SerializeField] IntVariable playerCount;

    [Tooltip("The result of the mission")]
    [SerializeField] MissionResultVariable missionResult;

    [Tooltip("The total of all played cards")]
    [SerializeField] IntVariable cardsTotal;

    [Tooltip("The total of all played cards")]
    [SerializeField] IntVariable missionDifficultyMod;

    [Tooltip("Invoked when the mission begins")]
    [SerializeField] GameEvent missionStarted;

    [Tooltip("Invoked when the mission end")]
    [SerializeField] GameEvent missionEnded;

    public override void Begin()
    {
        mission.Active = true;
        mission.StartMission();
        missionStarted?.Invoke();
    }

    public void EndMission()
    {
        Debug.Log("Mission should be ending now");
        completedMissions.Add(currentMission.Value);
        mission.Active = false;

        missionEnded?.Invoke();

        currentMission.Value.AfterEffectTriggered += OnEffectEnded;
        currentMission.Value.OnPlotPointTraversed += (point) => traversedPlotPoints.Add(point);
        //Trigger all effects
        currentMission.Value.TriggerValidEffects(cardsTotal - missionDifficultyMod);

        //Indexing at 1 here is fine because if a mission doesn't have 2 effects, we have bigger problems.
        if (cardsTotal.Value < currentMission.Value.effects[1].Value) missionResult.Value = MissionResult.Fail;
        else missionResult.Value = MissionResult.Success;
    }

    private void OnEffectEnded()
    {
        currentMission.Value.AfterEffectTriggered -= OnEffectEnded;
        //Delay so the result popup can have time to display the correct effects
        StartCoroutine(Coroutines.Delay(EndPhase));
    }

    void EndPhase()
    {
        Debug.Log("All mission effects have finished. Starting the next round");

        MissionCompletedEvent aEvent = new(currentMission, missionResult, playersOnMission, cardsTotal, playerCount);
        AnalyticsService.Instance.RecordEvent(aEvent);
        End();
    }
}

public class MissionCompletedEvent: Unity.Services.Analytics.Event
{
    public MissionCompletedEvent(Mission mission, MissionResult result, HivePlayerSet playersOnMission, int totalCardValue, int playerCount): base("missionCompleted")
    {
        MissionName = mission.MissionName;
        MissionResult = result.ToString();
        WaspsOnMission = playersOnMission.Value.Where((ply) => ply.Team.Value.Team == Team.Wasp).Count();
        BeesOnMission = playersOnMission.Value.Where((ply) => ply.Team.Value.Team == Team.Bee).Count();
        CardsTotal = totalCardValue;
        PlayerCount = playerCount;
    }

    public string MissionName { set { SetParameter("missionName", value); } }
    public string MissionResult { set { SetParameter("missionResult", value); } }
    public int WaspsOnMission { set { SetParameter("waspsOnMission", value); } }
    public int BeesOnMission { set { SetParameter("beesOnMission", value); } }
    public int PlayerCount { set { SetParameter("playerCount", value); } }
    public int CardsTotal { set { SetParameter("cardsTotal", value); } }
}