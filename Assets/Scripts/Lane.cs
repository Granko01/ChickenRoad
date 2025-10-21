using UnityEngine;
using System.Collections;
using UnityEngine.UI;

    public class Lane : MonoBehaviour
{
    public GameObject carPrefab;
    public Transform spawnPoint;
    public Text multiplierText;
    [HideInInspector] public float laneMultiplier = 1f;
    public float minSpeed = 700f;
    public float maxSpeed = 1200f;
    public float destroyY = -5f;


    [HideInInspector] public bool laneActive = true;

    [HideInInspector] public ChickenCrossing.Difficulty difficulty;

    private Coroutine spawnRoutine;

    void Start()
    {
        spawnRoutine = StartCoroutine(SpawnCars());
    }

    IEnumerator SpawnCars()
    {
        float spawnCooldown = 3f;

        while (true)
        {
            if (laneActive && carPrefab != null && spawnPoint != null)
            {
                float spawnChance = 0f;
                switch (difficulty)
                {
                    case ChickenCrossing.Difficulty.Easy: spawnChance = 0.5f; spawnCooldown = 3f; break;
                    case ChickenCrossing.Difficulty.Medium: spawnChance = 0.6f; spawnCooldown = 2f; break;
                    case ChickenCrossing.Difficulty.Hard: spawnChance = 0.7f; spawnCooldown = 1f; break;
                    case ChickenCrossing.Difficulty.Impossible: spawnChance = 0.8f; spawnCooldown = 0.5f; break;
                }

                if (Random.value < spawnChance)
                {
                    GameObject car = Instantiate(carPrefab, spawnPoint.position, Quaternion.identity, transform);
                    Car carScript = car.GetComponent<Car>();
                    if (carScript != null)
                        carScript.speed = Random.Range(minSpeed, maxSpeed);
                }
            }

            yield return new WaitForSeconds(spawnCooldown);
        }
    }


    public void StopLane()
    {
        laneActive = false;
    }

    public void StartLane()
    {
        laneActive = true;
    }
}
