using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private DraggableWeapon parent;
    [SerializeField]private TextMeshProUGUI infoText;
    private void Awake()
    {
        parent = GetComponentInParent<DraggableWeapon>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parent == null) return;

        // pointerDrag-ı parent obyektə yönləndir
        eventData.pointerDrag = parent.gameObject;
        parent.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parent == null) return;

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
        if (parent == null) return;

        eventData.pointerDrag = parent.gameObject;
        parent.OnEndDrag(eventData);
    }
}
