using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

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
    public List<RectTransform> Childs = new List<RectTransform>();

    RectTransform buttonRect;
    public List<DragProxy> ChildDrags = new List<DragProxy>();
    // Call when prefab is used as a spawned (not yet placed) object
    private void Awake()
    {
        buttonRect = GetComponent<RectTransform>();

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
        WeaponLevel+=1;
        MergeAnimation();
        foreach (var drag in ChildDrags)
        {
            drag.LevelUpgrade(WeaponLevel);
        }
        //Debug.Log("Merged to level " + weaponData.levelWeapon);
    }
    public void GetLevel(int level)
    {
        WeaponLevel += level;
        foreach (var drag in ChildDrags)
        {
            drag.LevelUpgrade(WeaponLevel);
        }
        //Debug.Log("Merged to level " + weaponData.levelWeapon);
    }
    public void MergeAnimation()
    {
        transform.DOKill();
        if (buttonRect != null)
        {
            float scaleUp = 1.2f;
            float scaleDuration = 0.2f;

            // Böyüdüb sonra kiçildək
            buttonRect.DOScale(scaleUp, scaleDuration).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() =>
            {
                buttonRect.DOScale(1f, scaleDuration).SetEase(Ease.InBack).SetUpdate(true);
            });
        }
        //Sequence seq = DOTween.Sequence();

        //// Başlanğıcda bir az böyüyür
        //seq.Append(transform.DOScale(1.12f, 0.18f)
        //    .SetEase(Ease.OutQuad)
        //    .SetUpdate(true));

        //// Kiçilib geri qayıdır
        //seq.Append(transform.DOScale(0.95f, 0.12f)
        //    .SetEase(Ease.InOutSine)
        //    .SetUpdate(true));

        //// Normal ölçüyə qayıdır
        //seq.Append(transform.DOScale(1f, 0.15f)
        //    .SetEase(Ease.OutBack)
        //    .SetUpdate(true));

        //// Paralel olaraq çox yüngül rotate titrəmə effekti
        //transform.DORotate(new Vector3(0f, 0f, Random.Range(-6f, 6f)), 0.05f)
        //    .SetEase(Ease.InOutSine)
        //    .SetLoops(6, LoopType.Yoyo)
        //    .SetUpdate(true);

        //seq.Play();
    }



    // Register into grid at pos and remember origin UI slot
    public void Place(InventorySlot slot)
    {
        firstPlaced = true;
        //Unplace();
        inventory = slot.inventory;
        origin = slot.gridPosition;
        //Debug.Log("Placing weapon " + weaponData.name + " at " + origin);
        originSlot = slot;
        inventory.PlacePlacedWeapon(this, origin);
    }
    public void PlaceSpawn(InventorySlot slot)
    {
        firstPlaced = true;
        //Unplace();
        //Debug.Log("Placing weapon " + weaponData.name + " at " + origin);
        inventory = slot.inventory;
        origin = slot.gridPosition;
        originSlot = slot;
        //inventory.PlacePlacedWeapon(this, origin);
    }
    // Unregister from grid but do not destroy GameObject (used when player picks up)
    public void Unplace()
    {
        if (inventory != null)
        {
        //Debug.Log("Unplacing weapon " + gameObject.name + " from " + origin);
            inventory.RemovePlacedWeapon(this);
            inventory = null;
        }
        origin = new Vector2Int(-1, -1);
        originSlot = null;
    }
}
