using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TileView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Sprite[] P1Pawns = new Sprite[3];
    [SerializeField] private Sprite[] P2Pawns = new Sprite[3];

    [SerializeField] private Image glow;
    [SerializeField] private Image Sprite;
    private GameManager gameManager;
    [SerializeField] private Tile tile;


    [SerializeField] private Color playableColor = Color.yellow;
    [SerializeField] private Color enhancedColor = Color.green;
    [SerializeField] private Color enfeebledColor = Color.red;

    public enum GlowType
    {
        None,
        Playable,
        Enhanced,
        Enfeebled
    }

    public void Initialize(Tile tile, GameManager gameManager)
    {
        this.tile = tile;
        this.gameManager = gameManager;

        Sprite = transform.GetChild(1).GetComponent<Image>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);

        glow.gameObject.SetActive(false);
    }

    public void SetGlow(GlowType type)
    {
        if (type == GlowType.None)
        {
            glow.gameObject.SetActive(false);
            return;
        }

        glow.gameObject.SetActive(true);

        switch (type)
        {
            case GlowType.Playable:
                glow.color = playableColor;
                break;

            case GlowType.Enhanced:
                glow.color = enhancedColor;
                break;

            case GlowType.Enfeebled:
                glow.color = enfeebledColor;
                break;
        }
    }

    public void PreviewPawns(int pawns, Player owner)
    {
        if (owner.Num == 1)
            Sprite.sprite = P1Pawns[pawns - 1];
        else
            Sprite.sprite = P2Pawns[pawns - 1];
    }

    public void ClearPreview()
    {
        if (tile.Owner != null && tile.Pawns > 0)
            FixSprite();
        else
            Sprite.sprite = null;
    }

    public void ShowEmptyTile()
    {
        Sprite.enabled = true;
        ClearPreview();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        gameManager.PreviewTile(tile);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        gameManager.ClearPreview();
    }

    private void OnClicked()
    {
        gameManager.PlaySelectedCard(tile);
    }

    public void SetGlow(bool active)
    {
        glow.gameObject.SetActive(active);
    }

    public void FixSprite()
    {
        if (tile.Owner.Num == 1)
        {
            Sprite.sprite = P1Pawns[tile.Pawns - 1];
        }
        else
        {
            Sprite.sprite = P2Pawns[tile.Pawns - 1];
        }
    }

    public void ShowCard()
    {
        Sprite.enabled = false;
    }
}