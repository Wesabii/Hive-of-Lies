using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Contains information on how the mission will be played (with a deck of cards, rolling dice etc.)
/// </summary>
public abstract class MissionType : NetworkBehaviour
{

    /// <summary>
    /// Whether the current phase of the game is the mission phase
    /// </summary>
    [HideInInspector]
    public bool Active;

    /// <summary>
    /// Called when the mission begins (after TeamLeader is successfully voted in)
    /// </summary>
    public virtual void StartMission()
    {
    }

    /// <summary>
    /// Called when the mission ends
    /// </summary>
    /// <param name="res">The result of the mission</param>
    /// <param name="triggerEffects">Whether the mission should trigger the success or fail effect</param>
    public virtual void EndMission()
    {
    }
}

/// <summary>
/// Currently only success or fail, but maybe we'll want something new in future
/// </summary>
public enum MissionResult
{
    Success,
    Fail
}
