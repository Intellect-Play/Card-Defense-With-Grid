using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyBehaviour : MonoBehaviour
{
    public static event Action<EnemyBehaviour> OnEnemyDestroyed;

    public Transform EnemyParent;
    [Header("Stats")]
    public float health;
    public int attack;
    public int rewardGold;

    [Header("Walking Bubble Effect (Scale)")]
    public Vector2 bubbleUpScale = new Vector2(0.95f, 1.05f);   // havada (yuxarıda) stretch
    public Vector2 bubbleDownScale = new Vector2(1.05f, 0.95f); // yerə dəyəndə squash
    public float bubbleScaleTime = 0.2f;                     // keçid müddəti


    [Header("Movement")]
    public float moveSpeed = 1f;

    // ── WALKING: tilt YOX, yalnız Y-bob ────────────────────────────────────────────
    [Header("Walking Y-Bob (no tilt)")]
    public float walkBobOffsetY = 0.2f;   // yuxarı-aşağı yüngül zıplama
    public float walkBobHalfTime = 0.2f;  // yarım period

    // ── Sword-like Fortress Swing (Z-axis) ─────────────────────────────────────────
    [Header("Sword-like Fortress Swing (Z-axis)")]
    public float attackWindupAngleZ = 28f;
    public float attackHitAngleZ = 62f;
    public float attackOvershootZ = 6f;
    public float attackRecoilBackZ = 7f;

    public float windupTime = 0.12f;
    public float hitTime = 0.06f;
    public float overshootTime = 0.04f;
    public float recoilTime = 0.08f;
    public float settleTime = 0.10f;

    [Header("Cadence")]
    public float fortressHitDelay = 0.5f;

    [Header("Damage Numbers")]
    public GameObject damageTextPrefab;
    public Vector3 damageTextOffset = new Vector3(0, 1f, 0);

    [Header("Hit Feedback")]
    public float hitScaleUpFactor = 1.12f;
    public float hitScaleTime = 0.08f;

    // ── COIN UI (no prefab; coins spawn in scene UI hierarchy, not under enemy) ──
    [Header("Coin Fly (UI)")]
    public Sprite coinSprite;
    public Vector2Int coinDropCountRange = new Vector2Int(2, 4);
    public float uiScatterRadius = 56f;
    public float coinPopHeightPx = 32f;
    public float coinPopTime = 0.22f;
    public Vector2 coinFlyDelayRange = new Vector2(0.05f, 0.25f);
    public float coinFlyTime = 0.65f;
    public float coinFlyScaleUp = 1.15f;
    public float coinUISize = 36f;
    public float coinDeathTimeout = 1.75f;

    // Scene deps injected at runtime (none of these are children of the enemy)
    [HideInInspector] public UIManager uiManager;
    [HideInInspector] public Canvas coinCanvas;
    [HideInInspector] public RectTransform coinFlyContainer;
    [HideInInspector] public RectTransform coinBarRect;
    [HideInInspector] public Camera worldCamera;

    // --- state ---
    private bool _isAttacking;
    private int _hitScaleTweenId_1 = -1;
    private int _hitScaleTweenId_2 = -1;
    private int _yBobTweenId = -1;
    private Coroutine _attackLoopCo;
    private bool _isProcessingHit = false;
    private bool _dead = false;   // finalized destroy
    private bool _dying = false;   // lethal hit received, ignore further damage

    // movement target (the "general parent") and locks
    private Transform _owner;                // immediate parent if exists; else this
    private float _lockX;
    private float _lockZ;
    private Rigidbody2D _rb2d;
    private RigidbodyConstraints2D _savedConstraints;

    // originals / children
    private Vector3 _originalScale;
    private Vector3 _originalLocalPos;
    private GameObject _hitVFXObject;   // Enemy/hit_vfx (optional)

    public Action<int> onFortressDamage;

    void Awake()
    {
        // Move the general parent along with us (no rotation applied to parent)
        _owner = transform.parent != null ? transform.parent : transform;

        _lockX = _owner.position.x;
        _lockZ = _owner.position.z;
        _originalScale = transform.localScale;
        _originalLocalPos = transform.localPosition;

        // Rigidbody is expected on the parent (owner)
        _rb2d = _owner.GetComponent<Rigidbody2D>();
        if (_rb2d)
        {
            _savedConstraints = _rb2d.constraints;
            _rb2d.constraints = _savedConstraints | RigidbodyConstraints2D.FreezePositionX;
        }

        var vfxT = transform.Find("hit_vfx");
        if (vfxT) { _hitVFXObject = vfxT.gameObject; _hitVFXObject.SetActive(false); }
    }

    void Start()
    {
        if (!worldCamera) worldCamera = Camera.main;

        // hər düşmən üçün yarım perioda kiçik random əlavə
        float jitter = UnityEngine.Random.Range(-0.05f, 0.05f);
        StartContinuousYBob(walkBobOffsetY, walkBobHalfTime + jitter);
    }


    void Update()
    {
        if (_dead || _dying) return;

        // Move the general parent in world space; do NOT rotate it.
        if (!_isAttacking&&!TetrisWeaponManager.isTetrisScene)
            _owner.Translate(Vector3.down * moveSpeed * Time.deltaTime, Space.World);
    }

    void LateUpdate()
    {
        if (_dead) return;

        // Lock owner X/Z to avoid drift
        Vector3 p = _owner.position;
        p.x = _lockX;
        p.z = _lockZ;
        _owner.position = p;

        // Keep RB velocity X at zero if present
        if (_rb2d)
            _rb2d.velocity = new Vector2(0f, _rb2d.velocity.y);
    }

    void OnDestroy()
    {
        if (_rb2d) _rb2d.constraints = _savedConstraints;
        if (_hitScaleTweenId_1 != -1) LeanTween.cancel(gameObject, _hitScaleTweenId_1);
        if (_hitScaleTweenId_2 != -1) LeanTween.cancel(gameObject, _hitScaleTweenId_2);
        if (_yBobTweenId != -1) LeanTween.cancel(gameObject, _yBobTweenId);
    }

    // ── Triggers ───────────────────────────────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (_dead || _dying) return;
        if (other.CompareTag("Fortress"))
            StartAttack();
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (_dead || _dying) return;
        if (other.CompareTag("Fortress"))
            StopAttack();
    }

    // ── Y-Bob (localPosition Y ping-pong) ─────────────────────────────────────────
    private void StartContinuousYBob(float offsetY, float halfTime)
    {
        if (_dead || _dying || _isProcessingHit) return;
        if (_yBobTweenId != -1) LeanTween.cancel(gameObject, _yBobTweenId);

        // Reset local position
        transform.localPosition = _originalLocalPos;

        float upY = _originalLocalPos.y + Mathf.Abs(offsetY);
        float dnY = _originalLocalPos.y - Mathf.Abs(offsetY);

        var t = LeanTween.moveLocalY(gameObject, upY, halfTime)
      .setEaseInOutSine()
      .setOnStart(() =>
      {
          // Yuxarı qalxanda – stretch (x=0.9, y=1.1)
          LeanTween.scale(gameObject, new Vector3(
              bubbleUpScale.x * _originalScale.x,
              bubbleUpScale.y * _originalScale.y,
              _originalScale.z
          ), bubbleScaleTime).setEaseInOutSine();
      })
      .setOnComplete(() =>
      {
          if (_dead || _dying || _isProcessingHit) return;
          var t2 = LeanTween.moveLocalY(gameObject, dnY, halfTime)
              .setEaseInOutSine()
              .setOnStart(() =>
              {
                  // Aşağı düşəndə – squash (x=1.1, y=0.9)
                  LeanTween.scale(gameObject, new Vector3(
                      bubbleDownScale.x * _originalScale.x,
                      bubbleDownScale.y * _originalScale.y,
                      _originalScale.z
                  ), bubbleScaleTime).setEaseInOutSine();
              })
              .setOnComplete(() =>
              {
                  // Tamamlandıqdan sonra yenidən loop davam etsin
                  StartContinuousYBob(offsetY, halfTime);
              });
          _yBobTweenId = t2.id;
      }).setIgnoreTimeScale(false); 

      
    }

    private void StopYBob()
    {
        if (_yBobTweenId != -1)
        {
            LeanTween.cancel(gameObject, _yBobTweenId);
            _yBobTweenId = -1;
        }
        // Local Y-ni və scale-i resetlə
        var lp = transform.localPosition;
        lp.y = _originalLocalPos.y;
        transform.localPosition = lp;
        transform.localScale = _originalScale;
    }


    // ── Fortress attack (rotation only on this child) ──────────────────────────────
    private void StartAttack()
    {
        if (_dead || _dying || _isAttacking) return;
        _isAttacking = true;
        StopYBob(); // yürüş bob-u dursun
        _attackLoopCo = StartCoroutine(AttackLoop());
    }
    private void StopAttack()
    {
        if (!_isAttacking) return;
        _isAttacking = false;
        if (_attackLoopCo != null) { StopCoroutine(_attackLoopCo); _attackLoopCo = null; }
        if (!_dead && !_dying) StartContinuousYBob(walkBobOffsetY, walkBobHalfTime);
    }
    private IEnumerator AttackLoop()
    {
        while (_isAttacking && !_dead && !_dying)
        {
            yield return SwordSwingCycle();
            onFortressDamage?.Invoke(attack);
            if (fortressHitDelay > 0f) yield return new WaitForSeconds(fortressHitDelay);
        }
    }
    private IEnumerator SwordSwingCycle()
    {
        float baseZ = 0f;
        float windupZ = -Mathf.Abs(attackWindupAngleZ);
        float hitZ = +Mathf.Abs(attackHitAngleZ);
        float overZ = hitZ + Mathf.Max(0f, attackOvershootZ);
        float recoilZ = -Mathf.Abs(attackRecoilBackZ);

        LeanTween.rotateZ(gameObject, windupZ, windupTime).setEaseInSine();
        yield return new WaitForSeconds(windupTime);

        LeanTween.rotateZ(gameObject, hitZ, hitTime).setEaseOutExpo();
        yield return new WaitForSeconds(hitTime);

        if (attackOvershootZ > 0f && overshootTime > 0f)
        {
            LeanTween.rotateZ(gameObject, overZ, overshootTime).setEaseOutQuad();
            yield return new WaitForSeconds(overshootTime);
        }

        LeanTween.rotateZ(gameObject, recoilZ, recoilTime).setEaseOutQuad();
        yield return new WaitForSeconds(recoilTime);

        LeanTween.rotateZ(gameObject, baseZ, settleTime).setEaseOutSine();
        yield return new WaitForSeconds(settleTime);
    }

    // ── Damage ─────────────────────────────────────────────────────────────────────
    public void ApplyDamage(float amount)
    {
        if (_dead || _dying) return;

        float newHealth = health - amount;
        if (newHealth <= 0)
        {
            _dying = true;
            health = 0;
            ShowDamage(amount);

            PrepareForDeath();
            StartCoroutine(DieWithCoinsUI());
            return;
        }

        PlayHitFeedback();
        health = newHealth;
        ShowDamage(amount);
    }

    private void PrepareForDeath()
    {
        StopAttack();
        StopYBob();

        var col = GetComponent<Collider2D>(); if (col) col.enabled = false;
        if (_rb2d) _rb2d.simulated = false;

        // set both child and parent to ignore raycast if needed
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        if (_owner != null) _owner.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    private IEnumerator DieWithCoinsUI()
    {
        if (_hitVFXObject != null)
        {
            _hitVFXObject.SetActive(true);
            yield return new WaitForSeconds(0.08f);
            _hitVFXObject.SetActive(false);
        }

        int active = SpawnUICoins(); // spawns in scene UI container, not under enemy
        float t = 0f;
        while (active > 0 && t < coinDeathTimeout)
        {
            yield return null;
            t += Time.unscaledDeltaTime;
            active = _trackedRemainingCoins;
        }

        DestroySelf();
    }

    private void DestroySelf()
    {
        if (_dead) return;
        _dead = true;

        try { OnEnemyDestroyed?.Invoke(this); } catch { }

        // Destroy the general parent (owner). If there is no parent, destroy this object.
        if (_owner != null && _owner != transform)
        {
            Destroy(_owner.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── UI Coins (spawned under coinFlyContainer in the UI hierarchy) ──────────────
    private int _trackedRemainingCoins = 0;

    private int SpawnUICoins()
    {
        if (coinSprite == null || coinCanvas == null || coinFlyContainer == null || coinBarRect == null || uiManager == null)
            return 0;

        if (!worldCamera) worldCamera = Camera.main;

        // Use the owner's world position as the coin start (not the rotating child)
        Vector3 screenStart = worldCamera
            ? worldCamera.WorldToScreenPoint(_owner.position)
            : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        Vector2 startAnchored = ScreenToAnchored(coinFlyContainer, screenStart, coinCanvas);
        Camera uiCam = (coinCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : coinCanvas.worldCamera;
        Vector3 coinBarScreen = RectTransformUtility.WorldToScreenPoint(uiCam, coinBarRect.position);
        Vector2 targetAnchored = ScreenToAnchored(coinFlyContainer, coinBarScreen, coinCanvas);

        int count = Mathf.Clamp(UnityEngine.Random.Range(coinDropCountRange.x, coinDropCountRange.y + 1), 0, 40);
        if (count <= 0) return 0;

        int remaining = count;
        _trackedRemainingCoins = remaining;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("CoinFlyUI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();
            img.sprite = coinSprite;
            img.raycastTarget = false;

            rect.SetParent(coinFlyContainer, false);
            rect.sizeDelta = new Vector2(coinUISize, coinUISize);
            rect.localScale = Vector3.one;
            rect.anchoredPosition = startAnchored;

            Vector2 rand = UnityEngine.Random.insideUnitCircle * uiScatterRadius;
            Vector2 scatterTarget = startAnchored + rand + new Vector2(0f, coinPopHeightPx);
            LeanTween.move(rect, (Vector3)scatterTarget, coinPopTime).setEaseOutQuad();

            float delay = UnityEngine.Random.Range(coinFlyDelayRange.x, coinFlyDelayRange.y);
            LeanTween.delayedCall(go, delay, () =>
            {
                if (go == null) { remaining--; _trackedRemainingCoins = remaining; return; }

                Vector3 startScale = rect.localScale;
                LeanTween.scale(rect, startScale * coinFlyScaleUp, coinFlyTime * 0.5f).setEaseOutQuad();

                LeanTween.move(rect, (Vector3)targetAnchored, coinFlyTime)
                    .setEaseInOutQuad()
                    .setOnComplete(() =>
                    {
                        int g = PlayerPrefs.GetInt("gold", 0) + 1;
                        PlayerPrefs.SetInt("gold", g);
                        PlayerPrefs.Save();
                        uiManager.SetCoins(g);

                        remaining--;
                        _trackedRemainingCoins = remaining;
                        Destroy(go);
                    });
            });
        }

        return count;
    }

    private static Vector2 ScreenToAnchored(RectTransform container, Vector3 screenPos, Canvas canvas)
    {
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPos, cam, out Vector2 local);
        return local;
    }

    // ── Feedback ───────────────────────────────────────────────────────────────────
    private void ShowDamage(float amount)
    {
        if (damageTextPrefab == null) return;

        // Spawn relative to the owner so the text follows the moving root
        Vector3 spawnPos = _owner.position + damageTextOffset;
        var go = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
        var dt = go.GetComponent<DamageText>();
        if (dt != null) dt.Initialize($"-{amount}");
    }

    private void PlayHitFeedback()
    {
        if (_dead || _dying) return;

        _isProcessingHit = true;
        StopYBob(); // hit zamanı bob dursun

        if (_hitVFXObject != null)
        {
            _hitVFXObject.SetActive(true);
            CancelInvoke(nameof(HideHitVFX));
            Invoke(nameof(HideHitVFX), 0.1f);
        }

        Vector3 upScale = _originalScale * Mathf.Max(hitScaleUpFactor, 1.0001f);

        if (_hitScaleTweenId_1 != -1) LeanTween.cancel(gameObject, _hitScaleTweenId_1);
        if (_hitScaleTweenId_2 != -1) LeanTween.cancel(gameObject, _hitScaleTweenId_2);

        _hitScaleTweenId_1 = LeanTween.scale(gameObject, upScale, hitScaleTime)
            .setEaseOutQuad()
            .setOnComplete(() =>
            {
                _hitScaleTweenId_2 = LeanTween.scale(gameObject, _originalScale, hitScaleTime)
                    .setEaseInQuad()
                    .setOnComplete(() =>
                    {
                        _isProcessingHit = false;
                        if (!_isAttacking && !_dead && !_dying)
                            StartContinuousYBob(walkBobOffsetY, walkBobHalfTime);
                    }).id;
            }).id;
    }

    private void HideHitVFX()
    {
        if (_hitVFXObject != null)
            _hitVFXObject.SetActive(false);
    }
}