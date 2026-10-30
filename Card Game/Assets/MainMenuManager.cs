using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text welcomeText;
    [SerializeField] private Button playButton;

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Game";

    private DeckSaveData deckData;

    private void Start()
    {
        if (!AuthSession.IsLoggedIn)
        {
            SceneManager.LoadScene("Login");
            return;
        }

        welcomeText.text = $"Welcome, {AuthSession.Username}!";

        playButton.interactable = false;

        StartCoroutine(LoadDecks());
    }

    private IEnumerator LoadDecks()
    {
        yield return DeckApiClient.Instance.GetDecks(data =>
        {
            deckData = data;
            UpdatePlayButton();
        });
    }

    private void UpdatePlayButton()
    {
        playButton.interactable = HasSelectedDeck();
    }

    private bool HasSelectedDeck()
    {
        if (deckData == null || deckData.decks == null)
            return false;

        int index = deckData.selectedDeckIndex;

        if (index < 0 || index >= deckData.decks.Count)
            return false;

        SavedDeck selectedDeck = deckData.decks[index];

        if (selectedDeck?.cards == null)
            return false;

        int cardCount = 0;

        foreach (SavedCard card in selectedDeck.cards)
        {
            if (card.copies < 1 || card.copies > 2)
                return false;

            cardCount += card.copies;
        }

        return cardCount == 15;
    }

    public void OpenDeckBuilder()
    {
        SceneManager.LoadScene("DeckBuilding");
    }

    public void Play()
    {
        if (!HasSelectedDeck())
            return;

        SceneManager.LoadScene(gameSceneName);
    }

    public void Logout()
    {
        AuthSession.Logout();
        SceneManager.LoadScene("Login");
    }
}