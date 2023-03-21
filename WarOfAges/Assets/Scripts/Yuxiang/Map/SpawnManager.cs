using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] GameObject clubMan;

    public static SpawnManager instance;

    public Image lastSpawnButtonImage;

    [SerializeField] GameObject testObject;

    public List<SpawnButton> spawnBtnList;

    //for unlocking units
    public int keys = 1;

    public void Awake()
    {
        instance = this;
    }

    public void spawn(Image spawnButtonImage, string path, int goldNeedToSpawn, GameObject spawnImage, GameObject unit)
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
