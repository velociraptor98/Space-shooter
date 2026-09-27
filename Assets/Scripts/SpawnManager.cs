using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SpawnManager : MonoBehaviour
{
    // Small, fast and fragile; fires aimed bursts.
    [SerializeField] private GameObject scout;
    // The standard ship; fires rings.
    [FormerlySerializedAs("enemies")]
    [SerializeField] private GameObject gunship;
    // Big and tough; parks just inside its entry edge and fires spirals.
    [SerializeField] private GameObject heavy;
    [SerializeField] private GameObject enemyContainer;
    [SerializeField] private GameObject[] powerups;
    [SerializeField] private UManager uiManager;
    // The gap between waves shrinks from the first value to the second over `rampWaves` waves.
    [SerializeField] private float minWaveGap = 1.8f;
    [SerializeField] private float maxWaveGap = 4.0f;
    [SerializeField] private int rampWaves = 15;
    [SerializeField] private int heavyEvery = 5;
    private bool stopSpawning = false;
    private int wave;

    // How much more often waves come from the top than from each other edge.
    [SerializeField] private float topEdgeWeight = 3.0f;
    // Rocks drifting through the arena: which ones can spawn, how often, how fast, and how many at once.
    [SerializeField] private GameObject[] asteroids;
    [SerializeField] private Vector2 asteroidInterval = new Vector2(2.5f, 5.0f);
    [SerializeField] private Vector2 asteroidSpeed = new Vector2(2.0f, 4.5f);
    [SerializeField] private int maxAsteroids = 8;

    // The screen edges a wave can enter from.
    private enum Edge { Top, Left, Right, Bottom }

    // Where power-ups appear: just above the arena, inset from its sides.
    private static float SpawnY => Playfield.Top + 1.5f;
    private static float MinX => Playfield.Left + 1.5f;
    private static float MaxX => Playfield.Right - 1.5f;

    // Start is called before the first frame update
    public void StartSpawn()
    {
        StartCoroutine(ObjectSpawn());
        StartCoroutine(SpawnPowerUps());
        StartCoroutine(SpawnAsteroids());
    }
    IEnumerator ObjectSpawn()
    {
        yield return new WaitForSeconds(0.5f);
        while (!stopSpawning)
        {
            ++wave;
            yield return wave % heavyEvery == 0 ? HeavyWave() : SpawnWave();
            float ramp = Mathf.Clamp01(wave / (float)rampWaves);
            yield return new WaitForSeconds(Mathf.Lerp(maxWaveGap, minWaveGap, ramp) * Random.Range(0.85f, 1.15f));
        }
    }

    private IEnumerator SpawnWave()
    {
        Edge edge = PickEdge();
        float side = Random.value < 0.5f ? -1.0f : 1.0f;
        // Room either side of the edge's centre to place ships, keeping them clear of the corners.
        float reach = HalfLength(edge) - 1.5f;
        // Later waves bring bigger formations.
        int extra = wave / 8;
        switch (Random.Range(0, 5))
        {
            case 0:
                // Scout snake: a weaving column, each ship following the one ahead.
                float snakeAt = Random.Range(-reach + 2.0f, reach - 2.0f);
                for (int i = 0; i < 5 + extra && !stopSpawning; ++i)
                {
                    SpawnEnemy(scout, edge, snakeAt, FlightPattern.Weave, side);
                    yield return new WaitForSeconds(0.25f);
                }
                break;

            case 1:
                // Scout pincers: zig-zaggers mirrored across the edge's centre, crossing paths.
                for (int pair = 0; pair < 2 + extra && !stopSpawning; ++pair)
                {
                    float offset = Random.Range(2.0f, reach * 0.8f);
                    SpawnEnemy(scout, edge, -offset, FlightPattern.ZigZag, 1.0f);
                    SpawnEnemy(scout, edge, offset, FlightPattern.ZigZag, -1.0f);
                    yield return new WaitForSeconds(0.7f);
                }
                break;

            case 2:
                // Gunship swoop squadron: peels in from one end of the edge and arcs across, ringing the screen with bullets.
                for (int i = 0; i < 3 + extra && !stopSpawning; ++i)
                {
                    SpawnEnemy(gunship, edge, -side * reach, FlightPattern.Swoop, side);
                    yield return new WaitForSeconds(0.5f);
                }
                break;

            case 3:
                // Gunship divers: hover and fire, line up on the player, then attack.
                for (int i = 0; i < 2 + extra && !stopSpawning; ++i)
                {
                    SpawnEnemy(gunship, edge, Random.Range(-reach, reach), FlightPattern.Dive, side);
                    yield return new WaitForSeconds(0.8f);
                }
                break;

            default:
                // Gunship line: a rank flying in together, their rings overlapping into a lattice.
                float lineAt = Random.Range(-reach + 4.0f, reach - 4.0f);
                for (int i = -1; i <= 1; ++i)
                {
                    SpawnEnemy(gunship, edge, lineAt + i * 4.0f, FlightPattern.Straight, side);
                }
                break;
        }
    }

    // A heavy parks just inside an edge and spirals while scouts weave in from that edge's ends.
    private IEnumerator HeavyWave()
    {
        if (uiManager)
        {
            uiManager.ShowBanner("WARNING", 1.5f);
        }
        yield return new WaitForSeconds(1.2f);
        Edge edge = PickEdge();
        float reach = HalfLength(edge) - 1.5f;
        float side = Random.value < 0.5f ? -1.0f : 1.0f;
        SpawnEnemy(heavy, edge, Random.Range(-reach * 0.3f, reach * 0.3f), FlightPattern.Hold, side);
        yield return new WaitForSeconds(2.0f);
        for (int i = 0; i < 3 && !stopSpawning; ++i)
        {
            SpawnEnemy(scout, edge, -reach, FlightPattern.Weave, 1.0f);
            SpawnEnemy(scout, edge, reach, FlightPattern.Weave, -1.0f);
            yield return new WaitForSeconds(0.35f);
        }
        // Give the player room to deal with it before the next wave arrives.
        yield return new WaitForSeconds(4.0f);
    }

    private Edge PickEdge()
    {
        float roll = Random.Range(0.0f, topEdgeWeight + 3.0f);
        if (roll < topEdgeWeight)
        {
            return Edge.Top;
        }
        return (Edge)(1 + Mathf.Min(2, Mathf.FloorToInt(roll - topEdgeWeight)));
    }

    // The direction a ship entering from this edge flies in.
    private static Vector2 Forward(Edge edge)
    {
        switch (edge)
        {
            case Edge.Left: return Vector2.right;
            case Edge.Right: return Vector2.left;
            case Edge.Bottom: return Vector2.up;
            default: return Vector2.down;
        }
    }

    // How far a formation spreads either side of its centre: about a screen's width (or height), so a whole
    // wave fits in view around the player rather than scattering across the arena.
    private static float HalfLength(Edge edge)
    {
        Rect view = Playfield.View;
        return edge == Edge.Top || edge == Edge.Bottom ? view.width * 0.5f : view.height * 0.5f;
    }

    // Spawns just outside the arena's `edge`, `offset` units along it from level with the player. Offsets and
    // `direction` are measured along the ship's own sideways axis, so a formation keeps its shape whichever
    // edge it arrives from, and it arrives in line with the player wherever they are in the arena.
    private void SpawnEnemy(GameObject prefab, Edge edge, float offset, FlightPattern pattern, float direction)
    {
        Rect bounds = Playfield.Bounds;
        Vector2 forward = Forward(edge);
        Vector2 sideways = Quaternion.Euler(0.0f, 0.0f, Vector2.SignedAngle(Vector2.down, forward)) * Vector2.right;
        float inset = forward.x != 0.0f ? bounds.width * 0.5f : bounds.height * 0.5f;
        float edgeHalf = forward.x != 0.0f ? bounds.height * 0.5f : bounds.width * 0.5f;
        Vector2 focus = Movement.Instance ? (Vector2)Movement.Instance.transform.position : bounds.center;
        float along = Mathf.Clamp(Vector2.Dot(focus - bounds.center, sideways) + offset, -edgeHalf + 1.5f, edgeHalf - 1.5f);
        Vector2 position = bounds.center - forward * (inset + 1.5f) + sideways * along;
        GameObject temp = Instantiate(prefab, position, Quaternion.identity);
        temp.transform.parent = enemyContainer.transform;
        temp.GetComponent<EnemyMovement>().Launch(pattern, direction, forward);
    }

    // Sends a rock in from a random edge, drifting across the arena on a line near the player.
    private IEnumerator SpawnAsteroids()
    {
        yield return new WaitForSeconds(1.5f);
        while (!stopSpawning)
        {
            if (asteroids.Length > 0 && Asteroid.Active.Count < maxAsteroids)
            {
                Rect bounds = Playfield.Bounds;
                Vector2 forward = Forward((Edge)Random.Range(0, 4));
                Vector2 sideways = new Vector2(-forward.y, forward.x);
                float inset = forward.x != 0.0f ? bounds.width * 0.5f : bounds.height * 0.5f;
                float edgeHalf = forward.x != 0.0f ? bounds.height * 0.5f : bounds.width * 0.5f;
                Vector2 start = bounds.center - forward * (inset + 2.0f) + sideways * Random.Range(-edgeHalf, edgeHalf);
                Vector2 focus = Movement.Instance ? (Vector2)Movement.Instance.transform.position : bounds.center;
                Vector2 target = Vector2.Lerp(bounds.center, focus, 0.5f) + Random.insideUnitCircle * 6.0f;
                GameObject rock = Instantiate(asteroids[Random.Range(0, asteroids.Length)], start, Quaternion.identity);
                rock.GetComponent<Asteroid>().Launch((target - start).normalized * Random.Range(asteroidSpeed.x, asteroidSpeed.y));
            }
            yield return new WaitForSeconds(Random.Range(asteroidInterval.x, asteroidInterval.y));
        }
    }

    private IEnumerator SpawnPowerUps()
    {
        yield return new WaitForSeconds(2.5f);
        while (!stopSpawning)
        {
            int powerChoice = Random.Range(0,3);
            // Drop within a screen's width of the player, so power-ups stay reachable in the wide arena.
            float halfView = Playfield.View.width * 0.5f;
            float nearX = Movement.Instance ? Movement.Instance.transform.position.x : Playfield.CenterX;
            float x = Mathf.Clamp(nearX + Random.Range(-halfView, halfView), MinX, MaxX);
            Instantiate(powerups[powerChoice], new Vector3(x, SpawnY, 0.0f), Quaternion.identity);
            yield return new WaitForSeconds(7.0f);
        }
    }
    public void PlayerDead()
    {
        stopSpawning = true;
    }
}
