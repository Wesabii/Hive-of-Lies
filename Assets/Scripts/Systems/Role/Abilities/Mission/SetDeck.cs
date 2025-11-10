using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetDeck : RoleAbility
{
    [SerializeField] private List<Card> deck;
    public void ReplaceDeck()
    {
        Owner.Deck.Value.DrawPile.Clear();
        Owner.Deck.Value.DrawPile.AddRange(deck);
    }
}