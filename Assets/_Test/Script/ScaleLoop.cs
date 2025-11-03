using UnityEngine;
using DG.Tweening;

public class ScaleLoop : MonoBehaviour
{
    public RectTransform target;   // UI obyekti
    public float scaleUp = 1.2f;   // böyümə dərəcəsi
    public float duration = 0.6f;  // bir tam dövrün müddəti

    private Tween scaleTween;

    void Start()
    {
        if (target == null)
            target = GetComponent<RectTransform>();

        StartScaleLoop();
    }

    void StartScaleLoop()
    {
        // Mövcud tweeni dayandır (əgər varsa)
        scaleTween?.Kill();

        // Sonsuz şəkildə böyüyüb-kiçilmə effekti
        scaleTween = target.DOScale(scaleUp, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo).SetUpdate(true); ;
    }

    void OnDisable()
    {
        scaleTween?.Kill();
    }
}
