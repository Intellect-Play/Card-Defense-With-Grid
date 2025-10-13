using System.Collections.Generic;
using DG.Tweening;
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
    public string Name;
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
        Name = weaponData.name;
        origin = new Vector2Int(-1, -1);
        inventory = null;
        originSlot = null;
        WeaponLevel = weapon.levelWeapon;
    }
    public void Merge(int level)
    {
        WeaponLevel+=level;
        MergeAnimation();
        foreach (var drag in ChildDrags)
        {
            drag.LevelUpgrade(WeaponLevel);
        }
        //Debug.Log("Merged to level " + weaponData.levelWeapon);
    }
    public void MergeAnimation()
    {
        transform.DOKill();

        Sequence seq = DOTween.Sequence();

        // Başlanğıcda bir az böyüyür
        seq.Append(transform.DOScale(1.12f, 0.18f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true));

        // Kiçilib geri qayıdır
        seq.Append(transform.DOScale(0.95f, 0.12f)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true));

        // Normal ölçüyə qayıdır
        seq.Append(transform.DOScale(1f, 0.15f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true));

        // Paralel olaraq çox yüngül rotate titrəmə effekti
        transform.DORotate(new Vector3(0f, 0f, Random.Range(-6f, 6f)), 0.05f)
            .SetEase(Ease.InOutSine)
            .SetLoops(6, LoopType.Yoyo)
            .SetUpdate(true);

        seq.Play();
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
