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

    public string path;

    [SerializeField] int goldNeedToSpawn;

    [SerializeField] GameObject spawnImage;

    [SerializeField] TextMeshProUGUI costText;

    [SerializeField] GameObject unit;

    [SerializeField] bool unlocked;
    [SerializeField] GameObject lockObject;

    [SerializeField] GameObject description;

    public List<Sprite> unitImages;

    void Awake()
    {
        costText.text = goldNeedToSpawn + " gold";

        displayImage.sprite = unitImages[0];

        //set all ages inactive except the current one
        foreach (Transform cur in spawnImage.transform)
        {
            cur.gameObject.SetActive(false);
        }
        spawnImage.transform.GetChild(0).gameObject.SetActive(true);

        //lock
        if (unlocked)
        {
            lockObject.SetActive(false);
        }
        else
        {
            lockObject.SetActive(true);
        }

        description.SetActive(false);
    }

    public void spawn()
    {
        //able to spawn if unlocked
        if (unlocked)
        {
            //not during taking turn phase
            if (!PlayerController.instance.turnEnded)
            {
                SpawnManager.instance.spawn(displayImage, path,
                    goldNeedToSpawn * (int)Mathf.Pow(Config.ageCostFactor, PlayerController.instance.age),
                    spawnImage, unit);
                UIManager.instance.updateInfoTabSpawn(unit.GetComponent<IUnit>());

                //gray tiles, condition same as in playerController spawn
                if (unit.CompareTag("Building"))
                {
                    foreach (Tile tile in PlayerController.instance.visibleTiles)
                    {
                        if (!(tile != null && tile.unit == null && PlayerController.instance.territory.Contains(tile)
                            && !PlayerController.instance.spawnList.ContainsKey(tile.pos) && tile.terrain == "land"))
                        {
                            tile.setGray(true);
                        }
                    }
                }
                else if (unit.CompareTag("Troop"))
                {
                    foreach (Tile tile in PlayerController.instance.visibleTiles)
                    {
                        if (!(tile != null && tile.unit == null && PlayerController.instance.territory.Contains(tile)
                            && !PlayerController.instance.spawnList.ContainsKey(tile.pos)
                            && PlayerController.instance.canSpawn[tile.pos.x, tile.pos.y] &&
                                (tile.terrain == "land" ||
                                unit.GetComponent<Amphibian>() != null)))
                        {
                            tile.setGray(true);
                        }
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
        }
    }

    public void ageAdvanceUpdate()
    {
        costText.text = goldNeedToSpawn
        * (int) Mathf.Pow(Config.ageCostFactor, PlayerController.instance.age)
        +" gold";

        displayImage.sprite = unitImages[PlayerController.instance.age];

        //set all ages inactive except the current one
        foreach (Transform cur in spawnImage.transform)
        {
            cur.gameObject.SetActive(false);
        }
        spawnImage.transform.GetChild(PlayerController.instance.age).gameObject.SetActive(true);
    }

    #region Hovering (Lock and description)

    public void OnPointerEnter()
    {
        if (SpawnManager.instance.keys > 0)
            lockObject.GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);

        description.SetActive(true);
    }

    public void OnPointerExit()
    {
        lockObject.GetComponent<Image>().color = new Color(0, 0, 0, 1f);

        description.SetActive(false);
    }

    #endregion
}
