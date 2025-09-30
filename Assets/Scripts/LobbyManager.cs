using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[Serializable]
public class UICard
{
    public GameObject lockIcon;   // optional
    public TextMeshProUGUI lvlTxt;     // optional
    public GameObject upgradeBtn; // Button root (expects child "Text (TMP)" for price)
}

public class LobbyManager : MonoBehaviour
{
    [Header("Boards")]
    public GameObject[] arenaBoards;
    public GameObject rankingBoard;
    public GameObject heroesBoard;
    // 0=Inventor, 1=Wizard, 2=Samurai
    public GameObject[] heroSingleBoards;

    [Header("Bars")]
    public TextMeshProUGUI coinsTxt;
    public TextMeshProUGUI gemsTxt;

    [Header("Arena / Match Progress UI")]
    [Tooltip("Shows current/total match count, e.g. '1/10'")]
    public List<TextMeshProUGUI> areaTxt;

    [Header("Bottom-Bar Buttons")]
    public Image rankingButtonImage;
    public Image heroesButtonImage;
    public Image battleButtonImage;

    [Header("UI cards for each Hero (order 0..3)")]
    public UICard[] inventorCards;
    public UICard[] wizardCards;
    public UICard[] samuraiCards;

    [Header("Unlock Prices (per card index)")]
    public int[] inventorUnlockPrices = new int[4] { 0, 150, 250, 350 };
    public int[] wizardUnlockPrices = new int[4] { 0, 150, 250, 350 };
    public int[] samuraiUnlockPrices = new int[4] { 0, 150, 250, 350 };

    [Header("Upgrade Price")]
    public int upgradePriceBase = 100;

    [Header("UX — Message Prefab")]
    [Tooltip("Prefab with a CanvasGroup and a TMP child (Text). Will be centered on the active hero page.")]
    public GameObject messagePrefab;
    public float messageFadeIn = 0.15f;
    public float messageHold = 0.90f;
    public float messageFadeOut = 0.35f;

    [Header("Progress Model")]
    [Tooltip("Matches per Arena (index = arena). Example: [10, 12, 15].")]
    public int[] matchesPerArena = new int[] { 10 };
    [Tooltip("Optional mapping from the global match number (0-based) to a LevelManager index.\nIf empty, we’ll just use defaultLevelIndex.")]
    public int[] levelOrderByGlobalMatch;
    [Tooltip("Used if levelOrderByGlobalMatch is empty or too short.")]
    public int defaultLevelIndex = 7;

    [Header("Board Pop Animation")]
    public float popStartScale = 0.90f;
    public float popPeakScale = 1.12f;
    public float popInTime = 0.18f;
    public float popSettleTime = 0.12f;

    // ==== Colors ====
    readonly Color32 normalColor = new Color32(0x1E, 0x24, 0x40, 255);
    readonly Color32 focusedColor = new Color32(0x44, 0x52, 0xD5, 255);
    readonly Color32 greenOK = new Color32(0x3B, 0xD6, 0x6B, 255);
    readonly Color32 redWarn = new Color32(0xFF, 0x5A, 0x5A, 255);
    readonly Color32 whiteShadow = new Color32(255, 255, 255, 220);

    // ==== Pref keys ====
    private const string PrefGold = "gold";
    private const string PrefGems = "gems";
    private const string PrefArenaIndex = "Arena_Index";
    private const string PrefMatchIndex = "Arena_Match_Index";
    private const string PrefNextLevelIndex = "NextLevelIndex";
    private const string PrefBattlesCompleted = "battlesCompleted";
    private const string PrefBattlesTotal = "battlesTotal";

    private readonly Dictionary<string, string[]> cardPrefKeys = new()
    {
        { "Inventor", new[]{ "inv_bee", "inv_cog", "inv_sword", "inv_bomb" } },
        { "Wizard",   new[]{ "wiz_dagger", "wiz_stone", "wiz_ring", "wiz_feather" } },
        { "Samurai",  new[]{ "sam_knife", "sam_hammer", "sam_arrow", "sam_shuriken" } },
    };

    // Track the currently-open hero page to place messages at its center.
    private RectTransform _activePageRT;
    public Button samuraiButton;
    public GameObject samuraiBG;
    void OnEnable()
    { //RefreshFromPrefsAndRedraw(); 
    }
    
