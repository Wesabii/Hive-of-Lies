using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class FavourController : NetworkBehaviour
{
    #region CLIENT
    [Tooltip("The favour of the local player")]
    [SerializeField] IntVariable favour;
    #endregion

    #region SERVER
    [Tooltip("Set of all players in the game")]
    [SerializeField] HivePlayerSet allPlayers;
    #endregion

    public void AfterSetup()
    {
        foreach (HivePlayer ply in allPlayers.Value)
        {
            ply.Favour.AfterVariableChanged += (val) => ChangeFavour(ply.connectionToClient, val);
            //Update the favour now, just in case
            ChangeFavour(ply.connectionToClient, ply.Favour);
        }
    }

    [TargetRpc]
    public void ChangeFavour(NetworkConnection conn, int change)
    {
        favour.Value = change;
    }
}