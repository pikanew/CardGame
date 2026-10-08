using System.Collections.Generic;
using UnityEngine;

public class Hand
{

    [SerializeField] private Card soldierPrefab;
    [SerializeField] private CardData soldierData;
    [SerializeField] private CardView cardViewPrefab;

    private List<Card> cards = new List<Card>();

    public List<Card> Cards => cards;

    public void AddCard(Card card)
    {
        cards.Add(card);
    }

    public void RemoveCard(Card card)
    {
        cards.Remove(card);
    }

    void Start()
    {
    }
}
