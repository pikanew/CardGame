using System.Collections.Generic;
using UnityEngine;

public class MatchSetup : MonoBehaviour
{
    [SerializeField] private CardDatabase cardDatabase;
    [SerializeField] private DeckData player2TestDeck;

    public IReadOnlyList<CardData> GetPlayer1Deck()
    {
        return DeckStorage.LoadSelectedDeck(cardDatabase);
    }

    public IReadOnlyList<CardData> GetPlayer2Deck()
    {
        return player2TestDeck.Cards;
    }
}