using System.Collections;
using UnityEngine;

public class RockBedManager : MonoBehaviour
{
    [Header("Target")]
    public SpriteRenderer spriteRenderer; // auto-fill if empty

    [Header("Colors & Pulse")]
    public Color colorA = Color.white;
    public Color colorB = new Color32(0xDB,0xDB,0xDB,0xFF);
    public float scaleAmplitude = 0.08f;  // ±8%

    [Header("Timings")]
    public Vector2 delayRange = new Vector2(0.08f, 0.25f); // hold between swaps
    public float   lerpTime   = 0.12f;                     // blend time
    public bool    useUnscaledTime = false;

    [Header("Collision")]
    public string enemyTag = "Enemy";

    [Header("Hit VFX")]
    [Tooltip("If empty, children named 'hit_vfx' will be auto-found.")]
    public GameObject[] hitVfx;
    public bool  autoFindVfx   = true;
    public float vfxBlinkPeriod = 0.5f; // toggle every 0.5s

    // internals
    Color   _origColor;
    Vector3 _origScale;
    int     _hits;
    Coroutine _flickerCo, _vfxBlinkCo;

    void Awake()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);

        _origColor  = spriteRenderer ? spriteRenderer.color : Color.white;
        _origScale  = transform.localScale;

        delayRange.x = Mathf.Max(0f, delayRange.x);
        delayRange.y = Mathf.Max(delayRange.x, delayRange.y);
        lerpTime     = Mathf.Max(0f, lerpTime);
        scaleAmplitude = Mathf.Max(0f, scaleAmplitude);
        vfxBlinkPeriod = Mathf.Max(0.05f, vfxBlinkPeriod);

        if (autoFindVfx && (hitVfx == null || hitVfx.Length == 0))
        {
            var list = new System.Collections.Generic.List<GameObject>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t != transform && t.name.ToLower().StartsWith("hit_vfx"))
                    list.Add(t.gameObject);
            hitVfx = list.ToArray();
        }

        SetVfxActive(false);
    }

    void OnDisable()
    {
        StopAllEffects();
        SetVfxActive(false);
    }

    // ---- Collision (3D) ----
    void OnTriggerEnter(Collider o)    => Enter(o);
    void OnTriggerExit (Collider o)    => Exit (o);
    void OnCollisionEnter(Collision c) => Enter(c.collider);
    void OnCollisionExit (Collision c) => Exit (c.collider);

    // ---- Collision (2D) ----
    void OnTriggerEnter2D(Collider2D o)    => Enter(o);
    void OnTriggerExit2D (Collider2D o)    => Exit (o);
    void OnCollisionEnter2D(Collision2D c) => Enter(c.collider);
    void OnCollisionExit2D (Collision2D c) => Exit (c.collider);

    void Enter(Component c)
    {
        if (c && c.CompareTag(enemyTag))
        {
            if (++_hits == 1)
            {
                if (spriteRenderer) _flickerCo = StartCoroutine(FlickerLoop());
                _vfxBlinkCo = StartCoroutine(VfxBlinkLoop());
            }
        }
    }

    void Exit(Component c)
    {
        if (c && c.CompareTag(enemyTag))
        {
            if (--_hits <= 0)
            {
                _hits = 0;
                StopAllEffects();
                SetVfxActive(false);
            }
        }
    }

    void StopAllEffects()
    {
        if (_flickerCo != null) { StopCoroutine(_flickerCo); _flickerCo = null; }
        if (_vfxBlinkCo != null) { StopCoroutine(_vfxBlinkCo); _vfxBlinkCo = null; }
        if (spriteRenderer)
        {
            spriteRenderer.color = _origColor;
            transform.localScale = _origScale;
        }
    }

    IEnumerator FlickerLoop()
    {
        bool toB = true, big = true;
        while (_hits > 0 && spriteRenderer)
        {
            Color   targetCol   = toB ? colorB : colorA;
            Vector3 targetScale = _origScale * (big ? (1f + scaleAmplitude) : (1f - scaleAmplitude));

            float t = 0f;
            Color   startCol   = spriteRenderer.color;
            Vector3 startScale = transform.localScale;

            while (t < lerpTime && _hits > 0)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                t += dt;
                float k = lerpTime <= 0f ? 1f : Mathf.Clamp01(t / lerpTime);
                spriteRenderer.color = Color.Lerp(startCol,   targetCol,   k);
                transform.localScale = Vector3.Lerp(startScale,targetScale,k);
                yield return null;
            }

            float hold = Random.Range(delayRange.x, delayRange.y);
            float w = 0f;
            while (w < hold && _hits > 0)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                w += dt; yield return null;
            }

            toB = !toB; big = !big;
        }

        if (spriteRenderer)
        {
            spriteRenderer.color = _origColor;
            transform.localScale = _origScale;
        }
    }

    IEnumerator VfxBlinkLoop()
    {
        bool on = true;
        while (_hits > 0)
        {
            SetVfxActive(on);
            on = !on;

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(vfxBlinkPeriod);
            else
                yield return new WaitForSeconds(vfxBlinkPeriod);
        }
        SetVfxActive(false);
    }

    void SetVfxActive(bool on)
    {
        if (hitVfx == null) return;
        foreach (var go in hitVfx)
        {
            if (!go) continue;
            go.SetActive(on);
        }
    }
}
