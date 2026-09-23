using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 나무·해바라기·울타리는 캐릭터와 꽃보다 위에 그려지므로, 밑둥에 겹친 꽃·새싹이 뒤로 숨어 어색해진다.
// 그래서 밑둥 자리에 작은 새싹·꽃을 한 겹 더 깔아 둔다. 이 층은 나무·울타리보다 위에 그리므로
// 밑둥 쪽은 풀이 앞에, 그 위(가로대·줄기·잎)는 나무·울타리가 앞에 보인다.
// 울타리 칸 안에 있던 꽃·새싹은 가로대 위로 올라와 보이므로 치운다.
public class BaseGrass : MonoBehaviour
{
    public TilemapRenderer[] grassRenderers;  // 꽃·새싹 타일맵
    public Tilemap fence;                     // 울타리 타일맵
    public TileBase fenceTile;                // 울타리 타일 (같은 타일맵의 벽 조각 등은 빼려고)
    public TilemapRenderer[] tallRenderers;   // 나무·해바라기 타일맵
    public TileBase[] tiles;                  // 밑둥에 깔 작은 새싹·꽃
    public TileBase[] bannedTiles;            // 쓰지 않을 장식 (한 칸에 새싹이 둘 그려진 그림 등)
    public int sortingOrder = 3;              // 캐릭터와 같은 층 (밑동 높이로 앞뒤가 정해짐)
    public int fenceGrassOrder = 3;           // 울타리 밑둥 풀도 같은 층
    public float baseOffset = 0.2f;           // 밑둥 그림 맨 아래에서 이만큼 위에 풀을 놓는다
    [Range(0f, 1f)] public float chance = 0.7f;
    public int seed = 7;

    private static readonly Dictionary<Sprite, Rect> spriteOutlines = new Dictionary<Sprite, Rect>();
    private System.Random random;
    private readonly List<Rect> objectAreas = new List<Rect>(); // 나무·헛간 그림이 차지하는 자리

    private void Start()
    {
        random = new System.Random(seed);
        Tilemap baseGrass = CreateTilemap("Base Grass", sortingOrder, TilemapRenderer.Mode.Individual);
        Tilemap fenceGrass = CreateTilemap("Fence Grass", fenceGrassOrder, TilemapRenderer.Mode.Individual);
        TidyGrass();
        CollectObjectAreas();

        if (fence != null)
        {
            foreach (Vector3Int cell in fence.cellBounds.allPositionsWithin)
            {
                if (fence.GetTile(cell) != fenceTile)
                    continue;

                ClearGrass(fence.GetCellCenterWorld(cell));
                // 세로로 이어진 울타리는 맨 아래 칸이 밑둥이다. 위 칸에 풀을 두면 가로대 위에 풀이 올라와 보인다.
                if (fence.GetTile(cell + Vector3Int.down) == fenceTile)
                    continue;

                PutFenceGrass(fenceGrass, fence, cell);
            }
        }

        foreach (TilemapRenderer tallRenderer in tallRenderers)
        {
            Tilemap tall = tallRenderer.GetComponent<Tilemap>();
            foreach (Vector3Int cell in tall.cellBounds.allPositionsWithin)
            {
                if (tall.GetSprite(cell) == null)
                    continue;

                ClearUnder(tall, cell);
                // 두 칸 이상을 차지하는 나무는 맨 아래 칸만 밑둥이다
                if (tall.GetSprite(cell + Vector3Int.down) == null)
                {
                    PutBaseGrass(baseGrass, tall, cell);
                }
            }
        }
    }

    private Tilemap CreateTilemap(string name, int order, TilemapRenderer.Mode mode)
    {
        var tilemapObject = new GameObject(name);
        tilemapObject.transform.SetParent(transform, false);
        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        TilemapRenderer tilemapRenderer = tilemapObject.AddComponent<TilemapRenderer>();
        tilemapRenderer.sharedMaterial = grassRenderers[0].sharedMaterial;
        tilemapRenderer.sortingLayerID = grassRenderers[0].sortingLayerID;
        tilemapRenderer.sortingOrder = order;
        tilemapRenderer.mode = mode;
        // 꽃 그림틀이 칸보다 높아서, 반 칸 올려야 그림이 제자리에 온다
        tilemapObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        return tilemap;
    }

    // 울타리 밑둥 풀. 캐릭터와 같은 층에 두므로 칸을 옮기지 않고 그대로 깐다.
    private void PutFenceGrass(Tilemap fenceGrass, Tilemap source, Vector3Int cell)
    {
        if (random.NextDouble() > chance)
            return;
        if (CoveredByObject(BasePosition(source, cell, baseOffset), AreaOf(source, cell)))
            return;

        Vector3 spot = source.GetCellCenterWorld(cell);
        if (PlantAt(spot) || PlantAt(spot + Vector3.up) || PlantAt(spot + Vector3.down))
            return;

        fenceGrass.SetTile(cell, tiles[random.Next(tiles.Length)]);
    }

    // 위아래로 붙어 있는 꽃·새싹은 아래 칸에 선 캐릭터 몸에 위 칸 그림이 걸친다.
    // 층이 달라도 마찬가지라 모든 꽃 타일맵을 한꺼번에 보고 위쪽 것을 치운다.
    private void TidyGrass()
    {
        foreach (TilemapRenderer grassRenderer in grassRenderers)
        {
            Tilemap grass = grassRenderer.GetComponent<Tilemap>();
            foreach (Vector3Int cell in grass.cellBounds.allPositionsWithin)
            {
                if (grass.GetTile(cell) == null)
                    continue;

                Vector3 here = grass.GetCellCenterWorld(cell);
                if (System.Array.IndexOf(bannedTiles, grass.GetTile(cell)) >= 0 || PlantAt(here + Vector3.down))
                {
                    grass.SetTile(cell, null);
                }
            }
        }
    }

