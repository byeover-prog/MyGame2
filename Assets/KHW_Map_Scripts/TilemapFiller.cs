using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapFiller : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase tile;

    public int width = 75;
    public int height = 75;

    void Start()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }
}