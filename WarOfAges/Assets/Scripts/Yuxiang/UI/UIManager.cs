using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public PhotonView PV;

    public Canvas healthbarCanvas;

    public GameObject arrowPrefab;

    [SerializeField] int turnNum;
    [SerializeField] TextMeshProUGUI turnNumText;

    [Header("Settting")]
    [SerializeField] int initialTime;
    [SerializeField] int timeInc;

    [Header("Start Game")]
    [SerializeField] GameObject IntroText;
    [SerializeField] GameObject Shop;
    [SerializeField] GameObject AgeUI;

    [Header("Turn")]
    [SerializeField] GameObject turnBtn;
    [SerializeField] GameObject cancelTurnBtn;
    [SerializeField] Coroutine timeCoroutine;
    [SerializeField] int curTimeUsed;
    [SerializeField] bool localTurnEnded;

    [Header("InfoTab - Unit")]
    [SerializeField] GameObject infoTabUnit;
    [SerializeField] TextMeshProUGUI unitNameText;
    [SerializeField] TextMeshProUGUI unitHealthText;
    [SerializeField] TextMeshProUGUI unitDamageText;
    [SerializeField] TextMeshProUGUI unitSellText;
    [SerializeField] TextMeshProUGUI unitUpgradeText;
    [SerializeField] GameObject sellBtn;
    [SerializeField] GameObject upgradeBtn;

    [Header("InfoTab - Player")]
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI goldText;

    [SerializeField] GameObject playerInfoCanvas;
    [SerializeField] List<PlayerUIManager> playerUIManagerList;

    [SerializeField] GameObject infoTabPlayer;
    [SerializeField] List<TextMeshProUGUI> playerInfoText;
    [SerializeField] List<GameObject> readyIconList;

    public Dictionary<Color, string> colorToString;

    public GameObject leaveBtn;

    [Header("Age")]
    public List<string> ageNameList;
    [SerializeField] GameObject ageAdvanceBtn;
    [SerializeField] TextMeshProUGUI ageText;
    [SerializeField] TextMeshProUGUI goldNeedToAdvanceText;

    void Awake()
    {
        instance = this;
        PV = GetComponent<PhotonView>();

        //everything set false first
        Shop.SetActive(false);
        goldText.gameObject.SetActive(false);
        turnBtn.SetActive(false);
        infoTabUnit.SetActive(false);
        infoTabPlayer.SetActive(false);
        AgeUI.SetActive(false);
        cancelTurnBtn.SetActive(false);
        IntroText.SetActive(true);
        timeText.gameObject.SetActive(false);
        playerInfoCanvas.SetActive(false);
        turnNumText.gameObject.SetActive(false);
        sellBtn.SetActive(false);
        upgradeBtn.SetActive(false);
        leaveBtn.SetActive(false);

        foreach (GameObject icon in readyIconList)
        {
            icon.SetActive(false);
        }

        //initialize color to string
        colorToString = new Dictionary<Color, string>
        {
            { new Color(0, 1, 1), "Cyan" },
            { new Color(1, 0, 0), "Red" },
            { new Color(1, 1, 0), "Yellow" },
            { new Color(1, 0, 1), "Purple" }
        };
    }

    #region Start Game

    public void startGame()
    {
        //time option setting
        initialTime = (int)PhotonNetwork.CurrentRoom.CustomProperties["initialTime"];
        timeInc = (int)PhotonNetwork.CurrentRoom.CustomProperties["timeInc"];

        //set UI active
        IntroText.SetActive(false);
        Shop.SetActive(true);
        goldText.gameObject.SetActive(true);
        AgeUI.SetActive(true);
        timeText.gameObject.SetActive(true);
        turnNumText.gameObject.SetActive(true);

        //Player list
        playerInfoCanvas.SetActive(true);
        for (int i = 0; i < GameManager.instance.allPlayersOriginal.Count; i++)
        {
            //playerNameList[i].text = GameManager.instance.allPlayersOriginal[i].PV.Owner.NickName;
            //playerNameList[i].gameObject.transform.parent.gameObject.SetActive(true);
            readyIconList[i].SetActive(true);
        }

        goldNeedToAdvanceText.text = "Advance: " + PlayerController.instance.goldNeedToAdvance + " gold";
    }

    #endregion

    #region Turn (shouldn't be call by buttons)

    //call by startTurn in gameManager
    public void startTurnUI()
    {
        turnNum++;
        turnNumText.text = "Turn: " + turnNum;

        //don't do if lost
        if (!PlayerController.instance.lost)
        {
            turnBtn.SetActive(true);

            localTurnEnded = false;

            //reset timer
            curTimeUsed = initialTime + timeInc * PlayerController.instance.age;
            timeCoroutine = StartCoroutine(nameof(timer));
        }

        //all icons show unready
        foreach (GameObject icon in readyIconList)
        {
            icon.GetComponent<Image>().color = Config.notReadyColor;
        }
    }

    IEnumerator timer()
    {
        for (int i = curTimeUsed; i > 0; i--)
        {
            timeText.text = i.ToString();

            curTimeUsed = i;

            yield return new WaitForSeconds(1f);
        }

        curTimeUsed = 0;

        cancelTurnBtn.SetActive(false);

        //only if local turn didn't end
        if (!localTurnEnded)
            GameManager.instance.endTurn();
    }

    //call by endTurn in gameManager
    public void endTurnUI()
    {
        localTurnEnded = true;

        turnBtn.SetActive(false);

        //show checkmark
        PV.RPC(nameof(setEndTurn), RpcTarget.All, PlayerController.instance.id, true);

        //only if have time left
        if (curTimeUsed > 0)
            cancelTurnBtn.SetActive(true);
    }

    //call by cancelEndTurn in gameManager
    public void cancelEndTurnUI()
    {
        localTurnEnded = false;

        //UI
        turnBtn.SetActive(true);
        cancelTurnBtn.SetActive(false);

        //hide checkmark
        PV.RPC(nameof(setEndTurn), RpcTarget.All, PlayerController.instance.id, false);
    }

    [PunRPC]
    public void turnPhase()
    {
        if (timeCoroutine != null)
            StopCoroutine(timeCoroutine);
        localTurnEnded = true;
        turnBtn.SetActive(false);
        cancelTurnBtn.SetActive(false);
    }

    [PunRPC]
    public void setEndTurn(int index, bool status)
    {
        //change color
        if (status)
        {
            Debug.Log("here");
            readyIconList[index].GetComponent<Image>().color = Config.readyColor;
        }
        else
        {
            readyIconList[index].GetComponent<Image>().color = Config.notReadyColor;
        }
    }

    #endregion

    #region Unit Info Tab

    //for existing units
    public void updateInfoTab(IUnit unit, bool myUnit)
    {
        infoTabPlayer.SetActive(false);
        infoTabUnit.SetActive(true);
        unit.fillInfoTab(unitNameText, unitHealthText, unitDamageText, unitSellText, unitUpgradeText);

        //only display buttons if my units
        if (myUnit)
        {
            sellBtn.SetActive(true);

            //able to upgrade if lower age
            if (unit.age < PlayerController.instance.age)
                upgradeBtn.SetActive(true);
        }
    }

    //for spawn buttons
    public void updateInfoTabSpawn(IUnit unit)
    {
        infoTabPlayer.SetActive(false);
        infoTabUnit.SetActive(true);
        unit.fillInfoTabSpawn(unitNameText, unitHealthText, unitDamageText, unitSellText, PlayerController.instance.age);
    }

    //for spawn images
    public void updateInfoTab(SpawnInfo spawnInfo)
    {
        infoTabPlayer.SetActive(false);
        infoTabUnit.SetActive(true);
        spawnInfo.unit.fillInfoTabSpawn(unitNameText, unitHealthText, unitDamageText, unitSellText, spawnInfo.age);
        sellBtn.SetActive(true);
    }

    public void hideInfoTab()
    {
        infoTabUnit.SetActive(false);
        sellBtn.SetActive(false);
        upgradeBtn.SetActive(false);
    }

    public void sell()
    {
        //sell
        if (PlayerController.instance.unitSelected != null)
        {
            PlayerController.instance.unitSelected.sell();
            PlayerController.instance.unitSelected = null;
        }
        //despawn
        else if (PlayerController.instance.spawnInfoSelected != null)
        {
            SpawnInfo cur = PlayerController.instance.spawnInfoSelected;

            //remove from list
            Destroy(cur.spawnImage);
            PlayerController.instance.spawnList.Remove(cur.spawnTile.pos);

            //return gold
            PlayerController.instance.gold += cur.spawnGold;
            updateGoldText();
        }

        infoTabUnit.SetActive(false);
    }

    public void upgrade()
    {
        PlayerController.instance.unitSelected.PV.RPC("upgrade", RpcTarget.All);
    }

    #endregion

    #region Player Info Tab

    public void updatePlayerInfoTab(int id)
    {
        infoTabPlayer.SetActive(true);
        GameManager.instance.allPlayersOriginal[id].PV.RPC("fillInfoTab",
            GameManager.instance.allPlayersOriginal[id].PV.Owner, PlayerController.instance.id);
    }

    [PunRPC]
    public void fillPlayerInfoTab(string nickName, string color, string age, int gold,
        int numTroop, int numBuilding, int numTerritory)
    {
        //name
        playerInfoText[0].text = nickName;

        //Color
        playerInfoText[1].text = color;

        //Age
        playerInfoText[2].text = age;

        //Gold
        playerInfoText[3].text = "Gold: " + gold;

        //Troop
        playerInfoText[4].text = "Troop: " + numTroop;

        //Buliding
        playerInfoText[5].text = "Building: " + numBuilding;

        //Territory
        playerInfoText[6].text = "Territory: " + numTerritory;
    }

    #endregion

    #region Age System

    public void ageAdvance()
    {
        //if enough gold
        if (PlayerController.instance.gold >= PlayerController.instance.goldNeedToAdvance)
        {
            infoTabUnit.SetActive(false);

            PlayerController.instance.gold -= PlayerController.instance.goldNeedToAdvance;

            //modify age
            PlayerController.instance.age++;
            ageText.text = ageNameList[PlayerController.instance.age];

            //modify gold
            PlayerController.instance.goldNeedToAdvance *= Config.ageCostFactor;
            updateGoldText();
            goldNeedToAdvanceText.text = "Click to advance! Costs " + PlayerController.instance.goldNeedToAdvance;

            //upgrade main base
            PlayerController.instance.mainBase.PV.RPC(nameof(upgrade), RpcTarget.All);

            //age limit
            if (PlayerController.instance.age >= 5)
            {
                ageAdvanceBtn.SetActive(false);
            }

            //update spawn buttons
            foreach (SpawnButton spawnBtn in SpawnManager.instance.spawnBtnList)
            {
                spawnBtn.ageAdvanceUpdate();
            }

            //able to unlock another unit
            SpawnManager.instance.keys++;
        }
    }

    #endregion

    [PunRPC]
    public void updateGoldText()
    {
        goldText.text = "Gold: " + PlayerController.instance.gold;
    }

    [PunRPC]
    public void updateTimeText(string message)
    {
        timeText.text = message;
    }

    public void lost()
    {
        cancelTurnBtn.SetActive(false);
        timeText.gameObject.SetActive(false);
        PV.RPC(nameof(setEndTurn), RpcTarget.All, PlayerController.instance.id, false);
    }
}
