using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;
    [Header("Grid")]
    [SerializeField] public GridInventory gridInventory;
    [SerializeField] public GameObject SlotPrefab;

    [Header("Slots")]
    [Tooltip("Grid slots in row-major order (x=0..width-1, y=0..height-1)")]
    [SerializeField] private List<InventorySlot> gridSlots = new List<InventorySlot>();
    [SerializeField] private List<InventorySlot> gridSlotsForWeapons = new List<InventorySlot>();

    [SerializeField] private List<InventorySlot> spawnSlots = new List<InventorySlot>();

    [Header("Weapons")]
    [SerializeField] private List<WeaponSO> availableweaponDatas = new List<WeaponSO>();
    [SerializeField] private GameObject availableWeapon;

    [SerializeField] private List<GameObject> availableWeapons = new List<GameObject>();
    [SerializeField] private List<GameObject> selectedWeapons = new List<GameObject>();
    [SerializeField] private List<Sprite> selectedIcons;

    [Header("UI")]
    [SerializeField] private Button spawnButton;
    [SerializeField] private GameObject draggablePrefab; 

    public List<DraggableWeapon> AllWeapons = new List<DraggableWeapon>();

    public List<StaticWeapon> staticWeapons = new List<StaticWeapon>();
    public Transform placedWeaponsContainer; // bütün weapon-lar burada toplanacaq
    int spawnName = 0;
    public int CellSize;
    public void AwakeInventoryManager()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        if (gridInventory == null)
        {
            //Debug.LogError("GridInventory reference required.");
            return;
        }
        CellSize = GameManager.Instance.gridLevelDatas.CellSize;
        gridSlots = gridInventory.AwakeGrid();
        SelectRandomWeapons();
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
       
        //SpawnWeapons();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            SpawnWeapons();

        }
    }
    public void SameSelected(PlacedWeapon weaponData)
    {
        //ClearSameSelected();
        foreach (DraggableWeapon draggableWeapon1 in AllWeapons) {
            PlacedWeapon _weaponData = draggableWeapon1.placedWeapon;
            if (weaponData != _weaponData && 
                weaponData.Name == _weaponData.Name && 
                weaponData.WeaponLevel == _weaponData.WeaponLevel&& _weaponData.inventory!=null)
            {
                draggableWeapon1.uIShake.StartBreathing();
                draggableWeapon1.ActiveChilds(true);
                //Debug.Log("Eyni növ tapıldı: " + draggableWeapon1.placedWeapon.Name + " "+ weaponData.Name);
                // Burada istədiyiniz əməliyyatları edə bilərsiniz, məsələn, onları birləşdirmək və ya silmək.
            }
        }
    }
    public void FillAllDrags()
    {
        foreach (DraggableWeapon draggableWeapon in AllWeapons)
        {
            draggableWeapon.DragChildFill();
        }
    }
    public void ClearSameSelected()
    {
        //Debug.Log("ClearSameSelected");
        foreach (DraggableWeapon draggableWeapon in AllWeapons)
        {
            //draggableWeapon.uIShake.StopBreathing();
            draggableWeapon.ActiveChildsArrow(false);
        }
    }

    public void SameSelectedAll()
    {
        ClearSameSelected();
        for (int i = 0; i < AllWeapons.Count; i++)
        {
            var draggableWeapon1 = AllWeapons[i];
            var weaponData1 = draggableWeapon1.placedWeapon;
            if (weaponData1 == null) continue;

            for (int j = 0; j < AllWeapons.Count; j++)
            {
                if (i == j) continue; // özünü yoxla

                var draggableWeapon2 = AllWeapons[j];
                var weaponData2 = draggableWeapon2.placedWeapon;
                if (weaponData2 == null) continue;

                if (weaponData1.Name == weaponData2.Name &&
                    weaponData1.WeaponLevel == weaponData2.WeaponLevel &&
                    weaponData2.inventory != null)
                {
                    // eyni növ və səviyyədə olan silah tapılıb
                    //draggableWeapon1.uIShake.StartBreathing();
                    draggableWeapon1.ActiveChildsArrow(true);

                    //draggableWeapon2.uIShake.StartBreathing();
                    draggableWeapon2.ActiveChildsArrow(true);
                }
            }
        }

    }
    public void FinishSelectedSame()
    {
        SameSelectedAll();
        foreach (DraggableWeapon draggableWeapon1 in AllWeapons)
        {
            draggableWeapon1.uIShake.StopBreathing();
            draggableWeapon1.ActiveChilds(false);

        }

    }

    private void SelectRandomWeapons()
    {
        selectedWeapons.Clear();
        foreach (WeaponSO child in availableweaponDatas)
        {
            availableWeapon.GetComponent<DraggableWeapon>().weaponData = child;
            availableWeapons.Add(Instantiate(availableWeapon));
        }
        // Əgər availableWeapons 4-dən azdırsa, hamısını götürür
        int count = Mathf.Min(4, availableWeapons.Count);

        // Siyahını qarışdırır
        List<GameObject> shuffled = new List<GameObject>(availableWeapons);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int randomIndex = Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[randomIndex]) = (shuffled[randomIndex], shuffled[i]);
            //Debug.Log($"Shuffled weapon {i}: {shuffled[i].name}");
            
        }

        // İlk 4 elementi seçilmiş siyahıya atır
        for (int i = 0; i < count; i++)
        {
            selectedWeapons.Add(shuffled[i]);
            selectedWeapons[i].GetComponent<DraggableWeapon>().SelectedSprite = selectedIcons[i];
        }
    
        //Debug.Log($"Selected {count} random weapons.");
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
    int AllWeaponMergeNum = 0;
    public GameObject GetDraggablePrefab()
    {
        if(AllWeapons.Count == 0) return null;
        foreach (GameObject child in selectedWeapons)
        {
            for(int i = 0; i < AllWeapons.Count; i++)
            {
                if (child.GetComponent<DraggableWeapon>().weaponData.name == AllWeapons[i].weaponData.name &&
                    AllWeapons[i].placedWeapon.WeaponLevel <= 3)
                {
                    AllWeaponMergeNum = i;
                    Debug.Log("Merge üçün silah tapıldı: " + child.GetComponent<DraggableWeapon>().weaponData.name + " Level: " + AllWeapons[i].placedWeapon.WeaponLevel+ " AllWeaponMergeNum "+ AllWeaponMergeNum);
                    return child;
                }
            }
         
        }
        return null;
    }
    public void SpawnWeapons()
    {
        // Əgər slot sayı silah sayından çoxdursa, siyahını qarışdır
        if (selectedWeapons.Count < spawnSlots.Count)
        {
            Debug.LogWarning("Spawn slot count is greater than available weapons! Some slots will stay empty.");
        }
        int randomMergeSpawner = Random.Range(0, spawnSlots.Count);

        // Silah siyahısını random sırala (Fisher-Yates shuffle)
        List<GameObject> shuffledWeapons = new List<GameObject>(selectedWeapons);
        for (int i = 0; i < shuffledWeapons.Count; i++)
        {
            int randomIndex = Random.Range(i, shuffledWeapons.Count);
            (shuffledWeapons[i], shuffledWeapons[randomIndex]) = (shuffledWeapons[randomIndex], shuffledWeapons[i]);
        }
        for (int i = 0; i < spawnSlots.Count; i++)
        {
            var slot = spawnSlots[i];
            spawnName++;

            // Köhnə child-ları sil
            foreach (Transform c in slot.transform)
            {
                RemoveDraggable(c.GetComponent<DraggableWeapon>());
                Destroy(c.gameObject);
            }
        }
            GameObject randomWeaponForMerge = GetDraggablePrefab();
        // Hər slot üçün unikal silah seç
        for (int i = 0; i < spawnSlots.Count; i++)
        {
            var slot = spawnSlots[i];
            spawnName++;

            // Köhnə child-ları sil
            foreach (Transform c in slot.transform)
            {
                RemoveDraggable(c.GetComponent<DraggableWeapon>());
                Destroy(c.gameObject);
            }

            // Əgər artıq silah qalmayıbsa, break
            if (i >= shuffledWeapons.Count)
                break;
            GameObject randomWeapon;
            if (i == randomMergeSpawner)
            {
                randomWeapon = randomWeaponForMerge;
                if (randomWeapon == null)
                    randomWeapon = shuffledWeapons[i];
                //Debug.Log("Random merge spawner seçildi: " + i);
            }
            else 
                randomWeapon = shuffledWeapons[i];

            // Instantiate
            var go = Instantiate(randomWeapon, slot.transform);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(CellSize, CellSize);
            go.transform.localPosition = Vector3.zero;
            go.name = go.name + spawnName;

            var drag = go.GetComponent<DraggableWeapon>();
            AddDraggable(drag);

            var placed = go.GetComponent<PlacedWeapon>();

            if (drag == null || placed == null)
            {
                Destroy(go);
                continue;
            }

            WeaponSO weaponData = drag.weaponData;

            drag.Init(weaponData, slot, placedWeaponsContainer);
            placed.InitAsSpawn(weaponData);

            slot.SetSlotIcon(randomWeapon != null ? weaponData.icon : null);
            if(i == randomMergeSpawner&& AllWeapons.Count>0&& randomWeaponForMerge !=null)
                placed.GetLevel(AllWeapons[AllWeaponMergeNum].placedWeapon.WeaponLevel-1);
        }
    }


    public PlacedWeapon SpawnSelectedWeapon(InventorySlot slot, DraggableWeapon _draggableWeapon)
    {
        ActiveWeaponsRay(true);
        // Clear existing children
        foreach (Transform c in slot.transform)
        {
            //Debug.Log("Destroying existing child: " + c.name);
            RemoveDraggable(c.GetComponent<DraggableWeapon>());

            Destroy(c.gameObject);
        }

        // pick random
        GameObject randomWeapon = selectedWeapons[Random.Range(0, selectedWeapons.Count)];

        // instantiate draggable prefab under the spawn slot
        var go = Instantiate(randomWeapon, slot.transform);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(CellSize, CellSize);

        go.transform.localPosition = Vector3.zero;
        DraggableWeapon draggableWeapon = go.GetComponent<DraggableWeapon>();
        draggableWeapon.weaponData = _draggableWeapon.weaponData;
        AddDraggable(draggableWeapon);

        var placed = go.GetComponent<PlacedWeapon>();

        if (draggableWeapon == null || placed == null)
        {
            //Debug.LogError("draggablePrefab must have DraggableWeapon and PlacedWeapon components.");
            Destroy(go);
            return null;
        }
        WeaponSO weaponData = draggableWeapon.weaponData;
        // init both: this object is a spawn copy (not placed)
        draggableWeapon.SelectedSprite = _draggableWeapon.SelectedSprite;
        draggableWeapon.Init(go.GetComponent<DraggableWeapon>().weaponData, slot, placedWeaponsContainer);
        placed.InitAsSpawn(go.GetComponent<DraggableWeapon>().weaponData);

        // optionally set slot icon
        slot.SetSlotIcon(randomWeapon != null ? weaponData.icon : null);
        return draggableWeapon.placedWeapon;
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
