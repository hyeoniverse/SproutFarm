using UnityEngine;
using Pathfinding;
using System.Collections;
using UnityEngine.Tilemaps;
using System.IO;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(AIDestinationSetter))]
[RequireComponent(typeof(AudioSource))]
public class Animal : MonoBehaviour
{
    public float baseSpeed = 3.0f;
    public float followDistance = 2.0f;
    public float fleeDistance = 3.0f;
    public float randomMoveRadius = 5.0f;
    public int characterSortingOrder = 3; // 플레이어 SpriteRenderer와 같은 값
    public float catchUpSpeedMultiplier = 2f; // 따라오다 화면 밖으로 벗어나면 이 배율로 빨리 따라온다
    public float offScreenWarpDelay = 4f;     // 그래도 이 시간 넘게 화면 밖이면 화면 가장자리 바로 바깥으로 옮긴다
    public float activationRadius = 5.0f; // 오디오 활성화 반경
    public float maxVolume = 1.0f; // 최대 볼륨
    public float minVolume = 0.1f; // 최소 볼륨
    public AudioClip capturedSound; // 동물이 잡혔을 때 효과음
    public AudioClip capturedInSound; // 울타리 안으로 들어갈 때 효과음
    public AudioClip escapingSound;
    public AudioClip surprisedSound;

    private AIPath aiPath;
    private Seeker seeker;
    private Rigidbody2D rigid;
    private Collider2D collider2d;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AIDestinationSetter destinationSetter;
    private AudioSource mainAudioSource; // 기존 AudioSource
    private AudioSource capturedAudioSource; // capturedSound 재생용 AudioSource
    private AudioSource capturedInAudioSource; // capturedInSound 재생용 AudioSource
    private AudioSource escapingSoundAudioSource;
    private AudioSource surprisedSoundAudioSource;

    public bool isCaptured = true;
    public bool isFollowing = false;

    private Vector2 escapeTarget;


    private Transform playerTransform;
    private TilemapCollider2D[] fenceColliders;
    private float offScreenTime;
    private House house;
    private ContactFilter2D solidOnly; // 트리거는 빼고 실제로 막히는 충돌체만 검사
    private readonly Collider2D[] overlapResults = new Collider2D[1];

    void Awake()
    {
        InitializeComponents();
        SetSortingOrder();
        aiPath.maxSpeed = baseSpeed;
        aiPath.canMove = false;
        destinationSetter.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Animal");
    }

    void Start()
    {
        isFollowing = false;

        FindPlayerTransform();
        FindFenceColliders();
        house = FindAnyObjectByType<House>();
        solidOnly.useTriggers = false;
        StartCoroutine(RandomMovement());
    }

