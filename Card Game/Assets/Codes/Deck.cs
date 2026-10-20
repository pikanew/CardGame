using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    private readonly List<Card> cards = new();

    public int Count => cards.Count;

    public void AddCard(Card card)
    {
        cards.Add(card);
    }

    public Card DrawCard()
    {
        if (cards.Count == 0)
            return null;

        Card card = cards[0];
        cards.RemoveAt(0);

        return card;
    }

    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);

            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
