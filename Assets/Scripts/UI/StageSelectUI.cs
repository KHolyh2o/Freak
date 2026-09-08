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
    public bool showTutorialButton = true;
    public int totalStages = 15;
    public float centerScale = 1.2f; // 중앙 아이템 확대 배율
    public float sideScale = 0.8f;   // 주변 아이템 축소 배율
    public float snapSpeed = 10f;    // 스냅 속도 (높을수록 빠름)
    public float buttonSpacing = 200f; // 버튼 간격 (임시, LayoutGroup 사용 시 무시됨)

    [Header("잠금(Lock) 설정")]
    public Sprite lockIconSprite;
    public bool useDebugUnlockStage = false;
    public int debugUnlockStage = 1;

    private List<RectTransform> stageButtons = new List<RectTransform>();
    private bool isDragging = false;
    private float[] distances; // 각 버튼의 중심까지 거리
    private int selectedStageIndex = 0;

    void Start()
    {
        InitializeStageButtons();
        AddPaddingForCentering();
        SetupDragEvents(); // 드래그 이벤트 자동 연결
        
        // 맵 리스트가 생성된 직후, 첫 번째 맵(Stage 1)이 화면 중앙에 오도록 강제 설정
        StartCoroutine(FocusFirstStageCoroutine());
    }

    IEnumerator FocusFirstStageCoroutine()
    {
        // UI LayoutGroup이 크기를 계산할 때까지 한 프레임 대기
        yield return null;
        
        if (scrollRect != null && stageButtons.Count > 0)
        {
            scrollRect.horizontalNormalizedPosition = 0f; // 스크롤을 맨 왼쪽으로 이동 (Padding 때문에 Map 1이 중앙에 옴)
            scrollRect.velocity = Vector2.zero;
        }
    }

    public int GetUnlockedStage()
    {
        if (useDebugUnlockStage) return debugUnlockStage;
        return PlayerPrefs.GetInt("UnlockedStage", 1);
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
            // ★ 튜토리얼 버튼 표시 여부에 따라 버튼 개수 결정
            totalStages = showTutorialButton ? mapFiles.Length + 1 : mapFiles.Length; 
            Debug.Log($"[StageSelectUI] 발견된 맵 파일 개수: {mapFiles.Length} (총 버튼 수: {totalStages})");
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
            
            bool isTutorial = showTutorialButton && i == 0;

            // 텍스트 설정
            TMPro.TextMeshProUGUI txt = btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (txt != null)
            {
                if (isTutorial) txt.text = "Tutorial"; // 첫 번째 버튼은 튜토리얼
                else txt.text = $"Map {(showTutorialButton ? i : i + 1)}"; // 나머지는 Map 1, Map 2 ...
            }

            // 3D 맵 썸네일(RenderTexture) 설정
            bool isLocked = false;
            
            if (!isTutorial) // CSV 맵인 경우
            {
                int csvIndex = showTutorialButton ? i - 1 : i;
                int mapNumber = csvIndex + 1;
                isLocked = mapNumber > GetUnlockedStage();
                
                if (originalGenerator != null && MapPreviewRenderer.Instance != null && mapFiles != null && csvIndex < mapFiles.Length)
                {
                    string mapData = isLocked ? MapPreviewRenderer.DummyLockedMapCSV : mapFiles[csvIndex].text;
                    RenderTexture rt = MapPreviewRenderer.Instance.GeneratePreview(i, mapData, originalGenerator);
                    RawImage rawImage = btnObj.GetComponentInChildren<RawImage>();
                    if (rawImage != null)
                    {
                        rawImage.texture = rt;
                        if (isLocked)
                        {
                            // 잠긴 상태: 전체 버튼을 덮는 오버레이 추가
                            CreateLockOverlay(btnObj.transform);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[StageSelectUI] Stage {i+1} 버튼 프리팹에 RawImage 컴포넌트가 없습니다! 프리팹을 확인해주세요.");
                    }
                }
            }
            else
            {
                // 튜토리얼 버튼은 렌더링할 3D 맵이 없으므로 RawImage를 투명하게 지워버립니다.
                RawImage rawImage = btnObj.GetComponentInChildren<RawImage>();
                if (rawImage != null)
                {
                    rawImage.color = new Color(0, 0, 0, 0);
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

    void CreateLockOverlay(Transform parent)
    {
        // 1. 전체 버튼 덮는 어두운 오버레이 생성
        GameObject overlayObj = new GameObject("LockOverlayBackground");
        overlayObj.transform.SetParent(parent, false);
        
        RectTransform overlayRt = overlayObj.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        // 가장 앞으로 오도록 설정
        overlayObj.transform.SetAsLastSibling();
        
        Image bgImg = overlayObj.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.5f); // 반투명 검정색으로 전체 덮기
        bgImg.raycastTarget = false;

        // 2. 자물쇠 아이콘
        GameObject lockObj = new GameObject("LockIcon");
        lockObj.transform.SetParent(overlayObj.transform, false);
        
        RectTransform rt = lockObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(80, 80); // 적당한 크기 지정
        
        Image img = lockObj.AddComponent<Image>();
        img.sprite = lockIconSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
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
        bool isTutorial = showTutorialButton && index == 0;
        if (!isTutorial)
        {
            int csvIndex = showTutorialButton ? index - 1 : index;
            int mapNumber = csvIndex + 1;
            if (mapNumber > GetUnlockedStage())
            {
                // SoundManager.Instance?.PlayCommandClick(); // 필요시 에러 사운드로 교체
                Debug.Log($"[StageSelectUI] Map {mapNumber}은(는) 아직 잠겨 있습니다!");
                return; // 잠겨있으면 로드 안함
            }
        }

        // 중앙에 있는 게 아니라면 클릭 시 중앙으로 이동 (선택 효과)
        if (index != selectedStageIndex)
        {
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
        
        bool isTutorial = showTutorialButton && index == 0;

        if (isTutorial)
        {
            Debug.Log("[StageSelectUI] 튜토리얼 선택 → Stage_00 씬으로 이동");
            // 튜토리얼 전용 씬
            SceneManager.LoadScene("Stage_00");
        }
        else
        {
            // 선택된 스테이지 번호를 저장 (Play 씬의 TestMapLoader가 읽어감, 0-indexed)
            int mapNumber = showTutorialButton ? index - 1 : index; 
            PlayerPrefs.SetInt("SelectedStage", mapNumber);
            PlayerPrefs.Save();
            
            Debug.Log($"[StageSelectUI] 스테이지 {mapNumber} 선택 → Play 씬으로 이동");
            SceneManager.LoadScene("Play");
        }
    }

    public void OnBackButtonClicked()
    {
        SoundManager.Instance?.PlayCommandClick();
        Debug.Log("[StageSelectUI] 뒤로 가기 선택 → Start 씬으로 이동");
        SceneManager.LoadScene("Start");
    }

    [Header("설정창 (선택사항)")]
    public GameObject settingsPanel;

    public void OnSettingsButtonClicked()
    {
        SoundManager.Instance?.PlayCommandClick();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[StageSelectUI] Settings Panel이 연결되어 있지 않습니다!");
        }
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
