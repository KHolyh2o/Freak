using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class StageSelectUI : MonoBehaviour
{
    [Header("UI 구성요소")]
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public GameObject stageButtonPrefab;

    [Header("설정")]
    public int totalStages = 15;
    public float centerScale = 1.2f; // 중앙 아이템 확대 배율
    public float sideScale = 0.8f;   // 주변 아이템 축소 배율
    public float snapSpeed = 10f;    // 스냅 속도 (높을수록 빠름)
    public float buttonSpacing = 200f; // 버튼 간격 (임시, LayoutGroup 사용 시 무시됨)

    private List<RectTransform> stageButtons = new List<RectTransform>();
    private bool isDragging = false;
    private float[] distances; // 각 버튼의 중심까지 거리
    private int selectedStageIndex = 0;

    void Start()
    {
        InitializeStageButtons();
    }

    void Update()
    {
        if (stageButtons.Count == 0) return;

        UpdateButtonScales();

        if (!isDragging)
        {
            SnapToNearest();
        }
    }

    void InitializeStageButtons()
    {
        stageButtons.Clear();

        // Content Panel 아래에 있는 모든 자식 오브젝트를 스테이지 버튼으로 인식
        if (contentPanel.childCount > 0)
        {
            for (int i = 0; i < contentPanel.childCount; i++)
            {
                Transform child = contentPanel.GetChild(i);
                stageButtons.Add(child.GetComponent<RectTransform>());

                // 버튼 컴포넌트가 있다면 클릭 이벤트 연결
                int index = i;
                Button btn = child.GetComponent<Button>();
                if (btn != null)
                {
                    // 기존 이벤트 유지하며 추가
                    btn.onClick.AddListener(() => OnStageClicked(index));
                }
            }
            totalStages = stageButtons.Count; // 실제 자식 개수로 업데이트
        }
        else if (stageButtonPrefab != null) // 자식이 없고 프리팹이 설정되어 있다면 기존 방식대로 생성 (하이브리드 지원)
        {
            CreateStageButtons();
        }

        // 거리 배열 초기화
        if (totalStages > 0)
        {
            distances = new float[totalStages];
        }
    }

    void CreateStageButtons()
    {
        for (int i = 0; i < totalStages; i++)
        {
            GameObject btnObj = Instantiate(stageButtonPrefab, contentPanel);
            btnObj.name = $"Stage_{i}";
            var textComp = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComp != null) textComp.text = (i + 1).ToString();

            int index = i;
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnStageClicked(index));
            }
            stageButtons.Add(btnObj.GetComponent<RectTransform>());
        }
    }

    void UpdateButtonScales()
    {
        float centerX = scrollRect.transform.position.x;
        float minDistance = float.MaxValue;
        int minIndex = -1;

        // 1. 가장 가까운(중앙에 있는) 버튼 찾기
        for (int i = 0; i < stageButtons.Count; i++)
        {
            float dist = Mathf.Abs(centerX - stageButtons[i].position.x);
            if (dist < minDistance)
            {
                minDistance = dist;
                minIndex = i;
            }
        }

        // 2. 스케일 적용 (가장 가까운 놈만 centerScale, 나머지는 sideScale)
        for (int i = 0; i < stageButtons.Count; i++)
        {
            float targetScale = (i == minIndex) ? centerScale : sideScale;
            stageButtons[i].localScale = Vector3.Lerp(stageButtons[i].localScale, Vector3.one * targetScale, Time.deltaTime * 10f);
        }
    }

    void SnapToNearest()
    {
        // Viewport(또는 ScrollRect)의 중앙 월드 좌표
        float viewportCenter = scrollRect.transform.position.x; 

        // 가장 가까운 버튼 찾기
        float minDistance = float.MaxValue;
        int minIndex = 0;

        for (int i = 0; i < stageButtons.Count; i++)
        {
            float dist = Mathf.Abs(viewportCenter - stageButtons[i].position.x);
            if (dist < minDistance)
            {
                minDistance = dist;
                minIndex = i;
            }
        }

        selectedStageIndex = minIndex;

        // 목표: 선택된 버튼(world X)을 뷰포트 중앙(world X)으로 이동
        // 현재 오차 = ViewportCenter - ButtonCenter
        // 이 오차만큼 Content를 이동시켜야 함
        float diff = viewportCenter - stageButtons[minIndex].position.x;

        // Content의 현재 위치에서 오차를 더함 (Lerp 이용)
        float targetX = contentPanel.anchoredPosition.x + diff;
        
        // *중요* 만약 오차가 아주 작으면(스냅 완료) 계산 중지 (떨림 방지)
        if (Mathf.Abs(diff) < 0.1f) 
        {
            contentPanel.anchoredPosition = new Vector2(targetX, contentPanel.anchoredPosition.y);
            return;
        }

        Vector2 newPos = contentPanel.anchoredPosition;
        newPos.x = Mathf.Lerp(contentPanel.anchoredPosition.x, targetX, Time.deltaTime * snapSpeed);
        contentPanel.anchoredPosition = newPos;
    }

    // ScrollRect 이벤트 (EventTrigger 등으로 연결 필요)
    public void OnBeginDrag()
    {
        isDragging = true;
    }

    public void OnEndDrag()
    {
        isDragging = false;
    }

    public void OnStageClicked(int index)
    {
        // 중앙에 있는 게 아니라면 클릭 시 중앙으로 이동 (선택 효과)
        if (index != selectedStageIndex)
        {
            // 사실상 SnapToNearest가 처리하므로, 여기서는 selected만 바꾸고 드래그 해제 효과를 줄 수도 있음
            // 하지만 현재는 클릭 시 바로 로드하거나 그냥 두거나.
            // 여기서는 단순히 로그만 찍고, 중앙에 왔을 때 '플레이' 버튼을 따로 두는 게 일반적.
            // 혹은 더블 클릭이나, 중앙에 온 상태에서 클릭해야 로드되도록.
            
            // 일단은 바로 로드하도록 구현 (요청사항엔 명시 안되었지만 일반적인 흐름)
            LoadStage(index);
        }
        else
        {
             LoadStage(index);
        }
    }

    void LoadStage(int index)
    {
        // 씬 이름 규칙에 따라 로드 (예: Stage_00, Stage_01 ...)
        string sceneName = $"Stage_{index:D2}"; // 0 -> "Stage_00"
        
        // 씬이 존재하는지 확인은 못하지만 로드 시도
        Debug.Log($"Loading Scene: {sceneName}");
        
        // 실제 로드 (빌드 세팅에 씬이 있어야 함)
        // SceneManager.LoadScene(sceneName); 
    }
}
