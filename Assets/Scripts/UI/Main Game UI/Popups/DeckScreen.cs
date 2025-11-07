using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DeckScreen : NetworkBehaviour
{
    [SerializeField] HivePlayerDictionary playersByConnection;
    [SerializeField] bool isDrawPile;

    #region Client
    [SerializeField] GameObject cardDisplay;
    [SerializeField] GameObject screen;
    [SerializeField] Transform cardPool;

    List<CardDisplay> cardList = new();
    #endregion

    private void Start()
    {
        foreach (KeyValuePair<NetworkConnection,HivePlayer> pair in playersByConnection.Value)
        {
            if (isDrawPile)
            {
                pair.Value.Deck.Value.DrawPile.AfterItemRemoved += (card) => { if (!card.IsSecret) CardRemoved(pair.Key, card); };
                pair.Value.Deck.Value.DrawPile.AfterItemAdded += (card) => { if (!card.IsSecret) CardAdded(pair.Key, card); };
            }
            else
            {
                pair.Value.Deck.Value.DiscardPile.AfterItemRemoved += (card) => { if (!card.IsSecret) CardRemoved(pair.Key, card); };
                pair.Value.Deck.Value.DiscardPile.AfterItemAdded += (card) => { if (!card.IsSecret) CardAdded(pair.Key, card); };
            }
        }
    }

    [TargetRpc]
    void CardRemoved(NetworkConnection conn, Card card)
    {
        CardDisplay display = null;

        foreach (CardDisplay c in cardList)
        {
            //Only the visuals of the card really matter, since this is a purely clientside thing.
            if (c.GetCard().Sprite == card.Sprite) display = c;
        }
        //Wasn't in the list anyway
        if (display == null) return;

        //Otherwise destroy it.
        cardList.Remove(display);
        Destroy(display.gameObject);
    }

    [TargetRpc]
    void CardAdded(NetworkConnection conn, Card card)
    {
        CardDisplay display = Instantiate(cardDisplay).GetComponent<CardDisplay>();
        display.SetCard(card);
        display.transform.SetParent(cardPool);

        //Sort by value
        for (int i = 0; i < cardPool.childCount; i++)
        {
            CardDisplay child = cardPool.GetChild(i).GetComponent<CardDisplay>();
            if (i == 0 && child.GetCard().Value < card.Value) display.transform.SetAsFirstSibling();

            if (child.GetCard().Value >= card.Value) display.transform.SetSiblingIndex(i);
        }

        cardList.Add(display);
    }

    public void Toggle()
    {
        screen.SetActive(!screen.activeInHierarchy);
    }
}