    // =================== Unity ===================
    void Start()
    {
        EnsurePriceArrays();
        EnsureDefaults_OnlyFirstUnlocked();
        EnsureProgressDefaults();
        UpdateCurrencyBars();
        UpdateArenaProgressText();
        ShowBattle();
        //if (PlayerPrefs.GetInt("NextLevelIndex", 0) == 7)
        //{
        //    samuraiButton.interactable = true;
        //    samuraiBG.SetActive(false);
        //}
        //else
        //{
        //    samuraiButton.interactable = false;
        //    samuraiBG.SetActive(true);
        //}
    }
    public void RefreshFromPrefsAndRedraw()
    {
        UpdateArenaProgressText();
        UpdateArenaBoardsVisual();
    }

    // =================== PlayerPrefs helpers (cards) ===================
    static string LevelKey(string k) => $"{k}_level";
    static string UnlockKey(string k) => k;

    static int GetLevel(string k, int fallback = 0) => PlayerPrefs.GetInt(LevelKey(k), fallback);
    static void SetLevel(string k, int level) => PlayerPrefs.SetInt(LevelKey(k), Mathf.Max(0, level));
    static bool IsUnlocked(string k) => PlayerPrefs.GetInt(UnlockKey(k), 0) == 1;
    static void SetUnlocked(string k, bool v) => PlayerPrefs.SetInt(UnlockKey(k), v ? 1 : 0);

    // =================== Coins / Gems ===================
    private int Coins
    {
        get => PlayerPrefs.GetInt(PrefGold, 0);
        set { PlayerPrefs.SetInt(PrefGold, Mathf.Max(0, value)); PlayerPrefs.Save(); UpdateCurrencyBars(); }
    }
    private bool HasEnough(int amt) => amt <= 0 || Coins >= amt;
    private int CurrentArenaBoardIndex()
    {
        int count = arenaBoards?.Length ?? 0;
        if (count == 0) return 0;
        // Arenaların sayı board sayını aşsa belə, panel indeksini dövrə salırıq
        return ((ArenaIndex % count) + count) % count;
    }


    private void UpdateArenaBoardsVisual()
    {
        int target = CurrentArenaBoardIndex();
        Debug.Log("Target" + target);
        for (int i = 0; i < (arenaBoards?.Length ?? 0); i++)
        {
            var go = arenaBoards[i];
            if (!go) continue;

            if (i == target)
            {
                if (!go.activeSelf) PopOpen(go);
            }
            else
            {
                if (go.activeSelf) go.SetActive(false);
            }
        }
    }

    private void Spend(int amt)
    {
        if (amt <= 0) return;
        Coins -= amt;

        if (coinsTxt)
        {
            LeanTween.scale(coinsTxt.gameObject, Vector3.one * 1.12f, 0.10f).setEaseOutQuad()
                .setOnComplete(() => LeanTween.scale(coinsTxt.gameObject, Vector3.one, 0.10f).setEaseInQuad());
        }
    }

    // =================== Prices ===================
    private void EnsurePriceArrays()
    {
        void Fix(ref int[] a)
        {
            if (a == null || a.Length != 4) a = new int[4];
            if (a.Length > 0) a[0] = 0;
            for (int i = 0; i < a.Length; i++) a[i] = Mathf.Max(0, a[i]);
        }
        Fix(ref inventorUnlockPrices);
        Fix(ref wizardUnlockPrices);
        Fix(ref samuraiUnlockPrices);
    }
    private int GetUnlockPrice(string deck, int index)
    {
        int[] a = deck == "Inventor" ? inventorUnlockPrices : deck == "Wizard" ? wizardUnlockPrices : samuraiUnlockPrices;
        if (a == null || index < 0 || index >= a.Length) return 999999;
        return a[index];
    }
    private int GetUpgradePrice(int currentLevel) => Mathf.Max(1, upgradePriceBase) * Mathf.Max(1, currentLevel);


