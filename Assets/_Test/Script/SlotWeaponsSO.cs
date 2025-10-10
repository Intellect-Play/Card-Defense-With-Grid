using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/SlotWeapon")]
public class SlotWeaponsSO : ScriptableObject
{
    public SlotWeaponType weaponName;

    [Header("Visual")]
    public List<Sprite> icons;
    public List<Sprite> FadeOutIcons;
    public Sprite icon;
    public Sprite openNewWeaponSprite;
    public Sprite upgradeWeaponSprite;
    public Sprite reduceCooldownSprite;

    [Header("Level Info")]
    public int[] levelToIconIndex = { 0, 1, 1, 2, 2 };
    public List<float> Damage;
    public List<float> Countdawn;
    public string description;

    public Sprite GetNewIcon(int level)
    {
        if (levelToIconIndex.Length > level)
        {
            return icons[levelToIconIndex[level]];
        }
        else
        {
            return icons[icons.Count - 1];
        }
    }
    public AttackType attackType;
    public GameObject weaponType;
}

public enum SlotWeaponType
{
    Arrow,
    Axe,
    Stone,
    Bomb
}