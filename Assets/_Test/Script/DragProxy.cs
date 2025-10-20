using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class DragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private DraggableWeapon parent;
    [SerializeField]private TextMeshProUGUI infoText;
    public RectTransform ChildImage;
    public Tween _breathTween;
    public Image image;
    public GameObject GreenImage;

    public float moveAmount = 10f;
    public float duration = .7f;
    private void Awake()
    {
        parent = GetComponentInParent<DraggableWeapon>();
    }
    public void GetImage(Sprite sprite)
    {
        image.sprite = sprite;
    }
    public void GetActivated(bool isActive)
    {
        if (GreenImage != null)
            GreenImage.SetActive(isActive);
        ChildImage.gameObject.SetActive(isActive);
        if(isActive)
        {
            _breathTween = ChildImage.DOAnchorPosY(ChildImage.anchoredPosition.y + moveAmount, duration)
                          .SetLoops(-1, LoopType.Yoyo)
                          .SetEase(Ease.InOutSine);
        }
        else
        {
            if (_breathTween != null && _breathTween.IsActive())
            {
                _breathTween.Kill();
            }
        }
        
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parent == null || !TetrisWeaponManager.isTetrisScene) return;

        // pointerDrag-ı parent obyektə yönləndir
        eventData.pointerDrag = parent.gameObject;
        parent.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parent == null || !TetrisWeaponManager.isTetrisScene) return;

        eventData.pointerDrag = parent.gameObject;
        parent.OnDrag(eventData);
    }
    public void LevelUpgrade(int level)
    {
        if (infoText != null)
        {
            infoText.text = level.ToString();
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (parent == null || !TetrisWeaponManager.isTetrisScene) return;

        eventData.pointerDrag = parent.gameObject;
        parent.OnEndDrag(eventData);
    }
}