    private void EnsureProgressDefaults()
    {
        if (matchesPerArena == null || matchesPerArena.Length == 0)
            matchesPerArena = new int[] { 10 };

        if (!PlayerPrefs.HasKey(PrefArenaIndex)) PlayerPrefs.SetInt(PrefArenaIndex, 0);
        if (!PlayerPrefs.HasKey(PrefMatchIndex)) PlayerPrefs.SetInt(PrefMatchIndex, 0);

        if (!PlayerPrefs.HasKey(PrefBattlesTotal)) PlayerPrefs.SetInt(PrefBattlesTotal, matchesPerArena[0]);
        if (!PlayerPrefs.HasKey(PrefBattlesCompleted)) PlayerPrefs.SetInt(PrefBattlesCompleted, 0);

        PlayerPrefs.Save();
    }

    private int ArenaIndex
    {
        get
        {
            if (PlayerPrefs.GetInt(PrefArenaIndex, 0) < 0)
            {
                PlayerPrefs.SetInt(PrefArenaIndex, 0);
                return 0;
            }
            else if (PlayerPrefs.GetInt(PrefArenaIndex, 0) >= (matchesPerArena?.Length ?? 1))
            {
                PlayerPrefs.SetInt(PrefArenaIndex, 0);
                return 0;
            }
            else return PlayerPrefs.GetInt(PrefArenaIndex, 0);
        }
        set { PlayerPrefs.SetInt(PrefArenaIndex, Mathf.Clamp(value, 0, Mathf.Max(0, (matchesPerArena?.Length ?? 1) - 1))); PlayerPrefs.Save(); }
    }

    private int MatchIndex
    {
        get
        {
            int total = MatchesTotalForArena(ArenaIndex);
            return Mathf.Clamp(PlayerPrefs.GetInt(PrefMatchIndex, 0), 0, Mathf.Max(0, total - 1));
        }
        set
        {
            int total = MatchesTotalForArena(ArenaIndex);
            PlayerPrefs.SetInt(PrefMatchIndex, Mathf.Clamp(value, 0, Mathf.Max(0, total - 1)));
            PlayerPrefs.Save();
        }
    }

    private int MatchesTotalForArena(int arenaIdx)
    {
        if (matchesPerArena == null || matchesPerArena.Length == 0) return 10;
        arenaIdx = Mathf.Clamp(arenaIdx, 0, matchesPerArena.Length - 1);
        int v = Mathf.Max(1, matchesPerArena[arenaIdx]);
        return v;
    }

    private int GlobalMatchNumber()
    {
        if (matchesPerArena == null || matchesPerArena.Length == 0)
            return MatchIndex;

        int global = 0;
        for (int i = 0; i < ArenaIndex && i < matchesPerArena.Length; i++)
            global += Mathf.Max(0, matchesPerArena[i]);

        global += MatchIndex;
        return Mathf.Max(0, global);
    }

    public int ComputeAndStoreNextLevelIndex()
    {
        int global = GlobalMatchNumber();

        // DEFAULT: play the CURRENT match
        int levelIndex = MatchIndex;

        if (levelOrderByGlobalMatch != null && levelOrderByGlobalMatch.Length > 0)
        {
            if (global >= 0 && global < levelOrderByGlobalMatch.Length)
                levelIndex = Mathf.Max(0, levelOrderByGlobalMatch[global]);
            else
                levelIndex = Mathf.Max(0, levelOrderByGlobalMatch[^1]);
        }

        //PlayerPrefs.SetInt(PrefNextLevelIndex, Mathf.Max(0, levelIndex));
        //PlayerPrefs.Save();
        return levelIndex;
    }

    public void AdvanceMatchProgress()
    {
        int total = MatchesTotalForArena(ArenaIndex);
        int nextMatch = MatchIndex + 1;
        Debug.Log($"[LobbyManager] Advancing match progress: Arena {ArenaIndex} Match {MatchIndex} -> {nextMatch} (of {total})");
        if (nextMatch < total)
        {
            MatchIndex = nextMatch;
        }
        else
        {
            // arena dəyiş (və ya loop)
            if (ArenaIndex < (matchesPerArena?.Length ?? 1) - 1)
            {
                Debug.Log("[LobbyManager] Advancing to the next arena.");
                ArenaIndex = ArenaIndex + 1;
                MatchIndex = 0;
            }
            else
            {
                Debug.Log("[LobbyManager] Reached the final arena and match. Looping back to the first arena.");
                // LOOP: son arenadan sonra 0-a qayıt
                ArenaIndex = 0;
                MatchIndex = 0;
            }
        }

        int cumulativeCompleted = GlobalMatchNumber();
        PlayerPrefs.SetInt(PrefBattlesCompleted, Mathf.Max(0, cumulativeCompleted));
        PlayerPrefs.SetInt(PrefBattlesTotal, MatchesTotalForArena(ArenaIndex));
        PlayerPrefs.Save();

        UpdateArenaProgressText();
        UpdateArenaBoardsVisual();
    }


