using UnityEngine;

public class Fade : MonoBehaviour
{
    [ContextMenu("Apply LOD CrossFade")]
    void Apply()
    {
        LODGroup[] lodGroups = GetComponentsInChildren<LODGroup>(true);

        foreach (LODGroup lod in lodGroups)
        {
            lod.fadeMode = LODFadeMode.CrossFade;
        }

        Debug.Log("Interior LODs updated");
    }
}