using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    // GameManager 및 DayNightCycle 참조
    public GameManager gameManager;
    public DayNightCycle dayNightCycle;

    // 이동 관련 변수들
    private Rigidbody2D rigidBody;
    public float baseSpeed;
    public float boostedSpeed;
    private float currentSpeed;
    public Vector2 inputVector;
    private Vector3 directionVector;

    // 스프라이트 및 애니메이션 관련 변수들
    private SpriteRenderer spriteRenderer;
    public Animator animator;

    // 스캔할 오브젝트 참조
    private GameObject scanObject;
    // 화살표 이미지 객체
    public Image arrowSpriteImage;

    // 이동 효과음 관련 변수들
    private AudioSource movementAudioSource;
    public AudioClip movementSound;

    // 파티클 시스템 참조
    private ParticleSystem movementParticleSystem;

    // 플레이어 상태 참조
    private PlayerStatus playerStatus;

    public Tilemap roofTilemap;

    // 체력이 이 값 이하로 떨어지면 화살표가 동물 대신 쉴 곳(집)을 가리킴
    public float lowStaminaThreshold = 30f;
    // 침대에서 쉬는 동안에는 움직이지 않음
    public bool isResting;
    private House house;

    // 컴포넌트 초기화
    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        baseSpeed = 3f;
        boostedSpeed = 4f;
        currentSpeed = baseSpeed;

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // 화살표 이미지 객체 초기화
        if (arrowSpriteImage != null)
        {
            arrowSpriteImage.gameObject.SetActive(false);
        }

        // 이동 효과음용 AudioSource 초기화
        movementAudioSource = gameObject.AddComponent<AudioSource>();
        movementAudioSource.clip = movementSound;
        movementAudioSource.loop = true;
        movementAudioSource.playOnAwake = false;

        // 파티클 시스템 초기화
        movementParticleSystem = GetComponentInChildren<ParticleSystem>();
        if (movementParticleSystem != null)
        {
            var emission = movementParticleSystem.emission;
            emission.enabled = false;
        }

        // PlayerStatus 참조 초기화
        playerStatus = FindObjectOfType<PlayerStatus>();
        house = FindAnyObjectByType<House>();
    }

    // 매 프레임 호출되는 업데이트 메서드
    void Update()
    {
        UpdateDirectionVector(); // 이동 방향 벡터 업데이트
        HandleDialogueInput(); // 대화 입력 처리
        HandleCaptureInput(); // 울타리에 동물 넣기 처리
        UpdatePlayerSpeed(); // 플레이어 속도 업데이트
        UpdateArrowDirection(); // 화살표 방향 업데이트
        HandleMovementSound(); // 이동 효과음 처리
        HandleStaminaRecovery(); // 스태미나 회복 처리
    }

    // 물리 업데이트 메서드
    private void FixedUpdate()
    {
        if (gameManager == null || dayNightCycle == null)
        {
            Debug.LogError("GameManager 또는 DayNightCycle이 할당되지 않았습니다.");
            return;
        }

        if (gameManager.IsInitialDialogue()) // 처음 다이얼로그를 재생 중일 때는 움직이지 않음
            return;

        if (isResting) // 침대에서 쉬는 중에는 움직이지 않음
            return;

        MovePlayer(); // 플레이어 이동 처리
        DetectObject(); // 객체 감지
    }

    // 이동 입력 처리
    private void OnMove(InputValue value)
    {
        inputVector = value.Get<Vector2>();
    }

    // 충돌 처리
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Animal"))
        {
            Animal animal = collision.gameObject.GetComponent<Animal>();
            animal?.StartFollowingPlayer();
        }
    }

    // 트리거 충돌 처리
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Animal animal = collision.gameObject.GetComponent<Animal>();
            if (animal != null && animal.isFollowing)
            {
                animal.StopFollowingPlayer();
                // 울타리 위치를 설정하고 울타리 안으로 넣기
                Vector2 fencePosition = new Vector2(4, 4); // 울타리의 중심 위치
                animal.CapturedIn(fencePosition);
            }
        }
    }

    // 애니메이션 업데이트
    private void LateUpdate()
    {
        UpdateAnimation();
    }

    // 이동 방향 벡터 업데이트
    private void UpdateDirectionVector()
    {
        if (inputVector.y == 1) directionVector = Vector3.up;
        else if (inputVector.y == -1) directionVector = Vector3.down;
        else if (inputVector.x == 1) directionVector = Vector3.right;
        else if (inputVector.x == -1) directionVector = Vector3.left;
    }

    // 대화 입력 처리
    private void HandleDialogueInput()
    {
        if (gameManager == null)
        {
            Debug.LogError("GameManager가 할당되지 않았습니다.");
            return;
        }

        if (Input.GetButtonDown("Jump") && scanObject != null)
        {
            gameManager.ScanAction(scanObject);
        }
    }

    // 플레이어 속도 업데이트
    private void UpdatePlayerSpeed()
    {
        if (dayNightCycle == null)
        {
            Debug.LogError("DayNightCycle이 할당되지 않았습니다.");
            return;
        }

        float currentBaseSpeed = Input.GetKey(KeyCode.LeftShift) ? boostedSpeed : baseSpeed;

        int followingAnimalsCount = GetFollowingAnimalsCount();
        currentSpeed = Mathf.Max(2.5f, currentBaseSpeed - 0.1f * followingAnimalsCount);

        // 체력은 실제로 움직일 때만 더 닳게 하고, 걷기를 1배로 두고 달리기는 그 비율만큼 더 닳게 함
        float staminaFactor = inputVector.magnitude > 0 ? currentBaseSpeed / baseSpeed : 0f;
        dayNightCycle.UpdatePlayerSpeed(staminaFactor);
    }

    // 플레이어 이동 처리
    private void MovePlayer()
    {
        Vector2 nextVector = inputVector.normalized * currentSpeed * Time.fixedDeltaTime;
        rigidBody.MovePosition(rigidBody.position + nextVector);
    }

    // 울타리를 보고 Space 바를 누르면 따라오는 동물을 울타리에 넣음
    // (키 입력은 FixedUpdate가 돌지 않는 프레임에 놓칠 수 있어서 Update에서 처리)
    private void HandleCaptureInput()
    {
        ObjectData objectData = scanObject != null ? scanObject.GetComponent<ObjectData>() : null;
        if (objectData == null || objectData.id != 1)
            return;

        if (!gameManager.HasFollowingAnimals())
        {
            InteractionHint.Show("울타리에 넣을 동물이 없어! 도망친 동물부터 잡아 와.", 1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (GameObject animal in GameObject.FindGameObjectsWithTag("Animal"))
            {
                Animal animalScript = animal.GetComponent<Animal>();
                if (animalScript != null && animalScript.isFollowing)
                {
                    Vector2 fencePosition = new Vector2(4, 4); // 울타리의 중심 위치
                    animalScript.CapturedIn(fencePosition);
                }
            }
        }
    }

    // 객체 감지 처리
    private void DetectObject()
    {
        Debug.DrawRay(rigidBody.position, directionVector * 0.7f, new Color(0, 1, 0));
        RaycastHit2D rayHit = Physics2D.Raycast(rigidBody.position, directionVector, 0.7f, LayerMask.GetMask("Object"));

        if (rayHit.collider != null)
        {
            scanObject = rayHit.collider.gameObject;
            ObjectData objectData = scanObject.GetComponent<ObjectData>();

            if (objectData != null && objectData.id >= 0 && gameManager != null)
            {
                if (gameManager.HasFollowingAnimals() && !gameManager.dialoguePanel.activeSelf)
                {
                    gameManager.ShowDialogue(objectData.id);
                }
            }
        }
        else
        {
            scanObject = null;
            if (gameManager != null)
            {
                gameManager.CloseScanAction();
            }
        }
    }

    // 애니메이션 업데이트
    private void UpdateAnimation()
    {
        if (gameManager == null)
        {
            Debug.LogError("GameManager가 할당되지 않았습니다.");
            return;
        }

        if (gameManager.dialoguePanel.activeSelf) return;

        if (inputVector.magnitude > 0)
        {
            if (inputVector.y > 0) animator.SetTrigger("MoveUp");
            else if (inputVector.y < 0) animator.SetTrigger("MoveDown");
            else if (inputVector.x > 0) animator.SetTrigger("MoveRight");
            else if (inputVector.x < 0) animator.SetTrigger("MoveLeft");
        }
        else
        {
            animator.SetTrigger("Idle");
        }
    }

    // 이동 효과음 처리
    private void HandleMovementSound()
    {
        if (gameManager != null && gameManager.IsInitialDialogue())
        {
            if (movementAudioSource.isPlaying)
            {
                movementAudioSource.Stop();
            }

            if (movementParticleSystem != null)
            {
                var emission = movementParticleSystem.emission;
                emission.enabled = false;
            }

            return;
        }

        if (inputVector.magnitude > 0)
        {
            if (!movementAudioSource.isPlaying)
            {
                movementAudioSource.Play();
            }

            if (movementParticleSystem != null)
            {
                var emission = movementParticleSystem.emission;
                emission.enabled = true;
            }
        }
        else
        {
            if (movementAudioSource.isPlaying)
            {
                movementAudioSource.Stop();
            }

            if (movementParticleSystem != null)
            {
                var emission = movementParticleSystem.emission;
                emission.enabled = false;
            }
        }
    }

    // 스태미나 회복 처리
    private void HandleStaminaRecovery()
    {
        if (inputVector.magnitude == 0)
        {
            playerStatus.IncreaseStamina(playerStatus.recoveryRate * Time.deltaTime);
        }
    }

    // 화살표 방향 업데이트
    private void UpdateArrowDirection()
    {
        if (arrowSpriteImage == null)
        {
            Debug.LogError("Arrow Sprite Image가 할당되지 않았습니다.");
            return;
        }

        // 체력이 부족하면 가장 가까운 동물 대신 쉴 곳(밖에서는 집 문, 집 안에서는 침대)을 가리킴
        if (house != null && playerStatus.stamina <= lowStaminaThreshold)
        {
            arrowSpriteImage.gameObject.SetActive(true);
            PointArrowAt(house.GetRestPoint(transform.position));
            return;
        }

        // 동물 태그를 가진 오브젝트들을 찾음
        GameObject[] animals = GameObject.FindGameObjectsWithTag("Animal");
        if (animals.Length == 0)
        {
            arrowSpriteImage.gameObject.SetActive(false);
            return;
        }

        // 가장 가까운 동물을 찾음
        GameObject closestAnimal = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject animal in animals)
        {
            Animal animalScript = animal.GetComponent<Animal>();
            if (animalScript == null || animalScript.isFollowing || animalScript.isCaptured)
            {
                continue; // isFollowing 중이거나 isCaptured 상태인 동물은 제외
            }

            float distance = Vector2.Distance(transform.position, animal.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestAnimal = animal;
            }
        }

        if (closestAnimal != null)
        {
            arrowSpriteImage.gameObject.SetActive(true);
            PointArrowAt(closestAnimal.transform.position);
        }
        else
        {
            arrowSpriteImage.gameObject.SetActive(true);

            // 모든 동물이 잡혔을 경우 0,0을 가리킴
            PointArrowAt(Vector2.zero);
        }
    }

    // 화살표가 target 쪽을 가리키도록 회전
    private void PointArrowAt(Vector2 target)
    {
        Vector2 direction = target - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        RectTransform arrowRectTransform = arrowSpriteImage.GetComponent<RectTransform>();
        if (arrowRectTransform != null)
        {
            arrowRectTransform.localRotation = Quaternion.Euler(0, 0, angle - 135); // 고양이 발 아이콘은 왼쪽 위(135도)를 가리키므로 그만큼 빼서 맞춤
        }
    }

    // 따라오는 동물의 수를 반환하는 메서드
    private int GetFollowingAnimalsCount()
    {
        int count = 0;
        foreach (GameObject animal in GameObject.FindGameObjectsWithTag("Animal"))
        {
            Animal animalScript = animal.GetComponent<Animal>();
            if (animalScript != null && animalScript.isFollowing)
            {
                count++;
            }
        }
        return count;
    }
}
