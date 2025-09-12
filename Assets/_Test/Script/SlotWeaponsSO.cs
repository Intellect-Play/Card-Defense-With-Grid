using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/SlotWeapon")]
public class SlotWeaponsSO : ScriptableObject
{
    public SlotWeaponType weaponName;
    public List<Sprite> icons;
    public List<Sprite> FadeOutIcons;

    public int levelWeapon = 1;
    public int[] levelToIconIndex = { 0, 1, 1, 2, 2 };

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