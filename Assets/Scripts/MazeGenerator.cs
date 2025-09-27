using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Settings")]
    [SerializeField] int _mazeGridSize = 10;
    [SerializeField] float _cellSize = 2f;

    [Header("Maze Parts")]
    [SerializeField] GameObject _wallPrefab;
    [SerializeField] GameObject _groundPrefab;
    [SerializeField] Transform _wallParent;

    [Header("Wall Settings")]
    [SerializeField] Vector3 _wallScale = new Vector3(2f, 3f, 2f);

    [Header("Props")]
    [SerializeField] GameObject _spawnPointPrefab;
    [SerializeField] GameObject _keyPrefab;
    [SerializeField] GameObject _doorPrefab;

    [Header("Other")]
    [SerializeField] Transform _cameraTransform;

    [SerializeField] TextMeshProUGUI _rewardText;
    [SerializeField] TextMeshProUGUI _episodeText;

    List<GameObject> _mazeObjects = new List<GameObject>();
    List<MeshFilter> _wallMeshFilters = new List<MeshFilter>();

    Transform _key;
    Transform _door;

    float maxReward = 0;
    private int[,] _maze;

    private void Start()
    {
        maxReward = 0;
    }

    public void Generate()
    {
        DeleteMaze();
        _maze = new int[_mazeGridSize, _mazeGridSize];
        GenerateMaze(0, 0);
        BuildMaze();
        CombineWalls();
        PlaceGround();
        SpawnProps();
        AdjustCamera();
    }

    public Vector3 GetKeyPos() => _key.localPosition;

    public void DestroyKey()
    {
        if (_mazeObjects.Contains(_key.gameObject))
            _mazeObjects.Remove(_key.gameObject);
        else
            Debug.Log("Oops!");

        Destroy(_key.gameObject);
    }

    public Vector3 GetDoorPos() => _door.localPosition;

    public void UpdateStatText(int episode, float reward, int step)
    {
        if (reward > maxReward) maxReward = reward;

        _rewardText.text = $"Reward: {reward} | Max Reward: {maxReward}";
        _episodeText.text = $"Episode: {episode} | Step: {step}";
    }

    void PlaceGround()
    {
        float groundPosX = (_mazeGridSize - 2) / 2f * _cellSize;
        float groundPosZ = (_mazeGridSize - 2) / 2f * _cellSize;
        Vector3 groundPos = new Vector3(groundPosX, 0, groundPosZ);

        GameObject ground = Instantiate(_groundPrefab, transform);
        ground.transform.localPosition = groundPos;
        _mazeObjects.Add(ground);

        float groundXScale = (_mazeGridSize + 2) / 10f * _cellSize;
        float groundZScale = (_mazeGridSize + 2) / 10f * _cellSize;
        ground.transform.localScale = new Vector3(groundXScale, 1, groundZScale);
    }

    void GenerateMaze(int x, int y)
    {
        _maze[x, y] = 1;

        List<Vector2Int> directions = new List<Vector2Int> { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Shuffle(directions);

        foreach (var dir in directions)
        {
            int nx = x + dir.x * 2;
            int ny = y + dir.y * 2;
            if (InBounds(nx, ny) && _maze[nx, ny] == 0)
            {
                _maze[x + dir.x, y + dir.y] = 1;
                GenerateMaze(nx, ny);
            }
        }
    }

    void BuildMaze()
    {
        _wallMeshFilters.Clear();

        for (int x = 0; x < _mazeGridSize; x++)
        {
            for (int y = 0; y < _mazeGridSize; y++)
            {
                if (_maze[x, y] == 0)
                    CreateWall(x, y);
            }
        }

        for (int x = -1; x <= _mazeGridSize - 1; x++)
        {
            CreateWall(x, -1);
            CreateWall(x, _mazeGridSize - 1);
        }

        for (int y = 0; y < _mazeGridSize; y++)
        {
            CreateWall(-1, y);
            CreateWall(_mazeGridSize - 1, y);
        }
    }

    void CreateWall(int x, int y)
    {
        GameObject wall = Instantiate(_wallPrefab, _wallParent);
        wall.transform.localPosition = new Vector3(x * _cellSize, 0, y * _cellSize);
        wall.transform.localScale = _wallScale;

        var mf = wall.GetComponent<MeshFilter>();
        if (mf != null) _wallMeshFilters.Add(mf);

        wall.SetActive(false);
        _mazeObjects.Add(wall);
    }

    void CombineWalls()
    {
        if (_wallMeshFilters.Count == 0) return;

        CombineInstance[] combine = new CombineInstance[_wallMeshFilters.Count];
        for (int i = 0; i < _wallMeshFilters.Count; i++)
        {
            combine[i].mesh = _wallMeshFilters[i].sharedMesh;
            combine[i].transform = _wallMeshFilters[i].transform.localToWorldMatrix;
            _wallMeshFilters[i].gameObject.SetActive(false);
        }

        GameObject combinedWall = new GameObject("CombinedWalls");
        combinedWall.transform.parent = _wallParent;
        combinedWall.transform.localPosition = Vector3.zero;
        combinedWall.transform.localRotation = Quaternion.identity;

        MeshFilter mfCombined = combinedWall.AddComponent<MeshFilter>();
        MeshRenderer mrCombined = combinedWall.AddComponent<MeshRenderer>();

        mfCombined.mesh = new Mesh();
        mfCombined.mesh.CombineMeshes(combine);
        mrCombined.sharedMaterial = _wallPrefab.GetComponent<MeshRenderer>().sharedMaterial;

        combinedWall.tag = "Wall";

        MeshCollider mc = combinedWall.AddComponent<MeshCollider>();
        mc.sharedMesh = mfCombined.mesh;
        mc.convex = false;

        _mazeObjects.Add(combinedWall);
        _wallMeshFilters.Clear();
    }


    void SpawnProps()
    {
        GameObject spawnPoint = Instantiate(_spawnPointPrefab, transform);
        spawnPoint.transform.localPosition = new Vector3(0, 1, 0);
        _mazeObjects.Add(spawnPoint);

        List<Vector2Int> pathTiles = new List<Vector2Int>();
        for (int x = 0; x < _mazeGridSize; x++)
            for (int y = 0; y < _mazeGridSize; y++)
                if (_maze[x, y] == 1) pathTiles.Add(new Vector2Int(x, y));

        Vector2Int keyPos;
        do { keyPos = pathTiles[Random.Range(0, pathTiles.Count)]; } while (keyPos == Vector2Int.zero);

        Vector2Int doorPos;
        do { doorPos = pathTiles[Random.Range(0, pathTiles.Count)]; } while (doorPos == keyPos || doorPos == Vector2Int.zero);

        _key = Instantiate(_keyPrefab, transform).transform;
        _key.localPosition = new Vector3(keyPos.x * _cellSize, 1f, keyPos.y * _cellSize);
        _mazeObjects.Add(_key.gameObject);

        _door = Instantiate(_doorPrefab, transform).transform;
        _door.localPosition = new Vector3(doorPos.x * _cellSize, 1f, doorPos.y * _cellSize);
        _mazeObjects.Add(_door.gameObject);
    }

    bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < _mazeGridSize && y < _mazeGridSize;

    void Shuffle(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    void DeleteMaze()
    {
        foreach (var obj in _mazeObjects)
            Destroy(obj);
        _mazeObjects.Clear();
        _wallMeshFilters.Clear();
    }

    public void ApplyMapSize(int size) => _mazeGridSize = size;
    public void ApplyCellSize(int size)
    {
        _cellSize = size;
        _wallScale = new Vector3(size, 3f, size);
    }

    void AdjustCamera()
    {
        float centerX = (_mazeGridSize - 2) / 2f * _cellSize;
        float centerZ = (_mazeGridSize - 2) / 2f * _cellSize;

        float mazeSize = _mazeGridSize * _cellSize;
        float camHeight = mazeSize * 1.4f;

        _cameraTransform.position = new Vector3(centerX, camHeight, centerZ);
        _cameraTransform.LookAt(new Vector3(centerX, 0, centerZ));
    }
}
