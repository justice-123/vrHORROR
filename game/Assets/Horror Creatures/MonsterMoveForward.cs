using UnityEngine;

public class MonsterMoveForward : MonoBehaviour
{
    public float moveSpeed = 2f;

    void Update()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }
}