using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckBuilderCardView : MonoBehaviour
{
    [SerializeField] private Image artwork;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text copyCountText;
    [SerializeField] private Button button;

    private CardData cardData;

    public void Initialize(
        CardData data,
        int playerNumber,
        int copies,
        int maxCopies,
        bool isInDeck,
        Action<CardData> onClick)
    {
        cardData = data;

        artwork.sprite = data.Artwork;
        powerText.text = data.Power.ToString();

        maxCopies = cardData.Legendary ? 1 : 2;

        copyCountText.text = isInDeck
            ? $"x{copies}"
            : $"{maxCopies}/{maxCopies}";

        GetComponent<CardTooltipHover>()?.Initialize(cardData);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(cardData));
    }

    public void UpdateCopyCount(int copies, int maxCopies, bool isInDeck)
    {
        maxCopies = cardData.Legendary ? 1 : 2;
        copyCountText.text = isInDeck
            ? $"x{copies}"
            : $"{maxCopies-copies}/{maxCopies}";
    }
}