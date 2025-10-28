using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class FollowMouseImage : MonoBehaviour, IPointerClickHandler
{
    public float followSpeed = 10f;
    public float scaleAmount = 1.2f;
    public float scaleDuration = 0.15f;

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            rectTransform
                .DOScale(scaleAmount, scaleDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    rectTransform.DOScale(1f, scaleDuration).SetEase(Ease.InBack);
                });
        }
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            Input.mousePosition,
            null,
            out pos);

        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, pos, followSpeed * Time.deltaTime);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //rectTransform
        //    .DOScale(scaleAmount, scaleDuration)
        //    .SetEase(Ease.OutBack)
        //    .OnComplete(() =>
        //    {
        //        rectTransform.DOScale(1f, scaleDuration).SetEase(Ease.InBack);
        //    });
    }
}
