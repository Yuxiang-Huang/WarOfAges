using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEditor;
using UnityEngine.UI;
using Unity.VisualScripting;

public class Building : MonoBehaviourPunCallbacks, IUnit
{
    public PhotonView PV { get; set; }

    public int ownerID { get; set; }

    public Tile tile;

    public int age { get; set; }

    [Header("UI")]
    protected int sellGold;
    public int upgradeGold { get; set; }

    public SpriteRenderer imageRenderer;

    [SerializeField] List<GameObject> unitImages;

    [Header("Health")]
    public Slider healthbar;
    public int health { get; set; }
    public int fullHealth;
    Vector3 offset = new Vector3(0, 0.5f, 0);

    [SerializeField] protected int damage;

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
        tile.updateStatus(ownerID, this);

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

        //modify health and damage according to age
        fullHealth *= (int)Mathf.Pow(Config.ageUnitFactor, age);
        damage *= (int)Mathf.Pow(Config.ageUnitFactor, age);

        //health
        health = fullHealth;
        healthbar.maxValue = fullHealth;
        healthbar.value = health;
        healthbar.gameObject.transform.SetParent(UIManager.instance.healthbarCanvas.gameObject.transform);
        healthbar.gameObject.transform.position = transform.position + offset;

        healthbar.gameObject.SetActive(false);
    }

    //can spawn troop on tiles around building
    public virtual void updateCanSpawn()
    {
        foreach (Tile neighbor in tile.neighbors)
        {
            PlayerController.instance.spawnable[neighbor.pos.x, neighbor.pos.y] = true;

            PlayerController.instance.spawnDirection[neighbor.pos.x, neighbor.pos.y] =
                neighbor.transform.position - tile.transform.position;
        }
    }

    public virtual void effect()
    {

    }

    #region UI

    public void fillInfoTab(TextMeshProUGUI nameText, TextMeshProUGUI healthText,
        TextMeshProUGUI damageText, TextMeshProUGUI sellText, TextMeshProUGUI upgradeText, TextMeshProUGUI healText)
    {
        string unitName = ToString();
        nameText.text = unitName.Substring(0, unitName.IndexOf("("));
        healthText.text = "Health: " + health + " / " + fullHealth;
        damageText.text = "Damage: " + damage;
        sellText.text = "Sell: " + sellGold + " Gold";
        upgradeText.text = "Upgrade: " + upgradeGold + " Gold";
        healText.text = "Heal: " + getHealGold() + " Gold";

        //main base
        if (sellGold == 0)
        {
            sellText.text = "Quit";
        }
    }

    public void fillInfoTabSpawn(TextMeshProUGUI nameText, TextMeshProUGUI healthText,
        TextMeshProUGUI damageText, TextMeshProUGUI sellText, int age)
    {
        string unitName = ToString();
        nameText.text = unitName.Substring(0, unitName.IndexOf("("));
        healthText.text = "Full Health: " + fullHealth * (int)Mathf.Pow(Config.ageUnitFactor, PlayerController.instance.age);
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
        healthbar.gameObject.SetActive(status);
    }

    [PunRPC]
    public void takeDamage(int incomingDamage)
    {
        health -= incomingDamage;
        healthbar.value = health;
    }

    public virtual void checkDeath()
    {
        if (health <= 0)
        {
            foreach (Tile neighbor in tile.neighbors)
            {
                neighbor.updateCanSpawn();
            }

            PV.RPC(nameof(destroy), RpcTarget.All);
        }
    }

    [PunRPC]
    public void destroy()
    {
        tile.unit = null;
        Destroy(healthbar.gameObject);
        Destroy(this.gameObject);
    }

    public void sell()
    {
        PlayerController.instance.gold += sellGold;
        UIManager.instance.updateGoldText();

        PlayerController.instance.allBuildings.Remove(this);

        if (this.gameObject.GetComponent<MainBase>() != null)
        {
            PlayerController.instance.end();
        }

        kill();
    }

    public bool notFullHealth()
    {
        return health < fullHealth;
    }

    [PunRPC]
    public void heal()
    {
        //health increase by basic unit
        health = (int)Mathf.Min(fullHealth, health + Mathf.Pow(Config.ageUnitFactor, age));
        healthbar.value = health;
    }

    public virtual int getHealGold()
    {
        //basic cost
        return (int)(Config.basicGoldUnit * Mathf.Pow(Config.ageCostFactor, age));
    }

    [PunRPC]
    public void upgrade()
    {
        //health double when age increase
        fullHealth *= Config.ageUnitFactor;
        health *= Config.ageUnitFactor;
        healthbar.maxValue = fullHealth;
        healthbar.value = health;

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

    public void kill()
    {
        health = 0;
        checkDeath();
    }

    #endregion
}
