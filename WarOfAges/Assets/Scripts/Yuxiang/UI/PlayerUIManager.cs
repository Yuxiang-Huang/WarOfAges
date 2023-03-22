using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    public PhotonView PV;

    public TextMeshProUGUI nameText;
    public GameObject sideColor;

    public TextMeshProUGUI ageText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI territoryText;
    public TextMeshProUGUI troopText;
    public TextMeshProUGUI buildingText;

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
    public void fillInfo(string age, int gold, int numTerritory, int numTroop, int numBuilding)
    {
        ageText.text = age;
        goldText.text = gold.ToString();
        territoryText.text = numTerritory.ToString();
        troopText.text = numTroop.ToString();
        buildingText.text = numBuilding.ToString();
    }
}
