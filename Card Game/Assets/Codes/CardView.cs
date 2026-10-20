using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    private Card card;
    private GameManager gameManager;
    [SerializeField] private Image image;

    [SerializeField] private Image powerIndicator;
    [SerializeField] private Sprite defaultPowerSprite;
    [SerializeField] private Sprite enhancedPowerSprite;
    [SerializeField] private Sprite enfeebledPowerSprite;

    [SerializeField] private GameObject mulliganOverlay;

    public Card Card => card;

    [SerializeField] private TMPro.TextMeshProUGUI powerText;

    Player player;




    public void Initialize(Card card, GameManager gameManager, Player player)
    {
        this.card = card;
        this.gameManager = gameManager;
        this.player = player;

        image.sprite = card.data.GetArtWork(player.Num);


        powerText.text = card.Power.ToString();

        card.PowerChanged += UpdatePower;

        UpdatePower(card.Power);
    }

    public void SetMulliganSelected(bool selected)
    {
        mulliganOverlay.SetActive(selected);
    }

    private void UpdatePower(int power)
    {
        Debug.Log(
    $"Card: {card.CardName}, Current: {power}, Original: {card.data.Power}"
);

        powerText.text = power.ToString();

        if (power > card.data.Power)
        {
            powerIndicator.sprite = enhancedPowerSprite;
        }
        else if (power < card.data.Power)
        {
            powerIndicator.sprite = enfeebledPowerSprite;

            Debug.Log($"Enfeebled sprite assigned: {powerIndicator.sprite.name}");
        }
        else
        {
            powerIndicator.sprite = defaultPowerSprite;
        }
    }

    public void RemoveFromHand()
    {
        Destroy(gameObject);
    }

    public void OnClick()
    {
        gameManager.OnCardClicked(card, player);
    }

    public void MoveToTile(TileView tileView)
    {
        // Move the card from the hand to the board
        transform.SetParent(tileView.transform, false);

        RectTransform rect = GetComponent<RectTransform>();

        LayoutElement layout = GetComponent<LayoutElement>();

        if (layout != null)
            layout.enabled = false;

        // Fill the tile
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // The card should no longer behave like a selectable hand card
        Button button = GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.interactable = false;

        GetComponent<Image>().raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (card != null)
            card.PowerChanged -= UpdatePower;
    }
}