using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableWeapon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public WeaponSO weaponData;
    public PlacedWeapon placedWeapon;
    public InventorySlot parentSlot; // current slot (spawn slot or grid's origin slot)

    [SerializeField] private RectTransform shapeContainer; // boş GameObject (RectTransform) slotları burda yaranacaq
    [SerializeField] private Image shapePrefab; // sadə Image prefab (bir hüceyrəni göstərir)

    public CanvasGroup canvasGroup;
    public Transform originalParent;
    public InventorySlot originalParentSlot;
    public Transform canvas;
    private void Awake()
    {
        placedWeapon = GetComponent<PlacedWeapon>();
        canvasGroup = GetComponent<CanvasGroup>();
        //canvas = GetComponentInParent<Canvas>();
        shapeContainer = GetComponent<RectTransform>();
    }

    // Initialize sprite and slot reference (used both for spawn and when creating placed item)
    public void Init(WeaponSO weapon, InventorySlot slot, Transform conteiner)
    {
        weaponData = weapon;
        parentSlot = slot;
        canvas = conteiner;
        // əvvəlkiləri sil
        foreach (Transform child in shapeContainer)
            Destroy(child.gameObject);

        if (weapon == null) return;

        // boşdursa ən azı (0,0)
        List<Vector2Int> offsets = weapon.shapeOffsets != null && weapon.shapeOffsets.Count > 0
            ? weapon.shapeOffsets
            : new List<Vector2Int>() { Vector2Int.zero };

        // prefab ölçüsü götür
        Vector2 cellSize = GetComponent<RectTransform>().sizeDelta;
        if (cellSize == Vector2.zero)
            cellSize = new Vector2(64, 64); // fallback

        // pivotlamaq üçün mərkəz tap → (0,0) həmişə ortada olsun
        // yəni ekran koordinatları üçün ofset = - (0,0) * cellSize
        Vector2 originOffset = new Vector2(0, 0);

        foreach (Vector2Int offset in offsets)
        {
            Image cell = Instantiate(shapePrefab, shapeContainer);
            cell.sprite = weapon.icon;
            cell.enabled = true;

            RectTransform rt = cell.GetComponent<RectTransform>();
            rt.sizeDelta = cellSize;
            rt.anchoredPosition = new Vector2(offset.x * cellSize.x, offset.y * cellSize.y) + originOffset;

            // Proxy əlavə et
            if (cell.GetComponent<DragProxy>() == null)
                cell.gameObject.AddComponent<DragProxy>();
            placedWeapon.ChildDrags.Add(cell.GetComponent<DragProxy>());
        }
        originalParent = transform.parent;
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (weaponData == null) return;

        //originalParent = transform.parent;
        originalParentSlot = parentSlot;

        var placed = GetComponent<PlacedWeapon>();
        if (placed != null && placed.IsPlaced)
        {
            placed.Unplace();
        }

        if (canvas != null)
            transform.SetParent(canvas.transform);

        canvasGroup.blocksRaycasts = false;
        InventoryManager.instance.ActiveWeaponsRay(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (weaponData == null) return;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPos
        );

        transform.localPosition = localPos;

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        InventoryManager.instance.ActiveWeaponsRay(true); 
        if (transform.parent == canvas) 
        { 
            transform.SetParent(originalParent); 
            transform.localPosition = Vector3.zero; 
            parentSlot = originalParentSlot; 
            //Debug.Log("Resetting dragged object to original slot."); 
        }
        transform.SetParent(parentSlot.inventory.placedWeaponsContainer); 
        if(placedWeapon.firstPlaced)
            placedWeapon.Place(parentSlot); 
        //Debug.Log("OnEndDrag: " + (parentSlot != null ? parentSlot.name : "no slot")); canvasGroup.blocksRaycasts = true; }
    }
}