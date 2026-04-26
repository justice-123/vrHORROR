using UnityEngine;
using System.Collections;

public class VRRecenter2 : MonoBehaviour
{
    public Transform head;
    public Transform spawnPoint;

    IEnumerator Start()
    {

        yield return null;
        yield return null;
        yield return null;

        Vector3 offset = spawnPoint.position - head.position;
        offset.y = 0f;
        transform.position += offset;
    }
}