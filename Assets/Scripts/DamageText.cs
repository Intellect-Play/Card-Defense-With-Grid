using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [Tooltip("How fast the text floats upward")]
    public float floatSpeed = 1f;
    [Tooltip("How long before the text destroys itself")]
    public float lifetime = 1f;

    private TextMeshPro _tmp;
    private float _timer;

    void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();
    }

    /// <summary>
    /// Sets the displayed text and color.
    /// </summary>
    public void Initialize(string message)
    {
        _tmp.text = message;
        _tmp.color = Color.red;
    }

    void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        _timer += Time.deltaTime;
        if (_timer >= lifetime)
            Destroy(gameObject);
    }
}