    void Update()
    {
        HandleMovement();
        HandleAnimation();
        CheckPlayerDistance(); // 플레이어와의 거리 체크
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StartFollowingPlayer();
        }
        else if (collision.CompareTag("Area"))
        {
            Physics2D.IgnoreCollision(collider2d, collision, true);
        }
    }

    private void InitializeComponents()
    {
        rigid = GetComponent<Rigidbody2D>();
        collider2d = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        seeker = GetComponent<Seeker>();
        aiPath = GetComponent<AIPath>();
        destinationSetter = GetComponent<AIDestinationSetter>();
        mainAudioSource = GetComponent<AudioSource>(); // 기존 AudioSource 초기화

        // 효과음 재생용 AudioSource 추가 및 초기화
        capturedAudioSource = gameObject.AddComponent<AudioSource>();
        capturedAudioSource.playOnAwake = false;

        capturedInAudioSource = gameObject.AddComponent<AudioSource>();
        capturedInAudioSource.playOnAwake = false;

        escapingSoundAudioSource = gameObject.AddComponent<AudioSource>();
        escapingSoundAudioSource.playOnAwake = false;

        surprisedSoundAudioSource = gameObject.AddComponent<AudioSource>();
        surprisedSoundAudioSource.playOnAwake = false;
    }

    // 플레이어·나무·울타리와 같은 순서에 두고, 화면 아래쪽에 있는 것이 앞에 그려지게 한다
    // (Renderer2D의 투명 정렬 축이 Y축이라 같은 순서끼리는 y 위치로 앞뒤가 정해진다)
    private void SetSortingOrder()
    {
        spriteRenderer.sortingOrder = characterSortingOrder;
    }

    private void FindPlayerTransform()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError("Player 객체가 씬에 없습니다. Player 태그가 올바르게 설정되었는지 확인하세요.");
        }
    }

    private void FindFenceColliders()
    {
        GameObject[] fenceObjects = GameObject.FindGameObjectsWithTag("Fence");
        fenceColliders = new TilemapCollider2D[fenceObjects.Length];
        for (int i = 0; i < fenceObjects.Length; i++)
        {
            fenceColliders[i] = fenceObjects[i].GetComponent<TilemapCollider2D>();
        }
    }

    private void HandleMovement()
    {
        if (isFollowing)
        {
            aiPath.canMove = true;
            destinationSetter.target = playerTransform;
            KeepNearScreen();
        }
        else if (!isCaptured && Vector2.Distance(transform.position, playerTransform.position) <= fleeDistance)
        {
            FleeFromPlayer();
        }
    }

    // 따라오는 동물이 화면 밖에 오래 머물지 않게 한다: 벗어나면 빨리 따라오고,
    // 그래도 오래 걸리면 같은 방향의 화면 가장자리 바로 바깥으로 옮겨 걸어 들어오는 모습만 보이게 한다.
    private void KeepNearScreen()
    {
        float followSpeed = GameManager.instance.player.baseSpeed;
        Camera mainCamera = Camera.main;
        Vector3 viewport = mainCamera.WorldToViewportPoint(transform.position);
        bool offScreen = viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f;
        if (!offScreen)
        {
            offScreenTime = 0f;
            aiPath.maxSpeed = followSpeed;
            return;
        }

        aiPath.maxSpeed = followSpeed * catchUpSpeedMultiplier;
        offScreenTime += Time.deltaTime;
        if (offScreenTime < offScreenWarpDelay)
            return;

        Vector3 edge = new Vector3(Mathf.Clamp(viewport.x, -0.05f, 1.05f), Mathf.Clamp(viewport.y, -0.05f, 1.05f), viewport.z);
        Vector3 target = mainCamera.ViewportToWorldPoint(edge);
        target.z = transform.position.z;

        // 나무나 울타리 속, 지붕 아래에는 두지 않는다 (막히면 다음 프레임에 다시 시도)
        bool blocked = Physics2D.OverlapCircle(target, 0.4f, solidOnly, overlapResults) > 0
            || (house != null && house.IsUnderRoof(target));
        if (!blocked)
        {
            aiPath.Teleport(target);
            offScreenTime = 0f;
        }
    }

    private void HandleAnimation()
    {
        // 동물은 물리 업데이트에서만 움직여서, 위치 비교로 판단하면 물리 업데이트가 없는 프레임마다
        // 멈춘 것으로 보여 달리기/대기 애니메이션이 번갈아 깜빡였다. AIPath가 계산한 속도로 판단하고,
        // 아주 작은 좌우 흔들림에는 방향을 뒤집지 않는다.
        Vector2 velocity = aiPath.velocity;
        animator.SetBool("isRunning", velocity.sqrMagnitude > 0.01f);
        if (Mathf.Abs(velocity.x) > 0.1f)
        {
            spriteRenderer.flipX = velocity.x < 0f;
        }
    }

    private void CheckPlayerDistance()
    {
        if (playerTransform == null)
            return;

        if (isFollowing || isCaptured)
        {
            if (mainAudioSource.isPlaying)
            {
                mainAudioSource.Stop();
            }
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= activationRadius)
        {
            if (!mainAudioSource.isPlaying)
            {
                mainAudioSource.Play();
            }

            // 거리 비례하여 볼륨 조절
            float volume = Mathf.Lerp(maxVolume, minVolume, distanceToPlayer / activationRadius);
            mainAudioSource.volume = volume;
        }
        else
        {
            if (mainAudioSource.isPlaying)
            {
                mainAudioSource.Stop();
            }
        }
    }

    public void StartFollowingPlayer()
    {
        if (!isFollowing)
        {
            isFollowing = true;
            aiPath.canMove = true;
            destinationSetter.enabled = true;
            aiPath.maxSpeed = GameManager.instance.player.baseSpeed;
            destinationSetter.target = playerTransform;
            aiPath.endReachedDistance = followDistance;
            StopCoroutine(RandomMovement());

            // 효과음 재생
            if (capturedSound != null)
            {
                capturedAudioSource.PlayOneShot(capturedSound);
            }
            else
            {
                Debug.LogWarning("capturedSound is not set.");
            }
        }
    }

    public void StopFollowingPlayer()
    {
        isFollowing = false;
        aiPath.canMove = false;
        destinationSetter.enabled = false;
        StartCoroutine(RandomMovement());
    }

    public void Escape()
    {
        escapeTarget = new Vector2(10, 10);

        isCaptured = false;
        isFollowing = false;

        transform.position = escapeTarget;
        aiPath.canMove = true;
        destinationSetter.target = null;

        // 탈출 시 효과음 재생
        if (escapingSound != null && surprisedSound != null)
        {
            surprisedSoundAudioSource.PlayOneShot(surprisedSound);
            //escapingSoundAudioSource.PlayOneShot(escapingSound);
        }
        else
        {
            Debug.LogWarning("escapingSound or surprisedSound is not set.");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            StartFollowingPlayer();
        }
    }

    private void FleeFromPlayer()
    {
        Vector2 fleeDirection = (transform.position - playerTransform.position).normalized;
        Vector2 fleePosition = (Vector2)transform.position + fleeDirection * fleeDistance;

        aiPath.destination = fleePosition;
        aiPath.canMove = true;
        aiPath.SearchPath();
    }

    private IEnumerator RandomMovement()
    {
        while (!isFollowing)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            Vector2 randomPosition = (Vector2)transform.position + randomDirection * randomMoveRadius;

            aiPath.destination = randomPosition;
            aiPath.canMove = true;
            aiPath.SearchPath();

            yield return new WaitForSeconds(Random.Range(3, 7));
        }
    }

    public void CapturedIn(Vector2 position)
    {
        StopFollowingPlayer();
        isCaptured = true;
        transform.position = position; // 위치를 즉시 변경
        aiPath.canMove = false;
        destinationSetter.target = null;

        // 울타리 안으로 들어갈 때 효과음 재생
        if (capturedInSound != null)
        {
            capturedInAudioSource.PlayOneShot(capturedInSound);
        }
        else
        {
            Debug.LogWarning("capturedInSound is not set.");
        }
    }
}
