using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEditor;
using UnityEngine.UI;
using Unity.VisualScripting;
using static System.TimeZoneInfo;

public class Troop : MonoBehaviourPunCallbacks, IUnit
{
    public PhotonView PV { get; set; }

    public int ownerID { get; set; }

    public int age { get; set; }

    [Header("UI")]
    [SerializeField] int sellGold;

    public int upgradeGold { get; set; }

    public SpriteRenderer imageRenderer;
    [SerializeField] List<GameObject> unitImages;

    [Header("Health")]
    public Slider healthbar;
    public int health { get; set; }
    public int fullHealth;
    public int damage;
    public Vector2 direction;
    protected Vector3 offset = new Vector3(0, 0.5f, 0);

    [Header("Movement")]
    public Tile tile;
    protected Tile lastTarget;
    protected List<Tile> path = new List<Tile>();
    protected GameObject arrow;

    [SerializeField] int numOfTilesMoved;
    [SerializeField] int speed;

    [SerializeField] Ship ship;

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    [PunRPC]
    public virtual void Init(int playerID, int startingtTileX, int startingtTileY, Vector2 startDirection,
        string path, int age, int sellGold)
    {
        //setting ID, direction, age, gold
        ownerID = playerID;
        direction = startDirection;
        this.age = age;
        this.sellGold = sellGold;
        this.upgradeGold = sellGold * 2;

        //update tile
        tile = TileManager.instance.tiles[startingtTileX, startingtTileY];
        tile.updateStatus(ownerID, this);

        //also conquer all water tiles around if not ship
        if (tile.terrain != "water")
        {
            foreach (Tile neighbor in tile.neighbors)
            {
                if (neighbor.terrain == "water" && neighbor.ownerID != ownerID)
                    neighbor.updateStatus(ownerID, null);
            }
        }

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

    #region Movement

    public virtual void attack() { }

    public virtual void findPath(Tile target)
    {
        if (lastTarget == target) return; //same path

        //same tile reset
        if (target == tile)
        {
            path = new List<Tile>();

            Destroy(arrow);

            lastTarget = null;

            return;
        }

        //otherwise find new path
        lastTarget = target;

        float minDist = dist(target, tile);

        //initiated a queue
        Queue<List<Tile>> allPath = new Queue<List<Tile>>();

        List<Tile> root = new List<Tile>();
        root.Add(tile);

        allPath.Enqueue(root);


        bool[,] visited = new bool[TileManager.instance.tiles.GetLength(0),
                                   TileManager.instance.tiles.GetLength(1)];

        bool reach = false;

        //bfs
        while (allPath.Count != 0 && !reach)
        {
            List<Tile> cur = allPath.Dequeue();
            Tile lastTile = cur[cur.Count - 1];

            foreach (Tile curTile in lastTile.neighbors)
            {
                //not visited and land tile or
                //water tile on ship or with ship on it
                if (!visited[curTile.pos.x, curTile.pos.y] &&
                    (curTile.terrain == "land" ||
                    (curTile.terrain == "water" &&
                    (ship != null ||
                    curTile.unit != null && curTile.unit.gameObject.GetComponent<Ship>() != null && curTile.unit.ownerID == ownerID))))
                {
                    //no team building
                    if (curTile.unit == null || !curTile.unit.gameObject.CompareTag("Building") ||
                        curTile.unit.ownerID != ownerID)
                    {
                        visited[curTile.pos.x, curTile.pos.y] = true;

                        //check this tile dist
                        List<Tile> dup = new List<Tile>(cur);
                        dup.Add(curTile);

                        float curDist = dist(target, curTile);

                        if (curDist < 0.01)
                        {
                            reach = true;
                            path = dup;
                            minDist = curDist;
                        }
                        else if (curDist < minDist)
                        {
                            minDist = curDist;
                            path = dup;
                        }

                        allPath.Enqueue(dup);
                    }
                }
            }
        }

        //a path is found
        if (path.Count != 0)
        {
            //remove first tile
            path.RemoveAt(0);
        }

        displayArrow();
    }

    public virtual void move()
    {
        //moved in this turn already
        if (numOfTilesMoved == speed) return;

        //destroy arrow
        if (arrow != null)
        {
            Destroy(arrow);
        }

        numOfTilesMoved++;

        //if has next tile to go
        if (path.Count != 0)
        {
            //update direction
            direction = TileManager.instance.getWorldPosition(path[0]) - TileManager.instance.getWorldPosition(tile);

            //if can move to tile
            if (canMoveToTile(path[0]))
            {
                PV.RPC(nameof(removeTileUnit), RpcTarget.All);
                PV.RPC(nameof(moveUpdate_RPC), RpcTarget.All, path[0].pos.x, path[0].pos.y);

                path.RemoveAt(0);
            }
            //ask it to move first
            //edge case when ship moved away
            else if (path[0].unit != null && path[0].unit.gameObject.CompareTag("Troop"))
            {
                //leave space
                PV.RPC(nameof(removeTileUnit), RpcTarget.All);

                path[0].unit.gameObject.GetComponent<Troop>().move();

                //try to move again
                if (canMoveToTile(path[0]))
                {
                    PV.RPC(nameof(moveUpdate_RPC), RpcTarget.All, path[0].pos.x, path[0].pos.y);

                    path.RemoveAt(0);
                }
                else
                {
                    //reverse leave space
                    PV.RPC(nameof(updateTileUnit), RpcTarget.All);
                }
            }
        }
    }

    public virtual void displayArrow()
    {
        //destroy arrow
        if (arrow != null)
        {
            Destroy(arrow);
        }

        //show arrow if there is a tile to go
        if (path.Count != 0)
        {
            arrow = Instantiate(UIManager.instance.arrowPrefab, transform.position, Quaternion.identity);

            Vector2 arrowDirection = TileManager.instance.getWorldPosition(path[0]) - TileManager.instance.getWorldPosition(tile);

            float angle = Mathf.Atan2(arrowDirection.y, arrowDirection.x);

            arrow.transform.Rotate(Vector3.forward, angle * 180 / Mathf.PI);
        }
    }

    [PunRPC]
    public virtual void moveUpdate_RPC(int nextTileX, int nextTileY)
    {
        //leave water
        if (tile.terrain == "water" && TileManager.instance.tiles[nextTileX, nextTileY].terrain == "land")
        {
            tile.unit = ship;
            ship.tile = tile;
            ship = null;
        }

        //leave land
        else if (tile.terrain == "land" && TileManager.instance.tiles[nextTileX, nextTileY].terrain == "water")
        {
            //onboard a ship
            ship = TileManager.instance.tiles[nextTileX, nextTileY].unit.gameObject.GetComponent<Ship>();
            transform.position = ship.transform.position;
        }

        //update tile
        tile = TileManager.instance.tiles[nextTileX, nextTileY];

        tile.updateStatus(ownerID, this);

        //also conquer all water tiles around if moved to land tile
        if (tile.terrain == "land")
        {
            foreach (Tile neighbor in tile.neighbors)
            {
                if (neighbor.terrain == "water" && neighbor.ownerID != ownerID)
                    neighbor.updateStatus(ownerID, null);
            }
        }

        //owner so animate movement
        if (ownerID == PlayerController.instance.id)
        {
            StartCoroutine(TranslateOverTime(transform.position, TileManager.instance.getWorldPosition(tile), Config.troopMovementTime));
        }
        else
        {
            //update position
            transform.position = TileManager.instance.getWorldPosition(tile);
            healthbar.gameObject.transform.position = transform.position + offset;
        }
    }

    IEnumerator TranslateOverTime(Vector3 startingPosition, Vector3 targetPosition, float time)
    {
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            transform.position = Vector3.Lerp(startingPosition, targetPosition, (elapsedTime / time));

            //ship movement
            if (ship != null)
            {
                ship.transform.position = transform.position;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
        healthbar.gameObject.transform.position = transform.position + offset;

        //ship movement
        if (ship != null)
        {
            ship.healthbar.gameObject.transform.position = ship.transform.position + offset;
        }

        displayArrow();
    }

    [PunRPC]
    public void updateTileUnit()
    {
        tile.unit = this;
    }

    [PunRPC]
    public void removeTileUnit()
    {
        tile.unit = null;
    }

    public void resetMovement()
    {
        numOfTilesMoved = 0;
        lastTarget = null;
        //reset path if not movable
        if (path.Count > 0 && !canMoveToTile(path[0]))
        {
            path = new List<Tile>();
        }
    }

    public virtual bool canMoveToTile(Tile cur)
    {
        //good if no unit there and land tile
        if (cur.unit == null && cur.terrain == "land")
        {
            return true;
        }

        //if on a ship
        if (ship != null)
        {
            //good if no unit
            return cur.unit == null;
        }
        else
        {
            //last possible good case:
            //if water tile and
            //is a ship or on ship or with ship on it
            return (cur.terrain == "water" &&
                    (GetComponent<Ship>() != null ||
                    ship != null ||
                    (cur.unit != null && cur.unit.gameObject.GetComponent<Ship>() != null && cur.unit.ownerID == ownerID)));
        }
    }

    #endregion

    #region Damage

    [PunRPC]
    public void takeDamage(int incomingDamage)
    {
        health -= incomingDamage;
        healthbar.value = health;
    }

    public void setHealthBar(bool status)
    {
        healthbar.gameObject.SetActive(status);
    }

    public void checkDeath()
    {
        if (health <= 0)
        {
            PV.RPC(nameof(destroy), RpcTarget.All);
        }
    }

    [PunRPC]
    public void destroy()
    {
        tile.unit = null;
        Destroy(arrow);
        Destroy(healthbar.gameObject);
        Destroy(this.gameObject);
    }

    #endregion

    #region UI

    public void setImage(Color color)
    {
        imageRenderer.color = color;
    }

    public void fillInfoTab(TextMeshProUGUI nameText, TextMeshProUGUI healthText,
    TextMeshProUGUI damageText, TextMeshProUGUI sellText, TextMeshProUGUI upgradeText)
    {
        string unitName = ToString();
        nameText.text = unitName.Substring(0, unitName.IndexOf("("));
        healthText.text = "Health: " + health + " / " + fullHealth;
        damageText.text = "Damage: " + damage;
        sellText.text = "Sell: " + sellGold + " Gold";
        upgradeText.text = "Upgrade: " + upgradeGold + " Gold";
    }

    public void fillInfoTabSpawn(TextMeshProUGUI nameText, TextMeshProUGUI healthText,
        TextMeshProUGUI damageText, TextMeshProUGUI sellText, int age)
    {
        string unitName = ToString();
        nameText.text = unitName.Substring(0, unitName.IndexOf("("));
        healthText.text = "Full Health: " + fullHealth * (int)Mathf.Pow(Config.ageUnitFactor, age);
        damageText.text = "Damage: " + damage * (int)Mathf.Pow(Config.ageUnitFactor, age);
        sellText.text = "Despawn";
    }

    public void sell()
    {
        PlayerController.instance.gold += sellGold;
        UIManager.instance.updateGoldText();

        PlayerController.instance.allTroops.Remove(this);

        kill();

        PlayerController.instance.mode = "select";
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

    //find distance between two tiles
    public float dist(Tile t1, Tile t2)
    {
        Vector2 p1 = TileManager.instance.getWorldPosition(t1);
        Vector2 p2 = TileManager.instance.getWorldPosition(t2);
        return Mathf.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y));
    }
}
