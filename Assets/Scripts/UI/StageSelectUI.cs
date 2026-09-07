using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
public class StageSelectUI : MonoBehaviour
{
    [Header("UI 구성요소")]
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public GameObject stageButtonPrefab;

    [Header("스테이지 설정")]
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
        AddPaddingForCentering();
        SetupDragEvents(); // 드래그 이벤트 자동 연결
    }

    void SetupDragEvents()
    {
        if (scrollRect == null) return;

        // 기존에 잘못 추가된 EventTrigger가 있다면 제거 (스크롤 먹통 원인)
        EventTrigger trigger = scrollRect.gameObject.GetComponent<EventTrigger>();
        if (trigger != null) Destroy(trigger);

        // 안전한 커스텀 드래그 리스너 부착
        DragListener listener = scrollRect.gameObject.GetComponent<DragListener>();
        if (listener == null) listener = scrollRect.gameObject.AddComponent<DragListener>();

        listener.scrollRect = scrollRect;
        listener.onBeginDrag = () => { isDragging = true; };
        listener.onEndDrag = () => { isDragging = false; };
    }

    void AddPaddingForCentering()
    {
        HorizontalLayoutGroup layout = contentPanel.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            // Viewport(스크롤 영역)의 절반 크기를 구함
            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
            float padding = viewport.rect.width / 2f;

            // 버튼 자체의 너비 절반을 빼줘야 버튼의 중심이 화면 중앙에 정확히 위치함
            if (stageButtons.Count > 0)
            {
                padding -= (stageButtons[0].rect.width * stageButtons[0].localScale.x) / 2f;
            }

            // 레이아웃의 좌우 여백 설정
            layout.padding.left = Mathf.Max(0, Mathf.RoundToInt(padding));
            layout.padding.right = Mathf.Max(0, Mathf.RoundToInt(padding));
            
            // ★ 수직 중앙 정렬 강제 적용 (위로 달라붙는 현상 방지)
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandHeight = false; // 높이를 강제로 늘리지 않음
            layout.childControlHeight = false;     // 자식의 원래 높이 존중
            
            // ★ 스크롤뷰 고무줄(튕김) 현상 방지: Content가 자기 크기를 제대로 알도록 설정
            ContentSizeFitter fitter = contentPanel.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = contentPanel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained; // 수직은 자유롭게

            // Content 자신의 앵커를 중앙으로 맞춤
            contentPanel.pivot = new Vector2(0.5f, 0.5f);
            contentPanel.anchorMin = new Vector2(0f, 0.5f);
            contentPanel.anchorMax = new Vector2(1f, 0.5f);
            contentPanel.anchoredPosition = new Vector2(contentPanel.anchoredPosition.x, 0f);

            // 레이아웃 즉시 업데이트
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel);
        }
    }

    void Update()
    {
        if (stageButtons.Count == 0) return;

        UpdateButtonScales();

        // 사용자가 터치 중(isDragging)이 아니며, 스크롤 관성 속도가 충분히 줄어들었을 때만 중앙 정렬(Snap) 실행
        if (!isDragging && Mathf.Abs(scrollRect.velocity.x) < 50f)
        {
            SnapToNearest();
        }
    }

    void InitializeStageButtons()
    {
        stageButtons.Clear();

        // 1. Resources/MapData 안의 CSV 파일 개수를 세서 totalStages를 덮어씁니다.
        TextAsset[] mapFiles = Resources.LoadAll<TextAsset>("MapData");
        if (mapFiles != null && mapFiles.Length > 0)
        {
            // 이름순(Stage01, Stage02...) 정렬
            System.Array.Sort(mapFiles, (a, b) => string.Compare(a.name, b.name));
            totalStages = mapFiles.Length;
            Debug.Log($"[StageSelectUI] 발견된 맵 파일 개수: {totalStages}");
        }

        // 2. 스크롤 뷰 안에 에디터에서 미리 배치해둔 더미(미리보기용) 버튼이 있다면 전부 삭제합니다.
        // 이렇게 하면 항상 CSV 파일 개수에 맞춰 새로 100% 자동 생성됩니다.
        for (int i = contentPanel.childCount - 1; i >= 0; i--)
        {
            Destroy(contentPanel.GetChild(i).gameObject);
        }

        // 3. 버튼 자동 생성
        if (stageButtonPrefab != null)
        {
            CreateStageButtons(mapFiles);
        }
        else
        {
            Debug.LogError("[StageSelectUI] Stage Button Prefab이 연결되어 있지 않습니다!");
        }

        // 거리 배열 초기화
        if (totalStages > 0)
        {
            distances = new float[totalStages];
        }
    }

    void CreateStageButtons(TextAsset[] mapFiles)
    {
        MapGenerator originalGenerator = FindObjectOfType<MapGenerator>();
        if (originalGenerator == null)
        {
            Debug.LogWarning("[StageSelectUI] 씬에 MapGenerator가 없어 3D 프리뷰를 생성할 수 없습니다.");
        }

        for (int i = 0; i < totalStages; i++)
        {
            GameObject btnObj = Instantiate(stageButtonPrefab, contentPanel);
            btnObj.name = $"Stage_{i}";
            
            // 텍스트 설정 (목업처럼 Map 1, Map 2)
            var textComp = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComp != null) textComp.text = $"Map {i + 1}";

            // 3D 맵 썸네일(RenderTexture) 설정
            if (originalGenerator != null && MapPreviewRenderer.Instance != null && mapFiles != null && i < mapFiles.Length)
            {
                RenderTexture rt = MapPreviewRenderer.Instance.GeneratePreview(i, mapFiles[i].text, originalGenerator);
                RawImage rawImage = btnObj.GetComponentInChildren<RawImage>();
                if (rawImage != null)
                {
                    rawImage.texture = rt;
                }
                else
                {
                    Debug.LogError($"[StageSelectUI] Stage {i+1} 버튼 프리팹에 RawImage 컴포넌트가 없습니다! 프리팹을 확인해주세요.");
                }
            }

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
        // 현재 오차 = ViewportCenter - ButtonCenter (World Space)
        float diffWorld = viewportCenter - stageButtons[minIndex].position.x;

        // 월드 좌표 오차를 로컬 좌표계 오차로 변환 (UI 캔버스 스케일 보정)
        float diffLocal = diffWorld / contentPanel.lossyScale.x;

        // Content의 현재 위치에서 로컬 오차를 더함
        float targetX = contentPanel.anchoredPosition.x + diffLocal;
        
        // *중요* 만약 오차가 아주 작으면(스냅 완료) 계산 중지 (떨림 방지)
        if (Mathf.Abs(diffLocal) < 0.1f) 
        {
            contentPanel.anchoredPosition = new Vector2(targetX, contentPanel.anchoredPosition.y);
            scrollRect.velocity = Vector2.zero; // 완벽히 멈춤
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
        // 일반 스테이지 선택 화면에서 진입할 경우 쇼룸 복귀 플래그를 꺼줍니다.
        PlayerPrefs.SetInt("ReturnToShowroom", 0);
        
        // 선택된 스테이지 번호를 저장 (play_scene에서 읽어감)
        PlayerPrefs.SetInt("SelectedStage", index);
        PlayerPrefs.Save();
        
        Debug.Log($"[StageSelectUI] 스테이지 {index} 선택 → Play 씬으로 이동");
        SceneManager.LoadScene("Play");
    }
}

public class DragListener : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public ScrollRect scrollRect;
    public System.Action onBeginDrag;
    public System.Action onEndDrag;

    public void OnBeginDrag(PointerEventData eventData)
    {
        onBeginDrag?.Invoke();
        if (scrollRect != null) scrollRect.OnBeginDrag(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (scrollRect != null) scrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        onEndDrag?.Invoke();
        if (scrollRect != null) scrollRect.OnEndDrag(eventData);
    }
}
