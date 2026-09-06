using UnityEngine;

public class Billboard : MonoBehaviour
{
    [Tooltip("모델의 기본 방향이 다를 경우 여기에 회전값을 넣어 보정합니다. (예: Y축에 90 또는 -90)")]
    public Vector3 rotationOffset = Vector3.zero;

    private Camera cam;

    void LateUpdate()
    {
        // 매 프레임마다 카메라를 캐싱/확인합니다
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                cam = FindObjectOfType<Camera>();
            }
        }

        if (cam != null)
        {
            // 카메라가 바라보는 방향을 똑같이 바라보게 함
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
                             cam.transform.rotation * Vector3.up);
            
            // 추가적인 회전 보정값이 있다면 더해줍니다
            if (rotationOffset != Vector3.zero)
            {
                transform.Rotate(rotationOffset);
            }
        }
    }
}
