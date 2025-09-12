// CardSO.cs
using UnityEngine;


public enum AttackType
{
    Wizard_MagicBall,
    Wizard_WizStone,
    Wizard_Dagger,
    Wizard_WindPush,
    Inventor_FireBomb,
    Inventor_Piercing_Cogs,
    Inventor_Dagger,
    Inventor_Drone,
    Samurai_Hammer,
    Samurai_Blades,
    Samurai_ArrowRain,
    Samurai_Shiruken_Spinning
}

[CreateAssetMenu(fileName = "NewCard", menuName = "ScriptableObjects/CardSO", order = 3)]
public class CardSO : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Display name of this card")]
    public string cardName;
    public int baseDmg;

    [Header("Visual")]
    [Tooltip("Image/icon representing the card")]
    public Sprite artwork;

    [Header("Fired Object Prefab")]
    [Tooltip("Prefab of the object this card fires")]
    public GameObject fireObject;
    public AttackType attackType;

    // --- RUNTIME ONLY ---
    public int sessionLevel = 1;

    // store originals so we can restore at end of play
    public int defaultBaseDmg;

    void OnEnable()
    {
        ResetRuntime();
    }

    /// <summary>
    /// Resets this card’s runtime‐only values back to defaults.
    /// </summary>
    public void ResetRuntime()
    {
        baseDmg       = defaultBaseDmg;
        sessionLevel  = 1;
    }

    /// <summary>
    /// Called whenever we apply an upgrade in‐session.
    /// </summary>
    public void IncrementSessionLevel()
    {
        sessionLevel++;
    }
}
