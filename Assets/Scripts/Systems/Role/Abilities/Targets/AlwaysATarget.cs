using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AlwaysATarget : RoleAbility
{
    [Tooltip("The set of all wasp players")]
    [SerializeField] HivePlayerSet waspPlayers;

    [Server]
    public void BeforeTargetsDisplayed()
    {
        foreach (HivePlayer ply in waspPlayers.Value)
        {
            //If we're already someone's target, then great! We don't need to do anything else.
            //This also makes sure we don't end up with duplicate targets across the wasps
            if (ply.Target.Value == Owner) return;
        }

        //Shuffle so we can be a random wasp's target
        waspPlayers.Shuffle();
        //If there are no wasps, then this ability doesn't do anything ig?
        if (waspPlayers.Value.Count == 0) return;

        foreach (HivePlayer ply in waspPlayers.Value)
        {
            //Don't give someone a target when they shouldn't have one
            if (ply.Target.Value == null) continue;

            //Set ourselves as a target for the first available person
            ply.Target.Value = Owner;
            return;
        }
    }
}
