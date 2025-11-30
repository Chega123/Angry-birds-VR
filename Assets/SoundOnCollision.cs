using UnityEngine;

public class PlaySoundOnCollision : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Reproduce el sonido cuando el objeto choca
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}
