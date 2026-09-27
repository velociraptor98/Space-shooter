using UnityEngine;

// Scrolls its child sprites down the screen as a seamless loop. Children are tiles stacked
// vertically one tile-height apart; when a tile drops a full height below this object it jumps
// back above the others.
public class ScrollingBackground : MonoBehaviour
{
    // Fraction of the scroll speed this layer moves at. Distant layers should be well below 1.
    [SerializeField] private float parallax = 0.15f;
    private Transform[] tiles;
    private float tileHeight;

    private void Start()
    {
        tiles = new Transform[transform.childCount];
        for (int i = 0; i < tiles.Length; ++i)
        {
            tiles[i] = transform.GetChild(i);
        }
        tileHeight = tiles[0].GetComponent<SpriteRenderer>().bounds.size.y;
    }

    private void Update()
    {
        Vector3 step = Vector3.down * SpaceScroller.Speed * parallax * Time.deltaTime;
        foreach (Transform tile in tiles)
        {
            tile.position += step;
            if (tile.position.y <= transform.position.y - tileHeight)
            {
                tile.position += Vector3.up * tileHeight * tiles.Length;
            }
        }
    }
}
