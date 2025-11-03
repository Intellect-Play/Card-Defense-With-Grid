using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;
    [SerializeField] List<GameObject> HandImages;
    [SerializeField] List<Button> HandButtons;
    [SerializeField] List<Canvas> HandCanvases;

    [SerializeField] GameObject Panels;
    [SerializeField] InventoryManager inventoryManager;
    [SerializeField] Button fightButton;
    [SerializeField] Button buyButton;


    public int currentLevel;
    Color PanelColor = new Color(67, 67, 67, 213);
    //public List<List<int>> ints = new List<List<int>>();
    public List<IntList> ints;
    public List<int[]> ints2;
    public GameObject hand;
    public GameObject handFight;
    public GameObject handBuy;

    public bool handBool=false;

    [Header("Hand")]
    public RectTransform target;   // Hərəkət edəcək obyekt
    public RectTransform pointA;   // Başlanğıc nöqtə (RectTransform)
    public RectTransform pointB;   // Son nöqtə (RectTransform)

    [Header("Settings")]
    public float duration = 1.5f;
    public Ease easeType = Ease.InOutSine;

    private Tween moveTween;
    public bool FightBool = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        target = hand.GetComponent<RectTransform>();
        //PlayerPrefs.SetInt("gold", 10000);
    }
    private void Start()
    {

        StartMoving();
        handBuy.SetActive(false);
        fightButton.onClick.AddListener(DestroyHandFight);
        try
        {
            TinySauce.OnGameStarted(PlayerPrefs.GetInt("NextLevelIndex", 0));

        }
        catch (Exception)
        {

            throw;
        }
    }
    private void Update()
    {
        if (buyButton.interactable)
        {
            //buyButton.interactable = false;
            //Debug.Log("Show HandBuy");
            handBuy.SetActive(true);
        }
        else
        {
            //Debug.Log("Show HandBuy 2");

            handBuy.SetActive(false);
        }
    }
    private void FixedUpdate()
    {
        FightButton();
        Check();
     
    }
    public void DestroyHandFight()
    {
        FightBool = true;
        //Debug.Log("DestroyHandFight");
        if (handFight) Destroy(handFight);
    }
    private void FightButton()
    {
        if(FightBool)
        {
            fightButton.interactable = true;
            return;
        }

        foreach (InventorySlot transform in inventoryManager.spawnSlots)
        {
            if (transform.transform.childCount > 0)
            {
                if(handFight) handFight.SetActive(false);
                fightButton.interactable = false;
                return;
            }
        }
        if (handFight) handFight.SetActive(true);
        fightButton.interactable = true;
    }
    private void Check()
    {
        if (handBool) return;
        foreach (InventorySlot transform in inventoryManager.spawnSlots)
        {
            if (transform.transform.childCount == 0)
            {
                handBool = true;
                hand.SetActive(false);
                return;
            }
        }
    }
    public void FinishTutorial()
    {
        PlayerPrefs.SetInt("TutorialFinish", 1);
    }
    public void StartMoving()
    {
        if (moveTween != null && moveTween.IsActive())
            moveTween.Kill();

        // Başlanğıc mövqeyə apar
        target.anchoredPosition = pointA.anchoredPosition;

        // A ilə B arasında gedib-gələn loop
        moveTween = target.DOAnchorPos(pointB.anchoredPosition, duration)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void StopMoving()
    {
        if (moveTween != null)
            moveTween.Kill();
    }
    private void StartTutorial()
    {
        PanelColor = Panels.GetComponent<Image>().color;
        PanelActive(false);
        currentLevel = PlayerPrefs.GetInt("NextLevelIndex", 0);
        Debug.Log("Current Level: " + currentLevel);
      
        if (PlayerPrefs.GetInt("TutorialFinish", 0) == 1)
        {
            Panels.SetActive(false);
            foreach (GameObject hand in HandImages)
                hand.SetActive(false);
            for (int i = 0; i < HandCanvases.Count; i++)
            {
                HandCanvases[i].sortingOrder = 1;
            }
            return;
        }


        if (currentLevel != 1)
        {
            Panels.SetActive(false);
            foreach (GameObject hand in HandImages)
                hand.SetActive(false);
            for (int i = 0; i < HandCanvases.Count; i++)
            {
                HandCanvases[i].sortingOrder = 1;
            }
            return;
        }
        else Panels.SetActive(true);
        PlayerPrefs.SetInt("TutorialStart", 1);
        for (int i = 0; i < HandButtons.Count; i++)
        {
            int index = i;
            HandButtons[i].onClick.AddListener(() => OnHandButtonClick(index));
            HandImages[i].SetActive(i == 0);
            HandCanvases[i].sortingOrder = i == 0 ? 6 : 1;

        }

    }
    public void PanelActive(bool active)
    {
        if (active)
        {
            Panels.SetActive(true);
            Panels.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        }
        else
        {
            Panels.GetComponent<Image>().color = PanelColor;
            Panels.SetActive(false);
        }
    }
    public void PopupBounce(GameObject go, float x)
    {
        if (!go) return;

        // Əvvəlcə scale 0-a salırıq
        go.transform.localScale = Vector3.zero;
        go.SetActive(true);

        // LeanTween ilə 0 → 1, yolda "bounce" effekti ilə
        LeanTween.scale(go, new Vector3(x, x, x), 1)
            .setEaseOutBack(); // bounce effekti üçün
    }
    public void PopClose(GameObject go, float duration = 1)
    {
        if (!go) return;

        LeanTween.cancel(go);

        LeanTween.scale(go, Vector3.zero, Mathf.Max(0.01f, duration))
            .setEaseInBack()
            .setOnComplete(() => go.SetActive(false));
    }
    public void OnHandButtonClick(int index)
    {
        //Debug.Log("Clicked button " + index);
        if (PlayerPrefs.GetInt("TutorialFinish", 0) == 1 || currentLevel != 1) return;
        StartCoroutine(WaitAndClose(index));
    }
    IEnumerator WaitAndClose(int index)
    {

        HandImages[index].SetActive(false);

        //Debug.Log("Button " + index + " clicked");
        if (index == 2)
        {
            Panels.SetActive(true);
            Panels.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            yield return new WaitForSeconds(2f);
            Panels.GetComponent<Image>().color = PanelColor;

        }
        if (index == 4)
        {
            Panels.SetActive(false);
            foreach (GameObject hand in HandImages)
                hand.SetActive(false);
            PlayerPrefs.SetInt("TutorialFinish", 1);
        }
        else HandImages[index + 1].SetActive(true);
        CloseCanvases(index);
    }
   
    public void CloseCanvases(int index)
    {
        for (int i = 0; i < HandCanvases.Count; i++)
        {
            if (i == index + 1)
                HandCanvases[i].sortingOrder = 6;
            else
                HandCanvases[i].sortingOrder = 1;
        }

    }

}
[System.Serializable]
public class IntList
{
    public List<int> values;
}