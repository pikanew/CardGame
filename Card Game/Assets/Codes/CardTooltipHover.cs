using UnityEngine;
using UnityEngine.EventSystems;

public class CardTooltipHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CardTooltip tooltipPrefab;
    [SerializeField] private float horizontalOffset = 10f;

    private CardData cardData;
    private CardTooltip activeTooltip;

    public void Initialize(CardData data)
    {
        cardData = data;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cardData == null || tooltipPrefab == null)
            return;

        if (activeTooltip != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;

        activeTooltip = Instantiate(
            tooltipPrefab,
            canvas.transform
        );

        activeTooltip.SetDescription(cardData.Description);

        // Prevent the tooltip from blocking mouse events.
        CanvasGroup group =
            activeTooltip.GetComponent<CanvasGroup>();

        if (group == null)
            group = activeTooltip.gameObject.AddComponent<CanvasGroup>();

        group.blocksRaycasts = false;
        group.interactable = false;

        // Get the card's right edge.
        RectTransform cardRect = GetComponent<RectTransform>();

        Vector3[] corners = new Vector3[4];
        cardRect.GetWorldCorners(corners);

        Vector3 rightCenter = (corners[2] + corners[3]) / 2f;

        // Convert to canvas coordinates.
        RectTransform canvasRect =
            canvas.GetComponent<RectTransform>();

        Camera uiCamera = canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                rightCenter
            );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            uiCamera,
            out Vector2 localPosition
        );

        // Position the tooltip.
        RectTransform tooltipRect =
            activeTooltip.GetComponent<RectTransform>();

        tooltipRect.anchorMin = canvasRect.pivot;
        tooltipRect.anchorMax = canvasRect.pivot;

        tooltipRect.pivot = new Vector2(0f, 0.5f);

        float x = localPosition.x + horizontalOffset;

        float y = Mathf.Clamp(
            localPosition.y,
            canvasRect.rect.yMin + tooltipRect.rect.height / 2f,
            canvasRect.rect.yMax - tooltipRect.rect.height / 2f
        );

        tooltipRect.anchoredPosition = new Vector2(x, y);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (activeTooltip != null)
        {
            Destroy(activeTooltip.gameObject);
            activeTooltip = null;
        }
    }
}