using UnityEngine;

public class WoodBreak : MonoBehaviour
{
    public bool woodBroken;
    public GameObject brokenPrefab;
    public AudioSource brokenSound;

    void Start()
    {
        woodBroken = false;
    }

    public void swapToBrokenModel()
    {
        brokenSound.Play();
        woodBroken = true;
        GameObject brokenModel = Instantiate(brokenPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }


}
