using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    public PhotonView PV;

    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] GameObject sideColor;

    [SerializeField] TextMeshProUGUI ageText;
    public TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI territoryText;
    [SerializeField] TextMeshProUGUI troopText;
    [SerializeField] TextMeshProUGUI buildingText;

    public Slider healthbar;

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    [PunRPC]
    public void initilize(string name, int colorIndex)
    {
        nameText.text = name;
        sideColor.GetComponent<Image>().color = TileManager.instance.ownerColors[colorIndex];
    }

    [PunRPC]
    public void fillInfo(string age, int gold, int numTerritory, int numTroop, int numBuilding, float healthPercent)
    {
        ageText.text = age;
        goldText.text = gold.ToString();
        territoryText.text = numTerritory.ToString();
        troopText.text = numTroop.ToString();
        buildingText.text = numBuilding.ToString();
        healthbar.value = healthPercent;
    }
}
