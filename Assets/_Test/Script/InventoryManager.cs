using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;
    [Header("Grid")]
    [SerializeField] public GridInventory gridInventory;

    [Header("Slots")]
    [Tooltip("Grid slots in row-major order (x=0..width-1, y=0..height-1)")]
    [SerializeField] private List<InventorySlot> gridSlots = new List<InventorySlot>();
    [SerializeField] private List<InventorySlot> gridSlotsForWeapons = new List<InventorySlot>();

    [SerializeField] private List<InventorySlot> spawnSlots = new List<InventorySlot>();

    [Header("Weapons")]
    [SerializeField] private List<GameObject> availableWeapons = new List<GameObject>();

    [Header("UI")]
    [SerializeField] private Button spawnButton;
    [SerializeField] private GameObject draggablePrefab; // prefab that contains Image + CanvasGroup + DraggableWeapon + PlacedWeapon

    public List<DraggableWeapon> AllWeapons = new List<DraggableWeapon>();

    public List<StaticWeapon> staticWeapons = new List<StaticWeapon>();
    public Transform placedWeaponsContainer; // bütün weapon-lar burada toplanacaq
    private void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        if (gridInventory == null)
        {
            Debug.LogError("GridInventory reference required.");
            return;
        }

        // tell gridInventory which prefab to use for placed items
        gridInventory.placedPrefab = draggablePrefab;

        // auto-assign grid positions
        for (int i = 0; i < gridSlots.Count; i++)
        {
            int x = i % gridInventory.width;
            int y = i / gridInventory.width;
            gridSlots[i].gridPosition = new Vector2Int(x, y);
            gridSlots[i].inventory = gridInventory;
            gridSlotsForWeapons.Add(gridSlots[i]);
        }

        // ensure spawn slots know inventory reference (useful for reset)
        foreach (var s in spawnSlots) s.inventory = gridInventory;
    }

    private void Start()
    {
        if (spawnButton != null) spawnButton.onClick.AddListener(SpawnWeapons);
        SpawnWeapons();
    }
    public void ActiveWeaponsRay(bool isActive)
    {
        for (int i = AllWeapons.Count - 1; i >= 0; --i)
        {
            var w = AllWeapons[i];
            if (w == null) AllWeapons.RemoveAt(i);
            else if (w.canvasGroup != null) w.canvasGroup.blocksRaycasts = isActive;
        }
    }
    public void RemoveFromSlot(DraggableWeapon drag)
    {
        //if (spawnSlots.Contains(drag))
         //   AllWeapons.Remove(drag);
    }
    public void RemoveDraggable(DraggableWeapon drag)
    {
        //Debug.Log("Removing draggable: " + drag.name);
        if (AllWeapons.Contains(drag))
            AllWeapons.Remove(drag);
    }
    public void AddDraggable(DraggableWeapon drag)
    {
        //Debug.Log("Adding draggable: " + drag.name);
        AllWeapons.Add(drag);
    }
    public void SpawnWeapons()
    {
        foreach (var slot in spawnSlots)
        {
            // Clear existing children
            foreach (Transform c in slot.transform)
            {
                //Debug.Log("Destroying existing child: " + c.name);
                RemoveDraggable(c.GetComponent<DraggableWeapon>());

                Destroy(c.gameObject);
            }

            // pick random
            GameObject randomWeapon = availableWeapons[Random.Range(0, availableWeapons.Count)];
            // instantiate draggable prefab under the spawn slot
            var go = Instantiate(randomWeapon, slot.transform);
            go.transform.localPosition = Vector3.zero;

            var drag = go.GetComponent<DraggableWeapon>();
            AddDraggable(drag);

            var placed = go.GetComponent<PlacedWeapon>();

            if (drag == null || placed == null)
            {
                //Debug.LogError("draggablePrefab must have DraggableWeapon and PlacedWeapon components.");
                Destroy(go);
                continue;
            }
            WeaponSO weaponData = drag.weaponData;
            // init both: this object is a spawn copy (not placed)
            drag.Init(go.GetComponent<DraggableWeapon>().weaponData, slot, placedWeaponsContainer);
            placed.InitAsSpawn(go.GetComponent<DraggableWeapon>().weaponData);

            // optionally set slot icon
            slot.SetSlotIcon(randomWeapon != null ? weaponData.icon : null);
        }
    }


    public void RegisterStaticWeapon(StaticWeapon sw)
    {
        if (!staticWeapons.Contains(sw))
            staticWeapons.Add(sw);
    }

    public StaticWeapon GetWeaponAt(Vector2Int pos)
    {
        foreach (var sw in staticWeapons)
        {
            if (sw.gridPosition == pos)
                return sw;
        }
        return null;
    }

    public InventorySlot GetRandomEmptyCell()
    {
        InventorySlot emptyCells;

        if(gridSlotsForWeapons.Count == 0) return null; // boş yer yoxdursa

        emptyCells = gridSlotsForWeapons[Random.Range(0, gridSlotsForWeapons.Count - 1)];
        gridSlotsForWeapons.Remove(emptyCells);
        return emptyCells;
    }
    public void ResetgridSlotForWeapons()
    {
        gridSlotsForWeapons.Clear();
        foreach (var slot in gridSlots)
            gridSlotsForWeapons.Add(slot);
    }
}
