using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // yuxarıda olmalıdır

public class StaticWeapon : MonoBehaviour
{
    public SlotWeaponType WeaponType;
    public RandomWeaponSpawner randomWeaponSpawner;
    public SlotWeaponsSO weaponData;
    public Vector2Int gridPosition;
    public bool isActive;
    public int currentLevel=1;
    public Transform BulletPos;
    [SerializeField] private Image icon;
    [SerializeField] private Image icon2;
    [SerializeField] private GameObject WeaponSpawned;

    [SerializeField] private PlacedWeapon? placedWeapon;
    public Camera mainCamera;
    public CardDeckAnimator cardDeckAnimator;

    private void Awake()
    {
        //placedWeapon = randomWeaponSpawner.inventoryManager.gridInventory.grid[gridPosition.x, gridPosition.y];
        //icon.GetComponent<Image>();

    }

    public void Init(SlotWeaponsSO _weaponData, int Level, Vector2Int pos, Camera camera)
    {
        Init(_weaponData, Level, pos, camera, WeaponSpawned);
    }
    public void GetWeaponSetting(WeaponSetting weaponSetting)
    {
        if(placedWeapon == null) return;
        int level = weaponSetting.Level;
        LevelUp(level);
        //Debug.Log(weaponSetting.Countdawn[0]+" Time");
        //Debug.Log("GetWeaponSetting " + placedWeapon.WeaponLevel + " " + weaponSetting.Damage[level] + " " + weaponSetting.Countdawn[level]);
        cardDeckAnimator.GetNewSetting(
            placedWeapon.WeaponLevel, 
            weaponSetting.slotWeaponsSO.Damage[0], 
            weaponSetting.slotWeaponsSO.Countdawn[0]);
    }
    public void GetFireTime(float scaleTime)
    {
        icon.fillAmount = scaleTime;
    }

public void FireChosen()
{

    // 🔹 DOTween ilə kiçik "pop" effekti (şəkil atır kimi)
    // Əvvəlcə obyektin ölçüsünü bir az böyüdürük və geri qaytarırıq
    transform.DOKill(); // əvvəlki animasiyalar təmizlə
    transform
        .DOScale(1.15f, 0.1f)  // 0.1 saniyəyə 15% böyüt
        .SetEase(Ease.OutBack)
        .OnComplete(() =>
        {
            transform.DOScale(1f, 0.15f).SetEase(Ease.InOutSine); // yenidən ölçüyə qaytar
        });

    // 🔹 Material revert effekti (əgər aktivdirsə)
   

 
}

public void Init(SlotWeaponsSO _weaponData, int Level, Vector2Int pos, Camera camera, GameObject weaponSpawned)
    {
        WeaponType = _weaponData.weaponName;
        gameObject.name = _weaponData.weaponName.ToString();
        weaponData = _weaponData;
        gridPosition = pos;
        isActive = false;
        WeaponSpawned = Instantiate(weaponData.weaponType,Vector3.zero,Quaternion.identity,transform);

        cardDeckAnimator = WeaponSpawned.GetComponentInChildren<CardDeckAnimator>();
        cardDeckAnimator.staticWeapon = this;
        Debug.Log(weaponData.attackType);
        cardDeckAnimator.cards[0].attackType = weaponData.attackType;
        WeaponSpawned.transform.localScale = new Vector3(.5f,.5f,.5f);
        SetIcon();
        WeaponSpawned.transform.SetParent(null); // detach UI parent

        ChangePosWeapon();

    }
    public void ChangePosWeapon()
    {
        WeaponSpawned.transform.position = new Vector3(
           BulletPos.position.x,
           BulletPos.position.y,
           0
       );
    }
    public void Shuffle(Vector2Int pos)
    {       
        gridPosition = pos;
    }
    public void Activate(PlacedWeapon[,] placedWeapons)
    {
        isActive = placedWeapons[gridPosition.x, gridPosition.y] != null;
        if(isActive)
        {
            placedWeapon = placedWeapons[gridPosition.x, gridPosition.y];
        }
        SetIcon();
    }
    public void LevelUp(int SpotWeaponLevel)
    {
        currentLevel = SpotWeaponLevel;
        SetIcon();


    }
    void SetIcon()
    {
        int iconNum = weaponData.levelToIconIndex[currentLevel - 1];
        icon2.sprite = weaponData.FadeOutIcons[iconNum];
        if (isActive)
        {
            icon.gameObject.SetActive(true);
            icon.sprite = weaponData.icons[iconNum];
        }
        else
        {
            icon.gameObject.SetActive(false);
        }
        //icon.sprite = isActive ? weaponData.icons[iconNum];

    }
}
