using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // (★ 이 줄을 스크립트 맨 위에 추가하세요!)

public class MapEditor : MonoBehaviour
{
    [Header("설치할 프리팹 연결")]
    public GameObject roadPrefab;
    public GameObject obstaclePrefab;
    public GameObject startBoxPrefab; // (Tag: StartBox 필수)
    public GameObject endPointPrefab; // (Tag: EndPoint 필수)
    public GameObject playerPrefab;   // (Player_Root 프리팹)

    [Header("설정")]
    public LayerMask groundLayer; // 바닥 감지용 레이어 (Ground)

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

        if (EventSystem.current.IsPointerOverGameObject())
        {
            // (선택) 고스트 블록도 UI 위에서는 안 보이게 하려면:
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
                // 클릭한 곳이 UI가 아닐 때만 설치 (EventSystem 필요하지만 여기선 생략)
                // 이미 블록이 있으면 덮어쓰기 위해 체크 안 함 (바로 PlaceBlock 호출)
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
            // 같은 프리팹이면 다시 설치 안 함 (최적화)
            // (이름 비교 등으로 체크 가능하나 여기선 생략하고 덮어쓰기 진행)
            RemoveBlock(gridCoord);
        }

        GameObject newObj = Instantiate(currentPrefab, position, Quaternion.identity);

        // 회전이 필요한 경우 (예: StartBox는 방향 중요) 여기서 처리 가능
        // newObj.transform.rotation = ...

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

    // --- 플레이 모드 전환 (Toggle) ---
    public void TogglePlayMode()
    {
        isPlayMode = !isPlayMode;

        if (isPlayMode)
        {
            // [편집 -> 플레이]
            if (ghostObject != null) ghostObject.SetActive(false);

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
                // 위치 잡기
                currentPlayerInstance.transform.position = startBoxObj.transform.position + Vector3.up * 1.33f;
                // (주의: PlayerController.Awake에서 StartBox를 태그로 찾도록 수정되어 있어야 함)

                Debug.Log("플레이 모드 시작!");
            }
            else
            {
                Debug.LogWarning("시작 지점(StartBox)이 없습니다!");
                isPlayMode = false; // 시작 취소
                if (ghostObject != null) ghostObject.SetActive(true);
            }
        }
        else
        {
            // [플레이 -> 편집]
            if (currentPlayerInstance != null) Destroy(currentPlayerInstance);
            if (ghostObject != null) ghostObject.SetActive(true);
            Debug.Log("편집 모드로 복귀");
        }
    }
}