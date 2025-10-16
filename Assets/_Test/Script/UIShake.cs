using UnityEngine;
using DG.Tweening;

public class UIShake : MonoBehaviour
{
    private Tween _breathTween;

    public void StartBreathing(float scaleAmount = 1.08f, float duration = .5f)
    {
        // Əvvəlki animasiyaları təmizlə
        transform.DOKill();

        // Sonsuz loop: böyüyür və geri qayıdır
        _breathTween = transform
            .DOScale(Vector3.one * scaleAmount, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)   // sonsuz təkrarlansın
            .SetUpdate(true);              // timeScale=0 olsa da işləsin
    }

    public void StopBreathing()
    {
        if (_breathTween != null && _breathTween.IsActive())
            _breathTween.Kill();

        // ölçünü normala qaytar
        transform.localScale = Vector3.one;
    }
}