    public void SetProgress(int arenaIdx, int matchIdx)
    {
        ArenaIndex = arenaIdx;
        MatchIndex = matchIdx;
        UpdateArenaProgressText();
        UpdateArenaBoardsVisual();
    }

    // =================== Top bars & progress text ===================
    private void UpdateCurrencyBars()
    {
        if (coinsTxt) coinsTxt.text = Coins.ToString();
        if (gemsTxt) gemsTxt.text = PlayerPrefs.GetInt(PrefGems, 0).ToString();
    }

    private void UpdateArenaProgressText()
    {
        foreach (var areaTxt in areaTxt)
        {
            if (!areaTxt) return;
            int match = MatchIndex;
            int total = MatchesTotalForArena(ArenaIndex);
            areaTxt.text = $"{match + 1}/{total}";
        }

    }

    // =================== Navigation ===================
    private void ResetAllButtonBackgrounds()
    {
        if (rankingButtonImage) rankingButtonImage.color = normalColor;
        if (heroesButtonImage) heroesButtonImage.color = normalColor;
        if (battleButtonImage) battleButtonImage.color = normalColor;
    }
    private void FocusButton(Image img)
    {
        ResetAllButtonBackgrounds();
        if (img) img.color = focusedColor;
    }

    public void ShowRanking()
    {
        foreach (var b in arenaBoards) if (b) b.SetActive(false);
        if (heroesBoard) heroesBoard.SetActive(false);
        HideAllHeroPages();

        PopOpen(rankingBoard);
        _activePageRT = null;
        FocusButton(rankingButtonImage);
    }

    public void ShowHeroes()
    {
        foreach (var b in arenaBoards) if (b) b.SetActive(false);
        if (rankingBoard) rankingBoard.SetActive(false);
        HideAllHeroPages();

        PopOpen(heroesBoard);
        _activePageRT = null;
        FocusButton(heroesButtonImage);
    }

    public void ShowBattle()
    {
        if (rankingBoard) rankingBoard.SetActive(false);
        if (heroesBoard) heroesBoard.SetActive(false);
        HideAllHeroPages();

        UpdateArenaBoardsVisual();   // <<-- yeni

        _activePageRT = null;
        FocusButton(battleButtonImage);
        UpdateArenaProgressText();
    }


    private void HideAllHeroPages()
    {
        foreach (var p in heroSingleBoards) if (p) p.SetActive(false);
    }

