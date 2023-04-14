using UnityEngine;

public class ChangeOrderInLayer : MonoBehaviour
{
    public string layerName = "Default";

    void Start()
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in objects)
        {
            if (obj.layer == LayerMask.NameToLayer(layerName))
            {
                Renderer renderer = obj.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    if (renderer.sortingOrder == 3)
                    {
                        renderer.sortingOrder = 4;
                    }
                    else if (renderer.sortingOrder == 4)
                    {
                        renderer.sortingOrder = 5;
                    }
                }
            }
        }
    }
}
