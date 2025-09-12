using UnityEngine;

public class ButtonPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    public float scaleUp = 1.2f;      // Max scale
    public float pulseSpeed = 0.5f;   // How fast it pulses

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
        // Start pulsing immediately
        LeanTween.scale(gameObject, originalScale * scaleUp, pulseSpeed)
                 .setEaseInOutSine()
                 .setLoopPingPong();
    }
}