    // =================== Hero page (cards) ===================
    public void ShowHeroPage(string deckName)
    {
        foreach (var b in arenaBoards) if (b) b.SetActive(false);
        if (rankingBoard) rankingBoard.SetActive(false);
        if (heroesBoard) heroesBoard.SetActive(false);
        HideAllHeroPages();

        int page = deckName == "Inventor" ? 0 : deckName == "Wizard" ? 1 : 2;
        GameObject pageGO = null;
        if (heroSingleBoards != null && page >= 0 && page < heroSingleBoards.Length && heroSingleBoards[page])
        {
            pageGO = heroSingleBoards[page];
            PopOpen(pageGO);
        }
        _activePageRT = pageGO ? pageGO.GetComponent<RectTransform>() : null;

        UICard[] ui = deckName == "Inventor" ? inventorCards : deckName == "Wizard" ? wizardCards : samuraiCards;
        if (!cardPrefKeys.TryGetValue(deckName, out var keys) || ui == null) { FocusButton(heroesButtonImage); return; }

        int n = Mathf.Min(ui.Length, keys.Length);
        for (int i = 0; i < n; i++)
        {
            string key = keys[i];
            bool unlocked = IsUnlocked(key);
            int level = GetLevel(key, 0);

            bool isPlayable = unlocked && level >= 1;

            if (ui[i].lockIcon) ui[i].lockIcon.SetActive(!isPlayable);

            // Dim card art when locked, but NEVER the button (handled by affordability)
            ApplyCardUnlockedVisual(ui[i], isPlayable);

            // ---- LEVEL BADGE: always refresh if playable ----
            if (ui[i].lvlTxt)
            {
                ui[i].lvlTxt.gameObject.SetActive(isPlayable);
                if (isPlayable)
                {
                    ui[i].lvlTxt.alpha = 1f;                    // ensure fully visible
                    ui[i].lvlTxt.enabled = true;
                    ui[i].lvlTxt.text = Mathf.Max(1, level).ToString(); // number only
                }
            }

            var buttonGO = ui[i].upgradeBtn;
            var btn = buttonGO ? buttonGO.GetComponent<Button>() : null;
            var label = FindTMPOnButton(buttonGO);
            if (!btn || !label) continue;

            // Show the first icon/image inside the button ONLY when the card is playable (unlocked)
            var firstImgChild = GetFirstImmediateImageChild(buttonGO);
            if (firstImgChild) firstImgChild.SetActive(isPlayable);

            btn.onClick.RemoveAllListeners();

            if (!isPlayable)
            {
                int price = GetUnlockPrice(deckName, i);
                bool affordable = HasEnough(price);

                label.text = price.ToString();
                btn.interactable = affordable;
                SetButtonAffordabilityVisual(buttonGO, affordable);

                int captured = i;
                btn.onClick.AddListener(() =>
                {
                    if (!HasEnough(price))
                    {
                        Pulse(btn.transform);
                        ShowMessage("Not enough coins!", redWarn);
                        return;
                    }

                    Spend(price);
                    SetUnlocked(keys[captured], true);
                    SetLevel(keys[captured], 1);
                    PlayerPrefs.Save();

                    ShowHeroPage(deckName);

                    var uiList = deckName == "Inventor" ? inventorCards : deckName == "Wizard" ? wizardCards : samuraiCards;
                    Celebrate(uiList[captured]);
                    ShowTransaction("Unlocked!", -price, greenOK);
                });
            }
            else
            {
                int price = GetUpgradePrice(level);
                bool affordable = HasEnough(price);

                label.text = price.ToString();
                btn.interactable = affordable;
                SetButtonAffordabilityVisual(buttonGO, affordable);

                int captured = i;
                btn.onClick.AddListener(() =>
                {
                    if (!HasEnough(price))
                    {
                        Pulse(btn.transform);
                        ShowMessage("Not enough coins!", redWarn);
                        return;
                    }

                    Spend(price);
                    SetLevel(keys[captured], level + 1);
                    PlayerPrefs.Save();

                    ShowHeroPage(deckName);

                    var uiList = deckName == "Inventor" ? inventorCards : deckName == "Wizard" ? wizardCards : samuraiCards;
                    Celebrate(uiList[captured]);
                    ShowTransaction("Level Up!", -price, greenOK);
                });
            }
        }

        FocusButton(heroesButtonImage);
    }

