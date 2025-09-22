using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class StaticWeapon : MonoBehaviour
{
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

    public void Init(SlotWeaponsSO _weaponData, int Level, Vector2Int pos, Camera camera, GameObject weaponSpawned)
    {
        gameObject.name = _weaponData.weaponName.ToString();
        weaponData = _weaponData;
        gridPosition = pos;
        isActive = false;
        WeaponSpawned = Instantiate(weaponData.weaponType,Vector3.zero,Quaternion.identity,transform);

        cardDeckAnimator = WeaponSpawned.GetComponentInChildren<CardDeckAnimator>();
        cardDeckAnimator.staticWeapon = this;
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
