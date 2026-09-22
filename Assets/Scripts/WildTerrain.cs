using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 농장에서 멀어져도 같은 벌판만 이어지지 않게, 플레이어 둘레의 땅을 chunkSize 칸짜리 구역으로 나눠
// 구역마다 숲·바위밭·꽃밭·과수원·빈터 중 하나를 깔아 둔다. 구역의 위치로 모양을 정하므로
// 같은 곳에 다시 가면 같은 풍경이 나오고, 멀리 갈수록 새로운 구역이 만들어진다.
// 농장 둘레(homeArea)는 원래 지형 그대로 두고, 나무·바위가 생기거나 사라지면 그 구역만 길찾기 그래프를 다시 읽는다.
public class WildTerrain : MonoBehaviour
{
    public Transform player;
    public int chunkSize = 20;
    public int viewChunks = 1;   // 플레이어가 있는 구역에서 이 칸 수만큼 둘레까지 만들어 둔다
    public int seed = 20260923;
    public RectInt homeArea = new RectInt(-22, -22, 44, 44); // 집·울타리·밭 둘레: 여기는 건드리지 않는다

    [Header("타일")]
    public TileBase[] trees;      // 키 큰 나무, 둥근 나무, 과일나무
    public TileBase[] mushrooms;
    public TileBase[] rocks;
    public TileBase[] stumps;     // 그루터기, 통나무
    public TileBase[] bushes;
    public TileBase[] flowers;    // 꽃, 새싹
    public TileBase sunflower;
    public TileBase[] baseGrass;  // 나무·해바라기 밑둥에 깔 작은 풀

    [Header("그리는 순서")]
    public TilemapRenderer rendererTemplate; // 재질·정렬 레이어를 가져올 타일맵
    public int flowerOrder = 3;              // 캐릭터와 같은 층 (y 위치로 앞뒤가 정해짐)
    public int objectOrder = 7;              // 나무와 같은 층 (캐릭터보다 위)
    public int baseGrassOrder = 8;           // 나무 밑둥에 깔리는 풀 (나무보다 위)
    public float baseOffset = 0.2f;

    private enum Theme { Woods, Rocky, Meadow, Orchard, Clearing }

    private Tilemap baseGrassMap;
    private Tilemap flowerMap;
    private Tilemap objectMap;
    private readonly HashSet<Vector2Int> builtChunks = new HashSet<Vector2Int>();
    private readonly Queue<Vector2Int> toBuild = new Queue<Vector2Int>();
    private readonly Queue<Vector2Int> toClear = new Queue<Vector2Int>();
    private Vector2Int center = new Vector2Int(int.MinValue, int.MinValue);

    private void Start()
    {
        if (player == null)
        {
            player = GameManager.instance.player.transform;
        }
        flowerMap = CreateTilemap("Wild Flowers", flowerOrder, TilemapRenderer.Mode.Individual, false);
        objectMap = CreateTilemap("Wild Objects", objectOrder, TilemapRenderer.Mode.Chunk, true);
        baseGrassMap = CreateTilemap("Wild Base Grass", baseGrassOrder, TilemapRenderer.Mode.Chunk, false);
    }

    private void Update()
    {
        // 한 프레임에 구역 하나씩만 만들거나 지워서 끊기지 않게 한다
        if (toClear.Count > 0)
        {
            Clear(toClear.Dequeue());
            return;
        }
        if (toBuild.Count > 0)
        {
            Build(toBuild.Dequeue());
            return;
        }

        Vector2Int playerChunk = ChunkOf(player.position);
        if (playerChunk == center)
            return;

        center = playerChunk;
        // 멀어진 구역은 지우고, 가까워진 구역은 새로 만든다
        var keep = new HashSet<Vector2Int>();
        for (int x = -viewChunks; x <= viewChunks; x++)
        {
            for (int y = -viewChunks; y <= viewChunks; y++)
            {
                keep.Add(new Vector2Int(playerChunk.x + x, playerChunk.y + y));
            }
        }

        var gone = new List<Vector2Int>();
        foreach (Vector2Int chunk in builtChunks)
        {
            if (!keep.Contains(chunk))
                gone.Add(chunk);
        }
        foreach (Vector2Int chunk in gone)
        {
            toClear.Enqueue(chunk);
            builtChunks.Remove(chunk);
        }
        foreach (Vector2Int chunk in keep)
        {
            if (builtChunks.Add(chunk))
            {
                toBuild.Enqueue(chunk);
            }
        }
    }

