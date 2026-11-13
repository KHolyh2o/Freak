using UnityEngine;

/// <summary>
/// ItemFloater의 로컬 좌표 버전. 부모가 움직이거나 회전해도 따라간다.
/// 홀로그램 촬영 씬 전용 (HoloShowcase가 자동으로 붙인다).
/// </summary>
public class HoloLocalFloater : MonoBehaviour
{
    public float rotateSpeed = 50f;
    public float floatSpeed = 2f;
    public float floatHeight = 0.25f;

    Vector3 startLocalPos;
    float elapsed;

    void OnEnable()
    {
        startLocalPos = transform.localPosition;
        elapsed = 0f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.Self);
        Vector3 p = startLocalPos;
        p.y += Mathf.Sin(elapsed * floatSpeed) * floatHeight;
        transform.localPosition = p;
    }
}
