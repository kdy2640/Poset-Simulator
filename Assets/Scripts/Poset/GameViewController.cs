using UnityEngine;
using UnityEngine.EventSystems;

public class GameViewController : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private RectTransform graphPanel;

    private Vector2 lastMousePos;
    private void Awake()
    {
        
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 드래그 시작 시 마우스 위치 저장
        lastMousePos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 현재 위치와 이전 위치 차이
        Vector2 delta = eventData.position - lastMousePos;

        // 패널 이동
        graphPanel.anchoredPosition += delta;

        // 현재 위치를 다음 기준점으로 갱신
        lastMousePos = eventData.position;
    }
    private void Update()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f) // 미세 입력 무시
        {
            float scale = Mathf.Clamp(graphPanel.localScale.x + scroll * 0.1f, 0.5f, 2f);
            graphPanel.localScale = new Vector3(scale, scale, 1);
        }
    }

}
