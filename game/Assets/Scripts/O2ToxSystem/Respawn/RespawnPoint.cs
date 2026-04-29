using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    public enum PointType { FirstArea, SecondArea }
    [SerializeField] private PointType pointType;

    // Start runs after ALL Awakes, so RespawnManager.Instance is guaranteed to exist
    void Start()
    {
        if (RespawnManager.Instance == null)
        {
            Debug.LogError("[RespawnPoint] RespawnManager.Instance is null in Start — something is very wrong.");
            return;
        }

        if (pointType == PointType.FirstArea)
            RespawnManager.Instance.RegisterFirstAreaSpawn(transform);
        else
            RespawnManager.Instance.RegisterSecondAreaSpawn(transform);

        Debug.Log($"[RespawnPoint] Registered {pointType} at {transform.position}");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = pointType == PointType.FirstArea ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawRay(transform.position, transform.forward * 0.6f);
    }
}