using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TetrisWeaponManager : MonoBehaviour
{
    public static TetrisWeaponManager instance;
    [SerializeField] private Canvas TetrisCancas;
    [SerializeField] private RectTransform TetrisWeaponPanel;
    [SerializeField] private Animator AnimatorController;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private RandomWeaponSpawner randomWeaponSpawner;

    [SerializeField] private Button FightButton;
    [SerializeField] private Button RerollButton;
    [SerializeField] private Button BuyButton;
    [SerializeField] private Button ShuffleButton;
    [SerializeField] private TextMeshProUGUI RerollButtonPrice;
    [SerializeField] private TextMeshProUGUI BuyButtonPrice;
    public static bool IsUnlockedT(SlotWeaponType k) => PlayerPrefs.GetInt(UnlockKeyT(k), SlotWeaponType.Arrow == k ? 1 : SlotWeaponType.Bomb == k ? 1 : 0) == 1;
    public static string UnlockKeyT(SlotWeaponType k) => k.ToString();

    public List<WeaponSetting> weaponSettings;
    public Dictionary<int,WeaponSetting> weaponSettingsD;

    public List<WeaponSetting> weaponUnlockedSettings;
    public List<WeaponSetting> weaponLockedSettings;
    public List<StaticWeapon> spawnedWeapons = new List<StaticWeapon>();

    public static bool isTetrisScene = false;
    private int unlockWeaponR = 0;
    private int upgradeWeaponR = 0;
    private int upgradeCooldownWeaponR = 0;
    public float Power;
    public TextMeshProUGUI PowerText;

    private void Awake()
    {
        //PlayerPrefs.SetInt("gold", 10000);
        if (instance == null) instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        FightButton.onClick.AddListener(Fight);
        RerollButton.onClick.AddListener(Reroll);
        BuyButton.onClick.AddListener(Buy);
        ShuffleButton.onClick.AddListener(Shuffle);
        
        UnlockedWeapons();
    }
    public void PowerFunc(float power, bool restore = false)
    {
        if (restore) Power = 0;
        Power += power;
        PowerText.text = ((int)Power).ToString();
    }
    private void Start()
    {
        isTetrisScene = true;
        UIRefleshGame();

        StartTetrisWawe();
    }
    public void StartTetrisWawe()
    {
        isTetrisScene = true;
        TetrisCancas.sortingOrder = 10;
        AnimatorController.SetBool("UpTetrisBool", true);
        UIRefleshGame();
        inventoryManager.SpawnWeapons();
        inventoryManager.SameSelectedAll();
        inventoryManager.FillAllDrags();

    }
    public void GetStaticWeapons(StaticWeapon staticWeapon)
    {
        spawnedWeapons.Add(staticWeapon);
    }
    #region Roguelike
    public WeaponSetting SelectWeaponforUnlock()
    {
        unlockWeaponR = UnityEngine.Random.Range(0, weaponLockedSettings.Count);
        //weaponLockedSettings[num].Unlocked = true;
        return weaponLockedSettings[unlockWeaponR];
    }
    public WeaponSetting SelectWeaponforUpgrade()
    {
        upgradeWeaponR = UnityEngine.Random.Range(0, weaponUnlockedSettings.Count);
        //weaponUnlockedSettings[num].Level++;
        return weaponUnlockedSettings[upgradeWeaponR];
    }
    public WeaponSetting SelectWeaponforReduceCooldown()
    {
        upgradeCooldownWeaponR = UnityEngine.Random.Range(0, weaponUnlockedSettings.Count);
        //weaponUnlockedSettings[num].LevelCountdawn++;
        return weaponUnlockedSettings[upgradeCooldownWeaponR];
    }
    public void OpenNewWeapon()
    {
        weaponLockedSettings[unlockWeaponR].Unlocked = true;
        UnlockedWeapons();
        WeaponsSettingCheck();
    }
    public void UpgradeWeapon()
    {
        weaponUnlockedSettings[upgradeWeaponR].Level++;
        WeaponsSettingCheck();
    }
    public void ReduceCooldown()
    {
        weaponUnlockedSettings[upgradeCooldownWeaponR].LevelCountdawn++;
        WeaponsSettingCheck();
    }
    #endregion
    public void WeaponsSettingCheck()
    {
        PowerFunc(0, true);
        randomWeaponSpawner.ActivatesWeapons();
        foreach (var w in spawnedWeapons)
        {
            for (int i = 0; i < weaponSettings.Count; i++)
            {
                if (w.WeaponType == weaponSettings[i].WeaponType)
                {
                    //Debug.Log("WeaponsSettingCheck " +i+" "+ w.WeaponType + " " + weaponSettings[i].Level+" "+ weaponSettings[i].Countdawn);
                    PowerFunc(w.GetWeaponSetting(weaponSettings[i]));
                    
                    continue;
                }
            }
        }
    }
    //public IEnumerator TetrisScene()
    //{
    //    TetrisCancas.sortingOrder = 4;
    //    //inventoryManager.SpawnWeapons();
    //    AnimatorController.SetBool("UpTetrisBool", true);
        
    //    //Time.timeScale = 0;
    //    yield return new WaitUntil(() => !isTetrisScene);
        
    //    StartCoroutine(WaitForAnimation());
    //}
    public void UnlockedWeapons()
    {
        weaponUnlockedSettings.Clear();
        weaponLockedSettings.Clear();
        foreach (var w in weaponSettings)
        {
            w.defaultDamage = PlayerPrefs.GetFloat(w.WeaponType.ToString() + "_Damage", w.defaultDamage);
            if (IsUnlockedT(w.WeaponType))
            {
                w.Unlocked = true;
                weaponUnlockedSettings.Add(w);
            }
            else
            {
                weaponLockedSettings.Add(w);
            }
        }
    }

    public SlotWeaponsSO GetSlotWeapon()
    {
        int num = UnityEngine.Random.Range(0, weaponUnlockedSettings.Count);
        return weaponUnlockedSettings[num].slotWeaponsSO;
    }
    IEnumerator WaitForAnimation()
    {
        AnimatorStateInfo stateInfo = AnimatorController.GetCurrentAnimatorStateInfo(0);
        //Debug.Log("WaitForAnimation 1 "+stateInfo);
        while (!stateInfo.IsName("TetrisDown"))
        {
            yield return null;
            stateInfo = AnimatorController.GetCurrentAnimatorStateInfo(0);
        }
        //Debug.Log("WaitForAnimation 2 " + stateInfo);

        while (stateInfo.normalizedTime < 1f)
        {
            yield return null;
            stateInfo = AnimatorController.GetCurrentAnimatorStateInfo(0);
        }
        //Debug.Log("WaitForAnimation 3 " + stateInfo);
        isTetrisScene = true;
        Time.timeScale = 1;
        randomWeaponSpawner.GetPossforWeapons();
        //GameManager.Instance.ResumeGameAfterRoguelike();
    }

    public void Reroll()
    {
        if(PriceCheck.instance.priceSO.RerollPrice > PlayerPrefs.GetInt("gold", 0)) return;
        GameManager.Instance.InCreaseGold(PriceCheck.instance.priceSO.RerollPrice);

        inventoryManager.SpawnWeapons();
        UIRefleshGame();
        inventoryManager.SameSelectedAll();

    }
    public void Fight()
    {
        AnimatorController.SetBool("UpTetrisBool", false);
        TetrisCancas.sortingOrder = 2;
        WeaponsSettingCheck();
        isTetrisScene = false;
        inventoryManager.ClearSameSelected();

    }
    public void Buy()
    {
        if (PriceCheck.instance.priceSO.BuyPrice > PlayerPrefs.GetInt("gold", 0)) return;
        if (!randomWeaponSpawner.SpawnRandomWeapons()) return;
       GameManager.Instance.InCreaseGold(PriceCheck.instance.priceSO.BuyPrice);

        UIRefleshGame();
    }
    public void UIRefleshGame()
    {
        int g = PlayerPrefs.GetInt("gold", 0);
        GameManager.Instance.uIManager.SetCoins(g);
        BuyButton.interactable = PriceCheck.instance.priceSO.BuyPrice <= g;
        RerollButton.interactable = PriceCheck.instance.priceSO.RerollPrice <= g;
     

        RerollButtonPrice.text = PriceCheck.instance.priceSO.RerollPrice.ToString();
        BuyButtonPrice.text = PriceCheck.instance.priceSO.BuyPrice.ToString();
    }
    public void Shuffle() {
        randomWeaponSpawner.Shuffle();
    }
}
[Serializable]
public class WeaponSetting
{
    public SlotWeaponType WeaponType;
    public SlotWeaponsSO slotWeaponsSO;
    public bool Unlocked=false;
    public int Level=1;
    public int LevelCountdawn = 1;
    public float defaultDamage;


}
