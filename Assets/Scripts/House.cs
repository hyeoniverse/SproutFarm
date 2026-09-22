using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// 문으로 걸어 들어가면 지붕을 걷어 집 내부를 보여주고, 침대에서 쉬면 체력을 모두 채워준다.
public class House : MonoBehaviour
{
    // 집을 이루는 타일맵들
    public Tilemap wallTilemap;
    public Tilemap borderLineTilemap;
    public Tilemap doorTilemap;
    public Tilemap furnitureTilemap;
    public Tilemap[] roofTilemaps; // 플레이어가 안에 있을 때 숨길 지붕과 굴뚝

    public TileBase bedTile;
    public TileBase strayGrassTile; // 집 안에 잘못 칠해진 잔디 타일 (지붕을 걷으면 보이므로 지운다)

    // 벽을 뺀 집 내부 칸 범위. 앞벽은 바로 아래 줄이다.
    public RectInt interiorCells = new RectInt(-10, 1, 8, 4);

    public float roofFadeDuration = 0.3f;
    public float bedReach = 1.5f; // 침대에서 이 거리 안에서 Space 바를 누르면 잠든다
    public float nearHouseDistance = 1.5f; // 집에서 이 거리 안이면 문으로 들어갈 수 있다고 알려준다
    public float sleepFadeDuration = 0.5f;
    public float sleepDuration = 1.2f;
    public string sleepMessage = "쿨쿨... 푹 자고 일어났더니 체력이 가득 찼어!";

    private readonly List<Vector2Int> doorCells = new List<Vector2Int>();
    private Vector3 bedPosition;
    private bool hasBed;
    private bool isPlayerInside;
    private bool isSleeping;
    private Coroutine roofFadeCoroutine;

    private Player player;
    private PlayerStatus playerStatus;
    private Image sleepOverlay;
    private TMP_Text sleepText;

    private void Awake()
    {
        RemoveStrayTiles();
        OpenDoors();
        FindBed();
    }

    private void Start()
    {
        player = GameManager.instance.player;
        playerStatus = FindAnyObjectByType<PlayerStatus>();
        CreateSleepOverlay();
    }

