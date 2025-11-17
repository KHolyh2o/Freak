using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonFollow : MonoBehaviour
{
    [Header("따라갈 대상 (Player_Root)")]
    public Transform target; // (필수) 플레이어의 Transform

    [Header("카메라 설정")]
    public float smoothSpeed = 0.125f; // 카메라가 따라오는 속도 (부드러움)
    public Vector3 offset; // 플레이어로부터의 거리 (예: X=0, Y=3, Z=-5)


    // LateUpdate는 모든 Update(플레이어 이동 등)가 끝난 후 마지막에 호출됩니다.
    // 카메라가 플레이어의 '최종' 위치를 따라가게 하므로, 떨림(Jitter) 현상을 방지합니다.
    void LateUpdate()
    {
        // 1. 타겟(플레이어)이 설정되지 않았으면 아무것도 하지 않음
        if (target == null)
        {
            return;
        }

        // 2. 원하는 카메라 위치 계산
        // (타겟의 현재 위치 + 타겟의 회전값에 따른 offset)
        Vector3 desiredPosition = target.position + (target.rotation * offset);

        // 3. 현재 카메라 위치에서 원하는 위치로 '부드럽게' 이동 (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // 4. 카메라 위치 적용
        transform.position = smoothedPosition;

        // 5. 카메라가 항상 타겟(플레이어)을 바라보도록 함
        transform.LookAt(target);
    }
}