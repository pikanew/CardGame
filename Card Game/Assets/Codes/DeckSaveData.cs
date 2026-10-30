using System;
using System.Collections.Generic;


[Serializable]
public class SavedCard
{
    public string cardId;
    public int copies;
}

[Serializable]
public class SavedDeck
{
    public List<SavedCard> cards = new();
}

[Serializable]
public class DeckSaveData
{
    public int currentDeckIndex = 0;
    public int selectedDeckIndex = -1;
    public List<SavedDeck> decks = new();
}