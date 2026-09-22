using System.Collections.Generic;
using UnityEngine;

// 맵이 끝없이 이어지므로 열매를 미리 깔아두지 않고,
// 플레이어 주변 화면 밖에 계속 뿌려서 어디를 가든 열매를 만날 수 있게 한다.
public class BerrySpawner : MonoBehaviour
{
    public Sprite berrySprite;
    public AudioClip eatSound;

    public int maxBerries = 8;
    public float staminaAmount = 10f;  // 열매 하나로 채워지는 체력
    public float respawnDelay = 6f;    // 열매를 먹은 뒤 새 열매가 생기기까지 걸리는 시간
    public float spawnRingWidth = 6f;  // 화면 가장자리 바깥으로 열매를 뿌리는 폭
    public float minBerrySpacing = 2f;
    public float berryRadius = 0.35f;

    private readonly List<Berry> berries = new List<Berry>();
    private readonly Collider2D[] overlapResults = new Collider2D[1];
    private ContactFilter2D solidOnly; // 트리거는 빼고 실제로 막히는 충돌체만 검사

    private Player player;
    private PlayerStatus playerStatus;
    private House house;
    private AudioSource eatAudioSource;
    private float nextSpawnTime;

    private void Start()
    {
        player = GameManager.instance.player;
        playerStatus = FindObjectOfType<PlayerStatus>();
        house = FindObjectOfType<House>();
        eatAudioSource = gameObject.AddComponent<AudioSource>();
        solidOnly.useTriggers = false;

        // 시작할 때는 화면 안에도 몇 개 보이도록 가까운 곳부터 뿌린다
        Material spriteMaterial = player.GetComponent<SpriteRenderer>().sharedMaterial;
        for (int i = 0; i < maxBerries; i++)
        {
            Berry berry = CreateBerry(spriteMaterial);
            TryPlace(berry, 2f, ViewRadius() + spawnRingWidth);
        }
    }

    private void Update()
    {
        float viewRadius = ViewRadius();
        Vector2 playerPosition = player.transform.position;

        // 너무 멀어진 열매는 거둬서 플레이어 근처에 다시 뿌린다
        foreach (Berry berry in berries)
        {
            if (berry.gameObject.activeSelf && Vector2.Distance(berry.transform.position, playerPosition) > viewRadius + spawnRingWidth * 2f)
            {
                berry.gameObject.SetActive(false);
            }
        }

        if (Time.time < nextSpawnTime)
            return;

        Berry idleBerry = berries.Find(berry => !berry.gameObject.activeSelf);
        if (idleBerry != null)
        {
            TryPlace(idleBerry, viewRadius + 1f, viewRadius + spawnRingWidth);
        }
    }

    public void TryEat(Berry berry)
    {
        // 체력이 가득 차 있으면 아껴두었다가 나중에 먹는다
        if (playerStatus.stamina >= playerStatus.maxStamina)
            return;

        playerStatus.IncreaseStamina(staminaAmount);
        if (eatSound != null)
        {
            eatAudioSource.PlayOneShot(eatSound);
        }
        berry.gameObject.SetActive(false);
        nextSpawnTime = Time.time + respawnDelay;
    }

    private Berry CreateBerry(Material spriteMaterial)
    {
        GameObject berryObject = new GameObject("Berry");
        berryObject.transform.SetParent(transform, false);

        SpriteRenderer spriteRenderer = berryObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = berrySprite;
        spriteRenderer.sharedMaterial = spriteMaterial;
        spriteRenderer.sortingOrder = 1; // 땅 위, 플레이어 아래

        CircleCollider2D trigger = berryObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = berryRadius;

        Berry berry = berryObject.AddComponent<Berry>();
        berry.spawner = this;
        berryObject.SetActive(false);
        berries.Add(berry);
        return berry;
    }

    private bool TryPlace(Berry berry, float minDistance, float maxDistance)
    {
        Vector2 center = player.transform.position;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(minDistance, maxDistance);
            if (IsFreeSpot(position))
            {
                berry.transform.position = position;
                berry.gameObject.SetActive(true);
                return true;
            }
        }
        return false;
    }

    // 나무·울타리·동물 같은 충돌체나 지붕 아래, 다른 열매 바로 옆은 피한다
    private bool IsFreeSpot(Vector2 position)
    {
        if (house != null && (house.IsUnderRoof(position) || house.IsInside(position)))
            return false;

        if (Physics2D.OverlapCircle(position, berryRadius, solidOnly, overlapResults) > 0)
            return false;

        foreach (Berry other in berries)
        {
            if (other.gameObject.activeSelf && Vector2.Distance(other.transform.position, position) < minBerrySpacing)
                return false;
        }
        return true;
    }

    // 화면 중심에서 모서리까지의 거리 (창 크기에 따라 달라진다)
    private float ViewRadius()
    {
        Camera mainCamera = Camera.main;
        float halfHeight = mainCamera.orthographicSize;
        float halfWidth = halfHeight * mainCamera.aspect;
        return Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
    }
}
