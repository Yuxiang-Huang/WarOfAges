using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screen : MonoBehaviour
{
    public string screenName;
    [HideInInspector] public bool displayed;

    public void Display()
    {
        gameObject.SetActive(true);
        displayed = true;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        displayed = false;
    }
}