    private static TextMeshProUGUI FindTMPOnButton(GameObject buttonGO)
    {
        if (!buttonGO) return null;
        var t = buttonGO.transform.Find("Text (TMP)");
        if (t) { var tmp = t.GetComponent<TextMeshProUGUI>(); if (tmp) return tmp; }
        return buttonGO.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private static GameObject GetFirstImmediateImageChild(GameObject buttonGO)
    {
        if (!buttonGO) return null;
        var tr = buttonGO.transform;
        for (int i = 0; i < tr.childCount; i++)
        {
            var c = tr.GetChild(i);
            if (c.GetComponent<Image>() != null) return c.gameObject;
        }
        return null;
    }

    // =================== Micro FX / Messages ===================
    private void Pulse(Transform t)
    {
        if (!t) return;
        t.localScale = Vector3.one;
        LeanTween.scale(t.gameObject, Vector3.one * 1.05f, 0.08f).setEaseOutQuad()
            .setOnComplete(() => LeanTween.scale(t.gameObject, Vector3.one, 0.08f).setEaseInQuad());
    }

    private void Celebrate(UICard ui)
    {
        Transform anchor = ui.lvlTxt ? ui.lvlTxt.transform : (ui.upgradeBtn ? ui.upgradeBtn.transform : null);
        if (anchor)
        {
            LeanTween.scale(anchor.gameObject, Vector3.one * 1.12f, 0.10f).setEaseOutQuad()
                .setOnComplete(() => LeanTween.scale(anchor.gameObject, Vector3.one, 0.10f).setEaseInQuad());
        }
    }

    private void ShowTransaction(string label, int delta, Color color)
    {
        string deltaStr = delta != 0 ? ((delta > 0 ? "+" : "-") + Mathf.Abs(delta)) : "";
        string text = string.IsNullOrEmpty(deltaStr) ? label : (deltaStr + "  •  " + label);
        ShowMessage(text, color);
    }

    private void ShowMessage(string message, Color color, RectTransform overrideParent = null)
    {
        if (!messagePrefab)
        {
            Debug.LogWarning("[LobbyManager] Message Prefab is not assigned.");
            return;
        }

        string oneLine = (message ?? string.Empty)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ");

        RectTransform parent = overrideParent ?? _activePageRT;
        if (!parent)
        {
            if (coinsTxt)
            {
                var canvas = coinsTxt.GetComponentInParent<Canvas>();
                parent = canvas ? canvas.GetComponent<RectTransform>() : null;
            }
        }
        if (!parent) parent = this.transform as RectTransform;

        var inst = Instantiate(messagePrefab);
        var rt = inst.GetComponent<RectTransform>() ?? inst.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        var tmp = inst.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp)
        {
            tmp.text = oneLine;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.maxVisibleLines = 1;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = whiteShadow;
        }
        else
        {
            var legacy = inst.GetComponentInChildren<Text>(true);
            if (legacy)
            {
                legacy.text = oneLine;
                legacy.color = color;
                legacy.alignment = TextAnchor.MiddleCenter;
                legacy.supportRichText = true;
                legacy.resizeTextForBestFit = false;
                legacy.horizontalOverflow = HorizontalWrapMode.Overflow;
                legacy.verticalOverflow = VerticalWrapMode.Truncate; // fixed enum
            }
        }

        var cg = inst.GetComponent<CanvasGroup>();
        if (!cg) cg = inst.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        rt.localScale = Vector3.one * 0.96f;

        LeanTween.value(inst, 0f, 1f, Mathf.Max(0.01f, messageFadeIn))
            .setOnUpdate((float a) => { cg.alpha = a; rt.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.0f, a); });

