using UnityEngine;

public class ThirdPersonFollow : MonoBehaviour
{
    [Header("따라갈 대상 (Player_Root)")]
    public Transform target;

    [Header("카메라 설정")]
    [Tooltip("카메라가 타겟을 따라잡는 데 걸리는 시간 (0.1 = 빠름, 0.5 = 느림)")]
    public float followTime = 0.1f; // <-- 'Smooth Speed' 대신 '따라가는 시간'

    public Vector3 offset; // 플레이어로부터의 거리

    [Header("카메라 각도")]
    public float fixedXRotation = 20.0f; // 고정된 X축(상하) 각도

    // SmoothDamp가 내부적으로 사용하는 변수 (참조 속도)
    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        // 1. 타겟 확인
        if (target == null)
        {
            return;
        }

        // 2. 위치 계산 (변경 없음)
        Vector3 desiredPosition = target.position + (target.rotation * offset);

        // --- (★ 3. 수정된 이동 로직) ---
        // Lerp 대신 SmoothDamp를 사용합니다.
        // "현재 위치(transform.position)"에서 "목표 위치(desiredPosition)"까지
        // "followTime"초 만에 도달하도록 부드럽게 이동합니다.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity, // SmoothDamp가 내부 계산에만 사용 (신경쓰지 않아도 됨)
            followTime
        );
        // ---

        // 4. 회전 계산 (변경 없음)
        float desiredYRotation = target.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(fixedXRotation, desiredYRotation, 0);

        // 5. 회전 적용 (Lerp 대신 Slerp가 더 부드러움)
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / followTime);
    }
}