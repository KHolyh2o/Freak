using UnityEngine;

namespace Showroom
{
    public class MuseumPlayerController : MonoBehaviour
    {
        public float moveSpeed = 8f;
        public float rotationSpeed = 150f; // A/D 키로 회전할 때의 속도
        public Camera mainCamera;
        
        [Header("카메라 팔로우 설정")]
        public bool followCamera = true;
        public float cameraDistance = 8f; // 캐릭터 뒤로 얼마나 떨어질지
        public float cameraHeight = 4f;   // 캐릭터 위로 얼마나 올라갈지
        public float cameraSmoothSpeed = 5f;

        private Rigidbody rb;
        private float currentH;
        private float currentV;

        void Start()
        {
            if (mainCamera == null) 
            {
                mainCamera = Camera.main;
                if (mainCamera == null) mainCamera = FindObjectOfType<Camera>();
            }
            
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            if (rb != null)
            {
                // Y축(상하)으로 뜨거나 가라앉지 않도록 위치를 고정하고 중력을 끕니다.
                rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate; 
            }

            // 바닥(Floor)과 충돌하여 물리 엔진이 플레이어를 위로(1.33 등) 밀어 올리는 현상을 완벽히 차단
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // 시작할 때 기본 위치 (초기값)
            Vector3 startPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Quaternion startRot = transform.rotation;

            // 만약 플레이 씬을 갔다가 쇼룸으로 다시 돌아온 경우, 원래 섰던 자리를 복구합니다.
            if (PlayerPrefs.GetInt("ReturnToShowroom", 0) == 1)
            {
                float px = PlayerPrefs.GetFloat("ShowroomPosX", transform.position.x);
                float pz = PlayerPrefs.GetFloat("ShowroomPosZ", transform.position.z);
                float ry = PlayerPrefs.GetFloat("ShowroomRotY", transform.rotation.eulerAngles.y);
                
                startPos = new Vector3(px, 0f, pz);
                startRot = Quaternion.Euler(0f, ry, 0f);
            }

            transform.position = startPos;
            transform.rotation = startRot;
        }

        void Update()
        {
            // 입력값만 받아옵니다.
            currentH = Input.GetAxisRaw("Horizontal");
            currentV = Input.GetAxisRaw("Vertical");
            
            // 만약 Rigidbody가 없다면 (물리 연산 불가 상태) Update에서 임시로 움직입니다.
            if (rb == null)
            {
                transform.Rotate(0f, currentH * rotationSpeed * Time.deltaTime, 0f);
                Vector3 newPos = transform.position + transform.forward * currentV * moveSpeed * Time.deltaTime;
                newPos.y = 0f; // 항상 0으로 유지
                transform.position = newPos;
            }
        }

        void FixedUpdate()
        {
            if (rb != null)
            {
                // 1. 물리 기반 회전 (방향 전환)
                if (Mathf.Abs(currentH) > 0.1f)
                {
                    Quaternion deltaRotation = Quaternion.Euler(0f, currentH * rotationSpeed * Time.fixedDeltaTime, 0f);
                    rb.MoveRotation(rb.rotation * deltaRotation);
                }

                // 2. 물리 기반 이동 (앞/뒤)
                Vector3 moveDir = transform.forward * currentV;
                Vector3 targetVelocity = moveDir * moveSpeed;
                targetVelocity.y = 0f; // Y축 이동 절대 불가
                
                if (rb.isKinematic)
                {
                    // Kinematic Ба디는 MovePosition을 사용하되, Y를 0으로 강제 고정
                    Vector3 nextPos = rb.position + targetVelocity * Time.fixedDeltaTime;
                    nextPos.y = 0f;
                    rb.MovePosition(nextPos);
                }
                else
                {
                    // 일반 바디
                    rb.velocity = targetVelocity;
                    // 물리 충돌로 인해 위로 튀어오르는 것을 방지하기 위해 강제로 0 고정
                    if (Mathf.Abs(rb.position.y) > 0.01f)
                    {
                        rb.MovePosition(new Vector3(rb.position.x, 0f, rb.position.z));
                    }
                }
            }
        }

        void LateUpdate()
        {
            if (followCamera && mainCamera != null)
            {
                // 항상 캐릭터의 '뒷통수(-transform.forward)' 쪽으로 카메라 목표 위치 설정
                Vector3 targetPos = transform.position - (transform.forward * cameraDistance) + (Vector3.up * cameraHeight);
                
                // 부드럽게 목표 위치로 이동
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPos, Time.deltaTime * cameraSmoothSpeed);
                
                // 카메라는 항상 캐릭터의 머리(살짝 위)를 바라봄
                mainCamera.transform.LookAt(transform.position + Vector3.up * (cameraHeight * 0.5f));
            }
        }
    }
}
