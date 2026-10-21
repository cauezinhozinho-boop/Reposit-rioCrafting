using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Click and drag/drop behavior for one generated inventory slot.</summary>
public sealed class InventorySlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Elementos visuais configuráveis no prefab")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI itemLabel;
    [SerializeField] private TextMeshProUGUI quantityLabel;
    [SerializeField] private TextMeshProUGUI numberLabel;
    [SerializeField] private Color selectedColor = new Color(0.48f, 0.32f, 0.17f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.09f, 0.11f, 0.13f, 0.74f);
    private InventoryUI owner;
    private Color normalColor;
    private ItemType? item;
    private int index;
    private bool isEquipmentSlot;
    private Color colorBeforeDrag;
    private GameObject dragVisual;
    private RectTransform dragVisualRect;
    private Canvas rootCanvas;
    private CanvasGroup slotCanvasGroup;

    internal bool IsEquipmentSlot => isEquipmentSlot;
    internal bool HasItem => item.HasValue;
    internal int SlotIndex => index;
    internal ItemType Item => item.GetValueOrDefault();

    public void Configure(InventoryUI ui, Image image, bool equipmentSlot = false, int equipmentNumber = 0)
    {
        owner = ui;
        if (background == null) background = image;
        if (background != null) normalColor = background.color;
        isEquipmentSlot = equipmentSlot;
        slotCanvasGroup = GetComponent<CanvasGroup>();
        if (slotCanvasGroup == null) slotCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        slotCanvasGroup.blocksRaycasts = true;
        slotCanvasGroup.interactable = true;
        if (background != null) background.raycastTarget = true;
        if (itemLabel != null) itemLabel.raycastTarget = false;
        if (quantityLabel != null) quantityLabel.raycastTarget = false;
        if (numberLabel != null) numberLabel.raycastTarget = false;
        if (background == null || itemLabel == null || quantityLabel == null)
            Debug.LogError("O prefab do slot precisa referenciar Background, ItemLabel e QuantityLabel.", this);
        if (numberLabel != null) numberLabel.text = equipmentNumber.ToString();
    }

    public void SetItem(ItemType type, int count, string displayName, int slotIndex, bool selected)
    {
        item = type; index = slotIndex; itemLabel.text = displayName; quantityLabel.text = "× " + count;
        background.color = selected ? selectedColor : normalColor;
    }

    public void SetEmpty(int slotIndex, string emptyLabel = "")
    {
        item = null; index = slotIndex; itemLabel.text = emptyLabel; quantityLabel.text = ""; background.color = emptyColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (item.HasValue && owner != null) owner.SelectItem(item.Value);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!item.HasValue) return;
        colorBeforeDrag = background.color;
        slotCanvasGroup.alpha = 0.25f;
        background.color = new Color(colorBeforeDrag.r, colorBeforeDrag.g, colorBeforeDrag.b, 0.45f);
        CreateDragVisual(eventData);
    }
    public void OnDrag(PointerEventData eventData) { UpdateDragVisualPosition(eventData); }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (item.HasValue) background.color = colorBeforeDrag;
        if (slotCanvasGroup != null) slotCanvasGroup.alpha = 1f;
        if (dragVisual != null) Destroy(dragVisual);
        dragVisual = null;
        dragVisualRect = null;
        if (owner != null) owner.UpdateUI();
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotView source = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<InventorySlotView>() : null;
        if (source != null && source.item.HasValue && owner != null) owner.HandleSlotDrop(source, this);
    }

    private void CreateDragVisual(PointerEventData eventData)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        rootCanvas = canvas != null ? canvas.rootCanvas : null;
        if (rootCanvas == null) return;

        dragVisual = Instantiate(gameObject, rootCanvas.transform, false);
        dragVisual.name = "DraggedItemPreview";
        InventorySlotView copiedBehavior = dragVisual.GetComponent<InventorySlotView>();
        if (copiedBehavior != null) Destroy(copiedBehavior);
        CanvasGroup canvasGroup = dragVisual.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = dragVisual.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 1f;

        Image ghostImage = dragVisual.GetComponent<Image>();
        if (ghostImage != null)
        {
            ghostImage.raycastTarget = false;
            Color ghostColor = colorBeforeDrag;
            ghostColor.a = 0.98f;
            ghostImage.color = ghostColor;
        }

        dragVisualRect = dragVisual.GetComponent<RectTransform>();
        RectTransform sourceRect = GetComponent<RectTransform>();
        dragVisualRect.anchorMin = new Vector2(0.5f, 0.5f);
        dragVisualRect.anchorMax = new Vector2(0.5f, 0.5f);
        dragVisualRect.pivot = new Vector2(0.5f, 0.5f);
        dragVisualRect.sizeDelta = sourceRect.rect.size;
        dragVisualRect.localScale = Vector3.one;
        UpdateDragVisualPosition(eventData);
    }

    private void UpdateDragVisualPosition(PointerEventData eventData)
    {
        if (dragVisualRect == null || rootCanvas == null) return;
        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
            dragVisualRect.anchoredPosition = localPoint;
    }
}
