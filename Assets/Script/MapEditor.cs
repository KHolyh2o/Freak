using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // UI 클릭 감지를 위해 필수

public class MapEditor : MonoBehaviour
{
    [Header("설치할 프리팹 연결")]
    public GameObject roadPrefab;
    public GameObject obstaclePrefab;
    public GameObject startBoxPrefab; // (Tag: "StartBox" 필수)
    public GameObject endPointPrefab; // (Tag: "EndPoint" 필수)
    public GameObject playerPrefab;   // (Player_Root 프리팹)

    [Header("설정")]
    public LayerMask groundLayer; // 바닥 감지용 레이어 (Ground)

    [Header("UI 및 카메라 연결")]
    public GameObject editorUI; // 맵 에디터용 UI (길, 나무 버튼 등)
    public GameObject gameUI;   // 가져온 GameUI_Canvas 프리팹
    public GameObject editorCamera; // 편집용 카메라 (Main Camera)
    public GameObject gameCameraManager; // 가져온 CameraManager 프리팹

    // 설치된 블록 관리 (좌표 중복 방지용)
    private Dictionary<Vector2Int, GameObject> placedObjects = new Dictionary<Vector2Int, GameObject>();

    private GameObject currentPrefab; // 현재 선택된 프리팹
    private GameObject currentPlayerInstance;
    private bool isPlayMode = false;

    // --- 고스트(미리보기) 관련 변수 ---
    private GameObject ghostObject;
    private Material ghostMaterial;

