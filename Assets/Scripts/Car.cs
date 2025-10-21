using UnityEngine;

public class Car : MonoBehaviour
{
    public float speed = 700f;
    public float destroyY = -5f; // destroy after passing bottom

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;

        // Destroy when off-screen
        if (transform.position.y <= destroyY)
            Destroy(gameObject);
    }
}
