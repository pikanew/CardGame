using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;

[Serializable]
public class LeaderboardEntry
{
    public string username;
    public int score;
    public int wins;
    public int losses;
    public int draws;
}

[Serializable]
public class LeaderboardResponse
{
    public LeaderboardEntry[] entries;
}

public class LeaderboardManager : MonoBehaviour
{
    private const string LeaderboardUrl =
        "https://localhost:7091/api/Leaderboard";

    [SerializeField] private Transform rowsParent;
    [SerializeField] private LeaderboardRow rowPrefab;
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        StartCoroutine(LoadLeaderboard());
    }

    private IEnumerator LoadLeaderboard()
    {
        statusText.text = "Loading leaderboard...";

        using (UnityWebRequest request =
            UnityWebRequest.Get(LeaderboardUrl))
        {
            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                statusText.text = "Failed to load leaderboard.";

                Debug.LogError(
                    $"Leaderboard error: {request.responseCode}\n" +
                    request.downloadHandler.text
                );

                yield break;
            }

            // The backend returns a JSON array.
            // JsonUtility requires an object, so wrap the array.
            string json =
                "{\"entries\":" +
                request.downloadHandler.text +
                "}";

            LeaderboardResponse response =
                JsonUtility.FromJson<LeaderboardResponse>(json);

            if (response?.entries == null)
            {
                statusText.text = "Invalid leaderboard response.";
                yield break;
            }

            statusText.text = "";

            if (response.entries.Length == 0)
            {
                statusText.text = "No players found.";
                yield break;
            }

            for (int i = 0; i < response.entries.Length; i++)
            {
                LeaderboardRow row = Instantiate(
                    rowPrefab,
                    rowsParent
                );

                row.Initialize(i + 1, response.entries[i]);
            }
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}