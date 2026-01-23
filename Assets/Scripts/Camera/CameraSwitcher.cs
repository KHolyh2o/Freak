using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("카메라 설정")]
    public Camera[] cameras; // 0: 근접, 1: 쿼터뷰(고정), 2: 3인칭(메인)
    // index 2번을 무조건 메인(3인칭)으로 고정합니다. (Inspector 설정 실수 방지)
    
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
            
            if (_mainTpf != null) Debug.Log($"[CameraSwitcher] Found TPF on {_mainTpf.gameObject.name}");
            else Debug.LogError($"[CameraSwitcher] CRITICAL: Could not find ThirdPersonFollow on/near {_mainCam.name}!");

            Debug.Log($"[CameraSwitcher] Main Camera Assigned: {_mainCam.name} (Index 2)");
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

    // SkyCam 회전 동기화
    void LateUpdate()
    {
        if (_skyCam != null && _mainCam != null)
        {
            _skyCam.transform.rotation = _mainCam.transform.rotation;
            // 위치는 Skybox 렌더링에 영향 없으므로 동기화 불필요 (하지만 해도 무방)
        }
    }

    public void SwitchCamera()
    {
        if (cameras.Length < 2) return;
        int nextIndex = (currentCameraIndex + 1) % cameras.Length;
        Debug.Log($"[CameraSwitcher] SwitchCamera Triggered. Next: {nextIndex}");
        SetSpecificCamera(nextIndex);
    }

    // ★ 단일 카메라 시스템: 메인 카메라 몸체 하나가 이곳저곳으로 날아다닙니다.
    public void SetSpecificCamera(int index)
    {
        if (index < 0 || index >= cameras.Length) return;
        if (index == currentCameraIndex) return;

        Debug.Log($"[CameraSwitcher] SetSpecificCamera: {currentCameraIndex} -> {index}");

        if (_currentTransition != null) StopCoroutine(_currentTransition);

        Camera targetCamRef = cameras[index];
        bool isTargetDynamic = (index == 2); 
        Debug.Log($"[CameraSwitcher] Is Target Dynamic? {isTargetDynamic} (Index {index})");

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
            
            Debug.Log("[CameraSwitcher] Fixed in Fake Ortho Mode.");
        }
        else
        {
            // 동적/정적 (원근 뷰) -> 순정 복귀
            if (isDynamicTarget && _mainTpf != null)
            {
                _mainTpf.enabled = true;
            }
            else
            {
                _mainCam.transform.position = targetRef.transform.position;
                _mainCam.transform.rotation = targetRef.transform.rotation;
            }

            // 원근 타겟의 원래 FOV로 복구
            _mainCam.fieldOfView = isDynamicTarget ? _defaultFOV : targetRef.fieldOfView;
            
            // 순정 매트릭스로 복구
            _mainCam.ResetProjectionMatrix();
            Debug.Log("[CameraSwitcher] Reset to Standard Perspective.");
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
}