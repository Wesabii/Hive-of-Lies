using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifyDeck : RoleAbility
{
    [SerializeField] private List<Card> cardsToAdd;
    [SerializeField] private List<Card> cardsToRemove;
    public void OnPick()
    {
        foreach (Card card in cardsToRemove)
        {
            Owner.Deck.Value.DrawPile.Remove(card);
        }
        Owner.Deck.Value.DrawPile.AddRange(cardsToAdd);
    }
}