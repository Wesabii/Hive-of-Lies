using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// A powerful ability given to players, typically based on their role
/// </summary>
public class RoleAbility : NetworkBehaviour
{

    #region Fields
    /// <summary>
    /// Private counterpart to <see cref="Active"/>
    /// </summary>
    [HideInInspector]
    public bool active = true;

    private HivePlayer owner;

    /// <summary>
    /// The player that owns this ability
    /// </summary>
    [HideInInspector]
    public HivePlayer Owner
    {
        get
        {
            return owner;
        }
        set
        {
            owner = value;
            OnRoleGiven();
        }
    }
    #endregion

    protected virtual void OnRoleGiven() { }

    public bool IsOwner(HivePlayer ply)
    {
        if (ply != null) return ply == owner;
        return owner.isLocalPlayer;
    }
}
