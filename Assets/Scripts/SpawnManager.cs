using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject enemies;
    [SerializeField] private GameObject enemyContainer;
    [SerializeField] private GameObject[] powerups;
    private bool stopSpawning = false;
   
    // Start is called before the first frame update
    public void StartSpawn()
    {
        StartCoroutine(ObjectSpawn());
        StartCoroutine(SpawnPowerUps());
    }
    IEnumerator ObjectSpawn()
    {
        yield return new WaitForSeconds(2.5f);
        while (!stopSpawning)
        {
            
            float randomX = Random.Range(-8.0f,19.0f);
            float randomY = 9.98f;
            GameObject temp = Instantiate(enemies, new Vector3(randomX,randomY,0.0f), Quaternion.identity);
            temp.transform.parent = enemyContainer.transform;
            yield return new WaitForSeconds(3.0f);
        }
    }

    private IEnumerator SpawnPowerUps()
    {
        yield return new WaitForSeconds(2.5f);
        while (!stopSpawning)
        {
            int powerChoice = Random.Range(0,3);
            Instantiate(powerups[powerChoice], new Vector3(Random.Range(-8.0f, 19.0f), 9.98f, 0.0f), Quaternion.identity);
            yield return new WaitForSeconds(7.0f);
        }
    }
    public void PlayerDead()
    {
        stopSpawning = true;
    }
}
