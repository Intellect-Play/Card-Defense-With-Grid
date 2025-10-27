using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the text floats upward")]
    public float floatSpeed = 1f;
    [Tooltip("Horizontal drift intensity (sideways movement)")]
    public float horizontalDrift = 0.5f;

    [Header("Lifetime Settings")]
    [Tooltip("How long text stays visible before starting to fade")]
    public float lifetime = 1f;
    [Tooltip("How long the fade-out lasts after lifetime ends")]
    public float fadeOutDuration = 0.5f;

    private TextMeshPro _tmp;
    private float _timer;
    private Vector3 _floatDirection;
    private Color _originalColor;
    private bool _fadingOut = false;
    private float _fadeTimer = 0f;

    void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();
        _originalColor = _tmp.color;

        // Yuxarı + bir az sağa/sola istiqamət
        float randomX = Random.Range(-horizontalDrift, horizontalDrift);
        _floatDirection = new Vector3(randomX, 1f, 0f).normalized;
    }

    /// <summary>
    /// Sets the displayed text and color.
    /// </summary>
    public void Initialize(string message, Color? color = null)
    {
        _tmp.text = message;
        //_tmp.color = color ?? Color.red;
        _originalColor = _tmp.color;
    }

    void Update()
    {
        // Hərəkət (yuxarı və bir az yan)
        transform.position += _floatDirection * floatSpeed * Time.deltaTime;

        // Əgər fade başlamayıbsa — lifetime sayılır
        if (!_fadingOut)
        {
            _timer += Time.deltaTime;
            if (_timer >= lifetime)
                _fadingOut = true; // fade mərhələsinə keç
        }
        else
        {
            // Fade-out mərhələsi
            _fadeTimer += Time.deltaTime;
            float fade = Mathf.Clamp01(1f - (_fadeTimer / fadeOutDuration));

            Color c = _originalColor;
            c.a = fade;
            _tmp.color = c;

            // Fade tamamlandısa, obyekt silinsin
            if (_fadeTimer >= fadeOutDuration)
                Destroy(gameObject);
        }
    }
}
