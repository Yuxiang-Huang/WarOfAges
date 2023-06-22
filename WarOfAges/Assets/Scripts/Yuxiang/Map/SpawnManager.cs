using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;

    public Image lastSpawnButtonImage;

    public List<SpawnButton> spawnBtnList;

    //for unlocking units
    public int keys = 1;

    private Color startColor = new Color (0, 0, 0, 0.6f);
    private Color endColor = new Color(0, 0, 0, 0.2f);

    public float duration;
    [SerializeField] float t = 0.0f;

    public void Awake()
    {
        instance = this;

        StartCoroutine(nameof(lockFlash));
    }

    IEnumerator lockFlash()
    {
        //lock flash
        while (t < 1)
        {
            Color curColor = new Color(0, 0, 0, 1f);

            if (keys > 0)
            {
                curColor = Color.Lerp(startColor, endColor, t);
            }

            foreach (SpawnButton spawnButton in spawnBtnList)
            {
                spawnButton.setLockColor(curColor);
            }
            
            t += Time.deltaTime / duration;
            yield return null;
        }

        StartCoroutine(nameof(lockFlashReverse));
    }

    IEnumerator lockFlashReverse()
    {
        //lock flash reverse
        while (t > 0)
        {
            Color curColor = new Color (0, 0, 0, 1f);

            if (keys > 0)
            {
                curColor = Color.Lerp(startColor, endColor, t);
            }

            foreach (SpawnButton spawnButton in spawnBtnList)
            {
                spawnButton.setLockColor(curColor);
            }

            t -= Time.deltaTime / duration;
            yield return null;
        }

        StartCoroutine(nameof(lockFlash));
    }

    public void selectSpawnUnit(Image spawnButtonImage, string path, int goldNeedToSpawn, GameObject spawnImage, GameObject unit)
    {
        //image color transition
        if (lastSpawnButtonImage != null)
        {
            lastSpawnButtonImage.color = Color.white;
        }

        spawnButtonImage.color = Color.grey;

        lastSpawnButtonImage = spawnButtonImage;

        //give the path to the prefab
        PlayerController.instance.mode = "spawn";
        PlayerController.instance.toSpawnPath = path;
        PlayerController.instance.toSpawnImage = spawnImage;
        PlayerController.instance.goldNeedToSpawn = goldNeedToSpawn;
        PlayerController.instance.toSpawnUnit = unit;
    }

    public void resetSpawnButtonImage()
    {
        //image color transition
        if (lastSpawnButtonImage != null)
        {
            lastSpawnButtonImage.color = Color.white;
        }

        lastSpawnButtonImage = null;
    }
}
