using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;   // <- added
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public RoguelikeManager roguelikeManager;

    [Header("Background")]
    public GameObject bgObject;

    [Header("Bars")]
    public TextMeshProUGUI coinsTxt;
    public TextMeshProUGUI gemsTxt;

    [Header("Level and Waves")]
    public TextMeshProUGUI levelTxt;
    public TextMeshProUGUI waveTxt;
    public GameObject slider;
    public TextMeshProUGUI healthTxt;

    [Header("Win/Lose Boards")]
    public GameObject winBoard;
    public GameObject loseBoard;

    [Header("RogueLike Board and elements")]
    public GameObject roguelikeBoard;
    public TextMeshProUGUI roguelikeText;
    public GameObject[] offeredCardSlots;
    public TextMeshProUGUI[] offeredCardSlotTxt;

    [Header("Empty Slot Sprite")]
    public Sprite emptyOfferedSprite;

    [Header("Panel Animation")]
    public float panelTweenTime = 0.5f;
    public float bounceScale = 1.2f;  // How far it inflates on entry
    public float squashScale = 0.85f; // Slight squash before expanding

    private Slider _sliderComponent;
    private bool[] _offeredEnlarged;
    private int _selectedOfferedIndex = -1;

    public int selectedRoguelikeIndex => _selectedOfferedIndex;

    void Start()
    {
        if (slider != null)
            _sliderComponent = slider.GetComponent<Slider>();

        _offeredEnlarged = new bool[(offeredCardSlots != null) ? offeredCardSlots.Length : 0];
        ScaleBackgroundSpriteToFullScreen();

        InitPanelState(winBoard);
        InitPanelState(loseBoard);
        InitPanelState(roguelikeBoard);

        ForceAnimatorsUnscaled(winBoard);
        ForceAnimatorsUnscaled(loseBoard);
        //ForceAnimatorsUnscaled(roguelikeBoard);
    }

    void ScaleBackgroundSpriteToFullScreen()
    {
        if (bgObject == null) return;

        var sr = bgObject.GetComponent<SpriteRenderer>();
        var cam = Camera.main;
        if (sr == null || sr.sprite == null || cam == null) return;

        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth  = worldHeight * cam.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

        float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);

        bgObject.transform.position = new Vector3(0f, 1f, 10f);
        bgObject.transform.localScale = new Vector3(scale+0.05f, scale+0.05f, 1f);
    }

    public void SetCoins(int amt) { if (coinsTxt != null) coinsTxt.text = amt.ToString(); }
    public void SetGems(int amt) { if (gemsTxt != null) gemsTxt.text = amt.ToString(); }
    public void SetLevelText(string t) { if (levelTxt != null) levelTxt.text = t; }
    public void SetWaveText(string t) { if (waveTxt != null) waveTxt.text = t; }
    public void SetHealthText(string t) { if (healthTxt != null) healthTxt.text = t; }
    public void SetSliderValue(float v) { if (_sliderComponent != null) _sliderComponent.value = Mathf.Clamp01(v); }
    public void SetRougelikeText(string text) { if (roguelikeText != null) roguelikeText.text = text; }

    public void SetWinBoardActive(bool a) { AnimatePanel(winBoard, a); }
    public void SetLoseBoardActive(bool a) { AnimatePanel(loseBoard, a); }

    // NEW: Continue button -> go back to Lobby scene
    public void OnClickContinue()
    {
        // restore time & audio in case we were paused on win/lose
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // hide boards (optional)
        if (winBoard)  winBoard.SetActive(false);
        if (loseBoard) loseBoard.SetActive(false);
        if (roguelikeBoard) roguelikeBoard.SetActive(false);

        SceneManager.LoadScene("Lobby");
    }

    // delayed show helpers (use real time, unaffected by timeScale)
    public void ShowWinAfterDelay(float seconds)  => StartCoroutine(ShowPanelAfterDelay(winBoard, seconds));
    public void ShowLoseAfterDelay(float seconds) => StartCoroutine(ShowPanelAfterDelay(loseBoard, seconds));

    private IEnumerator ShowPanelAfterDelay(GameObject panel, float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        AnimatePanel(panel, true);
    }

    public void RestartGame() { if (gameManager != null) gameManager.NextLevel(); }

    public void SetRougelikeBoardActive(bool active)
    {
        AnimatePanel(roguelikeBoard, active);

        if (active)
        {
            _selectedOfferedIndex = -1;
            if (offeredCardSlots != null)
            {
                for (int i = 0; i < offeredCardSlots.Length; i++)
                {
                    var slot = offeredCardSlots[i];
                    if (slot != null)
                    {
                        slot.transform.localScale = Vector3.one;
                        if (_offeredEnlarged != null && i < _offeredEnlarged.Length)
                            _offeredEnlarged[i] = false;
                    }
                }
            }
        }
    }

    public void ShowRoguelikeOptions()
    {
        var options = roguelikeManager != null ? roguelikeManager.CurrentOptions : null;
        int count = (options != null) ? options.Count : 0;

        if (offeredCardSlotTxt != null)
        {
            for (int t = 0; t < offeredCardSlotTxt.Length; t++)
                if (offeredCardSlotTxt[t] != null)
                    offeredCardSlotTxt[t].text = string.Empty;
        }

        if (offeredCardSlots == null) return;

        for (int i = 0; i < offeredCardSlots.Length; i++)
        {
            var slot = offeredCardSlots[i];
            if (slot == null) continue;

            bool isActive = i < count;
            slot.SetActive(isActive);

            if (isActive)
                SetRoguelikeOption(i, options[i]);
        }
    }

    public void SetRoguelikeOption(int index, RoguelikeOption option)
    {
        if (offeredCardSlots == null || index < 0 || index >= offeredCardSlots.Length) return;
        var slot = offeredCardSlots[index];
        if (slot == null || option == null) return;

        Sprite art = option.GetArtwork() ?? emptyOfferedSprite;
        var img = slot.GetComponentInChildren<Image>();
        if (img != null) img.sprite = art;

        if (offeredCardSlotTxt != null && index < offeredCardSlotTxt.Length && offeredCardSlotTxt[index] != null)
        {
            string label;
            switch (option.type)
            {
                case OptionType.NewHero:
                    label = option.deckName; break;
                case OptionType.UpgradeCard:
                    label = $"{option.oldLevel}->{option.newLevel}"; break;
                case OptionType.ReduceCooldown:
                    label = "Reduce CD -20%"; break;
                case OptionType.HealthUp:
                    label = $"Health +{Mathf.Max(1, option.healthAmount)}"; break;
                default:
                    label = option.deckName; break;
            }
            offeredCardSlotTxt[index].text = label;
        }

        slot.transform.localScale = Vector3.one;
        if (_offeredEnlarged != null && index < _offeredEnlarged.Length)
            _offeredEnlarged[index] = false;

        var btn = slot.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            string deckName = option.deckName;
            int capturedIndex = index;
            btn.onClick.AddListener(() =>
            {
                _selectedOfferedIndex = capturedIndex;
                OnOfferedCardClicked(deckName);
            });
        }
    }

    private void OnOfferedCardClicked(string deckName)
    {
        int deckIdx;
        switch (deckName)
        {
            case "Inventor": deckIdx = 0; break;
            case "Wizard":   deckIdx = 1; break;
            case "Samurai":  deckIdx = 2; break;
            default:
                SetRougelikeBoardActive(false);
                return;
        }

        if (gameManager != null)
            gameManager.ActivateDeckAnimatorParent(deckIdx);

        PlayerPrefs.SetInt($"Deck_{deckName}_Active", 1);
        PlayerPrefs.Save();

        SetRougelikeBoardActive(false);
    }

    public void DisableSlotsWithoutImage()
    {
        if (offeredCardSlots == null) return;

        foreach (var slot in offeredCardSlots)
        {
            if (slot == null) continue;
            var img = slot.GetComponentInChildren<Image>();
            if (img == null || img.sprite == null)
                slot.SetActive(false);
        }
    }

    private void InitPanelState(GameObject panel)
    {
        if (panel == null) return;
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        if (!panel.activeSelf)
        {
            panel.transform.localScale = Vector3.one * 0.9f;
            cg.alpha = 0f;
        }
        else
        {
            panel.transform.localScale = Vector3.one;
            cg.alpha = 1f;
        }
    }

    // ---------- Unscaled-time tween helpers ----------
    private LTDescr ScaleUnscaled(GameObject target, Vector3 to, float duration)
        => LeanTween.scale(target, to, duration).setIgnoreTimeScale(true);

    private LTDescr ValueUnscaled(GameObject target, float from, float to, float duration, Action<float> onUpdate)
        => LeanTween.value(target, from, to, duration).setOnUpdate(onUpdate).setIgnoreTimeScale(true);

    private void AnimatePanel(GameObject panel, bool show)
    {
        if (panel == null) return;
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        ForceAnimatorsUnscaled(panel);
        LeanTween.cancel(panel);

        if (show)
        {
            if (!panel.activeSelf) panel.SetActive(true);
            panel.transform.localScale = Vector3.one * squashScale;
            cg.alpha = 0f;

            ScaleUnscaled(panel, Vector3.one * bounceScale, panelTweenTime * 0.5f)
                .setEaseOutQuad()
                .setOnComplete(() =>
                {
                    ScaleUnscaled(panel, Vector3.one, panelTweenTime * 0.4f)
                        .setEaseOutBounce();
                });

            ValueUnscaled(panel, 0f, 1f, panelTweenTime, a => cg.alpha = a);
        }
        else
        {
            ScaleUnscaled(panel, Vector3.one * squashScale, panelTweenTime * 0.3f)
                .setEaseInBack()
                .setOnComplete(() =>
                {
                    cg.alpha = 0f;
                    panel.SetActive(false);
                });

            ValueUnscaled(panel, cg.alpha, 0f, panelTweenTime * 0.3f, a => cg.alpha = a);
        }
    }

    private void ForceAnimatorsUnscaled(GameObject root)
    {
        if (root == null) return;
        var animators = root.GetComponentsInChildren<Animator>(true);
        foreach (var a in animators)
        {
            if (a != null) a.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }
}
