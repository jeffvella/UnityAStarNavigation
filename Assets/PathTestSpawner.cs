using UnityEngine;

public class PathTestSpawner : MonoBehaviour
{
    public int quantity;
    public int areaSize;
    public GameObject pathTestObject;

    // Start is called before the first frame update
    void Start()
    {
        Spawn();
    }

    private void Spawn()
    {
        for (var i = 0; i < quantity; i++)
        {
            var randomPosition = RandomVector3(new Vector3(-areaSize, 1, - areaSize), new Vector3(areaSize, 2, areaSize));
            Instantiate(pathTestObject, randomPosition, Quaternion.identity, transform);
        }
    }

    private static Vector3 RandomVector3(Vector3 min, Vector3 max)
    {
        return new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z));
    }
}
