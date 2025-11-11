using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SleepyBeeAbility : RoleAbility
{
    [SerializeField] IntVariable WaspPoints;
    [SerializeField] IntVariable WaspPointsToWin;
    [SerializeField] Card deckReplacementCard;
    public override void OnStartServer()
    {
        WaspPoints.AfterVariableChanged += (int val) =>
        {
            if (val == WaspPointsToWin.Value - 1) UpgradeDeck();
        };
    }

    void UpgradeDeck()
    {
        int deckSize = Owner.Deck.Value.DrawPile.Count;
        Owner.Deck.Value.DrawPile.Clear();
        for (int i = 0; i < deckSize; i++)
        {
            Owner.Deck.Value.DrawPile.Add(Instantiate(deckReplacementCard));
        }
    }
}