using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionCostPlayerInfo", menuName = "Player Info/Mission Cost")]
public class MissionCostPlayerInfo : ScriptableObject
{
    public Dictionary<HivePlayer, MissionCostPlayer> PlayerInfo = new Dictionary<HivePlayer, MissionCostPlayer>();
}
public class MissionCostPlayer
{
    /// <summary>
    /// Amount of favour to contribute 1 point to the mission cost
    /// </summary>
    public int contributionCost;
    /// <summary>
    /// Multiply how many points are added to the mission cost per payment
    /// </summary>
    public int contributionMultiplier;
    /// <summary>
    /// How many points the player has contributed to the mission cost
    /// </summary>
    public int totalContribution;
}