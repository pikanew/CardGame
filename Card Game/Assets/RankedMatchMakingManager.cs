using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RankedMatchmakingManager : MonoBehaviour
{
    private const string BaseUrl =
        "https://localhost:7091/api/matchmaking";

    [Header("UI")]
    [SerializeField] private Button findMatchButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text ratingText;

    private bool isQueued;
    private bool isMatched;
    private bool isBusy;

    private Coroutine pollingCoroutine;

    [Serializable]
    private class MatchmakingStatus
    {
        public string status;
        public string roomCode;
        public int playerNumber;
        public int rating;
    }

    private void Start()
    {
        if (!AuthSession.IsLoggedIn)
        {
            SceneManager.LoadScene("Login");
            return;
        }

        findMatchButton.interactable = false;
        cancelButton.interactable = false;

        statusText.text = "Checking matchmaking status...";

        StartCoroutine(CheckStatus());
    }

    public void FindMatch()
    {
        if (isQueued || isMatched || isBusy)
            return;

        StartCoroutine(JoinQueue());
    }

    private IEnumerator JoinQueue()
    {
        isBusy = true;

        findMatchButton.interactable = false;
        statusText.text = "Joining ranked queue...";

        using (UnityWebRequest request =
            new UnityWebRequest($"{BaseUrl}/queue", "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Authorization",
                "Bearer " + AuthSession.Token
            );

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                statusText.text =
                    "Could not join matchmaking: " +
                    request.downloadHandler.text;

                findMatchButton.interactable = true;
                isBusy = false;
                yield break;
            }

            MatchmakingStatus response =
                JsonUtility.FromJson<MatchmakingStatus>(
                    request.downloadHandler.text
                );

            if (response == null)
            {
                statusText.text = "Invalid matchmaking response.";
                findMatchButton.interactable = true;
                isBusy = false;
                yield break;
            }

            UpdateStatus(response);
        }

        isBusy = false;
    }

    private IEnumerator CheckStatus()
    {
        using (UnityWebRequest request =
            UnityWebRequest.Get($"{BaseUrl}/status"))
        {
            request.SetRequestHeader(
                "Authorization",
                "Bearer " + AuthSession.Token
            );

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                statusText.text = "Failed to check matchmaking status.";
                findMatchButton.interactable = true;
                yield break;
            }

            MatchmakingStatus response =
                JsonUtility.FromJson<MatchmakingStatus>(
                    request.downloadHandler.text
                );

            if (response != null)
                UpdateStatus(response);
        }
    }

    private IEnumerator PollMatchmaking()
    {
        while (isQueued && !isMatched)
        {
            yield return new WaitForSeconds(2f);

            yield return StartCoroutine(CheckStatus());
        }

        pollingCoroutine = null;
    }

    private void UpdateStatus(MatchmakingStatus response)
    {
        ratingText.text = $"Rating: {response.rating}";

        switch (response.status)
        {
            case "notQueued":
                isQueued = false;
                isMatched = false;

                statusText.text = "Ready to find a match.";

                findMatchButton.interactable = true;
                cancelButton.interactable = false;
                break;

            case "queued":
                isQueued = true;

                statusText.text = "Searching for an opponent...";

                findMatchButton.interactable = false;
                cancelButton.interactable = true;

                if (pollingCoroutine == null)
                {
                    pollingCoroutine =
                        StartCoroutine(PollMatchmaking());
                }

                break;

            case "matched":
                isQueued = false;
                isMatched = true;

                findMatchButton.interactable = false;
                cancelButton.interactable = false;

                statusText.text =
                    $"Match Found!\n" +
                    $"Room: {response.roomCode}\n" +
                    $"You are Player {response.playerNumber}";

                RankedMatchSession.SetMatch(
                    response.roomCode,
                    response.playerNumber
                );

                SceneManager.LoadScene("Game");

                Debug.Log(
                    $"Ranked match found: {response.roomCode}, " +
                    $"Player {response.playerNumber}"
                );

                break;
        }
    }

    public void CancelSearch()
    {
        if (!isQueued || isBusy)
            return;

        StartCoroutine(LeaveQueue(false));
    }

    private IEnumerator LeaveQueue(bool returnToMenu)
    {
        isBusy = true;

        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
            pollingCoroutine = null;
        }

        cancelButton.interactable = false;
        statusText.text = "Leaving queue...";

        using (UnityWebRequest request =
            UnityWebRequest.Delete($"{BaseUrl}/queue"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Authorization",
                "Bearer " + AuthSession.Token
            );

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                isQueued = false;

                if (returnToMenu)
                {
                    SceneManager.LoadScene("MainMenu");
                    yield break;
                }

                statusText.text = "Search cancelled.";
                findMatchButton.interactable = true;
            }
            else
            {
                Debug.LogError(
                    "Failed to leave queue: " +
                    request.downloadHandler.text
                );

                statusText.text = "Could not cancel search.";
            }
        }

        isBusy = false;

        if (isQueued)
            yield return StartCoroutine(CheckStatus());
    }

    public void Back()
    {
        if (isBusy)
            return;

        if (isQueued)
        {
            StartCoroutine(LeaveQueue(true));
            return;
        }

        SceneManager.LoadScene("MainMenu");
    }
}