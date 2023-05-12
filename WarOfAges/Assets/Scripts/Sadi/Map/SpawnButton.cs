using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpawnButton : MonoBehaviour
{
    [SerializeField] Image image;

    public string path;

    [SerializeField] string type;

    [SerializeField] int goldNeedToSpawn;

    [SerializeField] GameObject spawnImage;

    [SerializeField] TextMeshProUGUI costText;

    [SerializeField] GameObject unit;

    public List<Sprite> unitImages;

    void Awake()
    {
        costText.text = goldNeedToSpawn + " gold";

        GetComponent<Image>().sprite = unitImages[0];

        //set all ages inactive except the current one
        foreach (Transform cur in spawnImage.transform)
        {
            cur.gameObject.SetActive(false);
        }
        spawnImage.transform.GetChild(0).gameObject.SetActive(true);
    }

    public void spawn()
    {
        //not during taking turn phase
        if (!PlayerController.instance.turnEnded)
        {
            SpawnManager.instance.spawn(image, path,
                goldNeedToSpawn * (int) Mathf.Pow(GameManager.instance.ageCostFactor, PlayerController.instance.age),
                spawnImage, type, unit.GetComponent<IUnit>());
            UIManager.instance.updateInfoTabSpawn(unit.GetComponent<IUnit>());
        }
        else
        {
            UIManager.instance.hideInfoTab();
        }
    }

    public void ageAdvanceUpdate()
    {
        costText.text = goldNeedToSpawn
        * (int) Mathf.Pow(GameManager.instance.ageCostFactor, PlayerController.instance.age)
        +" gold";

        GetComponent<Image>().sprite = unitImages[PlayerController.instance.age];

        //set all ages inactive except the current one
        foreach (Transform cur in spawnImage.transform)
        {
            cur.gameObject.SetActive(false);
        }
        spawnImage.transform.GetChild(PlayerController.instance.age).gameObject.SetActive(true);
    }
}
