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

    public Controller ownerController { get; set; }

    public int age { get; set; }

    [Header("UI")]
    [SerializeField] int sellGold;
    [SerializeField] List<string> unitNames;

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

    [SerializeField] int healFactor; 

    [Header("Movement")]
    protected List<Tile> path = new List<Tile>();
    public Tile tile { get; set; }
    public GameObject arrow;

    protected int speedUsed;
    [SerializeField] protected int speed;
    protected int numOfTileMoved;

    public IUnit toFollow;

    public Ship ship;

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
    }

    [PunRPC]
    public void Init(int playerID, int startingtTileX, int startingtTileY, Vector2 startDirection,
        string path, int age, int sellGold)
    {
        //setting ID, direction, age, gold
        ownerID = playerID;
        ownerController = GameManager.instance.allPlayersOriginal[ownerID];
        direction = startDirection;
        this.age = age;
        this.sellGold = sellGold;
        this.upgradeGold = sellGold * 2;

        //update tile
        tile = TileManager.instance.tiles[startingtTileX, startingtTileY];

        if (tile.terrain == "water")
        {    
            //spawned on a ship
            if (tile.unit != null)
            {
                ship = tile.unit.gameObject.GetComponent<Ship>();

                //reset path
                if (ship.arrow != null)
                {
                    Destroy(ship.arrow);
                }
            }
        }

        tile.updateStatus(ownerID, this);

        //also try to conquer all water tiles around if spawn on land tile
        if (tile.terrain == "land")
        {
            foreach (Tile neighbor in tile.neighbors)
            {
                neighbor.tryWaterConquer();
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
        //follow this troop if in my team
        if (target.unit != null && target.unit.ownerID == ownerID)
        {
            toFollow = target.unit;
        }
        else
        {
            toFollow = null;
        }

        //same tile reset
        if (target == tile)
        {
            path = new List<Tile>();

            Destroy(arrow);

            return;
        }

        float minDist = TileManager.instance.dist(target, tile);

        //initiated a queue
        Queue<List<Tile>> allPath = new Queue<List<Tile>>();

        List<Tile> root = new () { tile };

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
                    curTile.unit != null && curTile.unit.ownerID == ownerID))))
                {
                    //no team building
                    if (curTile.unit == null || !curTile.unit.gameObject.CompareTag("Building") ||
                        curTile.unit.ownerID != ownerID)
                    {
                        visited[curTile.pos.x, curTile.pos.y] = true;

                        //check this tile dist
                        List<Tile> dup = new List<Tile>(cur)
                        {
                            curTile
                        };

                        float curDist = TileManager.instance.dist(target, curTile);

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

    public virtual void findPathBot(Tile target)
    {
        //same tile reset
        if (target == tile)
        {
            path = new List<Tile>();

            Destroy(arrow);

            return;
        }

        float minDist = TileManager.instance.dist(target, tile);

        //initiated a queue
        Queue<List<Tile>> allPath = new Queue<List<Tile>>();

        List<Tile> root = new() { tile };

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
                //not visited (Doesn't matter what terrain)
                if (!visited[curTile.pos.x, curTile.pos.y])
                {
                    //no team building
                    if (curTile.unit == null || !curTile.unit.gameObject.CompareTag("Building") ||
                        curTile.unit.ownerID != ownerID)
                    {
                        visited[curTile.pos.x, curTile.pos.y] = true;

                        //check this tile dist
                        List<Tile> dup = new List<Tile>(cur)
                        {
                            curTile
                        };

                        float curDist = TileManager.instance.dist(target, curTile);

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

        // add all water tile to be ship needed tiles
        foreach (Tile curTile in path)
        {
            // if no water and no ship
            if (curTile.terrain == "water" && curTile.unit == null
                && !BotController.instance.spawnList.ContainsKey(curTile.pos))
            {
                BotController.instance.shipNeedTiles.Add(curTile);
            }
        }

        // recalculate path (edge case where a ship can't be spawned on time)
        path = new List<Tile>();
        findPath(target);
    }

    public void follow()
    {
        if (toFollow != null)
            findPath(toFollow.tile);

        if (arrow != null)
            Destroy(arrow);
    }

    public virtual void move()
    {
        if (Config.debugTestMode)
            Debug.Log("method move");

        //moved in this turn already
        if (speedUsed == speed) return;

        //destroy arrow
        if (arrow != null)
        {
            Destroy(arrow);
        }

        speedUsed++;

        //if has next tile to go
        if (path.Count != 0)
        {
            //update direction
            direction = path[0].transform.position - tile.transform.position;

            if (Config.debugTestMode)
                Debug.Log("Just before boolean check");

            //if can move to tile
            if (canMoveToTile(path[0]))
            {
                if (Config.debugTestMode)
                    Debug.Log("First case");

                PV.RPC(nameof(removeTileUnit), RpcTarget.All);
                PV.RPC(nameof(moveUpdate_RPC), RpcTarget.All, path[0].pos.x, path[0].pos.y);

                path.RemoveAt(0);
            }
            //ask it to move first
            //edge case when ship moved away
            else if (path[0].unit != null && path[0].unit.gameObject.CompareTag("Troop"))
            {
                if (Config.debugTestMode)
                    Debug.Log("Ask teammate to move");

                //leave space
                PV.RPC(nameof(removeTileUnit), RpcTarget.All);
                tile.unit = ship;

                path[0].unit.gameObject.GetComponent<Troop>().move();

                //try to move again
                if (canMoveToTile(path[0]))
                {
                    if (Config.debugTestMode)
                        Debug.Log("Try to move again");

                    PV.RPC(nameof(moveUpdate_RPC), RpcTarget.All, path[0].pos.x, path[0].pos.y);

                    path.RemoveAt(0);
                }
                else
                {
                    if (Config.debugTestMode)
                        Debug.Log("Can't move");

                    //can't move; reverse leave space
                    PV.RPC(nameof(updateTileUnit), RpcTarget.All);
                    tile.unit = this;

                    // clear follow
                    toFollow = null;
                }
            }
        }
        else
        {
            if (Config.debugTestMode)
                Debug.Log("Inside else");

            //still conquer water when not move if not ship
            if (gameObject != null)
            {
                if (gameObject.GetComponent<Ship>() == null)
                    PV.RPC(nameof(moveUpdate_RPC), RpcTarget.All, tile.pos.x, tile.pos.y);
            }                
        }
    }

    public virtual void displayArrow()
    {
        // want to see arow for testing bot though
        if (!Config.botTestMode)
            // don't show arrow if bot
            if (PhotonNetwork.OfflineMode && ownerID == 1)
                return;

        //destroy arrow
        if (arrow != null)
        {
            Destroy(arrow);
        }

        //show arrow if there is a tile to go
        if (path.Count != 0)
        {
            arrow = Instantiate(UIManager.instance.arrowPrefab, transform.position, Quaternion.identity);

            Vector2 arrowDirection = path[0].transform.position - tile.transform.position;

            float angle = Mathf.Atan2(arrowDirection.y, arrowDirection.x);

            arrow.transform.Rotate(Vector3.forward, angle * 180 / Mathf.PI);
        }
    }

    [PunRPC]
    public void moveUpdate_RPC(int nextTileX, int nextTileY)
    {
        if (Config.debugTestMode)
            Debug.Log("method moveUpdate_RPC");

        Tile nextTile = TileManager.instance.tiles[nextTileX, nextTileY];

        // ship to ship
        if (ship != null && nextTile.unit != null && nextTile.unit.gameObject.GetComponent<Ship>() != null &&
            nextTile.unit.ownerID == ownerID)
        {
            tile.unit = ship;
            ship.tile = tile;
            ship = nextTile.unit.gameObject.GetComponent<Ship>();
        }
        // leave water
        else if (tile.terrain == "water" && TileManager.instance.tiles[nextTileX, nextTileY].terrain == "land")
        {
            //edge case when exchange tile
            //if (tile.unit == null)
            tile.unit = ship;
            ship.tile = tile;
            ship = null;
        }
        // leave land
        else if (tile.terrain == "land" && nextTile.terrain == "water")
        {
            if (nextTile.unit == null)
            {
                Debug.Log("no ship exist bug?");
            }
            else
            {
                //board a ship
                ship = nextTile.unit.gameObject.GetComponent<Ship>();
            }
        }

        if (Config.debugTestMode)
        {
            Debug.Log("update tile");
        }

        // count number of tiles moved
        if (tile != TileManager.instance.tiles[nextTileX, nextTileY] && ownerController.id == ownerID)
        {
            numOfTileMoved++;
        }

        // update direction
        if (nextTile.transform.position.x >= tile.transform.position.x)
        {
            imageRenderer.flipX = false;
        }
        else
        {
            imageRenderer.flipX = true;
        }

        //update tile
        tile = nextTile;
        tile.updateStatus(ownerID, this);

        //also try to conquer all water tiles around if moved to land tile
        if (tile.terrain == "land")
        {
            foreach (Tile neighbor in tile.neighbors)
            {
                neighbor.tryWaterConquer();
            }
        }

        if (Config.debugTestMode)
        {
            Debug.Log("animate movement");
        }

        //owner so animate movement and don't translate if bot
        if (ownerID == ownerController.id && (!(PhotonNetwork.OfflineMode && ownerID == 1)))
        {
            StartCoroutine(TranslateOverTime(transform.position, tile.transform.position, Config.troopMovementTime));
            if (ship != null)
            {
                ship.StartCoroutine(ship.TranslateOverTime(ship.transform.position, tile.transform.position, Config.troopMovementTime));
            }
        }
        else
        {
            //update position
            transform.position = tile.transform.position;
            healthbar.gameObject.transform.position = transform.position + offset;
            if (ship != null)
            {
                ship.transform.position = transform.position;
                ship.healthbar.gameObject.transform.position = ship.transform.position + offset;
            }
        }
    }

    public IEnumerator TranslateOverTime(Vector3 startingPosition, Vector3 targetPosition, float time)
    {
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            transform.position = Vector3.Lerp(startingPosition, targetPosition, (elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
        healthbar.gameObject.transform.position = transform.position + offset;

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
        speedUsed = 0;
        numOfTileMoved = 0;
        //reset path if not movable
        if (path.Count > 0 && !canMoveToTile(path[0]) && toFollow == null)
        {
            path = new List<Tile>();
        }
    }

    public virtual bool canMoveToTile(Tile cur)
    {
        if (Config.debugTestMode)
            Debug.Log("Method Can move to tile");

        //good if no unit there and land tile
        if (cur.unit == null && cur.terrain == "land")
        {
            return true;
        }

        //if on a ship
        if (ship != null)
        {
            //good if no unit or it is an empty team ship
            return cur.unit == null || cur.unit.gameObject.GetComponent<Ship>() != null && cur.unit.ownerID == ownerID;
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

    public void displayArrowForSpawn(Tile location, Tile target, int correctID)
    {
        tile = location;

        ownerID = correctID;

        findPath(target);

        displayArrow();

        if (arrow != null)
            arrow.transform.position = location.transform.position;
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
        if (ship != null)
        {
            tile.unit = ship;
            ship.tile = tile;
        }
        else
        {
            tile.unit = null;
        }
        Destroy(arrow);
        Destroy(healthbar.gameObject);
        Destroy(this.gameObject);
    }

    public bool notFullHealth()
    {
        return health < fullHealth;
    }

    #endregion

    #region UI

    public void setImage(Color color)
    {
        imageRenderer.color = color;
    }

    [PunRPC]
    public void flipDirection(bool status)
    {
        imageRenderer.flipX = status;
    }

    public void fillInfoTab(TextMeshProUGUI nameText, TextMeshProUGUI healthText, TextMeshProUGUI damageText,
        TextMeshProUGUI typeText, TextMeshProUGUI sellText, TextMeshProUGUI upgradeText, TextMeshProUGUI healText)
    {
        nameText.text = unitNames[age];
        healthText.text = "Health: " + health + " / " + fullHealth;
        damageText.text = "Damage: " + damage;
        string typeName = ToString();
        typeText.text = "Type: " + typeName.Substring(0, typeName.IndexOf("("));
        sellText.text = "Sell: " + sellGold + " Gold";
        upgradeText.text = "Upgrade: " + upgradeGold + " Gold";
        healText.text = "Heal: " + getHealGold() + " Gold";
    }

    public void fillInfoTabSpawn(TextMeshProUGUI nameText, TextMeshProUGUI healthText, TextMeshProUGUI damageText,
        TextMeshProUGUI typeText, TextMeshProUGUI sellText, int age)
    {
        nameText.text = unitNames[age];
        healthText.text = "Full Health: " + fullHealth * (int)Mathf.Pow(Config.ageUnitFactor, age);
        damageText.text = "Damage: " + damage * (int)Mathf.Pow(Config.ageUnitFactor, age);
        string typeName = ToString();
        typeText.text = "Type: " + typeName.Substring(0, typeName.IndexOf("("));
        sellText.text = "Despawn";
    }

    public void sell()
    {
        ownerController.gold += sellGold;
        UIManager.instance.updateGoldText();

        ownerController.allTroops.Remove(this);
        if (gameObject.GetComponent<Ship>() != null)
            ownerController.allShips.Remove(gameObject.GetComponent<Ship>());

        destroy();

        PlayerController playerController = ownerController.gameObject.GetComponent<PlayerController>();
        if (playerController != null)
            playerController.mode = "select";
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

        //update sell and upgrade gold
        sellGold *= Config.ageCostFactor;
        upgradeGold *= Config.ageCostFactor;

        //image
        unitImages[age].SetActive(false);
        age++;
        unitImages[age].SetActive(true);
        imageRenderer = unitImages[age].GetComponent<SpriteRenderer>();
    }

    [PunRPC]
    public void heal()
    {
        //health increase by basic unit
        health = (int) Mathf.Min(fullHealth, health + Mathf.Pow(Config.ageUnitFactor, age));
        healthbar.value = health;
    }

    public int getHealGold()
    {
        //heal factor times basic cost
        return healFactor * (int) (Config.basicGoldUnit * Mathf.Pow(Config.ageCostFactor, age));
    }

    //for losing
    public void kill()
    {
        health = 0;
        checkDeath();
    }

    #endregion
}
