using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Deck
{
    /// <summary>
    /// The cards in the draw pile
    /// </summary>
    public CardSet DrawPile = ScriptableObject.CreateInstance<CardSet>();

    /// <summary>
    /// The cards in the discard pile
    /// </summary>
    public CardSet DiscardPile = ScriptableObject.CreateInstance<CardSet>();

    /// <summary>
    /// The cards in a players hand
    /// </summary>
    public CardSet Hand = ScriptableObject.CreateInstance<CardSet>();

    /// <summary>
    /// The cards the player has played
    /// </summary>
    public CardSet Played = ScriptableObject.CreateInstance<CardSet>();

    /// <summary>
    /// Invoked when a card is drawn
    /// </summary>
    public event DrawDelegate BeforeDraw;
    public delegate void DrawDelegate(ref Card card, bool simulated = false);

    /// <summary>
    /// Invoked when a card is played
    /// </summary>
    public event System.Action<Card> AfterPlay;

    /// <summary>
    /// Shuffle your discard pile into the deck
    /// </summary>
    public void Shuffle()
    {
        for (int i = DiscardPile.Count-1; i >= 0; i--)
        {
            Card card = DiscardPile[i];

            if (!card.DestroyOnDraw) DrawPile.Add(card);

            DiscardPile.Remove(card);
        }

        DrawPile.Shuffle();
    }

    /// <summary>
    /// Shuffle your discard pile, hand, and played cards into the deck
    /// </summary>
    public void Reshuffle()
    {
        foreach (Card card in Hand)
        {
            if (card.DestroyOnDraw) continue;
            DrawPile.Add(card);
        }
        Hand.Clear();

        foreach (Card card in Played)
        {
            if (card.DestroyOnDraw || card.DestroyOnPlay) continue;
            DrawPile.Add(card);
        }
        Played.Clear();
        Shuffle();
    }

    /// <summary>
    /// Draw cards from the deck
    /// </summary>
    public void Draw(int cards = 1)
    {
        for (int i = 0; i < cards; i++)
        {
            if (DrawPile.Count == 0)
            {
                if (DiscardPile.Count == 0)
                {
                    Debug.LogError("Tried to draw from a deck with no cards");
                    return;
                }
                Shuffle();
            }

            if (DrawPile.Count == 0)
            {
                Debug.LogError("Tried to draw from a deck with no cards.");
                return;
            }

            Card card = DrawPile[0];

            foreach (CardEffect effect in card.DrawEffects) effect.TriggerEffect();

            BeforeDraw?.Invoke(ref card);

            Hand.Add(card);

            //It's no longer secret if the player has drawn and seen it.
            card.IsSecret = false;

            if (DrawPile.Contains(card)) DrawPile.Remove(card);
        }
    }

    /// <summary>
    /// Returns the top card of the players deck, accounting for role manipulation
    /// </summary>
    public Card GetTopCard()
    {
        if (DrawPile.Count == 0)
        {
            if (DiscardPile.Count == 0)
            {
                Debug.LogError("Tried to draw from a deck with no cards");
            }
            Shuffle();
        }

        Card card = DrawPile[0];
        BeforeDraw?.Invoke(ref card, simulated:true);

        return card;
    }

    /// <summary>
    /// Discard a card from the hand
    /// </summary>
    public void Discard(Card card = null)
    {
        if (Hand.Count == 0) return;
        // By default discard the oldest card in the hand
        if (card == null) card = Hand[0];
        if (!Hand.Contains(card)) return;

        foreach (CardEffect effect in card.DiscardEffects) effect.TriggerEffect();
        DiscardPile.Add(card);
        Hand.Remove(card);
    }

    /// <summary>
    /// Play a card from the hand
    /// </summary>
    public Card Play(Card card = null)
    {
        if (Hand.Count == 0) return null;
        // By default play the oldest card in the hand
        if (card == null) card = Hand[0];
        if (!Hand.Contains(card)) return null;

        Played.Add(card);
        foreach (CardEffect effect in card.PlayEffects) effect.TriggerEffect();
        Hand.Remove(card);
        AfterPlay?.Invoke(card);
        return card;
    }

    public Deck()
    {

    }
}
