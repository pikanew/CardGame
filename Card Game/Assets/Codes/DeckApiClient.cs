using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DeckApiClient : MonoBehaviour
{
    private const string BaseUrl = "https://localhost:7091/api/decks";

    private static DeckApiClient instance;

    public static DeckApiClient Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("DeckApiClient");
                instance = obj.AddComponent<DeckApiClient>();
            }

            return instance;
        }
    }

    private class ApiRequest
    {
        public string url;
        public string json;
        public string token;
        public Action onSuccess;
        public Action<string> onFailure;
    }

    private readonly Queue<ApiRequest> requests = new();
    private bool isProcessing;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator GetDecks(Action<DeckSaveData> onSuccess)
    {
        // Finish pending saves before loading decks again.
        while (isProcessing || requests.Count > 0)
            yield return null;

        using (UnityWebRequest request = UnityWebRequest.Get(BaseUrl))
        {
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + AuthSession.Token
            );

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"Failed to load decks: {request.responseCode} - " +
                    request.error + "\n" +
                    request.downloadHandler.text
                );

                yield break;
            }

            DeckSaveData data =
                JsonUtility.FromJson<DeckSaveData>(
                    request.downloadHandler.text
                );

            if (data == null || data.decks == null)
            {
                Debug.LogError("Invalid deck data received from server.");
                yield break;
            }

            onSuccess?.Invoke(data);
        }
    }

    public void SaveDeck(int slot, Dictionary<CardData, int> cards)
    {
        SavedDeck savedDeck = new SavedDeck();

        foreach (var entry in cards)
        {
            savedDeck.cards.Add(new SavedCard
            {
                cardId = entry.Key.CardId,
                copies = entry.Value
            });
        }

        requests.Enqueue(new ApiRequest
        {
            url = $"{BaseUrl}/{slot}",
            json = JsonUtility.ToJson(savedDeck),
            token = AuthSession.Token,
            onSuccess = () =>
                Debug.Log($"Deck {slot + 1} saved successfully.")
        });

        StartProcessing();
    }

    public void SelectDeck(
        int slot,
        Action onSuccess,
        Action<string> onFailure)
    {
        requests.Enqueue(new ApiRequest
        {
            url = $"{BaseUrl}/{slot}/select",
            token = AuthSession.Token,
            onSuccess = onSuccess,
            onFailure = onFailure
        });

        StartProcessing();
    }

    private void StartProcessing()
    {
        if (!isProcessing)
        {
            isProcessing = true;
            StartCoroutine(ProcessRequests());
        }
    }

    private IEnumerator ProcessRequests()
    {
        while (requests.Count > 0)
        {
            ApiRequest job = requests.Dequeue();

            using (UnityWebRequest request =
                new UnityWebRequest(job.url, "PUT"))
            {
                if (job.json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(
                        Encoding.UTF8.GetBytes(job.json)
                    );

                    request.SetRequestHeader(
                        "Content-Type",
                        "application/json"
                    );
                }

                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader(
                    "Authorization",
                    "Bearer " + job.token
                );

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string error =
                        $"{request.responseCode}: {request.error}\n" +
                        request.downloadHandler.text;

                    Debug.LogError("Deck API error: " + error);

                    job.onFailure?.Invoke(error);
                }
                else
                {
                    job.onSuccess?.Invoke();
                }
            }
        }

        isProcessing = false;
    }
}