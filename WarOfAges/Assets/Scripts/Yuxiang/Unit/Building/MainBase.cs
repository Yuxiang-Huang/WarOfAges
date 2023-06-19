using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class MainBase : Building
{
    [PunRPC]
    public void updateTerritory()
    {
        foreach (Tile neigbhor in tile.neighbors)
        {
            neigbhor.updateStatus(ownerID, null);
        }
    }

    public override void fillInfoTab(TextMeshProUGUI nameText, TextMeshProUGUI healthText,
        TextMeshProUGUI damageText, TextMeshProUGUI sellText, TextMeshProUGUI upgradeText, TextMeshProUGUI healText)
    {
        base.fillInfoTab(nameText, healthText, damageText, sellText, upgradeText, healText);
        sellText.text = "Sell: ∞ Gold";
    }

    public override void sell()
    {
        base.sell();

        GameManager.instance.endTurn();

        PlayerController.instance.end();
    }
}