    private Vector2Int ChunkOf(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.y / chunkSize));
    }

    private Tilemap CreateTilemap(string name, int sortingOrder, TilemapRenderer.Mode mode, bool solid)
    {
        var tilemapObject = new GameObject(name);
        tilemapObject.layer = solid ? LayerMask.NameToLayer("Object") : gameObject.layer;
        tilemapObject.transform.SetParent(transform, false);
        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        TilemapRenderer tilemapRenderer = tilemapObject.AddComponent<TilemapRenderer>();
        tilemapRenderer.sharedMaterial = rendererTemplate.sharedMaterial;
        tilemapRenderer.sortingLayerID = rendererTemplate.sortingLayerID;
        tilemapRenderer.sortingOrder = sortingOrder;
        tilemapRenderer.mode = mode;
        if (solid)
        {
            tilemapObject.AddComponent<TilemapCollider2D>();
        }
        return tilemap;
    }

    private void Clear(Vector2Int chunk)
    {
        foreach (Vector3Int cell in CellsOf(chunk))
        {
            flowerMap.SetTile(cell, null);
            objectMap.SetTile(cell, null);
            baseGrassMap.SetTile(cell, null);
        }
        RescanPaths(chunk);
    }

    private void Build(Vector2Int chunk)
    {
        var random = new System.Random(seed ^ (chunk.x * 73856093) ^ (chunk.y * 19349663));
        Theme theme = (Theme)random.Next(System.Enum.GetValues(typeof(Theme)).Length);
        var taken = new HashSet<Vector3Int>();

        bool Free(Vector3Int cell, int spacing)
        {
            for (int x = -spacing; x <= spacing; x++)
            {
                for (int y = -spacing; y <= spacing; y++)
                {
                    if (taken.Contains(new Vector3Int(cell.x + x, cell.y + y, 0)))
                        return false;
                }
            }
            return true;
        }

        void Put(Tilemap tilemap, Vector3Int cell, TileBase tile)
        {
            tilemap.SetTile(cell, tile);
            taken.Add(cell);
            // 나무·해바라기는 밑둥 자리에 작은 풀을 깔아, 밑둥 쪽만 풀이 앞에 보이게 한다
            if (tilemap == objectMap && random.NextDouble() < 0.7)
            {
                BaseGrass.PlaceAtBase(baseGrassMap, objectMap, cell, baseGrass[random.Next(baseGrass.Length)], baseOffset);
            }
        }

        TileBase Pick(TileBase[] tiles) => tiles[random.Next(tiles.Length)];

        foreach (Vector3Int cell in CellsOf(chunk))
        {
            if (homeArea.Contains(new Vector2Int(cell.x, cell.y)))
                continue;

            double roll = random.NextDouble();
            switch (theme)
            {
                case Theme.Woods:
                    // 나무가 빽빽한 숲: 가로지르는 빈 줄을 남겨 지나갈 수 있게 한다
                    if ((cell.x + cell.y) % 7 == 0 || (cell.x - cell.y) % 9 == 0)
                        break;
                    if (roll < 0.34 && Free(cell, 1))
                        Put(objectMap, cell, trees[0]);
                    else if (roll < 0.44 && Free(cell, 2))
                        Put(objectMap, cell, Pick(trees));
                    else if (roll < 0.5)
                        flowerMap.SetTile(cell, Pick(mushrooms));
                    else if (roll < 0.53 && Free(cell, 1))
                        Put(objectMap, cell, Pick(stumps));
                    break;

                case Theme.Rocky:
                    if (roll < 0.14 && Free(cell, 1))
                        Put(objectMap, cell, Pick(rocks));
                    else if (roll < 0.18 && Free(cell, 1))
                        Put(objectMap, cell, Pick(stumps));
                    else if (roll < 0.22 && Free(cell, 1))
                        Put(objectMap, cell, Pick(bushes));
                    break;

                case Theme.Meadow:
                    if (roll < 0.5)
                        flowerMap.SetTile(cell, Pick(flowers));
                    else if (roll < 0.53 && Free(cell, 1))
                        Put(objectMap, cell, sunflower);
                    break;

                case Theme.Orchard:
                    // 과일나무를 세 칸 간격으로 줄 맞춰 심는다
                    if (cell.x % 3 == 0 && cell.y % 3 == 0 && Free(cell, 2))
                        Put(objectMap, cell, trees[trees.Length - 1]);
                    else if (roll < 0.25)
                        flowerMap.SetTile(cell, Pick(flowers));
                    break;

                case Theme.Clearing:
                    if (roll < 0.08 && Free(cell, 2))
                        Put(objectMap, cell, Pick(bushes));
                    else if (roll < 0.2)
                        flowerMap.SetTile(cell, Pick(flowers));
                    break;
            }
        }
        RescanPaths(chunk);
    }

    // 이 구역에 나무·바위가 생기거나 사라졌으니 길찾기 그래프에서 이 부분만 다시 읽는다
    private void RescanPaths(Vector2Int chunk)
    {
        if (AstarPath.active == null)
            return;

        var center3 = new Vector3((chunk.x + 0.5f) * chunkSize, (chunk.y + 0.5f) * chunkSize, 0f);
        AstarPath.active.UpdateGraphs(new Bounds(center3, new Vector3(chunkSize + 2, chunkSize + 2, 10f)));
    }

    private IEnumerable<Vector3Int> CellsOf(Vector2Int chunk)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                yield return new Vector3Int(chunk.x * chunkSize + x, chunk.y * chunkSize + y, 0);
            }
        }
    }
}
