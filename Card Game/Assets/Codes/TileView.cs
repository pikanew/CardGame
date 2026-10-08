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

    public void Initialize(Tile tile, GameManager gameManager)
    {
        this.tile = tile;
        this.gameManager = gameManager;

        Sprite = transform.GetChild(1).GetComponent<Image>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);

        glow.gameObject.SetActive(false);
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
}