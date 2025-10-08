using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Button BuyButton;
    [SerializeField] private Button ShuffleButton;

    public List<WeaponSetting> weaponSettings;
    public Dictionary<int,WeaponSetting> weaponSettingsD;

    public List<WeaponSetting> weaponUnlockedSettings;
    public List<WeaponSetting> weaponLockedSettings;
    public List<StaticWeapon> spawnedWeapons = new List<StaticWeapon>();

    public static bool isTetrisScene = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        FightButton.onClick.AddListener(Fight);
        BuyButton.onClick.AddListener(Buy);
        ShuffleButton.onClick.AddListener(Shuffle);
        
        UnlockedWeapons();
    }
    private void Start()
    {
        isTetrisScene = true;

        AnimatorController.SetBool("UpTetrisBool", true);
    }
    public void StartTetrisWawe()
    {
        AnimatorController.SetBool("UpTetrisBool", true);

    }
    public void GetStaticWeapons(StaticWeapon staticWeapon)
    {
        spawnedWeapons.Add(staticWeapon);
    }
    #region Roguelike
    public void OpenNewWeapon()
    {

    }
    public void UpgradeWeapon()
    {

    }
    public void ReduceCooldown()
    {

    }
    #endregion
    public void WeaponsSettingCheck()
    {
        foreach (var w in spawnedWeapons)
        {
            for (int i = 0; i < weaponSettings.Count; i++)
            {
                if (w.WeaponType == weaponSettings[i].WeaponType)
                {
                    //Debug.Log("WeaponsSettingCheck " +i+" "+ w.WeaponType + " " + weaponSettings[i].Level+" "+ weaponSettings[i].Countdawn);
                    w.GetWeaponSetting(weaponSettings[i]);
                    continue;
                }
            }
        }
    }
    public IEnumerator TetrisScene()
    {
        TetrisCancas.sortingOrder = 4;
        inventoryManager.SpawnWeapons();
        AnimatorController.SetBool("UpTetrisBool", true);
        isTetrisScene = true;
        Time.timeScale = 0;
        yield return new WaitUntil(() => !isTetrisScene);
        Time.timeScale = 1;
        StartCoroutine(WaitForAnimation());
    }
    public void UnlockedWeapons()
    {
        weaponUnlockedSettings.Clear();
        weaponLockedSettings.Clear();
        foreach (var w in weaponSettings)
        {
            w.defaultDamage = PlayerPrefs.GetFloat(w.WeaponType.ToString() + "_Damage", w.defaultDamage);
            if (w.Unlocked)
            {
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

        randomWeaponSpawner.GetPossforWeapons();
    }

    public void Fight()
    {
        AnimatorController.SetBool("UpTetrisBool", false);
        TetrisCancas.sortingOrder = 2;
        WeaponsSettingCheck();
        isTetrisScene = false;
    }
    public void Buy()
    {
        randomWeaponSpawner.SpawnRandomWeapons();
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
    public float defaultDamage;
    public List<float> Damage;
    public List<float> Countdawn;
    public Sprite icon;
}
