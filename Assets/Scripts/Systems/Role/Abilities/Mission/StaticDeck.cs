using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticDeck : RoleAbility
{

    public void SubscribeToEvents()
    {
        Owner.Deck.Value.DrawPile.OnItemAdded += PreventDeckAdd;
        Owner.Deck.Value.DiscardPile.OnItemAdded += PreventDiscardAdd;
        Owner.Deck.Value.DrawPile.OnItemRemoved += PreventDeckRemove;
        Owner.Deck.Value.DiscardPile.OnItemRemoved += PreventDiscardRemove;

        foreach (Card card in Owner.Deck.Value.DrawPile)
        {
            card.DestroyOnDraw = false;
            card.DestroyOnPlay = false;
        }
    }

    void PreventDeckAdd(ref Card card)
    {
        //If this card was just reshuffled from our discard pile, or our playe cards then we can let it be added
        if (Owner.Deck.Value.DiscardPile.Contains(card) || Owner.Deck.Value.Played.Contains(card)) return;
        card = null;
    }

    void PreventDeckRemove(ref Card card)
    {
        //If this card was just drawn, then we can let it be removed
        if (Owner.Deck.Value.Hand.Contains(card)) return;
        card = null;
    }

    void PreventDiscardAdd(ref Card card)
    {
        //If this card was just discarded, then we can let it be added
        if (Owner.Deck.Value.Hand.Contains(card)) return;
        card = null;
    }

    void PreventDiscardRemove(ref Card card)
    {
        //If this card was just reshuffled from our discard pile, then we can let it be added
        if (Owner.Deck.Value.DrawPile.Contains(card)) return;
        card = null;
    }
}