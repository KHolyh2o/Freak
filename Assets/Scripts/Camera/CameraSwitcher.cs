using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("카메라 설정")]
    public Camera[] cameras; // 0: 쿼터뷰(고정), 1: 탑뷰, 2: 3인칭(메인)
    // index 2번을 무조건 메인(3인칭)으로 고정합니다. (Inspector 설정 실수 방지)
    [Header("0번 카메라(쿼터뷰) 회전 설정")]
    public Transform isometricTarget; // 회전 중심축 (비워두면 카메라가 바라보는 땅이 축이 됨)
    public float isometricDragSpeed = 250f; // 좌우 드래그 회전 속도
    public int dragMouseButton = 0; // 0: 좌클릭, 1: 우클릭, 2: 휠클릭
    [Tooltip("화면에 맵이 너무 위/아래에 쏠릴 때 보정하는 오프셋 (양수면 맵이 화면 아래로 내려옵니다)")]
    public float isometricScreenYOffset = 0f;
    
    private float _currentIsometricYaw = 0f;
    private float _isometricPitch = 45f;
    private float _isometricDistance = 20f;
    private Vector3 _isometricPivotPosition = Vector3.zero;
    private Vector3 _previousMousePos;

    // 실제 게임 화면을 비추는 단 하나의 메인 카메라
    private Camera _mainCam;
    private Camera _skyCam; // ★ [Dual Camera] 배경(하늘) 전용 카메라
    private ThirdPersonFollow _mainTpf;

    // 원래 FOV 저장 (메인 카메라 복구용)
    private float _defaultFOV;

    private int currentCameraIndex = 0;
    private Coroutine _currentTransition;

    void Start()
    {
        if (cameras == null || cameras.Length == 0) return;

        // 1. 메인 카메라(Index 2) 설정
        if (cameras.Length > 2)
        {
            _mainCam = cameras[2];
            // ★ 수정: 스크립트가 카메라 본체가 아니라 부모/자식에 있을 수 있음
            _mainTpf = _mainCam.GetComponentInParent<ThirdPersonFollow>();
            if (_mainTpf == null) _mainTpf = _mainCam.GetComponentInChildren<ThirdPersonFollow>();
            
            if (_mainTpf == null) Debug.LogWarning($"[CameraSwitcher] ThirdPersonFollow not found on/near {_mainCam.name}");
        }
        else
        {
            // 예외 상황: 카메라가 3개 미만이면 그냥 마지막꺼 씀
            _mainCam = cameras[cameras.Length - 1];
            _mainTpf = _mainCam.GetComponent<ThirdPersonFollow>();
            Debug.LogWarning($"[CameraSwitcher] Not enough cameras! Fallback Main: {_mainCam.name}");
        }

        // ★ [Fix] 메인 카메라의 원래 FOV 저장 (변질 방지)
        _defaultFOV = _mainCam.fieldOfView;

        // ★ [Dual Camera Setup] SkyCam 생성
        // 메인 카메라는 지형/캐릭터만 찍고(Depth Only), 배경 카메라는 하늘만 찍습니다(Skybox).
        // 배경 카메라는 항상 '직교(Orthographic)' 모드이므로 하늘이 절대 왜곡되지 않습니다.
        GameObject skyCamObj = new GameObject("SkyCam_Background");
        _skyCam = skyCamObj.AddComponent<Camera>();
        _skyCam.transform.SetParent(_mainCam.transform.parent); 
        
        _skyCam.clearFlags = CameraClearFlags.Skybox;
        _skyCam.cullingMask = 0; 
        _skyCam.orthographic = true; 
        _skyCam.orthographicSize = 10f; 
        _skyCam.depth = -2; 
        
        // ★ [URP Support]
        // URP에서는 ClearFlags.DepthOnly가 작동하지 않고, 대신 Camera Stack을 써야 합니다.
        // SkyCam(Base) -> MainCam(Overlay) 순서로 쌓습니다.
        var skyCamData = _skyCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (skyCamData == null) skyCamData = _skyCam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        
        var mainCamData = _mainCam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (mainCamData == null) mainCamData = _mainCam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

        if (skyCamData != null && mainCamData != null)
        {
            skyCamData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
            mainCamData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
            skyCamData.cameraStack.Add(_mainCam);
            Debug.Log("[CameraSwitcher] Configured URP Camera Stack: SkyCam(Base) + MainCam(Overlay)");
        }
        else
        {
            // Built-in Pipeline Fallback
            _mainCam.clearFlags = CameraClearFlags.Depth;
        }

        // 2. 초기화
        for (int i = 0; i < cameras.Length; i++)
        {
            // 앵커 카메라들(0, 1번)에 혹시 모를 로직이 붙어있다면 제거
            if (i != 2)
            {
                var looseTpf = cameras[i].GetComponent<ThirdPersonFollow>();
                if (looseTpf != null) 
                {
                    looseTpf.enabled = false;
                    Debug.Log($"[CameraSwitcher] Disabled stray TPF on {cameras[i].name}");
                }
                
                cameras[i].gameObject.SetActive(false);
            }
            else
            {
                cameras[i].gameObject.SetActive(true);
            }
        }

        // 쿼터뷰(0번 카메라) 초기 상태 저장
        if (cameras.Length > 0 && cameras[0] != null)
        {
            Transform cam0 = cameras[0].transform;
            _currentIsometricYaw = cam0.eulerAngles.y + 90f;
            _isometricPitch = cam0.eulerAngles.x;

            if (isometricTarget != null)
            {
                _isometricPivotPosition = isometricTarget.position;
            }
            else
            {
                // 중심축이 없으면 카메라 정중앙이 바라보는 가상의 y=0 평면을 축으로 완벽히 고정 계산 (Collider 의존 X)
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                Ray ray = new Ray(cam0.position, cam0.forward);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    _isometricPivotPosition = ray.GetPoint(enter);
                }
                else
                {
                    // 카메라가 위를 보고 있거나 평면과 만나지 않는 예외 상황 처리
                    _isometricPivotPosition = cam0.position + cam0.forward * 20f;
                    _isometricPivotPosition.y = 0; // 강제로 축 높이를 0으로 고정
                }
            }
            
            // 중심축과 카메라 사이의 현재 거리를 저장
            _isometricDistance = Vector3.Distance(cam0.position, _isometricPivotPosition);

            // ★ 처음 시작할 때부터 Isometric Target 궤도에 맞춰 카메라 위치/회전 강제 정렬 (스크롤 튐 방지)
            Quaternion initRotation = Quaternion.Euler(_isometricPitch, _currentIsometricYaw, 0f);
            Vector3 properPos = _isometricPivotPosition + (initRotation * Vector3.back) * _isometricDistance;
            
            // 화면 상하 쏠림 오프셋 보정 (카메라의 로컬 Up 방향으로 추가 이동시켜 시점을 조절)
            properPos += initRotation * Vector3.up * isometricScreenYOffset;

            cam0.position = properPos;
            cam0.rotation = initRotation;
        }

        // 시작 인덱스 설정 (보통 0번부터 시작)
        currentCameraIndex = 0;
        
        // 게임 시작 시 0번 위치로 강제 이동
        if (cameras.Length > 0)
        {
            Debug.Log($"[CameraSwitcher] Initializing to Camera {currentCameraIndex}");
            if (currentCameraIndex == 2)
            {
                if (_mainTpf) _mainTpf.enabled = true;
                _mainCam.orthographic = false; // 3인칭은 무조건 원근
                _mainCam.fieldOfView = _defaultFOV;
            }
            else
            {
                if (_mainTpf) _mainTpf.enabled = false;
                
                // 타겟 속성 복사
                Camera callbackTarget = cameras[currentCameraIndex];
                _mainCam.transform.position = callbackTarget.transform.position;
                _mainCam.transform.rotation = callbackTarget.transform.rotation;
                _mainCam.orthographic = callbackTarget.orthographic;
                _mainCam.orthographicSize = callbackTarget.orthographicSize;
                _mainCam.fieldOfView = callbackTarget.fieldOfView;
            }
        }
    }

    // 비동적 카메라 전환 후 카메라가 고정될 좌표를 저장
    private Vector3 _lockedPosition;
    private Quaternion _lockedRotation;
    private bool _positionLocked = false;

    void LateUpdate()
    {
        if (_skyCam != null && _mainCam != null)
        {
            _skyCam.transform.rotation = _mainCam.transform.rotation;
        }

        // 비동적 카메라 상태일 때, 저장된 좌표로 매 프레임 강제 고정
        if (_positionLocked && _currentTransition == null && _mainCam != null)
        {
            _mainCam.transform.position = _lockedPosition;
            _mainCam.transform.rotation = _lockedRotation;
        }

        // 쿼터뷰(index 0) 고유 기능: 드래그 회전
        if (currentCameraIndex == 0 && _currentTransition == null && cameras.Length > 0)
        {
            HandleIsometricDrag();
        }
    }

    private bool IsDynamicCamera(int index)
    {
        if (index < 0 || index >= cameras.Length) return false;
        Camera cam = cameras[index];
        if (cam == null) return false;

        // 해당 카메라 혹은 부모에게 따라가기 스크립트가 있는지 체크
        var tpf = cam.GetComponentInParent<ThirdPersonFollow>();
        if (tpf == null) tpf = cam.GetComponentInChildren<ThirdPersonFollow>();
        return tpf != null;
    }

    private void HandleIsometricDrag()
    {
        if (Input.GetMouseButtonDown(dragMouseButton))
        {
            _previousMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(dragMouseButton))
        {
            Vector3 mouseDelta = Input.mousePosition - _previousMousePos;
            
            // 화면 해상도에 비례하도록 정규화하여 회전 각도 산출
            float dragAmount = mouseDelta.x / Screen.width; 
            _currentIsometricYaw += dragAmount * isometricDragSpeed;
            
            _previousMousePos = Input.mousePosition;

            // 계산된 새로운 각도 (피치는 고정)
            Quaternion rotation = Quaternion.Euler(_isometricPitch, _currentIsometricYaw, 0f);
            
            // 위치 지정: 중앙축(Pivot)에서 설정된 거리(Distance)만큼 뒷통수 방향(-forward)으로 물러난 곳
            Vector3 targetPos = _isometricPivotPosition + (rotation * Vector3.back) * _isometricDistance;
            
            // 화면 상하 쏠림 오프셋 보정
            targetPos += rotation * Vector3.up * isometricScreenYOffset;

            // 0번 카메라 앵커 본체의 위치와 각도를 최신화 (다음번 카메라 전환 시 이곳을 참조하게 됨)
            if (cameras[0] != null)
            {
                cameras[0].transform.position = targetPos;
                cameras[0].transform.rotation = rotation;
            }

            // 실제 게임 화면을 표시하는 메인 카메라 즉시 이동
            if (_mainCam != null)
            {
                _mainCam.transform.position = targetPos;
                _mainCam.transform.rotation = rotation;

                // ★ 마우스를 뗐을 때 카메라가 원래 자리(드래그 전 위치)로 돌아가는(Snap) 것을 방지하기 위해 잠금 좌표 갱신
                _lockedPosition = targetPos;
                _lockedRotation = rotation;
            }
        }
    }

    public void SwitchCamera()
    {
        if (cameras.Length < 2) return;
        int nextIndex = (currentCameraIndex + 1) % cameras.Length;
        SetSpecificCamera(nextIndex);
    }

    public void SetSpecificCamera(int index)
    {
        if (index < 0 || index >= cameras.Length) return;
        if (index == currentCameraIndex) return;

        if (_currentTransition != null) StopCoroutine(_currentTransition);

        Camera targetCamRef = cameras[index];
        bool isTargetDynamic = IsDynamicCamera(index); 

        // 타겟이 다이나믹(3인칭)이라면 해당 스크립트를 캐싱
        if (isTargetDynamic)
        {
            _mainTpf = targetCamRef.GetComponentInParent<ThirdPersonFollow>();
            if (_mainTpf == null) _mainTpf = targetCamRef.GetComponentInChildren<ThirdPersonFollow>();
        }

        _currentTransition = StartCoroutine(SmoothMoveRoutine(targetCamRef, isTargetDynamic, 1.5f));

        currentCameraIndex = index;
    }

    private IEnumerator SmoothMoveRoutine(Camera targetRef, bool isDynamicTarget, float duration)
    {
        // 1. 출발점 상태 저장
        Vector3 startPos = _mainCam.transform.position;
        Quaternion startRot = _mainCam.transform.rotation;
        Matrix4x4 startMat = _mainCam.projectionMatrix; 
        float startFOV = _mainCam.fieldOfView; // FOV 시작값 

        // TPF 끄기
        if (_mainTpf) _mainTpf.enabled = false;

        // ★ [Permanent Perspective Fix]
        // 엔진 모드를 Orthographic으로 바꾸는 순간 그림자/조명 파이프라인이 바뀌어 무조건 튑니다.
        // 따라서, 카메라는 영원히 'Perspective(원근)' 모드로 둡니다.
        // 대신, 직교 뷰가 필요할 때는 '매트릭스만' 직교로 강제 변환하여 눈을 속입니다.
        // 이렇게 하면 그림자/조명은 원근 모드 그대로 유지되므로 튀는 현상이 0이 됩니다.
        _mainCam.orthographic = false; 
        
        // 2. 도착점 투영 행렬(End Matrix) 계산
        Matrix4x4 endMat;
        float aspect = _mainCam.aspect;
        
        // 도착지 카메라의 실제 Clip Plane을 가져옵니다.
        float targetNear = targetRef.nearClipPlane;
        float targetFar = targetRef.farClipPlane;

        if (isDynamicTarget) 
        {
            targetNear = _mainCam.nearClipPlane;
            targetFar = _mainCam.farClipPlane;
        }

        if (targetRef.orthographic)
        {
            float size = targetRef.orthographicSize;
            endMat = Matrix4x4.Ortho(-size * aspect, size * aspect, -size, size, targetNear, targetFar);
        }
        else
        {
            float fov = isDynamicTarget ? _defaultFOV : targetRef.fieldOfView;
            endMat = Matrix4x4.Perspective(fov, aspect, targetNear, targetFar);
        }
        
        // 매트릭스 오버라이드 시작
        _mainCam.projectionMatrix = startMat;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (t > 1f) t = 1f;

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            smoothT = Mathf.SmoothStep(0f, 1f, smoothT); // Double SmoothStep

            // 3. 이동/회전
            Vector3 destPos;
            Quaternion destRot;

            if (isDynamicTarget && _mainTpf != null)
            {
                _mainTpf.GetDesiredPose(out destPos, out destRot);
            }
            else
            {
                destPos = targetRef.transform.position;
                destRot = targetRef.transform.rotation;
            }

            _mainCam.transform.position = Vector3.Lerp(startPos, destPos, smoothT);
            _mainCam.transform.rotation = Quaternion.Slerp(startRot, destRot, smoothT);

            // 4. 매트릭스 블렌딩 (Cubic Curve)
            bool goingToOrthoVisual = targetRef.orthographic;
            float matrixT = smoothT;
            
            if (goingToOrthoVisual)
            {
                float oneMinusT = 1f - smoothT;
                matrixT = 1f - (oneMinusT * oneMinusT * oneMinusT); // Ease Out Cubic
            }
            else
            {
                matrixT = smoothT * smoothT * smoothT; // Ease In Cubic
            }

            _mainCam.projectionMatrix = MatrixLerp(startMat, endMat, matrixT);

            // ★ [Dual Camera] FOV 복구
            // 이제 배경(Skybox)은 SkyCam이 담당하므로, 메인 카메라는 굳이 FOV를 4도로 줄일 필요가 없습니다.
            // 정상적인 FOV 블렌딩으로 돌아갑니다.
            float targetFOV = isDynamicTarget ? _defaultFOV : targetRef.fieldOfView;
            
            _mainCam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, smoothT);
            
            yield return null;
        }

        // 6. 도착 후 처리
        // ★ [Core Logic] 
        // 1) 도착지가 '직교 느낌'이어야 한다면 -> 매트릭스 오버라이드를 끄지 않고 그대로 둡니다! (Fake Ortho)
        // 2) 도착지가 '원근(기본)'이어야 한다면 -> 오버라이드를 끄고 순정 상태로 돌아옵니다.
        
        bool finishInOrthoVisual = (!isDynamicTarget && targetRef.orthographic);

        if (finishInOrthoVisual)
        {
            // 정적 (직교 뷰)
            _mainCam.transform.position = targetRef.transform.position;
            _mainCam.transform.rotation = targetRef.transform.rotation;
            
            // 매트릭스를 최종 결과물로 고정 (Reset 하지 않음!)
            _mainCam.projectionMatrix = endMat;
            
            // FOV도 타겟 값으로 고정
            _mainCam.fieldOfView = targetRef.fieldOfView;

            _lockedPosition = targetRef.transform.position;
            _lockedRotation = targetRef.transform.rotation;
            _positionLocked = true;
        }
        else
        {
            // 동적/정적 (원근 뷰) -> 순정 복귀
            if (isDynamicTarget && _mainTpf != null)
            {
                _mainTpf.enabled = true;
                _positionLocked = false;
            }
            else
            {
                _mainCam.transform.position = targetRef.transform.position;
                _mainCam.transform.rotation = targetRef.transform.rotation;
                _lockedPosition = targetRef.transform.position;
                _lockedRotation = targetRef.transform.rotation;
                _positionLocked = true;
            }

            _mainCam.fieldOfView = isDynamicTarget ? _defaultFOV : targetRef.fieldOfView;
            _mainCam.ResetProjectionMatrix();
        }

        _currentTransition = null;
    }

    // 행렬 보간 헬퍼 함수
    private Matrix4x4 MatrixLerp(Matrix4x4 from, Matrix4x4 to, float t)
    {
        Matrix4x4 ret = new Matrix4x4();
        for (int i = 0; i < 16; i++)
        {
            ret[i] = Mathf.Lerp(from[i], to[i], t);
        }
        return ret;
    }

    /// <summary>
    /// 외부(MapGenerator)에서 맵 생성이 완료되면 호출하여
    /// 0번(쿼터뷰)과 1번(탑뷰) 카메라를 생성된 맵의 크기와 중앙 좌표에 맞춰 즉시 세팅합니다.
    /// </summary>
    public void AlignCamerasToMapBounds(Vector3 center, float mapWidth, float mapDepth)
    {
        if (cameras == null || cameras.Length < 2) return;

        // --- [1. 0번 카메라 (쿼터뷰 회전)] 세팅 ---
        if (cameras[0] != null)
        {
            // 회전 축(Pivot)을 맵의 정중앙으로 강제로 잡습니다.
            _isometricPivotPosition = center;

            // ★ 맵 크기에 비례하여 카메라 후퇴 거리(Distance) 자동 조절 (FOV 활용)
            float mapRadius = Mathf.Max(mapWidth, mapDepth) * 0.5f * 1.25f; // 1.25는 시야에 한눈에 들어오게 하는 여백(Padding) 비율
            float fovRad = cameras[0].fieldOfView * 0.5f * Mathf.Deg2Rad;
            float requiredDistance = mapRadius / Mathf.Sin(fovRad);
            
            // 너무 확대되는 것을 방지하기 위한 최소 거리 보장
            if (requiredDistance < 20f) requiredDistance = 20f;
            
            _isometricDistance = requiredDistance;
            
            // 맵 중앙을 기준으로 설정된 각도에서 카메라 뒷통수(-forward) 방향으로 당겨 궤도상에 올립니다.
            Quaternion initRotation = Quaternion.Euler(_isometricPitch, _currentIsometricYaw, 0f);
            Vector3 properPos = _isometricPivotPosition + (initRotation * Vector3.back) * _isometricDistance;
            
            // 화면 상하 쏠림 오프셋 보정
            properPos += initRotation * Vector3.up * isometricScreenYOffset;

            cameras[0].transform.position = properPos;
            cameras[0].transform.rotation = initRotation;
            
            // 현재 메인 카메라가 0번을 보고 있다면 즉시 동기화
            if (currentCameraIndex == 0 && _mainCam != null && _currentTransition == null)
            {
                _mainCam.transform.position = properPos;
                _mainCam.transform.rotation = initRotation;
                
                // ★ 잠금 좌표 동기화 (되돌아감 방지)
                _lockedPosition = properPos;
                _lockedRotation = initRotation;
            }
        }

        // --- [2. 1번 카메라 (수직 탑뷰)] 세팅 ---
        if (cameras[1] != null)
        {
            float maxDimension = Mathf.Max(mapWidth, mapDepth);
            
            // 시야 보정을 위해 넉넉한 여백 추가 (2.0배)
            float requiredHeight = maxDimension * 2.0f; 
            // 너무 낮게 깔리지 않도록 최소 높이 보장
            if (requiredHeight < 20f) requiredHeight = 20f; 

            // 위치: X, Z는 맵 중앙, Y는 높이
            Vector3 topViewPos = new Vector3(center.x, requiredHeight, center.z);
            // 각도: 완벽하게 아래를 내려다보도록 (Pitch = 90)
            Quaternion topViewRot = Quaternion.Euler(90f, 0f, 0f);

            cameras[1].transform.position = topViewPos;
            cameras[1].transform.rotation = topViewRot;

            // 만약 1번 카메라가 직교(Orthographic) 투영을 쓴다면 Size도 맵 폭에 맞춰 늘려줍니다.
            if (cameras[1].orthographic)
            {
                cameras[1].orthographicSize = maxDimension * 1.0f; 
            }
            
            // 현재 메인 카메라가 1번을 보고 있다면 즉시 동기화
            if (currentCameraIndex == 1 && _mainCam != null && _currentTransition == null)
            {
                _mainCam.transform.position = topViewPos;
                _mainCam.transform.rotation = topViewRot;
                if (_mainCam.orthographic) _mainCam.orthographicSize = cameras[1].orthographicSize;

                // ★ 잠금 좌표 동기화 (되돌아감 방지)
                _lockedPosition = topViewPos;
                _lockedRotation = topViewRot;
            }
        }

        Debug.Log($"[CameraSwitcher] 카메라가 맵 정중앙({center})에 맞춰 세팅되었습니다!");
    }
}