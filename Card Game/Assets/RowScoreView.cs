using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RowScoreView : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Image background;

    public void SetScore(int score)
    {
        scoreText.text = score.ToString();
    }
}
