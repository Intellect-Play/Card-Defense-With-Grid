using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CardDeckAnimator : MonoBehaviour
{
    public enum DeckType { Inventor, Wizard, Samurai }

    [Header("Deck")]
    public DeckType deckType = DeckType.Inventor;
    public CardSO[] cards;
    public UIManager uiManager;

    [Header("Deck Materials (URP/Lit)")]
    public Material InvCardBackMat;
    public Material WizCardBackMat;
    public Material SamCardBackMat;
    public Sprite   defaultBackSprite;

    [Header("Animator")]
    public string   cardAnimChildName = "card_anim";
    public Animator cardAnim;

    [Header("Pause Window")]
    [Min(0)] public int pauseOnFrame = 30;
    [Min(0)] public int earlyMarginFrames = 2;
    [Min(0)] public int lateMarginFrames  = 0;

    [Header("Cooldown & Timing")]
    public float animCooldownSecs = 1.0f;
    public bool  useUnscaledTime  = false;

    [Header("Smoothing")]
    [Range(0.05f, 1f)] public float approachSlowSpeed = 0.35f;
    [Min(0f)] public float resumeRampTime = 0.20f;
    public bool onlyForLoopingClips = true;

    [Header("Selection")]
    public bool avoidImmediateRepeat = true;
    public bool revertMaterialAfterFire = false;
    public float revertDelay = 0.25f;
    public StaticWeapon staticWeapon;
    [Header("Debug")]
    public bool verboseLogs = false;

    // Internals
    public readonly List<CardSO> _pool = new List<CardSO>();
    private CardSO _lastFired, _currentCycleCard;
    private int _materialSetVersion = 0;

    private bool  _animPaused = false;
    private float _cooldownT  = 0f;
    private bool  _rampingOut = false;
    private float _rampT      = 0f;

    private int  _lastCycle = -1, _totalFrames = 0;
    private bool _pausedThisCycle = false;
    private bool _externalPause = false;

    private Renderer _cardFaceRenderer;

    private static readonly int BaseMapProp = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseMapST   = Shader.PropertyToID("_BaseMap_ST");

    // -------------------------------

    void OnEnable()
    {
        RebuildPool();
        ResolveCardAnimAndFace(true);
        ApplyBackToCardFace();

        _animPaused = false; _cooldownT = 0f; _rampingOut = false; _rampT = 0f;
        _lastCycle = -1; _pausedThisCycle = false; _currentCycleCard = null;
        if (cardAnim) cardAnim.speed = 1f;
    }

    void OnDisable()
    {
        _materialSetVersion++;
        if (cardAnim) cardAnim.speed = 1f;
        _animPaused = false; _rampingOut = false;
    }

    public void PauseFiring()  => _externalPause = true;
    public void ResumeFiring() => _externalPause = false;

    public void ReduceShuffleDelay(int percent)
    {
        float f = Mathf.Clamp01(1f - (percent / 100f));
        animCooldownSecs = Mathf.Max(0.05f, animCooldownSecs * f);
        if (verboseLogs) Debug.Log($"[CardDeckAnimator] animCooldownSecs={animCooldownSecs:0.###}");
    }
    private float timer;
    public float fireInterval = 2f; // neçə saniyədən bir işləsin

    void Update()
    {
        if(TetrisWeaponManager.isTetrisScene) return;
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // Timer artır
        timer += dt;

        // Əgər vaxt çatıbsa → işlə
        if (timer >= fireInterval)
        {
            timer = 0f; // sıfırla

            if (!staticWeapon.isActive) return;
            _currentCycleCard = PickRandomCard();
            if (_currentCycleCard)
            {
                EnsureRendererUsesDeckMaterial();
                if (_currentCycleCard.artwork)
                    SetDeckMaterialFromSprite(GetDeckMaterial(), _currentCycleCard.artwork);
                else if (defaultBackSprite)
                    SetDeckMaterialFromSprite(GetDeckMaterial(), defaultBackSprite);

                FireChosen(); // 🔹 güllə at
            }
        }
    }


    //void Update()
    //{
    //    float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    //    bool gate = (uiManager && uiManager.roguelikeBoard && uiManager.roguelikeBoard.activeSelf) || _externalPause;

    //    if (!cardAnim || cardAnim.layerCount == 0 || !cardAnim.gameObject.activeInHierarchy) return;

    //    var clipInfos = cardAnim.GetCurrentAnimatorClipInfo(0);
    //    if (clipInfos == null || clipInfos.Length == 0) return;

    //    var state = cardAnim.GetCurrentAnimatorStateInfo(0);
    //    var clip  = clipInfos[0].clip;
    //    if (!clip) return;
    //    if (onlyForLoopingClips && !clip.isLooping) return;

    //    _totalFrames = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
    //    if (pauseOnFrame >= _totalFrames) return;

    //    float normalized = state.normalizedTime;
    //    int cycle = Mathf.FloorToInt(normalized);
    //    float within = normalized - cycle;
    //    int frame = Mathf.Clamp(Mathf.FloorToInt(within * _totalFrames), 0, _totalFrames - 1);

    //    // --- new cycle: choose a card & set material texture ---
    //    if (cycle != _lastCycle)
    //    {
    //        _lastCycle = cycle;
    //        _pausedThisCycle = false;

    //        _currentCycleCard = PickRandomCard();

    //        EnsureRendererUsesDeckMaterial();
    //        if (_currentCycleCard && _currentCycleCard.artwork)
    //            SetDeckMaterialFromSprite(GetDeckMaterial(), _currentCycleCard.artwork);
    //        else if (defaultBackSprite)
    //            SetDeckMaterialFromSprite(GetDeckMaterial(), defaultBackSprite);

    //        if (!_animPaused) cardAnim.speed = 1f;
    //        if (verboseLogs && _currentCycleCard)
    //            Debug.Log($"[CardDeckAnimator] Cycle {cycle} start → {_currentCycleCard.name}");
    //    }

    //    if (_animPaused)
    //    {
    //        _cooldownT -= dt;
    //        if (_cooldownT <= 0f)
    //        {
    //            _animPaused = false;
    //            StartResumeRamp();
    //        }
    //        return;
    //    }

    //    int framesToTarget = pauseOnFrame - frame;
    //    if (!_pausedThisCycle && framesToTarget > 0 && framesToTarget <= Mathf.Max(1, earlyMarginFrames))
    //    {
    //        if (!_rampingOut) cardAnim.speed = Mathf.Min(cardAnim.speed, approachSlowSpeed);
    //    }
    //    else if (!_rampingOut)
    //    {
    //        cardAnim.speed = 1f;
    //    }

    //    int lower = Mathf.Max(0, pauseOnFrame - earlyMarginFrames);
    //    int upper = Mathf.Min(_totalFrames - 1, pauseOnFrame + lateMarginFrames);
    //    bool inWindow = frame >= lower && frame <= upper;

    //    if (!_pausedThisCycle && inWindow)
    //    {
    //        _pausedThisCycle = true;
    //        _animPaused = true;
    //        _cooldownT = Mathf.Max(0f, animCooldownSecs);
    //        cardAnim.speed = 0f;

    //        if (!gate) FireChosen();
    //        if (verboseLogs) Debug.Log($"[CardDeckAnimator] Pause @ frame {frame} for {_cooldownT:0.###}s");
    //    }

    //    HandleResumeRamp(dt);
    //}

    // ---------------- helpers ----------------

    private void StartResumeRamp()
    {
        _rampingOut = (resumeRampTime > 0f);
        _rampT = resumeRampTime;
        cardAnim.speed = _rampingOut ? approachSlowSpeed : 1f;
    }

    private void HandleResumeRamp(float dt)
    {
        if (!_rampingOut || !cardAnim) return;
        _rampT -= dt;
        float t = 1f - Mathf.Clamp01(_rampT / Mathf.Max(0.0001f, resumeRampTime));
        cardAnim.speed = Mathf.Lerp(approachSlowSpeed, 1f, t);
        if (_rampT <= 0f) { _rampingOut = false; cardAnim.speed = 1f; }
    }

    private void FireChosen()
    {
        var chosen = _currentCycleCard ?? PickRandomCard();
        Debug.Log("_currentCycleCard "+_currentCycleCard);
        if (chosen == null) return;

        if (revertMaterialAfterFire && defaultBackSprite)
        {
            _materialSetVersion++;
            int v = _materialSetVersion;
            StartCoroutine(RevertDeckMaterialAfterDelay(v));
        }

        Bullet.Fire(chosen, transform.position);
        _lastFired = chosen;
        if (verboseLogs) Debug.Log($"[CardDeckAnimator] Fired: {chosen.name}");
    }

    private System.Collections.IEnumerator RevertDeckMaterialAfterDelay(int version)
    {
        float t = 0f;
        while (t < revertDelay)
        {
            if (version != _materialSetVersion) yield break;
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
        if (version != _materialSetVersion) yield break;

        if (defaultBackSprite)
            SetDeckMaterialFromSprite(GetDeckMaterial(), defaultBackSprite);
    }

    private void RebuildPool()
    {
        _pool.Clear();
        if (cards != null) foreach (var c in cards) if (c) _pool.Add(c);
        if (verboseLogs) Debug.Log($"[CardDeckAnimator] Pool={_pool.Count}");
    }

    private CardSO PickRandomCard()
    {
        if (_pool.Count == 0) return null;
        if (!avoidImmediateRepeat || _lastFired == null || _pool.Count == 1)
            return _pool[Random.Range(0, _pool.Count)];
        for (int i = 0; i < 4; i++)
        {
            var cand = _pool[Random.Range(0, _pool.Count)];
            if (cand != _lastFired) return cand;
        }
        return _pool[Random.Range(0, _pool.Count)];
    }

    private void ResolveCardAnimAndFace(bool findFace)
    {
        if (!cardAnim)
        {
            if (!string.IsNullOrEmpty(cardAnimChildName))
            {
                var child = transform.Find(cardAnimChildName);
                if (child) cardAnim = child.GetComponent<Animator>();
            }
            if (!cardAnim) cardAnim = GetComponentInChildren<Animator>(true);
        }
        if (!findFace || !cardAnim) return;

        if (!_cardFaceRenderer)
        {
            var t = FindChildRecursive(cardAnim.transform, "Card_face");
            if (t) _cardFaceRenderer = t.GetComponent<Renderer>();
            if (verboseLogs && !_cardFaceRenderer)
                Debug.LogWarning("[CardDeckAnimator] 'Card_face' renderer not found.");
        }
    }

    private Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindChildRecursive(c, name);
            if (r) return r;
        }
        return null;
    }

    private Material GetDeckMaterial()
    {
        switch (deckType)
        {
            case DeckType.Inventor: return InvCardBackMat;
            case DeckType.Wizard:   return WizCardBackMat;
            case DeckType.Samurai:  return SamCardBackMat;
            default:                return InvCardBackMat;
        }
    }

    private void EnsureRendererUsesDeckMaterial()
    {
        if (!_cardFaceRenderer) return;
        var mat = GetDeckMaterial();
        if (mat && _cardFaceRenderer.sharedMaterial != mat)
        {
            _cardFaceRenderer.sharedMaterial = mat;
            _cardFaceRenderer.SetPropertyBlock(null);
        }
    }

    private void ApplyBackToCardFace()
    {
        EnsureRendererUsesDeckMaterial();
        if (defaultBackSprite)
            SetDeckMaterialFromSprite(GetDeckMaterial(), defaultBackSprite);
    }

    private void SetDeckMaterialFromSprite(Material mat, Sprite sprite)
    {
        if (!mat || !sprite || !sprite.texture) return;

        mat.SetTexture(BaseMapProp, sprite.texture);
        mat.SetVector(BaseMapST, new Vector4(1f, 1f, 0f, 0f));
    }

    // --------- NEW: allow manager to replace the deck at runtime ----------
    /// <summary>
    /// Replace the deck with a new list and rebuild the internal random pool
    /// immediately (no enable/disable hack needed).
    /// </summary>
    public void ApplyCardsList(CardSO[] list)
    {
        //cards = list ?? System.Array.Empty<CardSO>();
        //RebuildPool();

        //// Reset cycle state so next Update picks from the new pool right away.
        //_lastFired = null;
        //_currentCycleCard = null;
        //_lastCycle = -1;

        //// Make sure visuals are bound to the selected deck material & show back.
        //ResolveCardAnimAndFace(true);
        //ApplyBackToCardFace();
    }
}
