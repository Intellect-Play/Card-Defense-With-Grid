using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;
    [SerializeField] List<GameObject> HandImages;
    [SerializeField] List<Button> HandButtons;
    [SerializeField] List<Canvas> HandCanvases;

    [SerializeField] GameObject Panels;

    public int currentLevel;
    Color PanelColor = new Color(67, 67, 67, 213);
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
        //PlayerPrefs.SetInt("gold", 10000);
    }
    private void Start()
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
        Debug.Log("Clicked button " + index);
        if (PlayerPrefs.GetInt("TutorialFinish", 0) == 1 || currentLevel != 1) return;
        StartCoroutine(WaitAndClose(index));
    }
    IEnumerator WaitAndClose(int index)
    {

        HandImages[index].SetActive(false);

        Debug.Log("Button " + index + " clicked");
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
