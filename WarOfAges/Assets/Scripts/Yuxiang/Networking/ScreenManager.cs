using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance;

    [SerializeField] Screen[] screens;

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < screens.Length; i++)
        {
            if (screens[i].screenName == "Title")
            {
                screens[i].Display();
            }
            else
            {
                HideScreen(screens[i]);
            }
        }
    }

    public void DisplayScreen(string screenName)
    {
        for (int i = 0; i < screens.Length; i++)
        {
            if (screens[i].screenName == screenName)
            {
                DisplayScreen(screens[i]);
            }
            else if (screens[i].displayed)
            {
                HideScreen(screens[i]);
            }
        }
    }

    public void DisplayScreen(Screen screen)
    {
        for (int i = 0; i < screens.Length; i++)
        {
            if (screens[i].displayed)
            {
                HideScreen(screens[i]);
            }
        }
        screen.Display();
    }

    public void HideScreen(Screen screen)
    {
        screen.Hide();
    }
}