    private void Update()
    {
        bool inside = IsInside(player.transform.position);
        if (inside != isPlayerInside)
        {
            isPlayerInside = inside;
            if (roofFadeCoroutine != null)
            {
                StopCoroutine(roofFadeCoroutine);
            }
            roofFadeCoroutine = StartCoroutine(Fade(roofTilemaps[0].color.a, inside ? 0f : 1f, roofFadeDuration, SetRoofAlpha));
        }

        if (CanSleep())
        {
            InteractionHint.Show("Space 바를 누르면 침대에서 푹 쉴 수 있어!", 1);
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartCoroutine(Sleep());
            }
        }
        else if (!isPlayerInside && IsNearHouse(player.transform.position))
        {
            InteractionHint.Show("문으로 걸어 들어가면 집에 들어갈 수 있어!");
        }
    }

    // 벽과 지붕까지 포함한 집 둘레에서 nearHouseDistance 안에 있는지 확인
    private bool IsNearHouse(Vector3 worldPosition)
    {
        Vector3 min = wallTilemap.CellToWorld(new Vector3Int(interiorCells.xMin - 1, interiorCells.yMin - 1, 0));
        Vector3 max = wallTilemap.CellToWorld(new Vector3Int(interiorCells.xMax + 1, interiorCells.yMax + 2, 0));
        float dx = Mathf.Max(min.x - worldPosition.x, 0f, worldPosition.x - max.x);
        float dy = Mathf.Max(min.y - worldPosition.y, 0f, worldPosition.y - max.y);
        return dx * dx + dy * dy <= nearHouseDistance * nearHouseDistance;
    }

    // 집 안(문간 포함)인지 확인
    public bool IsInside(Vector3 worldPosition)
    {
        Vector2Int cell = (Vector2Int)WorldToCell(worldPosition);
        return interiorCells.Contains(cell) || doorCells.Contains(cell);
    }

    // 지붕이 덮고 있는 곳인지 확인 (열매를 두면 가려지는 곳)
    public bool IsUnderRoof(Vector3 worldPosition)
    {
        Vector3Int cell = WorldToCell(worldPosition);
        foreach (Tilemap roof in roofTilemaps)
        {
            if (roof.HasTile(cell))
                return true;
        }
        return false;
    }

    // 체력이 부족할 때 가야 할 곳: 집 안이면 침대, 밖이면 가장 가까운 문
    public Vector3 GetRestPoint(Vector3 from)
    {
        if (hasBed && IsInside(from))
            return bedPosition;

        Vector3 closestDoor = transform.position;
        float closestDistance = float.MaxValue;
        foreach (Vector2Int door in doorCells)
        {
            Vector3 doorPosition = doorTilemap.GetCellCenterWorld((Vector3Int)door);
            float distance = Vector2.Distance(from, doorPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestDoor = doorPosition;
            }
        }
        return closestDoor;
    }

    private Vector3Int WorldToCell(Vector3 worldPosition)
    {
        Vector3Int cell = wallTilemap.WorldToCell(worldPosition);
        cell.z = 0;
        return cell;
    }

    private void RemoveStrayTiles()
    {
        if (strayGrassTile == null)
            return;

        Tilemap[] tilemaps = { borderLineTilemap, doorTilemap, furnitureTilemap };
        foreach (Vector2Int position in interiorCells.allPositionsWithin)
        {
            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap.GetTile((Vector3Int)position) == strayGrassTile)
                {
                    tilemap.SetTile((Vector3Int)position, null);
                }
            }
        }
    }

    // 앞벽(아래)과 뒷벽(위)에서 문이 있는 칸은 막히지 않게 해서 드나들 수 있게 한다
    private void OpenDoors()
    {
        int[] wallRows = { interiorCells.yMin - 1, interiorCells.yMax };
        foreach (int row in wallRows)
        {
            for (int x = interiorCells.xMin; x < interiorCells.xMax; x++)
            {
                Vector3Int cell = new Vector3Int(x, row, 0);
                if (!doorTilemap.HasTile(cell))
                    continue;

                // 벽은 규칙 타일이라 지우면 이웃 모양까지 바뀌므로 충돌만 끄고,
                // 경계선 타일은 바닥에 가려 보이지 않으므로 지운다
                wallTilemap.SetColliderType(cell, Tile.ColliderType.None);
                borderLineTilemap.SetTile(cell, null);
                doorCells.Add((Vector2Int)cell);
            }
        }
    }

    private void FindBed()
    {
        foreach (Vector2Int position in interiorCells.allPositionsWithin)
        {
            if (furnitureTilemap.GetTile((Vector3Int)position) == bedTile)
            {
                bedPosition = furnitureTilemap.GetCellCenterWorld((Vector3Int)position);
                hasBed = true;
                return;
            }
        }
    }

    private bool CanSleep()
    {
        return hasBed && isPlayerInside && !isSleeping
            && !GameManager.instance.dialoguePanel.activeSelf
            && Vector2.Distance(player.transform.position, bedPosition) <= bedReach;
    }

    // 화면이 어두워졌다 밝아지는 동안 푹 자고 체력을 모두 채운다
    private IEnumerator Sleep()
    {
        isSleeping = true;
        player.isResting = true;

        yield return StartCoroutine(Fade(0f, 1f, sleepFadeDuration, SetOverlayAlpha));
        sleepText.gameObject.SetActive(true);
        yield return new WaitForSeconds(sleepDuration);
        // 깨어나는 순간 100%가 보이도록 화면이 밝아지기 직전에 채운다
        playerStatus.IncreaseStamina(playerStatus.maxStamina);
        sleepText.gameObject.SetActive(false);
        yield return StartCoroutine(Fade(1f, 0f, sleepFadeDuration, SetOverlayAlpha));

        player.isResting = false;
        isSleeping = false;
    }

    private IEnumerator Fade(float from, float to, float duration, Action<float> apply)
    {
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            apply(Mathf.Lerp(from, to, time / duration));
            yield return null;
        }
        apply(to);
    }

    private void SetRoofAlpha(float alpha)
    {
        foreach (Tilemap roof in roofTilemaps)
        {
            Color color = roof.color;
            color.a = alpha;
            roof.color = color;
        }
    }

    private void SetOverlayAlpha(float alpha)
    {
        Color color = sleepOverlay.color;
        color.a = alpha;
        sleepOverlay.color = color;
    }

    // 잠들 때 화면을 덮을 검은 막과 안내 문구
    private void CreateSleepOverlay()
    {
        GameObject canvasObject = new GameObject("SleepOverlay", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        sleepOverlay = CreateFullScreenChild<Image>(canvasObject.transform, "Fade");
        sleepOverlay.color = new Color(0f, 0f, 0f, 0f);
        sleepOverlay.raycastTarget = false;

        sleepText = CreateFullScreenChild<TextMeshProUGUI>(canvasObject.transform, "Message");
        sleepText.font = GameManager.instance.dialogueText.font;
        sleepText.fontSize = 32f;
        sleepText.alignment = TextAlignmentOptions.Center;
        sleepText.color = Color.white;
        sleepText.text = sleepMessage;
        sleepText.raycastTarget = false;
        sleepText.gameObject.SetActive(false);
    }

    private static T CreateFullScreenChild<T>(Transform parent, string objectName) where T : Graphic
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(T));
        child.transform.SetParent(parent, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return child.GetComponent<T>();
    }
}
