using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    private const string BaseUrl = "https://localhost:7091/api/rooms";

    [Header("UI")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text statusText;

    private string currentRoomCode;
    private int playerNumber;
    private Coroutine pollingCoroutine;
    private bool isBusy;

    [Serializable]
    private class RoomInfo
    {
        public string code;
        public int playerNumber;
        public bool isReady;
    }

    private void Start()
    {
        if (!AuthSession.IsLoggedIn)
        {
            SceneManager.LoadScene("Login");
            return;
        }

        statusText.text = "Create a room or enter a room code.";
    }

    public void CreateRoom()
    {
        if (isBusy || !string.IsNullOrEmpty(currentRoomCode))
            return;

        StartCoroutine(SendRoomRequest(BaseUrl));
    }

    public void JoinRoom()
    {
        if (isBusy || !string.IsNullOrEmpty(currentRoomCode))
            return;

        string code = roomCodeInput.text.Trim().ToUpperInvariant();

        if (code.Length != 6)
        {
            statusText.text = "Enter a valid 6-character room code.";
            return;
        }

        StartCoroutine(SendRoomRequest($"{BaseUrl}/{code}/join"));
    }

    private IEnumerator SendRoomRequest(string url)
    {
        isBusy = true;
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;

        statusText.text = "Connecting...";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
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
                    $"Failed to connect: {request.responseCode}";

                Debug.LogError(request.downloadHandler.text);

                isBusy = false;
                createRoomButton.interactable = true;
                joinRoomButton.interactable = true;
                yield break;
            }

            RoomInfo room = JsonUtility.FromJson<RoomInfo>(
                request.downloadHandler.text
            );

            if (room == null || string.IsNullOrEmpty(room.code))
            {
                statusText.text = "Invalid room response.";
                isBusy = false;
                createRoomButton.interactable = true;
                joinRoomButton.interactable = true;
                yield break;
            }

            currentRoomCode = room.code;
            playerNumber = room.playerNumber;

            roomCodeText.text = $"Room Code: {currentRoomCode}";

            UpdateRoomStatus(room);

            if (room.isReady)
            {
                EnterGame();
                yield break;
            }

            pollingCoroutine = StartCoroutine(PollRoomStatus());
        }

        isBusy = false;
    }

    private IEnumerator PollRoomStatus()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);

            using (UnityWebRequest request =
                UnityWebRequest.Get($"{BaseUrl}/{currentRoomCode}"))
            {
                request.SetRequestHeader(
                    "Authorization",
                    "Bearer " + AuthSession.Token
                );

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    statusText.text = "Connection lost. Retrying...";
                    continue;
                }

                RoomInfo room = JsonUtility.FromJson<RoomInfo>(
                    request.downloadHandler.text
                );

                if (room != null)
                {
                    UpdateRoomStatus(room);

                    if (room.isReady)
                    {
                        EnterGame();
                        yield break;
                    }
                }
            }
        }

    }

    private void EnterGame()
    {
        RankedMatchSession.SetMatch(
            currentRoomCode,
            playerNumber
        );

        SceneManager.LoadScene("Game");
    }

    private void UpdateRoomStatus(RoomInfo room)
    {
        if (room.isReady)
        {
            statusText.text =
                $"Both players connected! You are Player {playerNumber}.";
        }
        else
        {
            statusText.text =
                $"Waiting for Player 2... You are Player {playerNumber}.";
        }
    }

    public void Back()
    {
        if (pollingCoroutine != null)
            StopCoroutine(pollingCoroutine);

        SceneManager.LoadScene("MainMenu");
    }
}