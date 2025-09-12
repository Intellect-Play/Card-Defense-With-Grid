using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon")]
public class WeaponSO : ScriptableObject
{
    public string weaponName;
    public Sprite icon;
    public int levelWeapon = 1;
    public WeaponSO nextLevelWeapon;

    [Header("Size / Shape")]
    public Vector2Int size = Vector2Int.one;

    // Custom shape (relative cell offsets, (0,0) = origin)
    public List<Vector2Int> shapeOffsets = new List<Vector2Int>() { Vector2Int.zero };
}
