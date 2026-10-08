using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    private Card card;
    private GameManager gameManager;
    private Image image;

    public Card Card => card;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    public void Initialize(Card card, GameManager gameManager)
    {
        this.card = card;
        this.gameManager = gameManager;

        image.sprite = card.data.Artwork;
    }

    public void RemoveFromHand()
    {
        Destroy(gameObject);
    }

    public void OnClick()
    {
        gameManager.SelectCard(card);
    }
}