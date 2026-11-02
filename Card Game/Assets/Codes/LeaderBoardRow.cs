using TMPro;
using UnityEngine;

public class LeaderboardRow : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text ratingText;
    [SerializeField] private TMP_Text winsText;
    [SerializeField] private TMP_Text lossesText;
    [SerializeField] private TMP_Text drawsText;

    public void Initialize(int rank, LeaderboardEntry entry)
    {
        rankText.text = rank.ToString();
        usernameText.text = entry.username;
        ratingText.text = (1000 + entry.score).ToString();

        winsText.text = entry.wins.ToString();
        lossesText.text = entry.losses.ToString();
        drawsText.text = entry.draws.ToString();
    }
}