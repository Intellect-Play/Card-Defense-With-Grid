using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GridInventory : MonoBehaviour
{
    public int width = 5;
    public int height = 5;

    // cell -> PlacedWeapon occupying it (or null)
    public PlacedWeapon[,] grid;

    // prefab used for creating placed result (set by InventoryManager on Awake)
    [HideInInspector] public GameObject placedPrefab;
    public Transform placedWeaponsContainer; // bütün weapon-lar burada toplanacaq
    string line = "";
    PlacedWeapon pw;
    private void Awake()
    {
        grid = new PlacedWeapon[width, height];
        GameObject placedGO = Instantiate(new GameObject());
        placedGO.SetActive(false);
        pw = placedGO.AddComponent<PlacedWeapon>();
        //GetComponent<GridLayoutGroup>().enabled = false;
    }
    public void GetNull(Vector2 pos)
    {
        grid[(int)pos.x,(int)pos.y] = pw;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            int z = 0;
            //Debug.Log("Grid state:");
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[x, y] != null)
                    {
                        line = z + " " + grid[x, y].name.ToString() + x + " " + y;
                        //Debug.Log(line);
                        ++z;
                    }


                }
            }
        }
    }
    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

    // check if area for weapon is fully free (no PlacedWeapon)

 
    public bool CanPlace(WeaponSO weapon, Vector2Int pos)
    {
        foreach (var offset in weapon.shapeOffsets)
        {
            int x = pos.x + offset.x;
            int y = pos.y + offset.y;
            if (!InBounds(x, y)) return false;
            //Debug.Log($"CanPlace check at {x},{y}");
            //Debug.Log($"CanPlace check at {x},{y}, found: " + (grid[x, y] != null ? grid[x, y].weaponData.name : "null"));
            if (grid[x, y] != null) return false;
            //Debug.Log($"Cell {x},{y} is free.");
        }
        return true;
    }

    // Place the PlacedWeapon reference into all cells described by its weaponData
    public void PlacePlacedWeapon(PlacedWeapon placed, Vector2Int pos)
    {
        //Debug.Log("Placing weapon: " + placed.name + " at " + pos);
        foreach (var offset in placed.weaponData.shapeOffsets)
        {
            int x = pos.x + offset.x;
            int y = pos.y + offset.y;
            grid[x, y] = placed;
        }
        RandomWeaponSpawner.instance.ActivatesWeapons();
    }

    // Remove references to this PlacedWeapon in grid cells
    public void RemovePlacedWeapon(PlacedWeapon placed)
    {
        //Debug.Log("Removing placed weapon: " + placed.name);
        for (int x = 0; x < height; x++)
            for (int y = 0; y < width; y++)
                if (grid[x, y] == placed)
                {
                    grid[x, y] = null;
                    //Debug.Log($" - cleared cell {x},{y}");
                }
        int z = 0;
        //Debug.Log("Grid state:");
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null)
                {
                    line = z + " " + grid[x, y].name.ToString() + x + " " + y;
                    //Debug.Log(line);
                    ++z;
                }


            }
        }
        RandomWeaponSpawner.instance.ActivatesWeapons();

    }

    public PlacedWeapon GetPlacedAt(int x, int y)
    {
        if (!InBounds(x, y)) return null;
        return grid[x, y];
    }

    // Returns true & list of placed objects covering the offsets if each cell contains the same weapon type
    public bool CanMergeAt(PlacedWeapon weapon, Vector2Int pos, out List<PlacedWeapon> placedOut)
    {
        placedOut = new List<PlacedWeapon>();
        //Debug.Log($"CanMergeAt check for {weapon.weaponData.name} at {pos.x},{pos.y}");
        foreach (var offset in weapon.weaponData.shapeOffsets)
        {
            int x = pos.x + offset.x;
            int y = pos.y + offset.y;
            if (!InBounds(x, y)) return false;
            var p = grid[x, y];
            //Debug.Log($" - cell {x},{y} has " + (p != null ? p.weaponData.name : "null"));
            //if (p == null || p.weaponData != weapon.weaponData || p.WeaponLevel != weapon.WeaponLevel) return false;
            if (p == null || p.weaponData != weapon.weaponData || p.WeaponLevel != weapon.WeaponLevel) return false;
            //Debug.Log($"Checking cell {x},{y} for merge, found: " + p.WeaponLevel + " " + weapon.WeaponLevel);

            placedOut.Add(p);
        }
        // deduplicate placed instances
        placedOut = placedOut.Distinct().ToList();
        return true;
    }
    //public List<PlacedWeapon> placedWeapons = new List<PlacedWeapon>();
    //public bool CanSpawnAt(PlacedWeapon weapon, Vector2Int pos, out List<PlacedWeapon> placedOut2)
    //{
    //    placedOut2 = new List<PlacedWeapon>();
    //    placedWeapons = placedOut2;
    //    //Debug.Log($"CanMergeAt check for {weapon.weaponData.name} at {pos.x},{pos.y}");
    //    foreach (var offset in weapon.weaponData.shapeOffsets)
    //    {
    //        int x = pos.x + offset.x;
    //        int y = pos.y + offset.y;
    //        if (!InBounds(x, y)) return false;
    //        var p = grid[x, y];
    //        //Debug.Log($" - cell {x},{y} has " + (p != null ? p.weaponData.name : "null"));
    //        //if (p == null || p.weaponData != weapon.weaponData || p.WeaponLevel != weapon.WeaponLevel) return false;
    //        //Debug.Log($"Checking cell {x},{y} for merge, found: " + p.WeaponLevel + " " + weapon.WeaponLevel);

    //        placedOut2.Add(p);
    //    }
    //    // deduplicate placed instances
    //    placedOut2 = placedOut2.Distinct().ToList();
    //    return true;
    //}
    // Helper to create a placed weapon (instantiates the provided prefab under slot transform).
    // Prefab must contain DraggableWeapon + PlacedWeapon
    public PlacedWeapon CreatePlacedWeaponFromPrefab(GameObject prefab, WeaponSO weapon, Vector2Int pos, InventorySlot slot)
    {
        //Debug.Log("Creating placed weapon " + weapon.name + " at " + pos);
        var go = Instantiate(prefab, slot.transform);
        go.transform.localPosition = Vector3.zero;

        var draggable = go.GetComponent<DraggableWeapon>();
        var placed = go.GetComponent<PlacedWeapon>();

        if (draggable == null || placed == null)
        {
            //Debug.LogError("Prefab must contain DraggableWeapon and PlacedWeapon components.");
            Destroy(go);
            return null;
        }

        // initialize visuals and state
        draggable.Init(weapon, slot, InventoryManager.instance.placedWeaponsContainer);
        placed.weaponData = weapon;
        placed.Place(slot);
        return placed;
    }

  

}
