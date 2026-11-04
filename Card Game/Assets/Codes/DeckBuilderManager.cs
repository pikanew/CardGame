using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class DeckBuilderManager : MonoBehaviour
{
    private const int MaxDeckSize = 15;
    private const int MaxCopies = 2;

    [Header("Card Collection")]
    [SerializeField] private CardDatabase cardDatabase;
    [SerializeField] private DeckBuilderCardView cardViewPrefab;

    [Header("UI")]
    [SerializeField] private Transform deckCardsContainer;
    [SerializeField] private Transform databaseContent;
    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private TMP_Text deckNameText;
    [SerializeField] private Button selectDeckButton;
    [SerializeField] private TMP_Text selectDeckButtonText;

    private bool isSelectingDeck;

    private int selectedDeckIndex = -1;

    private string SavePath =>
    Path.Combine(Application.persistentDataPath, "decks.json");

    private const int DeckSlotCount = 5;

    private readonly Dictionary<CardData, int>[] decks = new Dictionary<CardData, int>[DeckSlotCount];

    private int currentDeckIndex = 0;

    private Dictionary<CardData, int> deck => decks[currentDeckIndex];

    private readonly Dictionary<CardData, DeckBuilderCardView> databaseViews = new();
    private readonly Dictionary<CardData, DeckBuilderCardView> deckViews = new();

    private int deckSize => GetDeckSize();

    private int GetDeckSize()
    {
        int count = 0;

        foreach (int copies in deck.Values)
            count += copies;

        return count;
    }


    private int GetMaxCopies(CardData data)
    {
        return data.Legendary ? 1 : 2;
    }
    private void OnDecksLoaded(DeckSaveData saveData)
    {
        Debug.Log("OnDecksLoaded called");
        selectedDeckIndex = saveData.selectedDeckIndex;

        if (selectedDeckIndex < 0 || selectedDeckIndex >= DeckSlotCount)
            selectedDeckIndex = -1;

        Dictionary<string, CardData> cardsById = new();

        foreach (CardData card in cardDatabase.Cards)
        {
            cardsById[card.CardId] = card;
        }

        for (int i = 0; i < DeckSlotCount; i++)
        {
            decks[i].Clear();

            if (i >= saveData.decks.Count)
                continue;

            SavedDeck savedDeck = saveData.decks[i];

            if (savedDeck?.cards == null)
                continue;

            foreach (SavedCard savedCard in savedDeck.cards)
            {
                if (cardsById.TryGetValue(savedCard.cardId, out CardData card))
                {
                    decks[i][card] = savedCard.copies;
                }
            }
        }

        currentDeckIndex = selectedDeckIndex >= 0
            ? selectedDeckIndex
            : 0;

        databaseContent.gameObject.SetActive(true);

        RefreshDeckUI();

        Debug.Log("Decks loaded from backend successfully.");
    }

    private void Start()
    {
        Debug.Log("DeckBuilderManager Start called");

        for (int i = 0; i < DeckSlotCount; i++)
        {
            decks[i] = new Dictionary<CardData, int>();
        }

        PopulateDatabase();

        databaseContent.gameObject.SetActive(false);

        StartCoroutine(
            DeckApiClient.Instance.GetDecks(OnDecksLoaded)
        );
    }

    public void NextDeck()
    {
        if (isSelectingDeck)
            return;

        currentDeckIndex = (currentDeckIndex + 1) % DeckSlotCount;
        RefreshDeckUI();
    }

    public void PreviousDeck()
    {
        if (isSelectingDeck)
            return;
        currentDeckIndex = (currentDeckIndex - 1 + DeckSlotCount) % DeckSlotCount;
        RefreshDeckUI();
    }

    private void RefreshDeckUI()
    {
        foreach (DeckBuilderCardView view in deckViews.Values)
        {
            Destroy(view.gameObject);
        }

        deckViews.Clear();

        foreach (var entry in deck)
        {
            CardData data = entry.Key;
            int copies = entry.Value;

            DeckBuilderCardView view = Instantiate(cardViewPrefab, deckCardsContainer);

            view.Initialize(
                data,
                1,
                copies,
                MaxCopies,
                true,
                RemoveCard
            );

            RectTransform rect = view.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(90, 145);

            deckViews.Add(data, view);
        }

        foreach (CardData data in cardDatabase.Cards)
        {
            deck.TryGetValue(data, out int copies);

            databaseViews[data].UpdateCopyCount(copies, MaxCopies, false);
        }
        deckNameText.text = $"Deck {currentDeckIndex + 1} / {DeckSlotCount}";

        UpdateDeckCount();
    }

    private void PopulateDatabase()
    {
        foreach (CardData data in cardDatabase.Cards)
        {
            DeckBuilderCardView view = Instantiate(cardViewPrefab, databaseContent);

            view.Initialize(
                data,
                1,
                0,
                GetMaxCopies(data),
                false,
                AddCard
            );

            databaseViews.Add(data, view);
        }
    }

    private void AddCard(CardData data)
    {
        if (isSelectingDeck)
            return;

        if (deckSize >= MaxDeckSize)
            return;

        deck.TryGetValue(data, out int copies);

        int maxCopies = GetMaxCopies(data);

        if (copies >= maxCopies)
            return;

        copies++;
        deck[data] = copies;

        if (deckViews.TryGetValue(data, out DeckBuilderCardView view))
        {
            view.UpdateCopyCount(copies, MaxCopies, true);
        }
        else
        {
            view = Instantiate(cardViewPrefab, deckCardsContainer);

            view.Initialize(
                data,
                1,
                copies,
                MaxCopies,
                true,
                RemoveCard
            );

            deckViews.Add(data, view);

            // Current deck cards are smaller than database cards.
            RectTransform rect = view.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(90, 145);
        }

        databaseViews[data].UpdateCopyCount(copies, MaxCopies, false);
        UpdateDeckCount();
        SaveDecks();
    }

    private void RemoveCard(CardData data)
    {
        if (isSelectingDeck)
            return;

        if (!deck.TryGetValue(data, out int copies))
            return;

        copies--;

        if (copies == 0)
        {
            deck.Remove(data);

            Destroy(deckViews[data].gameObject);
            deckViews.Remove(data);
        }
        else
        {
            deck[data] = copies;
            deckViews[data].UpdateCopyCount(copies, MaxCopies, true);
        }

        databaseViews[data].UpdateCopyCount(copies, MaxCopies, false);
        if (currentDeckIndex == selectedDeckIndex && deckSize < MaxDeckSize)
        {
            selectedDeckIndex = -1;
            UpdateSelectDeckButton();
        }
        UpdateDeckCount();
        SaveDecks();
    }

    private void UpdateDeckCount()
    {
        deckCountText.text = $"Deck: {deckSize}/{MaxDeckSize}";
        UpdateSelectDeckButton();
    }

    private void LoadDecks()
    {
        if (!File.Exists(SavePath))
            return;

        string json = File.ReadAllText(SavePath);

        DeckSaveData saveData = JsonUtility.FromJson<DeckSaveData>(json);

        if (saveData == null || saveData.decks == null)
            return;

        currentDeckIndex = Mathf.Clamp(
            saveData.currentDeckIndex,
            0,
            DeckSlotCount - 1
        );

        selectedDeckIndex = saveData.selectedDeckIndex;

        if (selectedDeckIndex < 0 || selectedDeckIndex >= DeckSlotCount)
            selectedDeckIndex = -1;

        Dictionary<string, CardData> cardsById = new();

        foreach (CardData card in cardDatabase.Cards)
        {
            cardsById[card.CardId] = card;
        }

        int slotCount = Mathf.Min(DeckSlotCount, saveData.decks.Count);

        for (int i = 0; i < slotCount; i++)
        {
            SavedDeck savedDeck = saveData.decks[i];

            if (savedDeck?.cards == null)
                continue;

            foreach (SavedCard savedCard in savedDeck.cards)
            {
                if (cardsById.TryGetValue(savedCard.cardId, out CardData card))
                {
                    decks[i][card] = savedCard.copies;
                }
            }
        }
        if (selectedDeckIndex >= 0)
        {
            int selectedDeckSize = 0;

            foreach (int copies in decks[selectedDeckIndex].Values)
                selectedDeckSize += copies;

            if (selectedDeckSize != MaxDeckSize)
                selectedDeckIndex = -1;
        }
    }

    private void UpdateSelectDeckButton()
    {
        bool isFull = deckSize == MaxDeckSize;
        bool isSelected = currentDeckIndex == selectedDeckIndex;

        selectDeckButton.interactable =
        isFull && !isSelected && !isSelectingDeck;

        if (isSelected)
            selectDeckButtonText.text = "Selected";
        else
            selectDeckButtonText.text = "Select Deck";
    }

    public void SelectDeck()
    {
        if (deckSize != MaxDeckSize || isSelectingDeck)
            return;

        int slot = currentDeckIndex;

        isSelectingDeck = true;
        UpdateSelectDeckButton();

        DeckApiClient.Instance.SelectDeck(
            slot,
            () =>
            {
                selectedDeckIndex = slot;
                isSelectingDeck = false;

                UpdateSelectDeckButton();

                Debug.Log($"Deck {slot + 1} selected successfully.");
            },
            error =>
            {
                isSelectingDeck = false;
                UpdateSelectDeckButton();

                Debug.LogError("Failed to select deck: " + error);
            }
        );
    }

    private void SaveDecks()
    {
        DeckApiClient.Instance.SaveDeck(currentDeckIndex, deck);
    }
}
