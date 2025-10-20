using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public Vector2Int gridPosition; // auto assigned by InventoryManager
    public GridInventory inventory;
    public bool SlotSpawn=false;
    public Button slotUnlockButton;
    bool Unlocked = true;
    public TextMeshProUGUI lockText;
    [SerializeField] private Image highlightImage; // optional, for icon or highlight
    // On drop we either place, merge, or reset dragged back to its original slot

    public void LockSlot()
    {
        inventory.GetNull(gridPosition);

        Unlocked = false;
        slotUnlockButton.gameObject.SetActive(true);
        lockText.text = PriceCheck.instance.priceSO.UnlockSlotPrice.ToString();
        Debug.Log("LockSlot " + gridPosition);
    }
    public void UnlockButtondActive()
    {
        if (!TetrisWeaponManager.isTetrisScene) return;
        if (PriceCheck.instance.priceSO.UnlockSlotPrice > PlayerPrefs.GetInt("gold", 0)) return;
        slotUnlockButton.gameObject.SetActive(false);
        Unlocked = true;
        inventory.grid[gridPosition.x, gridPosition.y] = null;
    }
    public void OnDrop(PointerEventData eventData)
{
       // if (!TetrisWeaponManager.isTetrisScene||!Unlocked) return;
        var dragged = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<DraggableWeapon>() : null;
    //Debug.Log("OnDrop event received. +"+ eventData.pointerDrag.name);
        if (dragged == null || dragged.weaponData == null || inventory == null) return;
        //Debug.Log($"OnDrop called on slot {gridPosition} with weapon {dragged.weaponData.name}");
        WeaponSO weapon = dragged.placedWeapon.weaponData;
    Vector2Int pos = gridPosition;
        if (SlotSpawn)
        {

            Debug.Log("Placing in spawn slot. "+dragged.gameObject.name);

            Debug.Log(this.transform.childCount);
            if (this.transform.childCount != 0) return;
            dragged.placedWeapon.Unplace();
            InventoryManager.instance.RemoveDraggable(dragged);
            InventoryManager.instance.FinishSelectedSame();

            Destroy(dragged.gameObject);
            InventoryManager.instance.SpawnSelectedWeapon(this, dragged).GetLevel(dragged.placedWeapon.WeaponLevel - 1);


            return;
            dragged.transform.SetParent(transform, false);
            dragged.originalParent = transform;
            dragged.transform.localPosition = Vector3.zero;

            var placedComp = dragged.GetComponent<PlacedWeapon>();
            if (placedComp != null)
            {

                //Debug.Log("_CanPlace existing dragged object.");
                placedComp.weaponData = weapon;
                placedComp.PlaceSpawn(this);
                dragged.parentSlot = this;
                //dragged.transform.SetParent(inventory.placedWeaponsContainer);
                Debug.Log("_Placing new object from prefab2.");

            }
            return;
        }

        // 1) Merge
        if ( inventory.CanMergeAt(dragged.placedWeapon, pos, out List<PlacedWeapon> placedList))
        {
            int level=0;
            foreach (var p in placedList)
            {
                Debug.Log("Merging weapons into next level: " + dragged.gameObject.name + " " +p.gameObject.name);

                Debug.Log("Merging weapon at " + p.origin + " level " + p.WeaponLevel);
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
                Debug.Log("_Placing new object from prefab2.");

            }
            else
            {
                Debug.Log("_Placing new object from prefab.");
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