    // 이 자리에 꽃·새싹이 있는지 (어느 층이든)
    public bool PlantAt(Vector3 worldPosition)
    {
        foreach (TilemapRenderer grassRenderer in grassRenderers)
        {
            Tilemap grass = grassRenderer.GetComponent<Tilemap>();
            if (grass.GetTile(grass.WorldToCell(worldPosition)) != null)
                return true;
        }
        return false;
    }

    // 꽃·새싹 타일맵들에서 이 자리의 장식을 지운다
    private void ClearGrass(Vector3 worldPosition)
    {
        foreach (TilemapRenderer grassRenderer in grassRenderers)
        {
            Tilemap grass = grassRenderer.GetComponent<Tilemap>();
            grass.SetTile(grass.WorldToCell(worldPosition), null);
        }
    }

    private void PutBaseGrass(Tilemap baseGrass, Tilemap source, Vector3Int cell)
    {
        if (random.NextDouble() > chance)
            return;
        // 이웃한 나무 잎이나 헛간 위에 풀이 얹히면 그림을 가려 어색하다
        if (CoveredByObject(BasePosition(source, cell, baseOffset), AreaOf(source, cell)))
            return;

        PlaceAtBase(baseGrass, source, cell, tiles[random.Next(tiles.Length)], baseOffset);
    }

    // 나무 그림이 덮는 칸의 꽃·새싹을 치운다. 같은 칸에 있으면 앞뒤 기준이 같아 잎 위로 올라와 보인다.
    private void ClearUnder(Tilemap source, Vector3Int cell)
    {
        Rect area = AreaOf(source, cell);
        for (float y = area.yMin + 0.5f; y < area.yMax; y += 1f)
        {
            for (float x = area.xMin + 0.5f; x < area.xMax; x += 1f)
            {
                ClearGrass(new Vector3(x, y, 0f));
            }
        }
    }

    // 나무·해바라기·헛간 그림이 차지하는 자리를 모아 둔다
    private void CollectObjectAreas()
    {
        foreach (TilemapRenderer tallRenderer in tallRenderers)
        {
            Tilemap tall = tallRenderer.GetComponent<Tilemap>();
            foreach (Vector3Int cell in tall.cellBounds.allPositionsWithin)
            {
                if (tall.GetSprite(cell) != null)
                {
                    objectAreas.Add(AreaOf(tall, cell));
                }
            }
        }
    }

    private bool CoveredByObject(Vector3 position, Rect own)
    {
        foreach (Rect area in objectAreas)
        {
            if (area == own)
                continue;
            if (area.Contains(position))
                return true;
        }
        return false;
    }

    // 이 칸의 그림이 실제로 덮는 자리 (세계 좌표)
    private static Rect AreaOf(Tilemap source, Vector3Int cell)
    {
        Sprite sprite = source.GetSprite(cell);
        if (sprite == null)
            return Rect.zero;

        Rect outline = OutlineOf(sprite);
        Vector3 shift = source.GetTransformMatrix(cell).GetColumn(3);
        Vector3 origin = source.GetCellCenterWorld(cell) + shift;
        return new Rect(origin.x + outline.xMin, origin.y + outline.yMin, outline.width, outline.height);
    }

    // source의 cell에 있는 그림 맨 아래(밑둥) 자리
    public static Vector3 BasePosition(Tilemap source, Vector3Int cell, float offset)
    {
        Rect outline = OutlineOf(source.GetSprite(cell));
        Vector3 shift = source.GetTransformMatrix(cell).GetColumn(3);
        return source.GetCellCenterWorld(cell) + shift + new Vector3(outline.center.x, outline.yMin + offset, 0f);
    }

    // source의 cell에 있는 그림 맨 아래(밑둥) 높이에 맞춰 baseGrass에 작은 풀 타일을 놓는다
    public static void PlaceAtBase(Tilemap baseGrass, Tilemap source, Vector3Int cell, TileBase tile, float offset)
    {
        Vector3 bottom = BasePosition(source, cell, offset);

        var target = new Vector3Int(Mathf.FloorToInt(bottom.x), Mathf.FloorToInt(bottom.y), 0);
        if (baseGrass.HasTile(target))
            return;

        // 칸 안에서 옮기면 앞뒤 순서가 어긋나므로, 밑둥이 있는 칸에 그대로 놓는다
        baseGrass.SetTile(target, tile);
    }

    // 그림에서 실제로 칠해진 부분의 범위 (스프라이트 기준점 기준, 유닛 단위)
    public static Rect OutlineOf(Sprite sprite)
    {
        if (spriteOutlines.TryGetValue(sprite, out Rect outline))
            return outline;

        Vector2[] vertices = sprite.vertices;
        Vector2 min = vertices[0];
        Vector2 max = vertices[0];
        foreach (Vector2 vertex in vertices)
        {
            min = Vector2.Min(min, vertex);
            max = Vector2.Max(max, vertex);
        }
        outline = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        spriteOutlines[sprite] = outline;
        return outline;
    }
}
