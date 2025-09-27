using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Settings")]
    [SerializeField] int _width = 10;
    [SerializeField] int _height = 10;
    [SerializeField] float _cellSize = 2f;
    [Header("Maze Parts")]
    [SerializeField] GameObject _wallPrefab;
    [SerializeField] GameObject _groundPrefab;
    [SerializeField] Transform _wallParent;
    [Header("Props")]
    [SerializeField] GameObject _spawnPointPrefab;
    [SerializeField] GameObject _keyPrefab;
    [SerializeField] GameObject _doorPrefab;
    [Header("Stats Props")]
    [SerializeField] GameObject _statTextPrefab;
    [SerializeField] Transform _statTextParent;

    List<GameObject> _mazeObjects = new List<GameObject>();

    Transform _key;
    Transform _door;

    TextMeshPro _rewardText;
    TextMeshPro _episodeText;

    float maxReward = 0;

    private int[,] _maze;
    private void Start()
    {
        maxReward = 0;
    }
    public void Generate()
    {
        DeleteMaze();
        _maze = new int[_width, _height];
        GenerateMaze(0, 0);
        BuildMaze();
        PlaceGround();
        SpawnProps();
        PlaceStatTexts();
    }
    public Vector3 GetKeyPos()
    {
        return _key.localPosition;
    }
    public void DestroyKey()
    {
        if (_mazeObjects.Contains(_key.gameObject))
        {
            _mazeObjects.Remove(_key.gameObject);
        }else
        {
            Debug.Log("Oops!");
        }
        Destroy(_key.gameObject);
    }
    public Vector3 GetDoorPos()
    {
        return _door.localPosition;
    }
    public void UpdateStatText(int episode, float reward, int step)
    {
        if (reward > maxReward)
        {
            maxReward = reward;
        }

        _rewardText.text = $"Reward: {reward} | Max Reward: {maxReward}";
        _episodeText.text = $"Episode: {episode} | Step: {step}";
    }
    void PlaceStatTexts()
    {
        GameObject statText1 = Instantiate(_statTextPrefab, _statTextParent);
        statText1.transform.localPosition = new Vector3((float)(_width - 2) / 2 * _cellSize, 6, ((float)(_height - 2) / 2 * _cellSize) + 5);
        _mazeObjects.Add(statText1);
        _episodeText = statText1.GetComponent<TextMeshPro>();

        GameObject statText2 = Instantiate(_statTextPrefab, _statTextParent);
        statText2.transform.localPosition = new Vector3((float)(_width - 2) / 2 * _cellSize, 6, ((float)(_height - 2) / 2 * _cellSize) - 5);
        _mazeObjects.Add(statText2);
        _rewardText = statText2.GetComponent<TextMeshPro>();
    }
    void PlaceGround()
    {
        float groundPosX = (float)(_width - 2) / 2 * _cellSize;
        float groundPosZ = (float)(_height - 2) / 2 * _cellSize;
        Vector3 groundPos = new Vector3(groundPosX, 0, groundPosZ);

        GameObject ground = Instantiate(_groundPrefab, transform);
        ground.transform.localPosition = groundPos;
        _mazeObjects.Add(ground);

        float groundXScale = (float)(_width + 2) / 10 * _cellSize;
        float groundZScale = (float)(_height + 2) / 10 * _cellSize;
        Vector3 groundScale = new Vector3(groundXScale, 1, groundZScale);

        ground.transform.localScale = groundScale;

    }

    void GenerateMaze(int x, int y)
    {
        _maze[x, y] = 1;

        List<Vector2Int> directions = new List<Vector2Int> {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };
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
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_maze[x, y] == 0)
                {
                    GameObject wall = Instantiate(_wallPrefab, _wallParent);
                    wall.transform.localPosition = new Vector3(x * _cellSize, 1.5f, y * _cellSize);
                    _mazeObjects.Add(wall);
                }
            }
        }
        for (int x = -1; x <= _width - 1; x++)
        {
            GameObject wall1 = Instantiate(_wallPrefab, _wallParent);
            wall1.transform.localPosition = new Vector3(x * _cellSize, 1.5f, -1 * _cellSize);
            _mazeObjects.Add(wall1);

            GameObject wall2 = Instantiate(_wallPrefab, _wallParent);
            wall2.transform.localPosition = new Vector3(x * _cellSize, 1.5f, _height * _cellSize - _cellSize);
            _mazeObjects.Add(wall2);
        }

        for (int y = 0; y < _height; y++)
        {
            GameObject wall1 = Instantiate(_wallPrefab, _wallParent);
            wall1.transform.localPosition = new Vector3(-1 * _cellSize, 1.5f, y * _cellSize);
            _mazeObjects.Add(wall1);

            GameObject wall2 = Instantiate(_wallPrefab, _wallParent);
            wall2.transform.localPosition = new Vector3(_width * _cellSize - _cellSize, 1.5f, y * _cellSize);
            _mazeObjects.Add(wall2);
        }

    }
    void SpawnProps()
    {
        GameObject spawnPoint = Instantiate(_spawnPointPrefab,  transform);
        spawnPoint.transform.localPosition = new Vector3(0, 1, 0);
        _mazeObjects.Add(spawnPoint);

        List<Vector2Int> pathTiles = new List<Vector2Int>();

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_maze[x, y] == 1)
                {
                    pathTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        Vector2Int keyPos;
        do
        {
            keyPos = pathTiles[Random.Range(0, pathTiles.Count)];
        } while (keyPos == Vector2Int.zero);

        Vector2Int doorPos;

        do
        {
            doorPos = pathTiles[Random.Range(0, pathTiles.Count)];
        } while (doorPos == keyPos || doorPos == Vector2Int.zero);

        Vector3 keyWorldPos = new Vector3(keyPos.x * _cellSize, 1f, keyPos.y * _cellSize);
        Vector3 doorWorldPos = new Vector3(doorPos.x * _cellSize, 1f, doorPos.y * _cellSize);

        GameObject key = Instantiate(_keyPrefab, transform);
        key.transform.localPosition = keyWorldPos;
        _key = key.transform;
        _mazeObjects.Add(key);

        GameObject door = Instantiate(_doorPrefab, transform);
        door.transform.localPosition = doorWorldPos;
        _door = door.transform;
        _mazeObjects.Add(door);

    }
    bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < _width && y < _height;
    }

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
        {
            Destroy(obj);
        }
        _mazeObjects.Clear();
    }
}