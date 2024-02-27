using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    public GameObject hexPrefab;
    public GameObject riverTilePrefab;

    public int width = 10;
    public int height = 10;

    float hexWidth = 1.0f;
    float hexHeight = 1.0f;

    float noiseScale = 0.1f;
    float waveAmplitude = 1.0f;

    private GameObject[,] hexes;

    // Start is called before the first frame update
    void Start()
    {
        hexes = new GameObject[width, height];
        hexWidth = hexPrefab.GetComponent<SpriteRenderer>().bounds.size.x;
        hexHeight = hexPrefab.GetComponent<SpriteRenderer>().bounds.size.y;
        CreateGrid();
        // CreateRiver();
    }

    void CreateGrid() {
        for (int y=0, i=0; y < height; y++) {
            for (int x=0; x < width; x++, i++) {
                Vector3 pos = HexOffset(x, y, i);
                hexes[x,y] = Instantiate(hexPrefab, pos, Quaternion.identity);
                hexes[x,y].name = $"Hex_{x}_{y}";
                HexCell cell = hexes[x,y].AddComponent<HexCell>();
                cell.isWall = true;
                cell.gridPosition = new Vector2Int(x,y);
            }
        }

        StartCoroutine(MazeVisualizerRight());
        StartCoroutine(MazeVisualizerLeft());
        // GenerateMaze();
    }

    Vector3 HexOffset(int x, int z, int i) {
        float x_offset = hexWidth;
        float y_offset = hexHeight * 0.75f;

        Vector3 position = Vector3.zero;

        if (z % 2 == 0) {
            position.x = x * x_offset;
            position.y = z * y_offset;
        } else {
            position.x = (x + 0.5f) * x_offset;
            position.y = z * y_offset;
        }

        return position;
    }

    List<HexCell> GetNeighbors(HexCell cell) {
        List<HexCell> neighbors = new List<HexCell>();
        Vector2Int[] directions = new Vector2Int[] {
            new Vector2Int(1,0),
            new Vector2Int(-1,0), new Vector2Int(0, 1)
        };

        foreach (var direction in directions) {
            int x, y;
            x = cell.gridPosition.x + direction.x;
            y = cell.gridPosition.y + direction.y;
            Vector2Int neighborPos = new Vector2Int(cell.gridPosition.x + direction.x, cell.gridPosition.y + direction.y);
            if (x >= 0 && x < width && y >= 0 && y < height) {
                neighbors.Add(hexes[x, y].GetComponent<HexCell>());
            }
        }

        return neighbors;
    }

    void GenerateMaze() {
        List<HexCell> frontier = new List<HexCell>();
        HexCell startCell = hexes[0,0].GetComponent<HexCell>();
        startCell.isWall = false;
        frontier.AddRange(GetNeighbors(startCell));

        while (frontier.Count > 0) {
            HexCell currentCell = frontier[UnityEngine.Random.Range(0, frontier.Count)];
            List<HexCell> neighbors = GetNeighbors(currentCell);

            HexCell nextCell = neighbors.Find(n => n.isWall);
            if (nextCell != null) {
                nextCell.isWall = false;
                foreach (var neighbor in neighbors) {
                    if (!frontier.Contains(neighbor) && neighbor.isWall) {
                        frontier.Add(neighbor);
                        UpdateGrid();
                    }
                }
            }

            frontier.Remove(currentCell);
        }

    }

    private IEnumerator MazeVisualizerRight() {
        List<HexCell> frontier = new List<HexCell>();
        HexCell startCell = hexes[0,0].GetComponent<HexCell>();
        startCell.isWall = false;
        frontier.AddRange(GetNeighbors(startCell));
        frontier.RemoveAt(UnityEngine.Random.Range(0, frontier.Count));

        while (frontier.Count > 0) {
            HexCell currentCell = frontier[0];
            currentCell.isWall = false;
            List<HexCell> neighbors = GetNeighbors(currentCell).Where(n => n.isWall).ToList();

            if(neighbors.Count == 0) break;
            var x = UnityEngine.Random.Range(0, neighbors.Count);
            HexCell nextCell = neighbors[x];
            if (nextCell != null) {
                nextCell.isWall = false;
                if (!frontier.Contains(nextCell)) {
                    frontier.Add(nextCell);
                    UpdateGrid();
                    yield return new WaitForSeconds(0.25f);
                }
            }

            frontier.Remove(currentCell);
        }
    }

    private IEnumerator MazeVisualizerLeft() {
        List<HexCell> frontier = new List<HexCell>();
        HexCell startCell = hexes[width - 1, 0].GetComponent<HexCell>();
        startCell.isWall = false;
        frontier.AddRange(GetNeighbors(startCell));
        frontier.RemoveAt(UnityEngine.Random.Range(0, frontier.Count));

        while (frontier.Count > 0) {
            HexCell currentCell = frontier[0];
            currentCell.isWall = false;
            List<HexCell> neighbors = GetNeighbors(currentCell).Where(n => n.isWall).ToList();

            if(neighbors.Count == 0) break;
            var x = UnityEngine.Random.Range(0, neighbors.Count);
            HexCell nextCell = neighbors[x];
            if (nextCell != null) {
                nextCell.isWall = false;
                if (!frontier.Contains(nextCell)) {
                    frontier.Add(nextCell);
                    UpdateGrid();
                    yield return new WaitForSeconds(0.25f);
                }
            }

            frontier.Remove(currentCell);
        }
    }

    void UpdateGrid() {
        for (int y=0; y < height; y++) {
            for (int x=0; x < width; x++) {
                HexCell cell = hexes[x,y].GetComponent<HexCell>();
                if(cell.isWall) {
                    hexes[x,y].GetComponent<SpriteRenderer>().color = Color.black;
                } else {
                    hexes[x,y].GetComponent<SpriteRenderer>().color = Color.white;
                }
            }
        }
    }

    void CreateRiver() {
        int riverPathWidth = 1;
        int riverStartX = width / 2;

        for (int y=0; y < height; y++) {
            for (int xOffset = -riverPathWidth; xOffset <= riverPathWidth; xOffset++) {
                int x = riverStartX + xOffset;
                if (x >=0 && x < width) {
                    GameObject hex = hexes[x,y];
                    SpriteRenderer renderer = hex.GetComponent<SpriteRenderer>();
                    renderer.color = riverTilePrefab.GetComponent<SpriteRenderer>().color;
                }
            }
        }

        PathWaviness();
    }

    void PathWaviness() {
        int riverStartX = width / 2;
        float maxOffset = 0.5f;

        for(int y=0; y<height; y++) {
            for(int x=riverStartX -1; x <= riverStartX + 1; x++) {
                if (x >=0 && x < width) {
                    GameObject hex = hexes[x,y];
                    Vector3 position = hex.transform.position;

                    float noise = Mathf.PerlinNoise(x * noiseScale, y * noiseScale) * 2 - 1;
                    float xOffset = noise * maxOffset;

                    hex.transform.position = new Vector3(position.x + xOffset, position.y, position.z);
                }
            }
        }
    }
}
