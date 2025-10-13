using UnityEngine;
using DG.Tweening;

public class UIShake : MonoBehaviour
{
    [Header("Shake")]
    public float shakeDuration = 0.45f;
    public float shakeStrength = 18f;
    public int shakeVibrato = 20;
    public float shakeRandomness = 90f;

    [Header("Pulse")]
    public float pulseScale = 1.18f;
    public float pulseDuration = 0.18f;
    public int pulseLoops = 1;
    public float pulseEaseOvershoot = 1.3f;
    RectTransform rt;
    [Header("Sequence")]
    public bool usePunchScale = true;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }
    public void ShakeRect()
    {
        if (rt == null) return;
        rt.DOKill();

        Sequence seq = DOTween.Sequence();

        // PULSE efekti (zoom in-out)
        if (usePunchScale)
        {
            seq.Append(
                rt.DOPunchScale(Vector3.one * (pulseScale - 1f), pulseDuration, 8, 0.8f)
                  .SetEase(Ease.OutBack)
                  .SetUpdate(true) // 👈 works even if timeScale = 0
            );
        }
        else
        {
            seq.Append(rt.DOScale(pulseScale, pulseDuration).SetEase(Ease.OutBack).SetUpdate(true));
            seq.Append(rt.DOScale(1f, pulseDuration * 0.8f).SetEase(Ease.OutQuad).SetUpdate(true));
        }

        seq.AppendInterval(0.02f);

        // Shake efekti
        seq.Append(
            rt.DOShakeAnchorPos(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness, false, true)
              .SetUpdate(true) // 👈 unscaled time
        );

        seq.OnComplete(() =>
        {
            rt.localScale = Vector3.one;
        });

        seq.SetUpdate(true); // 👈 bütün sequence unscaled time ilə getsin
        seq.Play();
    }
}
