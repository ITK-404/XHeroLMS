using UnityEngine;

public class RandomWindDirection : MonoBehaviour
{
    private WindZone windZone;

    [SerializeField] private float timer;
    [SerializeField]private float minRandomDuration = 1;
    [SerializeField]private float maxRandomDuration = 60;

    private void Awake()
    {
        windZone = GetComponent<WindZone>();
    }

    private void Update()
    {
        if (!windZone)
        {
            return;
        }

        if (timer < 0)
        {
            float randomTime = Random.Range(minRandomDuration, maxRandomDuration);
            float randomY = Random.Range(0, 360);
            timer = randomTime;
            windZone.transform.rotation = Quaternion.Euler(0, randomY, 0);
        }
        else
        {
            timer -= Time.deltaTime;
        }
    }
}