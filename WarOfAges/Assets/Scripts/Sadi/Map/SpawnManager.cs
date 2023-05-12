using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] GameObject clubMan;

    public static SpawnManager instance;

    public Image lastImage;

    [SerializeField] GameObject testObject;

    public List<SpawnButton> spawnBtnList;

    public void Awake()
    {
        instance = this;
    }

    public void spawn(Image image, string path, int goldNeedToSpawn, GameObject spawnImage,
        string type, IUnit unit)
    {
        //image color transition
        if (lastImage != null)
        {
            lastImage.color = Color.white;
        }

        image.color = Color.grey;

        lastImage = image;

        //give the path to the prefab
        PlayerController.instance.mode = "spawn";
        PlayerController.instance.toSpawnPath = path;
        PlayerController.instance.toSpawnImage = spawnImage;
        PlayerController.instance.goldNeedToSpawn = goldNeedToSpawn;
        PlayerController.instance.toSpawnType = type;
        PlayerController.instance.curSpawnImage = lastImage;
        PlayerController.instance.toSpawnUnit = unit;
    }
}
