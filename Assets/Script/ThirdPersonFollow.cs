using UnityEngine;

public class ThirdPersonFollow : MonoBehaviour
{
    [Header("따라가는 카메라 (T_Camera)")]
    public Transform cameraTransform;   // ← ★ T_Camera를 여기 연결

    [Header("따라갈 대상 (Player_Root)")]
    public Transform target;

    [Header("카메라 설정")]
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    [Header("카메라 각도")]
    public float fixedXRotation = 20.0f;

    void LateUpdate()
    {
        if (target == null || cameraTransform == null)
            return;

        // 1. 목표 위치 계산
        Vector3 desiredPosition = target.position + (target.rotation * offset);

        // 2. 카메라(T_Camera) 위치를 부드럽게 이동
        cameraTransform.position = Vector3.Lerp(
            cameraTransform.position,
            desiredPosition,
            smoothSpeed
        );

        // 3. 목표 회전 계산 (X는 고정 / Y는 플레이어 방향)
        float desiredYRotation = target.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(
            fixedXRotation,
            desiredYRotation,
            0
        );

        // 4. 카메라(T_Camera) 회전 적용
        cameraTransform.rotation = Quaternion.Lerp(
            cameraTransform.rotation,
            targetRotation,
            smoothSpeed
        );
    }
}
