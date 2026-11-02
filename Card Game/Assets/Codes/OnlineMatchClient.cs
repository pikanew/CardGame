using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class OnlineCardInfo
{
    public int instanceId;
    public string cardId;
}

[Serializable]
public class OnlineMatchAction
{
    public int sequence;
    public string type;
    public int playerNumber;

    public int instanceId;
    public string cardId;

    public int row;
    public int column;
}

[Serializable]
public class RankedResultRequest
{
    public int player1Score;
    public int player2Score;
}

[Serializable]
public class RankedResultResponse
{
    public string status;

    public int player1RatingChange;
    public int player2RatingChange;

    public int player1Rating;
    public int player2Rating;
}

[Serializable]
public class OnlineMatchView
{
    public string roomCode;
    public bool isRanked;

    public int playerNumber;
    public int currentPlayer;

    public string phase;

    public bool mulliganCompleted;
    public bool opponentMulliganCompleted;

    public OnlineMatchAction[] actions;
    public OnlineCardInfo[] hand;

    public int deckCount;
    public int opponentHandCount;
    public int opponentDeckCount;

    public int turnSecondsRemaining;
    public int mulliganSecondsRemaining;
}

public class OnlineMatchClient : MonoBehaviour
{
    private const string BaseUrl =
        "https://localhost:7091/api/matches";

    [Serializable]
    private class MulliganRequest
    {
        public int[] instanceIds;
    }

    [Serializable]
    private class PlayCardRequest
    {
        public int instanceId;
        public int row;
        public int column;
    }



    public IEnumerator GetMatch(
        Action<OnlineMatchView> onSuccess)
    {
        string url =
            $"{BaseUrl}/{RankedMatchSession.RoomCode}";

        yield return SendRequest(
            url,
            "GET",
            null,
            onSuccess
        );
    }

    public IEnumerator ConfirmMulligan(
        int[] instanceIds,
        Action<OnlineMatchView> onSuccess)
    {
        string url =
            $"{BaseUrl}/{RankedMatchSession.RoomCode}/mulligan";

        MulliganRequest data = new MulliganRequest
        {
            instanceIds = instanceIds
        };

        string json = JsonUtility.ToJson(data);

        yield return SendRequest(
            url,
            "POST",
            json,
            onSuccess
        );
    }

    private IEnumerator SendRequest(
        string url,
        string method,
        string json,
        Action<OnlineMatchView> onSuccess)
    {
        using (UnityWebRequest request =
            new UnityWebRequest(url, method))
        {
            request.downloadHandler =
                new DownloadHandlerBuffer();

            if (json != null)
            {
                request.uploadHandler =
                    new UploadHandlerRaw(
                        Encoding.UTF8.GetBytes(json)
                    );

                request.SetRequestHeader(
                    "Content-Type",
                    "application/json"
                );
            }

            request.SetRequestHeader(
                "Authorization",
                "Bearer " + AuthSession.Token
            );

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"Online match request failed: " +
                    $"{request.responseCode}\n" +
                    request.downloadHandler.text
                );

                yield break;
            }

            OnlineMatchView response =
                JsonUtility.FromJson<OnlineMatchView>(
                    request.downloadHandler.text
                );

            if (response == null)
            {
                Debug.LogError(
                    "Invalid online match response."
                );

                yield break;
            }

            onSuccess?.Invoke(response);
        }
    }

    public IEnumerator PlayCard(
    int instanceId,
    int row,
    int column,
    Action<OnlineMatchView> onSuccess)
    {
        string url =
            $"{BaseUrl}/{RankedMatchSession.RoomCode}/play";

        PlayCardRequest data = new PlayCardRequest
        {
            instanceId = instanceId,
            row = row,
            column = column
        };

        yield return SendRequest(
            url,
            "POST",
            JsonUtility.ToJson(data),
            onSuccess
        );
    }

    public IEnumerator PassTurn(
        Action<OnlineMatchView> onSuccess)
    {
        string url =
            $"{BaseUrl}/{RankedMatchSession.RoomCode}/pass";

        yield return SendRequest(
            url,
            "POST",
            null,
            onSuccess
        );
    }

    public IEnumerator SubmitRankedResult(
    int player1Score,
    int player2Score,
    Action<RankedResultResponse> onSuccess)
    {
        string url =
            $"{BaseUrl}/{RankedMatchSession.RoomCode}/result";

        RankedResultRequest data = new RankedResultRequest
        {
            player1Score = player1Score,
            player2Score = player2Score
        };

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request =
            new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler =
                new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(json)
                );

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            request.SetRequestHeader(
                "Authorization",
                "Bearer " + AuthSession.Token
            );

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"Failed to submit ranked result: " +
                    $"{request.responseCode}\n" +
                    request.downloadHandler.text
                );

                yield break;
            }

            RankedResultResponse response =
                JsonUtility.FromJson<RankedResultResponse>(
                    request.downloadHandler.text
                );

            onSuccess?.Invoke(response);
        }
    }
}