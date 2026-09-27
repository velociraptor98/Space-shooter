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
    // Big and tough; parks near the top and fires spirals.
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

    // Where ships appear: just above the screen, inset from its sides.
    private static float SpawnY => Playfield.Top + 1.5f;
    private static float MinX => Playfield.Left + 1.5f;
    private static float MaxX => Playfield.Right - 1.5f;
    private static float CenterX => Playfield.CenterX;

    // Start is called before the first frame update
    public void StartSpawn()
    {
        StartCoroutine(ObjectSpawn());
        StartCoroutine(SpawnPowerUps());
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
        float side = Random.value < 0.5f ? -1.0f : 1.0f;
        // Later waves bring bigger formations.
        int extra = wave / 8;
        switch (Random.Range(0, 5))
        {
            case 0:
                // Scout snake: a weaving column, each ship following the one ahead.
                float snakeX = Random.Range(MinX + 3.0f, MaxX - 3.0f);
                for (int i = 0; i < 5 + extra && !stopSpawning; ++i)
                {
                    SpawnEnemy(scout, snakeX, FlightPattern.Weave, side);
                    yield return new WaitForSeconds(0.25f);
                }
                break;

            case 1:
                // Scout pincers: zig-zaggers mirrored across the centre, crossing paths.
                for (int pair = 0; pair < 2 + extra && !stopSpawning; ++pair)
                {
                    float offset = Random.Range(3.0f, (MaxX - MinX) * 0.4f);
                    SpawnEnemy(scout, CenterX - offset, FlightPattern.ZigZag, 1.0f);
                    SpawnEnemy(scout, CenterX + offset, FlightPattern.ZigZag, -1.0f);
                    yield return new WaitForSeconds(0.7f);
                }
                break;

            case 2:
                // Gunship swoop squadron: peels in from one edge and arcs across, ringing the screen with bullets.
                float swoopX = side > 0.0f ? MinX + 1.0f : MaxX - 1.0f;
                for (int i = 0; i < 3 + extra && !stopSpawning; ++i)
                {
                    SpawnEnemy(gunship, swoopX, FlightPattern.Swoop, side);
                    yield return new WaitForSeconds(0.5f);
                }
                break;

            case 3:
                // Gunship divers: hover and fire, line up on the player, then attack.
                for (int i = 0; i < 2 + extra && !stopSpawning; ++i)
                {
                    SpawnEnemy(gunship, Random.Range(MinX + 2.0f, MaxX - 2.0f), FlightPattern.Dive, side);
                    yield return new WaitForSeconds(0.8f);
                }
                break;

            default:
                // Gunship line: a rank dropping together, their rings overlapping into a lattice.
                float lineX = Random.Range(MinX + 4.0f, MaxX - 4.0f);
                for (int i = -1; i <= 1; ++i)
                {
                    SpawnEnemy(gunship, lineX + i * 4.0f, FlightPattern.Straight, side);
                }
                break;
        }
    }

    // A heavy parks near the top and spirals while scouts weave in beside it.
    private IEnumerator HeavyWave()
    {
        if (uiManager)
        {
            uiManager.ShowBanner("WARNING", 1.5f);
        }
        yield return new WaitForSeconds(1.2f);
        float side = Random.value < 0.5f ? -1.0f : 1.0f;
        SpawnEnemy(heavy, CenterX + Random.Range(-4.0f, 4.0f), FlightPattern.Hold, side);
        yield return new WaitForSeconds(2.0f);
        for (int i = 0; i < 3 && !stopSpawning; ++i)
        {
            SpawnEnemy(scout, MinX + 2.0f, FlightPattern.Weave, 1.0f);
            SpawnEnemy(scout, MaxX - 2.0f, FlightPattern.Weave, -1.0f);
            yield return new WaitForSeconds(0.35f);
        }
        // Give the player room to deal with it before the next wave arrives.
        yield return new WaitForSeconds(4.0f);
    }

    private void SpawnEnemy(GameObject prefab, float x, FlightPattern pattern, float direction)
    {
        GameObject temp = Instantiate(prefab, new Vector3(x, SpawnY, 0.0f), Quaternion.identity);
        temp.transform.parent = enemyContainer.transform;
        temp.GetComponent<EnemyMovement>().Launch(pattern, direction);
    }

    private IEnumerator SpawnPowerUps()
    {
        yield return new WaitForSeconds(2.5f);
        while (!stopSpawning)
        {
            int powerChoice = Random.Range(0,3);
            Instantiate(powerups[powerChoice], new Vector3(Random.Range(MinX, MaxX), SpawnY, 0.0f), Quaternion.identity);
            yield return new WaitForSeconds(7.0f);
        }
    }
    public void PlayerDead()
    {
        stopSpawning = true;
    }
}
