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

    GameObject gameObject { get; }

    int health { get; }

    int age { get; }

    public void takeDamage(int incomingDamage);

    public void setHealthBar(bool status);

    public void fillInfoTab(TextMeshProUGUI nameText, TextMeshProUGUI healthText,
        TextMeshProUGUI damageText, TextMeshProUGUI sellText, TextMeshProUGUI upgradeText);

    public void fillInfoTabSpawn(TextMeshProUGUI nameText, TextMeshProUGUI healthText,
       TextMeshProUGUI damageText, TextMeshProUGUI sellText, int age);

    public void setImage(Color color);

    public void sell();
    public void upgrade();
}
