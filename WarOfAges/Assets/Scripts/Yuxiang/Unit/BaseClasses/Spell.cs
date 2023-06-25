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

    public Controller ownerController { get; set; }

    public Tile tile { get; set; }

    public int age { get; set; }

    [Header("Effect")]
    [SerializeField] int turnSinceSpawned;
    [SerializeField] int turnNeeded;

    [Header("UI")]
    [SerializeField] int sellGold;
    public int upgradeGold { get; set; }
    [SerializeField] List<string> unitNames;

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
    public virtual void Init(int playerID, int startingtTileX, int startingtTileY,
        string path, int age, int sellGold)
    {
        ownerID = playerID;
        ownerController = GameManager.instance.allPlayersOriginal[ownerID];
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

    public void fillInfoTab(TextMeshProUGUI nameText, TextMeshProUGUI healthText, TextMeshProUGUI damageText,
        TextMeshProUGUI typeText, TextMeshProUGUI sellText, TextMeshProUGUI upgradeText, TextMeshProUGUI healText)
    {
        //can't be selected after spawn
    }

    public void fillInfoTabSpawn(TextMeshProUGUI nameText, TextMeshProUGUI healthText, TextMeshProUGUI damageText,
        TextMeshProUGUI typeText, TextMeshProUGUI sellText, int age)
    {
        nameText.text = unitNames[age];
        healthText.text = "Full Health: n/a";
        damageText.text = "Damage: " + damage * (int)Mathf.Pow(Config.ageUnitFactor, age);
        string typeName = ToString();
        typeText.text = "Type: " + typeName.Substring(0, typeName.IndexOf("("));
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
        // spell doesn't take damage
    }

    public void sell()
    {
        // can't sell spells after spawn
    }

    [PunRPC]
    public void upgrade()
    {
        // can't upgrade spells
    }

    //can't heal a spell
    public bool notFullHealth()
    {
        return false;
    }

    //can't heal a spell
    public void heal()
    {

    }

    //can't heal a spell
    public int getHealGold()
    {
        return -1;
    }

    [PunRPC]
    public void kill()
    {
        Destroy(this.gameObject);
    }

    #endregion
}
