using System.Collections.Generic;
using UnityEngine;

// A layer of background scenery (nebulae, galaxies, planets) at a given depth. Pieces drift down with the
// scroll and shift less than the world as the camera moves, so they read as far away. Whatever leaves the
// screen is recycled beyond the far side as a different random piece, so the backdrop never visibly repeats.
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private int count = 4;
    // How much the layer moves with the world: 0 is infinitely far (fixed to the screen), 1 moves with it.
    [SerializeField] private float depth = 0.1f;
    [SerializeField] private int sortingOrder = -12;
    // Extra space beyond the screen that pieces are spread through. More padding means fewer on screen.
    [SerializeField] private Vector2 padding = new Vector2(6.0f, 6.0f);
    private readonly List<SpriteRenderer> pieces = new List<SpriteRenderer>();
    // Each piece's position relative to the camera.
    private readonly List<Vector2> offsets = new List<Vector2>();
    private Vector2 lastCamera;

    private void Start()
    {
        lastCamera = Camera.main.transform.position;
        Vector2 half = HalfWindow();
        for (int i = 0; i < count; ++i)
        {
            var go = new GameObject("Piece " + i);
            go.transform.SetParent(transform, false);
            var piece = go.AddComponent<SpriteRenderer>();
            piece.sortingOrder = sortingOrder;
            pieces.Add(piece);
            // Spread the starting pieces through evenly spaced bands so they don't clump.
            float band = (i + Random.Range(0.2f, 0.8f)) / count;
            offsets.Add(new Vector2(Random.Range(-half.x, half.x), Mathf.Lerp(-half.y, half.y, band)));
            Reroll(i);
        }
    }

    private void LateUpdate()
    {
        Vector2 cameraPosition = Camera.main.transform.position;
        Vector2 cameraMoved = cameraPosition - lastCamera;
        lastCamera = cameraPosition;
        Vector2 half = HalfWindow();
        Vector2 step = Vector2.down * SpaceScroller.Speed * depth * Time.deltaTime - cameraMoved * depth;
        for (int i = 0; i < pieces.Count; ++i)
        {
            Vector2 offset = offsets[i] + step;
            if (offset.y < -half.y)
            {
                // Gone off the bottom: bring a fresh piece in above the top.
                offset.y += half.y * 2.0f;
                offset.x = Random.Range(-half.x, half.x);
                Reroll(i);
            }
            else if (offset.y > half.y)
            {
                offset.y -= half.y * 2.0f;
                Reroll(i);
            }
            if (offset.x < -half.x)
            {
                offset.x += half.x * 2.0f;
                Reroll(i);
            }
            else if (offset.x > half.x)
            {
                offset.x -= half.x * 2.0f;
                Reroll(i);
            }
            offsets[i] = offset;
            pieces[i].transform.position = new Vector3(cameraPosition.x + offset.x, cameraPosition.y + offset.y, 0.0f);
        }
    }

    private void Reroll(int index)
    {
        pieces[index].sprite = sprites[Random.Range(0, sprites.Length)];
        pieces[index].flipX = Random.value < 0.5f;
    }

    private Vector2 HalfWindow()
    {
        return Playfield.View.size * 0.5f + padding;
    }
}
