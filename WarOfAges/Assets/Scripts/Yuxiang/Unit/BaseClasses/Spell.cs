using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEditor;
using UnityEngine.UI;
using Unity.VisualScripting;

public class Spell : MonoBehaviourPunCallbacks, IUnit
{
    public PhotonView PV { get; set; }

    public int ownerID { get; set; }

    public Tile tile;

    public int age { get; set; }

    [Header("Effect")]
    [SerializeField] int turnSinceSpawned;
    [SerializeField] int turnNeeded;

    [Header("UI")]
    [SerializeField] int sellGold;
    public int upgradeGold { get; set; }

    public SpriteRenderer imageRenderer;

    [SerializeField] List<GameObject> unitImages;

    [Header("Health")]
    [SerializeField] protected int damage;
    public int health { get; set; }

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    [PunRPC]
    public void Init(int playerID, int startingtTileX, int startingtTileY,
        string path, int age, int sellGold)
    {
        ownerID = playerID;
        tile = TileManager.instance.tiles[startingtTileX, startingtTileY];

        this.age = age;
        this.sellGold = sellGold;
        this.upgradeGold = sellGold * 2;

        //modify images
        foreach (GameObject cur in unitImages)
        {
            cur.SetActive(false);
        }
        unitImages[age].SetActive(true);
        imageRenderer = unitImages[age].GetComponent<SpriteRenderer>();

        health = 0;
        damage *= (int)Mathf.Pow(Config.ageUnitFactor, age);

        //reveal this tile
        tile.setDark(false);
        GameManager.instance.spellTiles.Add(tile);
    }

    public void countDown()
    {
        //take effect after turn needed
        turnSinceSpawned++;

        if (turnSinceSpawned == turnNeeded)
        {
            effect();
        }
    }

    public virtual void effect()
    {
        PV.RPC(nameof(removeSpellTileReveal), RpcTarget.All);
    }

    [PunRPC]
    public void removeSpellTileReveal()
    {
        GameManager.instance.spellTiles.Remove(tile);
    }

    #region UI

    public void fillInfoTab(TextMeshProUGUI nameText, TextMeshProUGUI healthText,
        TextMeshProUGUI damageText, TextMeshProUGUI sellText, TextMeshProUGUI upgradeText)
    {
        string unitName = ToString();
        nameText.text = unitName.Substring(0, unitName.IndexOf("("));
        healthText.text = "Health: n/a"; 
        damageText.text = "Damage: " + damage;
        sellText.text = "Sell: " + sellGold + " Gold";
        upgradeText.text = "Upgrade: " + upgradeGold + " Gold";
    }

    public void fillInfoTabSpawn(TextMeshProUGUI nameText, TextMeshProUGUI healthText,
        TextMeshProUGUI damageText, TextMeshProUGUI sellText, int age)
    {
        string unitName = ToString();
        nameText.text = unitName.Substring(0, unitName.IndexOf("("));
        healthText.text = "Full Health: n/a";
        damageText.text = "Damage: " + damage * (int)Mathf.Pow(Config.ageUnitFactor, age);
        sellText.text = "Despawn";
    }

    public void setImage(Color color)
    {
        imageRenderer.color = color;
    }

    #endregion

    #region Damage

    public void setHealthBar(bool status)
    {
        //health bar not applicable for spells
    }

    [PunRPC]
    public void takeDamage(int incomingDamage)
    {
        health -= incomingDamage;
    }

    public void sell()
    {
        //add gold
        PlayerController.instance.gold += sellGold;
        UIManager.instance.updateGoldText();

        //remove from spell list
        PlayerController.instance.allSpells.Remove(this);

        //destroy
        PV.RPC(nameof(kill), RpcTarget.All);
    }

    [PunRPC]
    public void upgrade()
    {
        //deduct gold if owner
        if (PlayerController.instance.id == ownerID)
        {
            PlayerController.instance.gold -= upgradeGold;
            UIManager.instance.updateGoldText();
        }

        //damage double when age increase
        damage *= Config.ageUnitFactor;

        //update sell gold
        sellGold *= Config.ageCostFactor;
        upgradeGold *= Config.ageCostFactor;

        //image
        unitImages[age].SetActive(false);
        age++;
        unitImages[age].SetActive(true);
        imageRenderer = unitImages[age].GetComponent<SpriteRenderer>();
    }

    [PunRPC]
    public void kill()
    {
        Destroy(this.gameObject);
    }

    #endregion

    //find distance between two tiles
    public float dist(Tile t1, Tile t2)
    {
        Vector2 p1 = TileManager.instance.getWorldPosition(t1);
        Vector2 p2 = TileManager.instance.getWorldPosition(t2);
        return Mathf.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y));
    }
}
