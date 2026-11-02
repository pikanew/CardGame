using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PrivateRematchManager : MonoBehaviour
{
    [SerializeField] private Button playAgainButton;
    [SerializeField] private TMP_Text statusText;

    private const string BaseUrl =
        "https://localhost:7091/api/matches";

    private bool isWaiting;

    [Serializable]
    private class RematchStatus
    {
        public int round;
        public string phase;
        public bool playerReady;
        public bool opponentReady;
    }

    public void PlayAgain()
    {
        if (isWaiting)
            return;

        if (!RankedMatchSession.HasMatch)
        {
            SceneManager.LoadScene(
                SceneManager.GetActiveScene().name
            );
            return;
        }

        StartCoroutine(RequestPrivateRematch());
    }

    private IEnumerator RequestPrivateRematch()
    {
        isWaiting = true;
        playAgainButton.interactable = false;

        string url =
            $"{BaseUrl}/{RankedMatchSession.RoomCode}/rematch";

        // Read the current round before requesting a rematch.
        RematchStatus initialStatus = null;

        yield return StartCoroutine(
            SendRequest(url, "GET",
                response => initialStatus = response)
        );

        if (initialStatus == null ||
            initialStatus.phase != "finished")
        {
            RematchFailed();
            yield break;
        }

        int previousRound = initialStatus.round;

        statusText.text = "Waiting for opponent...";

        RematchStatus response = null;

        yield return StartCoroutine(
            SendRequest(url, "POST",
                result => response = result)
        );

        if (response == null)
        {
            RematchFailed();
            yield break;
        }

        // Both players may already have agreed.
        if (response.round > previousRound)
        {
            RestartGame();
            yield break;
        }

        // Wait until the other player agrees.
        while (true)
        {
            yield return new WaitForSeconds(1f);

            RematchStatus status = null;

            yield return StartCoroutine(
                SendRequest(url, "GET",
                    result => status = result)
            );

            if (status == null)
                continue;

            if (status.round > previousRound)
            {
                RestartGame();
                yield break;
            }
        }
    }

    private IEnumerator SendRequest(
        string url,
        string method,
        Action<RematchStatus> onSuccess)
    {
        using (UnityWebRequest request =
            new UnityWebRequest(url, method))
        {
            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Authorization",
                "Bearer " + AuthSession.Token
            );

            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"Rematch request failed: " +
                    $"{request.responseCode}\n" +
                    request.downloadHandler.text
                );

                yield break;
            }

            RematchStatus response =
                JsonUtility.FromJson<RematchStatus>(
                    request.downloadHandler.text
                );

            onSuccess?.Invoke(response);
        }
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    private void RematchFailed()
    {
        isWaiting = false;
        playAgainButton.interactable = true;

        statusText.text = "Could not start rematch.";
    }
}