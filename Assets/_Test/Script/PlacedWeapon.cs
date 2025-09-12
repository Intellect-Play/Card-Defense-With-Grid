using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DraggableWeapon))]
public class PlacedWeapon : MonoBehaviour
{
    public WeaponSO weaponData;
    public DraggableWeapon draggableWeapon;
    [HideInInspector] public Vector2Int origin = new Vector2Int(-1, -1);

    public GridInventory inventory;
    public InventorySlot originSlot;

    public bool IsPlaced => origin.x >= 0 && inventory != null;
    public int WeaponLevel;
    public bool firstPlaced = false;


    public List<DragProxy> ChildDrags = new List<DragProxy>();
    // Call when prefab is used as a spawned (not yet placed) object
    private void Awake()
    {
        draggableWeapon = GetComponent<DraggableWeapon>();
    }
    public void InitAsSpawn(WeaponSO weapon)
    {
        weaponData = weapon;
        origin = new Vector2Int(-1, -1);
        inventory = null;
        originSlot = null;
        WeaponLevel = weapon.levelWeapon;
    }
    public void Merge(int level)
    {
        WeaponLevel+=level;
        foreach (var drag in ChildDrags)
        {
            drag.LevelUpgrade(WeaponLevel);
        }
        //Debug.Log("Merged to level " + weaponData.levelWeapon);
    }
    // Register into grid at pos and remember origin UI slot
    public void Place(InventorySlot slot)
    {
        firstPlaced = true;
        //Unplace();
        //Debug.Log("Placing weapon " + weaponData.name + " at " + origin);
        inventory = slot.inventory;
        origin = slot.gridPosition;
        originSlot = slot;
        inventory.PlacePlacedWeapon(this, origin);
    }

    // Unregister from grid but do not destroy GameObject (used when player picks up)
    public void Unplace()
    {
        //Debug.Log("Unplacing weapon " + weaponData.name + " from " + origin);
        if (inventory != null)
        {
            inventory.RemovePlacedWeapon(this);
            inventory = null;
        }
        origin = new Vector2Int(-1, -1);
        originSlot = null;
    }
}
