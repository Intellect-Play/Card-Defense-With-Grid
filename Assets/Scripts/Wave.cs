using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyCount
{
    [Tooltip("Reference to the Enemy ScriptableObject")]
    public EnemySO enemy;
    [Tooltip("Number of enemies to spawn")]
    public int count;
}

public enum OptionType
{
    OpenNewWeapon,
    UpgradeCard,
    ReduceCooldown,
    HealthUp,
    
}

[Serializable]
public class RoguelikeOption
{
    public string     deckName;
    public OptionType type;

    // for UpgradeCard & ReduceCooldown
    [HideInInspector] public CardSO targetCard;
    [HideInInspector] public string targetDeckName;

    // for NewHero
    [HideInInspector] public HeroSO targetHero;

    // preview levels for UpgradeCard
    public int oldLevel;
    public int newLevel;

    // NEW: for HealthUp (how much to heal the fortress)
    [Tooltip("Amount of HP to restore when picked (HealthUp).")]
    public int healthAmount = 25;

    // Optional override sprite (e.g., a heart icon)
    public Sprite overrideArtwork;

    public Sprite GetArtwork()
    {
        if (overrideArtwork != null)
            return overrideArtwork;

        return targetCard?.artwork;
    }
}


[Serializable]
public class Wave
{
    [Header("Gold Amount Multiplier")]
    public float goldAmountMultiplier = 1.0f;
    public bool RoguelikeBool = false;

    [Header("Wave Composition")]
    public List<EnemyCount> enemies = new List<EnemyCount>();

    [Header("Roguelike Options")]
    public List<RoguelikeOption> roguelikeOptions = new List<RoguelikeOption>();
}
