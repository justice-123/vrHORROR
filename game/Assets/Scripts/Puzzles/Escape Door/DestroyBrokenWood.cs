using System.Collections;
using UnityEngine;

public class DestroyBrokenWood : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(destroyWood());
    }

    public IEnumerator destroyWood()
    {
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
}
