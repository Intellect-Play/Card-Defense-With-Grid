using System.Collections.Generic;
using UnityEngine;

public class RandomWeaponSpawner : MonoBehaviour
{
    public static RandomWeaponSpawner instance;
    public List<SlotWeaponsSO> possibleWeapons;    // hansı silahlar çıxa bilər
    public int spawnCount = 3;                // neçə dənə spawn edilsin
    public GameObject weaponPrefab;           // sadə prefab (DraggableWeapon DEYİL!)

    public List<StaticWeapon> spawnedWeapons = new List<StaticWeapon>();
    public InventoryManager inventoryManager;
    public Transform Conteiner;
    PlacedWeapon[,] placedWeapons;
    public Camera mainCamera;
    TetrisWeaponManager tetrisWeaponManager;
    public InventorySlot emptySlots;
    public int CellSize;
    private void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    private void Start()
    {
        CellSize = GameManager.Instance.gridLevelDatas.CellSize;
        tetrisWeaponManager = TetrisWeaponManager.instance;
        inventoryManager = InventoryManager.instance;
        SpawnRandomWeapons();
        spawnCount = GameManager.Instance.gridLevelDatas.LockCellCount;
        SpawnLockSlots();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            SpawnRandomWeapons();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            SpawnLockSlots();
            //Shuffle();
        }
    }
    public void ChangePosWeapons()
    {
        foreach (var w in spawnedWeapons)
        {
            w.ChangePosWeapon();
        }
    }
    public void SpawnLockSlots()
    {
        for (int i = 0; i < spawnCount; i++)
        {
             emptySlots = inventoryManager.GetRandomEmptyCell();
            if (emptySlots == null) break;
            emptySlots.LockSlot();
        }
    }
    public bool SpawnRandomWeapons()
    {

        for (int i = 0; i < spawnCount; i++)
        {
            InventorySlot emptySlots = inventoryManager.GetRandomEmptyCell();
            if (emptySlots == null) return false;

            SlotWeaponsSO randomWeapon = tetrisWeaponManager.GetSlotWeapon();
            GameObject go = Instantiate(weaponPrefab, emptySlots.transform);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(CellSize,CellSize);
            go.transform.localPosition = Vector3.zero;
            //go.transform.SetParent(Conteiner);

            StaticWeapon staticW = go.GetComponent<StaticWeapon>();
            staticW.Init(randomWeapon,1, emptySlots.gridPosition, mainCamera);

            spawnedWeapons.Add(staticW);
            tetrisWeaponManager.GetStaticWeapons(staticW);
        }
        ActivatesWeapons();
        tetrisWeaponManager.WeaponsSettingCheck();
        return true;
    }
  
    public void GetPossforWeapons()
    {
        foreach (var w in spawnedWeapons)
        {
            w.ChangePosWeapon();
        }
    }
    public void Shuffle()
    {
        if(spawnedWeapons.Count == 0) return;
        inventoryManager.ResetgridSlotForWeapons();
        foreach (var item in spawnedWeapons)
        {
            InventorySlot emptySlots = inventoryManager.GetRandomEmptyCell();
            item.transform.SetParent(emptySlots.transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.SetParent(Conteiner);
            item.Shuffle(emptySlots.gridPosition);
        }
        ActivatesWeapons();

    }
    public void ActivatesWeapons()
    {
        placedWeapons = inventoryManager.gridInventory.grid;
        foreach (var w in spawnedWeapons)
        {
            w.Activate(placedWeapons);
        }
    }
}
