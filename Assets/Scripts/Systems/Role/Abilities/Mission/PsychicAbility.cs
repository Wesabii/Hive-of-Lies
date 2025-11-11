using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PsychicAbility : RoleAbility
{
    private Card topCard => Owner.Deck.Value.DrawPile.Value.Count > 0 ? Owner.Deck.Value.DrawPile[0] : null;
    private CardsMission mission;
    protected override void OnRoleGiven()
    {
        Owner.RedrawsLeft.AfterVariableChanged += (_) => OnOwnerDraw();
    }

    public void RegisterMission(GamePhase phase)
    {
        if (mission != null) return;
        if (phase is not RunMission) return;
        if ((phase as RunMission).mission is not CardsMission) return;
        mission = (phase as RunMission).mission as CardsMission;
    }

    private void OnOwnerDraw()
    {
        if (!mission) return;
        if (!topCard) return;
        mission.ShowTopCardAs(Owner.connectionToClient, topCard.Sprite);
    }
}
