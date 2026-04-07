using UnityEngine;

public class WoodBreak : MonoBehaviour
{
    public bool woodBroken;
    public GameObject brokenPrefab;

    void Start()
    {
        woodBroken = false;
    }

    public void swapToBrokenModel()
    {
        woodBroken = true;
        GameObject brokenModel = Instantiate(brokenPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }


}
