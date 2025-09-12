using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public Vector2Int gridPosition; // auto assigned by InventoryManager
    public GridInventory inventory;

    [SerializeField] private Image highlightImage; // optional, for icon or highlight
    // On drop we either place, merge, or reset dragged back to its original slot
    public void OnDrop(PointerEventData eventData)
{
    var dragged = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<DraggableWeapon>() : null;
    //Debug.Log("OnDrop event received. +"+ eventData.pointerDrag.name);
        if (dragged == null || dragged.weaponData == null || inventory == null) return;
        //Debug.Log($"OnDrop called on slot {gridPosition} with weapon {dragged.weaponData.name}");
        WeaponSO weapon = dragged.placedWeapon.weaponData;
    Vector2Int pos = gridPosition;

        // 1) Merge
        if ( inventory.CanMergeAt(dragged.placedWeapon, pos, out List<PlacedWeapon> placedList))
        {
            int level=0;
            //Debug.Log("Merging weapons into next level: " );
            foreach (var p in placedList)
            {
                p.Unplace();
                InventoryManager.instance.RemoveDraggable(p.GetComponent<DraggableWeapon>());
                level = p.WeaponLevel;
                Destroy(p.gameObject);
            }
            dragged.placedWeapon.Merge(level);
            //Destroy(dragged.gameObject);
            dragged.placedWeapon.Place(this);
            dragged.parentSlot = this;
            dragged.originalParent = this.transform;

            //var mergedPlaced = inventory.CreatePlacedWeaponFromPrefab(inventory.placedPrefab, weapon.nextLevelWeapon, pos, this);
            //if (highlightImage != null) highlightImage.sprite = mergedPlaced.weaponData.icon;
            //return;
        }

        // 2) Place
        if (inventory.CanPlace(weapon, pos))
        {
            //Debug.Log("CanPlace weapon at " + pos);
            var placedComp = dragged.GetComponent<PlacedWeapon>();
            if (placedComp != null)
            {
                dragged.transform.SetParent(transform, false);
                dragged.originalParent = transform;
                dragged.transform.localPosition = Vector3.zero;
                //Debug.Log("_CanPlace existing dragged object.");
                placedComp.weaponData = weapon;
                placedComp.Place(this);
                dragged.parentSlot = this;
                dragged.transform.SetParent(inventory.placedWeaponsContainer);

            }
            else
            {
                //Debug.Log("_Placing new object from prefab.");
                Destroy(dragged.gameObject);
                var newPlaced = inventory.CreatePlacedWeaponFromPrefab(inventory.placedPrefab, weapon, pos, this);
                if (highlightImage != null) highlightImage.sprite = newPlaced.weaponData.icon;
            }
            return;
        }
        else
        {

        }

        // 3) Reset
        if (dragged.parentSlot != null)
        {
            //Debug.Log("Resetting dragged object to original slot.");
            dragged.transform.SetParent(dragged.parentSlot.transform, false);
            dragged.transform.localPosition = Vector3.zero;
            //var placedComp = dragged.GetComponent<PlacedWeapon>();
            //placedComp.Place(inventory, pos, this);

            //dragged.transform.SetParent(dragged.originalParent, false);

        }
        else
        {
            Destroy(dragged.gameObject);
        }
    }


    // Optional helper for showing icon on the slot background
    public void SetSlotIcon(Sprite icon)
    {
        if (highlightImage == null) return;
        highlightImage.sprite = icon;
        highlightImage.enabled = icon != null;
    }
}
