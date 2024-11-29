using UnityEngine;

public class Bullet : MonoBehaviour
{
    public GameObject creator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.tag == "wall")
            Destroy(gameObject);
    }
    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