    void Start()
    {
        // 1. 고스트용 반투명 재질 만들기 (URP 호환)
        // URP 쉐이더를 우선 찾고, 없으면 기본 쉐이더를 찾음
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Standard"); // 최후의 수단

        ghostMaterial = new Material(shader);

        // 투명(Transparent) 모드 설정
        ghostMaterial.SetFloat("_Surface", 1); // 1 = Transparent
        ghostMaterial.SetFloat("_Blend", 0);   // Alpha
        ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ghostMaterial.SetInt("_ZWrite", 0);
        ghostMaterial.renderQueue = 3000;

        // 흰색 + 50% 투명도
        ghostMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.5f));
        if (shader.name == "Standard") ghostMaterial.color = new Color(1f, 1f, 1f, 0.5f); // Standard용

        // 초기 선택 (길)
        SelectRoad();
    }

    void Update()
    {
        // 플레이 모드일 때는 편집 기능 정지
        if (isPlayMode)
        {
            if (ghostObject != null) ghostObject.SetActive(false);
            return;
        }

        // 마우스가 UI 위에 있다면 레이캐스트 무시 (뒤에 설치되는 것 방지)
        if (EventSystem.current.IsPointerOverGameObject())
        {
            if (ghostObject != null) ghostObject.SetActive(false);
            return;
        }

        // 마우스 위치로 레이저 발사
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            // 정수 좌표 계산 (Grid Snapping)
            Vector3 hitPoint = hit.point;
            int x = Mathf.RoundToInt(hitPoint.x);
            int z = Mathf.RoundToInt(hitPoint.z);
            Vector3 finalPos = new Vector3(x, 0, z); // y는 0으로 고정

            // --- 고스트 이동 및 표시 ---
            if (ghostObject != null)
            {
                if (!ghostObject.activeSelf) ghostObject.SetActive(true);
                ghostObject.transform.position = finalPos;
            }

            // [좌클릭] 설치
            if (Input.GetMouseButton(0))
            {
                PlaceBlock(new Vector2Int(x, z), finalPos);
            }
            // [우클릭] 삭제
            else if (Input.GetMouseButton(1))
            {
                RemoveBlock(new Vector2Int(x, z));
            }
        }
        else
        {
            // 마우스가 바닥을 벗어나면 고스트 숨김
            if (ghostObject != null) ghostObject.SetActive(false);
        }
    }

    // --- 고스트 교체 함수 ---
    void ChangeGhost(GameObject prefab)
    {
        if (ghostObject != null) Destroy(ghostObject);

        ghostObject = Instantiate(prefab);
        ghostObject.name = "GhostBlock";

        // 1. 충돌체 제거 (마우스 레이저 방해 금지)
        Collider[] colliders = ghostObject.GetComponentsInChildren<Collider>();
        foreach (var col in colliders) Destroy(col);

        // 2. 반투명 재질 적용
        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.sharedMaterial = ghostMaterial;
        }
    }

    // --- UI 버튼 연결용 함수 ---
    public void SelectRoad() { currentPrefab = roadPrefab; ChangeGhost(roadPrefab); }
    public void SelectObstacle() { currentPrefab = obstaclePrefab; ChangeGhost(obstaclePrefab); }
    public void SelectStart() { currentPrefab = startBoxPrefab; ChangeGhost(startBoxPrefab); }
    public void SelectEnd() { currentPrefab = endPointPrefab; ChangeGhost(endPointPrefab); }

    // --- 설치 및 삭제 로직 ---
    void PlaceBlock(Vector2Int gridCoord, Vector3 position)
    {
        // 이미 그 자리에 블록이 있다면?
        if (placedObjects.ContainsKey(gridCoord))
        {
            // 기존 거 삭제하고 덮어쓰기
            RemoveBlock(gridCoord);
        }

        GameObject newObj = Instantiate(currentPrefab, position, Quaternion.identity);
        placedObjects.Add(gridCoord, newObj);
    }

    void RemoveBlock(Vector2Int gridCoord)
    {
        if (placedObjects.ContainsKey(gridCoord))
        {
            Destroy(placedObjects[gridCoord]);
            placedObjects.Remove(gridCoord);
        }
    }

    // --- 플레이 모드 전환 (Toggle 연결용) ---
    // 토글(Toggle) UI가 호출할 함수 (체크되면 isOn = true, 해제되면 isOn = false)
    public void SetPlayMode(bool isOn)
    {
        isPlayMode = isOn;

        if (isPlayMode)
        {
            // [편집 -> 플레이 모드 진입]
            if (ghostObject != null) ghostObject.SetActive(false);

            // 1. UI 전환: 에디터 UI 끄고, 게임 UI 켜기
            if (editorUI != null) editorUI.SetActive(false);
            if (gameUI != null) gameUI.SetActive(true);

            // 2. 카메라 전환: 에디터 카메라 끄고, 게임 카메라 켜기
            if (editorCamera != null) editorCamera.SetActive(false);
            if (gameCameraManager != null) gameCameraManager.SetActive(true);

            // StartBox 위치 찾기
            GameObject startBoxObj = null;
            foreach (var obj in placedObjects.Values)
            {
                if (obj.CompareTag("StartBox"))
                {
                    startBoxObj = obj;
                    break;
                }
            }

            if (startBoxObj != null)
            {
                // 플레이어 생성
                currentPlayerInstance = Instantiate(playerPrefab);
                currentPlayerInstance.transform.position = startBoxObj.transform.position + Vector3.up * 1.33f;

                // ★ 중요: 생성된 플레이어에게 StartBox와 UI 연결해주기
                PlayerController pc = currentPlayerInstance.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.startBox = startBoxObj;
                }

                // UI Auto Connector에게 새 플레이어 연결
                UI_Auto_Connector uiConnector = FindObjectOfType<UI_Auto_Connector>();
                if (uiConnector != null && pc != null)
                {
                    uiConnector.BindPlayer(pc); // (UI_Auto_Connector에 BindPlayer 함수가 있어야 함)
                }

                Debug.Log("플레이 모드 시작!");
            }
            else
            {
                Debug.LogWarning("시작 지점(StartBox)이 없습니다!");
                // 강제로 토글을 끕니다 (변수만 복구)
                isPlayMode = false;
                if (ghostObject != null) ghostObject.SetActive(true);

                // UI 복구
                if (editorUI != null) editorUI.SetActive(true);
                if (gameUI != null) gameUI.SetActive(false);
            }
        }
        else
        {
            // [플레이 -> 편집 모드 복귀]
            // 1. UI 전환: 게임 UI 끄고, 에디터 UI 켜기
            if (gameUI != null) gameUI.SetActive(false);
            if (editorUI != null) editorUI.SetActive(true);

            // 2. 카메라 전환: 게임 카메라 끄고, 에디터 카메라 켜기
            if (gameCameraManager != null) gameCameraManager.SetActive(false);
            if (editorCamera != null) editorCamera.SetActive(true);

            if (currentPlayerInstance != null) Destroy(currentPlayerInstance);
            if (ghostObject != null) ghostObject.SetActive(true);
            Debug.Log("편집 모드로 복귀");
        }
    }
}