using UnityEngine;

public class Bird : MonoBehaviour
{
    [Header("Parametros del pajaro")]
    public float damage = 10f;  // cuanto danio hace este pajaro

    private void OnCollisionEnter(Collision collision)
    {
        // verificacion si ha chocado con el bloque
        Block block = collision.gameObject.GetComponent<Block>();
        if (block != null)
        {
            block.TakeDamage(damage);
        }
    }
}
