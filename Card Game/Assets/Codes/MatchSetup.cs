using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchSetup : MonoBehaviour
{
    [SerializeField] private CardDatabase cardDatabase;
    [SerializeField] private DeckData player2TestDeck;

    public IEnumerator LoadMatchDecks(
        Action<IReadOnlyList<CardData>, IReadOnlyList<CardData>> onLoaded)
    {
        DeckSaveData saveData = null;

        yield return DeckApiClient.Instance.GetDecks(
            data => saveData = data
        );

        if (saveData == null || saveData.decks == null)
        {
            Debug.LogError("Failed to load decks from backend.");
            yield break;
        }

        int index = saveData.selectedDeckIndex;

        if (index < 0 || index >= saveData.decks.Count)
        {
            Debug.LogError("No deck selected.");
            yield break;
        }

        SavedDeck selectedDeck = saveData.decks[index];

        if (selectedDeck?.cards == null)
        {
            Debug.LogError("Selected deck is invalid.");
            yield break;
        }

        Dictionary<string, CardData> cardsById = new();

        foreach (CardData card in cardDatabase.Cards)
        {
            cardsById.Add(card.CardId, card);
        }

        List<CardData> player1Cards = new();
        HashSet<string> usedCardIds = new();

        foreach (SavedCard savedCard in selectedDeck.cards)
        {
            if (savedCard.copies < 1 || savedCard.copies > 2 ||
                !usedCardIds.Add(savedCard.cardId) ||
                !cardsById.TryGetValue(savedCard.cardId, out CardData card))
            {
                Debug.LogError("Selected deck contains invalid cards.");
                yield break;
            }

            for (int i = 0; i < savedCard.copies; i++)
            {
                player1Cards.Add(card);
            }
        }

        if (player1Cards.Count != 15)
        {
            Debug.LogError("Selected deck must contain 15 cards.");
            yield break;
        }

        onLoaded?.Invoke(player1Cards, player2TestDeck.Cards);
    }
}