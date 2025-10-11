using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Unity.Services.Analytics;

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

        int waspsOnMission = 0;
        foreach (HivePlayer ply in playersOnMission.Value)
        {
            if (ply.Team.Value.Team == Team.Wasp) waspsOnMission++;
        }
        Dictionary<string, object> parameters = new()
        {
            { "missionName",  currentMission.Value.MissionName},
            { "missionResult", missionResult.Value.ToString()},
            { "playerCount", playerCount.Value },
            { "waspsOnMission", waspsOnMission },
            { "beesOnMission", playersOnMission.Value.Count - waspsOnMission },
            { "cardsTotal", cardsTotal.Value }
        };
        AnalyticsService.Instance.CustomData("missionCompleted", parameters);
        End();
    }
}