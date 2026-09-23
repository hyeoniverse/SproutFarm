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
    // 달릴 때는 걸을 때보다 이 배율만큼 체력이 더 빨리 닳음
    public float runDrainMultiplier = 3f;
    // 쉬지 않고 움직일수록 체력이 점점 더 빨리 닳음: 계속 움직인 시간이 fatigueRampSeconds가 되면 maxFatigueMultiplier배
    public float fatigueRampSeconds = 60f;
    public float maxFatigueMultiplier = 2f;
    public float fatigueRecoveryRate = 3f; // 멈춰 있으면 쌓인 피로가 움직일 때보다 이 배만큼 빨리 풀림
    private float continuousMoveTime;
    // 체력이 떨어질수록 느려짐: 체력이 0이면 원래 속도의 minStaminaSpeedRatio배 (그보다 느려지지는 않음)
    public float minStaminaSpeedRatio = 0.6f;

    // 흙길 위에서는 걷기·달리기가 이 배율만큼 빨라지고 체력이 닳지 않는다
    public float pathSpeedMultiplier = 1.25f;
    public float feetOffset = 0.4f;   // 캐릭터 기준점에서 발까지의 높이
    // 열매를 먹으면 잠깐 빨라짐
    public float berrySpeedMultiplier = 1.5f;
    public float berrySpeedDuration = 4f;
    private float berrySpeedUntil;
    // 울타리(동물 우리)가 차지하는 곳과, 동물을 넣을 수 있는 거리
    public Rect penArea = new Rect(0f, 0f, 12f, 7f);
    public float fenceReach = 1.2f;
    // 대화를 Space 바로 닫은 바로 그 프레임의 입력이 울타리 판정에 쓰이지 않도록 지난 프레임 상태를 기억
    private bool dialogueOpenLastFrame;
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

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool isMoving = inputVector.magnitude > 0;
        float currentBaseSpeed = isRunning ? boostedSpeed : baseSpeed;

        int followingAnimalsCount = GetFollowingAnimalsCount();
        currentSpeed = Mathf.Max(2.5f, currentBaseSpeed - 0.1f * followingAnimalsCount);
        currentSpeed *= Mathf.Lerp(minStaminaSpeedRatio, 1f, playerStatus.stamina / playerStatus.maxStamina);
        if (Time.time < berrySpeedUntil)
        {
            currentSpeed *= berrySpeedMultiplier;
        }

        // 흙길 위에서는 조금 더 빠르고 체력도 닳지 않는다
        bool onPath = WildTerrain.OnPathAt(transform.position + Vector3.down * feetOffset);
        if (onPath)
        {
            currentSpeed *= pathSpeedMultiplier;
        }

        // 쉬지 않고 움직인 시간만큼 피로가 쌓이고, 멈춰 있으면 풀림
        continuousMoveTime = isMoving
            ? Mathf.Min(fatigueRampSeconds, continuousMoveTime + Time.deltaTime)
            : Mathf.Max(0f, continuousMoveTime - fatigueRecoveryRate * Time.deltaTime);
        float fatigue = Mathf.Lerp(1f, maxFatigueMultiplier, continuousMoveTime / fatigueRampSeconds);

        // 체력은 실제로 움직일 때만 더 닳게 하고, 걷기를 1배로 두고 달리기는 runDrainMultiplier배로 닳게 한 뒤 피로 배율을 곱함
        float staminaFactor = isMoving ? (isRunning ? runDrainMultiplier : 1f) * fatigue : 0f;
        dayNightCycle.UpdatePlayerSpeed(staminaFactor);
        dayNightCycle.SetOnPath(onPath);
    }

    // 열매를 먹으면 berrySpeedDuration초 동안 berrySpeedMultiplier배로 빨라짐
    public void ApplyBerrySpeedBonus()
    {
        berrySpeedUntil = Time.time + berrySpeedDuration;
    }

    // 플레이어 이동 처리
    private void MovePlayer()
    {
        Vector2 nextVector = inputVector.normalized * currentSpeed * Time.fixedDeltaTime;
        rigidBody.MovePosition(rigidBody.position + nextVector);
    }

    // 울타리 근처에서 Space 바를 누르면 따라오는 동물을 울타리에 넣음
    // (키 입력은 FixedUpdate가 돌지 않는 프레임에 놓칠 수 있어서 Update에서 처리)
    private void HandleCaptureInput()
    {
        bool dialogueOpen = gameManager.dialoguePanel.activeSelf;
        bool dialogueJustOpenOrClosed = dialogueOpen || dialogueOpenLastFrame;
        dialogueOpenLastFrame = dialogueOpen;
        if (!IsNearFence() || gameManager.IsInitialDialogue() || dialogueJustOpenOrClosed)
            return;

        InteractionHint.Show("Space 바를 누르면 데려온 동물을 울타리에 넣을 수 있어!", 1);
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        if (!gameManager.HasFollowingAnimals())
        {
            InteractionHint.Flash("울타리에 넣을 동물이 없어! 도망친 동물부터 잡아 와.", 2f);
            return;
        }

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

    // 울타리 바깥 둘레에서 fenceReach 안에 있는지 확인
    private bool IsNearFence()
    {
        Vector2 position = transform.position;
        float dx = Mathf.Max(penArea.xMin - position.x, 0f, position.x - penArea.xMax);
        float dy = Mathf.Max(penArea.yMin - position.y, 0f, position.y - penArea.yMax);
        return dx * dx + dy * dy <= fenceReach * fenceReach;
    }

    // 객체 감지 처리
    private void DetectObject()
    {
        Debug.DrawRay(rigidBody.position, directionVector * 0.7f, new Color(0, 1, 0));
        RaycastHit2D rayHit = Physics2D.Raycast(rigidBody.position, directionVector, 0.7f, LayerMask.GetMask("Object"));

        if (rayHit.collider != null)
        {
            scanObject = rayHit.collider.gameObject;
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
            // 나침반이 동물이 아닌 집을 가리키는 중임을 알려줌 (근처 안내가 있으면 그쪽이 먼저 보임)
            InteractionHint.Show("체력이 얼마 안 남았어! 지금 나침반은 집을 가리키고 있어. 집에 가서 침대에서 푹 쉬자!", -1);
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
