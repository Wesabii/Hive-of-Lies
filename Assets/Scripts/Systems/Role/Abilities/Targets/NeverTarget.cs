using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NeverTarget : RoleAbility
{
    [SerializeField] HivePlayerSet waspPlayers;
    [SerializeField] HivePlayerSet beePlayers;

    protected override void OnRoleGiven()
    {
        waspPlayers.Shuffle();
        foreach (HivePlayer ply in waspPlayers.Value)
        {
            ply.Target.OnVariableChanged += OnTargetChosen;

            if (ply.Target == Owner)
            {
                HivePlayer newTarget = Owner;
                ReRandomiseTarget(ref newTarget);
                ply.Target.Value = newTarget;
                return;
            }
        }
    }

    [Server]
    void OnTargetChosen(HivePlayer oldTarget, ref HivePlayer newTarget)
    {
        ReRandomiseTarget(ref newTarget);
    }

    void ReRandomiseTarget(ref HivePlayer target)
    {
        if (target != Owner) return;

        beePlayers.Shuffle();

        foreach (HivePlayer ply in beePlayers.Value)
        {
            if (ply == Owner) continue;
            target = ply;
            return;
        }
    }
}
