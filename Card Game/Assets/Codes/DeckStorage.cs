using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DeckStorage
{
    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "decks.json");

    public static List<CardData> LoadSelectedDeck(CardDatabase database)
    {
        List<CardData> result = new();

        if (!File.Exists(SavePath))
            return result;

        string json = File.ReadAllText(SavePath);
        DeckSaveData saveData = JsonUtility.FromJson<DeckSaveData>(json);

        if (saveData?.decks == null || saveData.decks.Count == 0)
            return result;

        int index = Mathf.Clamp(
            saveData.selectedDeckIndex,
            0,
            saveData.decks.Count - 1
        );

        SavedDeck selectedDeck = saveData.decks[index];

        if (selectedDeck?.cards == null)
            return result;

        Dictionary<string, CardData> cardsById = new();

        foreach (CardData card in database.Cards)
        {
            cardsById[card.CardId] = card;
        }

        foreach (SavedCard savedCard in selectedDeck.cards)
        {
            if (!cardsById.TryGetValue(savedCard.cardId, out CardData card))
                continue;

            for (int i = 0; i < Mathf.Clamp(savedCard.copies, 0, 2); i++)
            {
                result.Add(card);
            }
        }

        return result;
    }
}