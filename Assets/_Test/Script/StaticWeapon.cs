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
    public Image dragProxy;
    BoxCollider2D boxCollider2D;
    private void Awake()
    {
        //placedWeapon = randomWeaponSpawner.inventoryManager.gridInventory.grid[gridPosition.x, gridPosition.y];
        //icon.GetComponent<Image>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        boxCollider2D.enabled = false;
    }

   
    public void Init(SlotWeaponsSO _weaponData, int Level, Vector2Int pos, Camera camera)
    {
        Init(_weaponData, Level, pos, camera, WeaponSpawned);
        MergeAnimation();
    }
    public void MergeAnimation()
    {
        RectTransform buttonRect = GetComponent<RectTransform>();
        transform.DOKill();
        if (buttonRect != null)
        {
            float scaleUp = 1.2f;
            float scaleDuration = 0.1f;

            // Böyüdüb sonra kiçildək
            buttonRect.DOScale(scaleUp, scaleDuration).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() =>
            {
                buttonRect.DOScale(.9f, scaleDuration).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
                {
                    buttonRect.DOScale(1f, scaleDuration).SetEase(Ease.InBack).SetUpdate(true);
                });
            });
        }

    }
    public float GetWeaponSetting(WeaponSetting weaponSetting)
    {
        if(placedWeapon == null) return 0;
        int level = weaponSetting.Level;
        Debug.Log("GetWeaponSetting Level " + level);
        LevelUp(level);
        //Debug.Log(weaponSetting.Countdawn[0]+" Time");
        //Debug.Log("GetWeaponSetting " + placedWeapon.WeaponLevel + " " + weaponSetting.Damage[level] + " " + weaponSetting.Countdawn[level]);
        cardDeckAnimator.GetNewSetting(
            placedWeapon.WeaponLevel, 
            weaponSetting.slotWeaponsSO.Damage[level - 1], 
            weaponSetting.slotWeaponsSO.Countdawn[level - 1]);
        return placedWeapon.WeaponLevel * weaponSetting.slotWeaponsSO.Damage[level-1];
    }
    public float GetWeaponPower(WeaponSetting weaponSetting)
    {
        if (placedWeapon == null) return 0;
        int level = weaponSetting.Level;
        LevelUp(level);
        //Debug.Log(weaponSetting.Countdawn[0]+" Time");
        //Debug.Log("GetWeaponSetting " + placedWeapon.WeaponLevel + " " + weaponSetting.Damage[level] + " " + weaponSetting.Countdawn[level]);
        cardDeckAnimator.GetNewSetting(
            placedWeapon.WeaponLevel,
            weaponSetting.slotWeaponsSO.Damage[0],
            weaponSetting.slotWeaponsSO.Countdawn[0]);
        return placedWeapon.WeaponLevel * weaponSetting.slotWeaponsSO.Damage[0];
    }
    public void GetFireTime(float scaleTime)
    {
        if(dragProxy != null)
            dragProxy.fillAmount = scaleTime;
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
        //Debug.Log(weaponData.attackType);
        cardDeckAnimator.cards[0].attackType = weaponData.attackType;
        WeaponSpawned.transform.localScale = new Vector3(.5f,.5f,.5f);
        SetIcon();
        WeaponSpawned.transform.SetParent(null); // detach UI parent

        ChangePosWeapon();

    }
    public void ChangePosWeapon()
    {
        boxCollider2D.enabled = false;

        boxCollider2D.enabled = true;

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
        else
        {
            placedWeapon = null;
        }
            SetIcon();
    }
    public void LevelUp(int SpotWeaponLevel)
    {
        currentLevel = SpotWeaponLevel;
        SetIcon();


    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("dragProxy"))
        {
            dragProxy = collision.GetComponent<DragProxy>().image;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("dragProxy"))
        {
            // if(dragProxy != null&& collision.GetComponent<DragProxy>()==dragProxy)
            //    dragProxy = null;
        }

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
            dragProxy = null;
            icon.gameObject.SetActive(false);
        }
        //icon.sprite = isActive ? weaponData.icons[iconNum];

    }
}