        LeanTween.value(inst, 1f, 0f, Mathf.Max(0.01f, messageFadeOut))
            .setDelay(Mathf.Max(0f, messageFadeIn + messageHold))
            .setOnUpdate((float a) => cg.alpha = a)
            .setOnComplete(() => Destroy(inst));
    }

    // =================== Public hooks for UI buttons ===================
    public void OnClickPlay()
    {
        int levelIdx = ComputeAndStoreNextLevelIndex();
        ShowMessage($"Starting Level {levelIdx + 1}", greenOK);
    }

    public void OnClickAdvanceMatch()
    {
        AdvanceMatchProgress();
        ShowMessage("Progress advanced", greenOK);
    }

    public void OnClickResetProgress()
    {
        SetProgress(0, 0);
        PlayerPrefs.SetInt(PrefBattlesCompleted, 0);
        PlayerPrefs.SetInt(PrefBattlesTotal, MatchesTotalForArena(0));
        PlayerPrefs.Save();
        ShowMessage("Progress reset", greenOK);
    }

    public void OnClickStartButton()
    {
        ComputeAndStoreNextLevelIndex(); // writes NextLevelIndex based on MatchIndex/mapping
        SceneManager.LoadScene("Game");
    }

    // =================== Card defaults ===================
    private void EnsureDefaults_OnlyFirstUnlocked()
    {
        foreach (var kv in cardPrefKeys)
        {
            var keys = kv.Value;
            if (keys == null || keys.Length == 0) continue;

            // First card: unlocked & at least level 1
            if (!IsUnlocked(keys[0])) SetUnlocked(keys[0], true);
            if (GetLevel(keys[0], 0) < 1) SetLevel(keys[0], 1);

            // Others: initialize if missing (preserve existing)
            for (int i = 1; i < keys.Length; i++)
            {
                if (!PlayerPrefs.HasKey(UnlockKey(keys[i]))) SetUnlocked(keys[i], false);
                if (!PlayerPrefs.HasKey(LevelKey(keys[i]))) SetLevel(keys[i], 0);
            }
        }
        PlayerPrefs.Save();
    }

    // =================== Optional direct card hooks ===================
    public void UnlockCard(string deckName, int index)
    {
        if (!cardPrefKeys.TryGetValue(deckName, out var keys) || index < 0 || index >= keys.Length) return;
        if (IsUnlocked(keys[index])) return;
        int price = GetUnlockPrice(deckName, index);
        if (!HasEnough(price)) { ShowMessage("Not enough coins!", redWarn); return; }

        Spend(price);
        SetUnlocked(keys[index], true);
        SetLevel(keys[index], 1);
        PlayerPrefs.Save();
        ShowHeroPage(deckName);

        var list = deckName == "Inventor" ? inventorCards : deckName == "Wizard" ? wizardCards : samuraiCards;
        Celebrate(list[index]);
        ShowTransaction("Unlocked!", -price, greenOK);
    }

    public void UpgradeCard(string deckName, int index)
    {
        if (!cardPrefKeys.TryGetValue(deckName, out var keys) || index < 0 || index >= keys.Length) return;
        if (!IsUnlocked(keys[index])) return;

        int cur = Mathf.Max(1, GetLevel(keys[index], 1));
        int price = GetUpgradePrice(cur);
        if (!HasEnough(price)) { ShowMessage("Not enough coins!", redWarn); return; }

        Spend(price);
        SetLevel(keys[index], cur + 1);
        PlayerPrefs.Save();
        ShowHeroPage(deckName);

        var list = deckName == "Inventor" ? inventorCards : deckName == "Wizard" ? wizardCards : samuraiCards;
        Celebrate(list[index]);
        ShowTransaction("Level Up!", -price, greenOK);
    }

    // =================== Pop animation helper ===================
    private void PopOpen(GameObject go)
    {
        if (!go) return;

        LeanTween.cancel(go);
        if (!go.activeSelf) go.SetActive(true);

        go.transform.localScale = Vector3.one * Mathf.Max(0.01f, popStartScale);
        LeanTween.scale(go, Vector3.one * Mathf.Max(1f, popPeakScale), Mathf.Max(0.01f, popInTime))
            .setEaseOutQuad()
            .setOnComplete(() =>
            {
                LeanTween.scale(go, Vector3.one, Mathf.Max(0.01f, popSettleTime))
                    .setEaseInQuad();
            });
    }

    // =================== Visual helpers ===================
    private static bool IsUnder(Transform t, Transform maybeParent)
        => t && maybeParent && t.IsChildOf(maybeParent);

    /// <summary>
    /// Dims the card art/level when locked, but never the button visuals.
    /// </summary>
    private void ApplyCardUnlockedVisual(UICard ui, bool isUnlockedAndUsable)
    {
        if (ui == null) return;

        Transform root =
            (ui.upgradeBtn ? ui.upgradeBtn.transform.parent :
            (ui.lvlTxt ? ui.lvlTxt.transform.parent : null));

        if (!root) return;

        float imgAlphaUnlocked = 1f;
        float imgAlphaLocked = 0.5f;
        float textAlphaLocked = 0.65f;

        var images = root.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (!img) continue;
            if (ui.lockIcon && img.gameObject == ui.lockIcon) continue;
            if (ui.upgradeBtn && IsUnder(img.transform, ui.upgradeBtn.transform)) continue;
            var c = img.color;
            c.a = isUnlockedAndUsable ? imgAlphaUnlocked : imgAlphaLocked;
            img.color = c;
        }

        var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in tmps)
        {
            if (!t) continue;
            if (ui.upgradeBtn && IsUnder(t.transform, ui.upgradeBtn.transform)) continue;
            var c = t.color;
            c.a = isUnlockedAndUsable ? 1f : textAlphaLocked;
            t.color = c;
        }
    }

    /// <summary>
    /// Makes the button semi-transparent ONLY when not affordable.
    /// Works for both unlock and upgrade buttons; independent of lock state.
    /// </summary>
    private void SetButtonAffordabilityVisual(GameObject buttonGO, bool affordable)
    {
        if (!buttonGO) return;
        var cg = buttonGO.GetComponent<CanvasGroup>();
        if (!cg) cg = buttonGO.AddComponent<CanvasGroup>();
        cg.alpha = affordable ? 1f : 0.45f;
    }
}
