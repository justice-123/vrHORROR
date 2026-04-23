using Unity.VisualScripting;
using UnityEngine;

public class GirlBear : MonoBehaviour
{

    public GameObject doorKeyPrefab;


    // Update is called once per frame
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && InventoryManager.Instance.getActiveItemID() == "teddy")
        {
            Vector3 keyPos = new Vector3(transform.position.x+1.78f, transform.position.y, transform.position.z);
            GameObject key = Instantiate(doorKeyPrefab, keyPos, transform.rotation);
            Rigidbody rb = key.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            Destroy(gameObject);
            InventoryManager.Instance.DeleteCurrentItem();
        }
    }
}
