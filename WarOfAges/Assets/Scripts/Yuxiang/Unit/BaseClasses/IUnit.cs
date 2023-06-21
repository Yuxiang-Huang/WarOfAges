using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public interface IUnit
{
    PhotonView PV { get; }

    int ownerID { get; }

    public Controller ownerController { get; }

    Tile tile { get; }

    GameObject gameObject { get; }

    int health { get; }

    int age { get; }

    int upgradeGold { get; }

    public void takeDamage(int incomingDamage);

    public void setHealthBar(bool status);

    public void fillInfoTab(TextMeshProUGUI nameText, TextMeshProUGUI healthText,
        TextMeshProUGUI damageText, TextMeshProUGUI unitTypeText, TextMeshProUGUI sellText, TextMeshProUGUI upgradeText, TextMeshProUGUI healText);

    public void fillInfoTabSpawn(TextMeshProUGUI nameText, TextMeshProUGUI healthText,
       TextMeshProUGUI damageText, TextMeshProUGUI typeText, TextMeshProUGUI sellText, int age);

    public void setImage(Color color);

    public void sell();
    public void upgrade();
    public void heal();
    public bool notFullHealth();

    public int getHealGold();
}
