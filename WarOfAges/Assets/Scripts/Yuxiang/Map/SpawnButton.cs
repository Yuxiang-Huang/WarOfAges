using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.EventSystems;

public class SpawnButton : MonoBehaviour
{
    [SerializeField] Image displayImage;
    public List<Sprite> unitImages;

    public string path;

    [SerializeField] int goldNeedToSpawn;
    [SerializeField] TextMeshProUGUI costText;

    [SerializeField] GameObject spawnUnit;
    [SerializeField] GameObject spawnImage;

    [SerializeField] bool unlocked;
    [SerializeField] GameObject lockObject;
    [SerializeField] GameObject grayFilter;

    [SerializeField] GameObject description;

    void Awake()
    {
        //initial cost text and image
        costText.text = goldNeedToSpawn.ToString();

        createImageList();

        displayImage.sprite = unitImages[0];

        //lock
        if (unlocked)
        {
            lockObject.SetActive(false);
            grayFilter.SetActive(false);
        }
        else
        {
            lockObject.SetActive(true);
            grayFilter.SetActive(true);
        }

        //set description off
        description.SetActive(false);

        createSpawnImage();
    }

    public void createImageList()
    {
        //create image list using spawn unit
        unitImages = new List<Sprite>();

        Transform imageHolder = spawnUnit.transform.GetChild(0);

        for (int i = 0; i < imageHolder.childCount; i ++)
        {
            unitImages.Add(imageHolder.GetChild(i).GetComponent<SpriteRenderer>().sprite);
        }
    }

    public void createSpawnImage()
    { 
        //create spawn image using spawn unit
        spawnImage = Instantiate(spawnUnit);
        spawnImage.SetActive(false);

        //destroy script
        Component[] components = spawnImage.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component is IUnit)
            {
                DestroyImmediate(component, true);
            }
        }
        //change transparency
        Transform imageHolder = spawnImage.transform.GetChild(0);

        for (int i = imageHolder.childCount - 1; i >= 0; i --)
        {
            imageHolder.GetChild(i).GetComponent<SpriteRenderer>().color = Config.spawnImageColor;
            imageHolder.GetChild(i).transform.parent = spawnImage.transform;
        }

        //set all ages inactive except the current one
        foreach (Transform cur in spawnImage.transform)
        {
            cur.gameObject.SetActive(false);
        }
        //+1 because of image holder and slider
        spawnImage.transform.GetChild(Config.numAges + 1).gameObject.SetActive(true);

        //destroy the image holder and slider
        Destroy(spawnImage.transform.GetChild(0).gameObject);
        Destroy(spawnImage.transform.GetChild(1).gameObject);
    }

    public void setLockColor(Color color)
    {
        lockObject.GetComponent<Image>().color = color;
    }

    public void selectSpawnUnitPlayer()
    {
        //able to spawn if unlocked
        if (unlocked)
        {
            //not during taking turn phase
            if (!PlayerController.instance.turnEnded)
            {
                SpawnManager.instance.selectSpawnUnit(displayImage, path,
                    goldNeedToSpawn * (int)Mathf.Pow(Config.ageCostFactor, PlayerController.instance.age),
                    spawnImage, spawnUnit);
                UIManager.instance.updateInfoTabSpawn(spawnUnit.GetComponent<IUnit>());

                //gray tiles
                foreach (Tile curTile in PlayerController.instance.visibleTiles)
                {
                    if (!PlayerController.instance.canSpawn(curTile, spawnUnit))
                    {
                        curTile.setGray(true);
                    }
                }
            }
            else
            {
                UIManager.instance.hideInfoTab();
            }
        }
        //unlock if has key
        else if (SpawnManager.instance.keys > 0)
        {
            SpawnManager.instance.keys--;
            unlocked = true;
            lockObject.SetActive(false);
            grayFilter.SetActive(false);
        }
    }

    public void selectSpawnUnitBot()
    {
        BotController.instance.toSpawnPath = path;
        BotController.instance.toSpawnImage = spawnImage;
        BotController.instance.goldNeedToSpawn = goldNeedToSpawn * (int)Mathf.Pow(Config.ageCostFactor, BotController.instance.age);
        BotController.instance.toSpawnUnit = spawnUnit;
    }

    public void ageAdvanceUpdate()
    {
        //update cost text
        costText.text = (goldNeedToSpawn
        * (int)Mathf.Pow(Config.ageCostFactor, PlayerController.instance.age)).ToString();
        displayImage.sprite = unitImages[PlayerController.instance.age];

        //set all ages inactive except the current one
        foreach (Transform cur in spawnImage.transform)
        {
            cur.gameObject.SetActive(false);
        }
        spawnImage.transform.GetChild(Config.numAges - PlayerController.instance.age - 1).gameObject.SetActive(true);
    }

    #region Hovering (Lock and description)

    public void OnPointerEnter()
    {
        if (SpawnManager.instance.keys > 0)
            lockObject.SetActive(false);

        description.SetActive(true);
    }

    public void OnPointerExit()
    {
        if (!unlocked)
            lockObject.SetActive(true);

        description.SetActive(false);
    }

    #endregion
}
