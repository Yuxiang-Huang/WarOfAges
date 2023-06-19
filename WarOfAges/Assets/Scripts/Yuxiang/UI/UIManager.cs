using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class UIManager : MonoBehaviour
{
    #region Fields

    public static UIManager instance;

    public PhotonView PV;

    public Canvas healthbarCanvas;

    public GameObject arrowPrefab;

    [Header("Settting")]
    [SerializeField] int initialTime;
    [SerializeField] int timeInc;

    [Header("Start Game")]
    [SerializeField] GameObject Shop;
    [SerializeField] GameObject AgeUI;
    [SerializeField] TextMeshProUGUI roomNameText;
    [SerializeField] GameObject topBar;
    [SerializeField] GameObject bottomBar;

    [Header("Turn")]
    [SerializeField] GameObject turnBtn;
    [SerializeField] int turnNum;
    [SerializeField] TextMeshProUGUI turnNumText;
    [SerializeField] GameObject cancelTurnBtn;
    [SerializeField] Coroutine timeCoroutine;
    [SerializeField] int curTimeUsed;
    [SerializeField] bool localTurnEnded;
    public bool timerPaused;

    [Header("InfoTab - Unit")]
    [SerializeField] GameObject infoTabUnit;
    [SerializeField] TextMeshProUGUI unitNameText;
    [SerializeField] TextMeshProUGUI unitHealthText;
    [SerializeField] TextMeshProUGUI unitDamageText;
    [SerializeField] TextMeshProUGUI unitSellText;
    [SerializeField] TextMeshProUGUI unitUpgradeText;
    [SerializeField] TextMeshProUGUI unitHealText;
    [SerializeField] GameObject sellBtn;
    [SerializeField] GameObject upgradeBtn;
    [SerializeField] GameObject healBtn;

    [Header("InfoTab - Player")]
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI shopGoldText;
    [SerializeField] TextMeshProUGUI incomeText;
    //[SerializeField] TextMeshProUGUI marginalIncomeText;
    [SerializeField] GameObject playerUIManagerParent;
    public List<PlayerUIManager> playerUIManagerList;
    [SerializeField] List<GameObject> readyIconList;

    [Header("Age")]
    public List<string> ageNameList;
    [SerializeField] GameObject ageAdvanceBtn;
    [SerializeField] TextMeshProUGUI ageText;
    [SerializeField] TextMeshProUGUI goldNeedToAdvanceText;

    [Header("End")]
    [SerializeField] GameObject surrenderButton;
    [SerializeField] GameObject leaveBtn;

    #endregion

    void Awake()
    {
        instance = this;
        PV = GetComponent<PhotonView>();

        //everything set false first
        Shop.SetActive(false);
        infoTabUnit.SetActive(false);
        turnBtn.SetActive(false);
        cancelTurnBtn.SetActive(false);
        timeText.gameObject.SetActive(false);
        turnNumText.gameObject.SetActive(false);
        sellBtn.SetActive(false);
        upgradeBtn.SetActive(false);
        leaveBtn.SetActive(false);
        bottomBar.SetActive(false);
        topBar.SetActive(false);

        foreach (GameObject icon in readyIconList)
        {
            icon.SetActive(false);
        }

        foreach (PlayerUIManager player in playerUIManagerList)
        {
            player.gameObject.SetActive(false);
        }

        ageText.text = ageNameList[0];
    }

    #region Start Game

    // don't show until player place down base
    public void startGameLocal()
    {
        //time option setting
        initialTime = (int)PhotonNetwork.CurrentRoom.CustomProperties["initialTime"];
        timeInc = (int)PhotonNetwork.CurrentRoom.CustomProperties["timeInc"];

        //set some UI active
        bottomBar.SetActive(true);
        topBar.SetActive(true);
        AgeUI.SetActive(true);
        timeText.gameObject.SetActive(true);
        turnNumText.gameObject.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        //icons
        for (int i = 0; i < GameManager.instance.allPlayersOriginal.Count; i++)
        {
            readyIconList[i].SetActive(true);
        }

        goldNeedToAdvanceText.text = "Click to advance! Costs " + PlayerController.instance.goldNeedToAdvance;
    }

    // don't show until everyone place down base
    public void startGameAll()
    {
        // show shop
        Shop.SetActive(true);
                
        // set gold text
        goldText = PlayerController.instance.playerUIManager.goldText;

        // initilize player UI
        PlayerController.instance.playerUIManager.PV.RPC("initilize", RpcTarget.All, PlayerController.instance.PV.Owner.NickName,
            PlayerController.instance.id);
    }

    #endregion

    #region Turn (shouldn't be call by buttons) 

    //call by startTurn in gameManager
    public void startTurnUI()
    {
        if (turnNum == 0)
        {
            //UI start game all when first turn
            startGameAll();
        }

        turnNum++;
        turnNumText.text = "Turn " + turnNum;

        setIncomeText();

        // don't do if lost
        if (!PlayerController.instance.lost)
        {
            turnBtn.SetActive(true);

            localTurnEnded = false;

            // reset timer
            // time limit increase by timeInc every 5 turn
            curTimeUsed = initialTime + timeInc * (turnNum / 5);
            timeCoroutine = StartCoroutine(nameof(timer));

            //set my ready icon to false
            PV.RPC(nameof(setEndTurn), RpcTarget.All, PlayerController.instance.id, false);
        }
    }

    IEnumerator timer()
    {
        timeText.text = curTimeUsed + "s left";

        int i = curTimeUsed;
        while (i > 0)
        {
            // for tutorial
            if (!timerPaused)
            {
                timeText.text = i + "s left";
                curTimeUsed = i;
                i--;
            }

            yield return new WaitForSeconds(1f);
        }

        curTimeUsed = 0;

        cancelTurnBtn.SetActive(false);

        timeText.text = "Waiting...";

        //only if local turn didn't end
        if (!localTurnEnded)
            GameManager.instance.endTurn();
    }

    //call by endTurn in gameManager
    public void endTurnUI()
    {
        localTurnEnded = true;

        turnBtn.SetActive(false);

        //show ready state
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

        //set end turn status to false
        PV.RPC(nameof(setEndTurn), RpcTarget.All, PlayerController.instance.id, false);
    }

    [PunRPC]
    public void turnPhase()
    {
        localTurnEnded = true;

        //couroutine
        if (timeCoroutine != null)
            StopCoroutine(timeCoroutine);

        //buttons
        turnBtn.SetActive(false);
        cancelTurnBtn.SetActive(false);
    }

    [PunRPC]
    public void setEndTurn(int index, bool status)
    {
        //change color
        if (status)
        {
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
        infoTabUnit.SetActive(true);
        unit.fillInfoTab(unitNameText, unitHealthText, unitDamageText, unitSellText, unitUpgradeText, unitHealText);

        //only display buttons if my units
        if (myUnit)
        {
            //Debug.Log(GameManager.instance.unsellableUnits.Count);

            //foreach (IUnit u in GameManager.instance.unsellableUnits)
            //{
            //    Debug.Log(u);
            //}

            //Debug.Log(PlayerController.instance.unitSelected);

            //if not unsellable due to spell
            if (!GameManager.instance.unsellableUnits.Contains(unit))
                sellBtn.SetActive(true);

            // can't sell main base unless tutorial
            if (TutorialManager.instance == null && unit.gameObject.GetComponent<MainBase>() != null)
                sellBtn.SetActive(false);

            //able to upgrade if lower age
            if (unit.age < PlayerController.instance.age)
                upgradeBtn.SetActive(true);

            ////able to heal if health not full
            //if (unit.notFullHealth())
            //    healBtn.SetActive(true);
        }
    }

    //for spawn buttons
    public void updateInfoTabSpawn(IUnit unit)
    {
        infoTabUnit.SetActive(true);
        unit.fillInfoTabSpawn(unitNameText, unitHealthText, unitDamageText, unitSellText, PlayerController.instance.age);
    }

    //for spawn images
    public void updateInfoTab(SpawnInfo spawnInfo)
    {
        infoTabUnit.SetActive(true);
        spawnInfo.unit.fillInfoTabSpawn(unitNameText, unitHealthText, unitDamageText, unitSellText, spawnInfo.age);
        sellBtn.SetActive(true);
    }

    public void hideInfoTab()
    {
        infoTabUnit.SetActive(false);
        sellBtn.SetActive(false);
        upgradeBtn.SetActive(false);
        healBtn.SetActive(false);
    }

    public void sell()
    {
        //sell
        if (PlayerController.instance.unitSelected != null)
        {
            PlayerController.instance.unitSelected.sell();
            PlayerController.instance.toSell.Add(PlayerController.instance.unitSelected);
            PlayerController.instance.unitSelected = null;
        }
        //despawn
        else if (PlayerController.instance.spawnInfoSelected != null)
        {
            SpawnInfo spawnInfo = PlayerController.instance.spawnInfoSelected;

            //destroy arrow if necessary
            if (spawnInfo.arrow != null)
                Destroy(spawnInfo.arrow);

            if (spawnInfo.unit.gameObject.TryGetComponent<Troop>(out var troopScript))
            {
                troopScript.tile = null;
            }

            //remove from list
            Destroy(spawnInfo.spawnImage);
            PlayerController.instance.spawnList.Remove(spawnInfo.spawnTile.pos);

            //return gold
            PlayerController.instance.gold += spawnInfo.spawnGold;
            updateGoldText();
        }

        infoTabUnit.SetActive(false);
    }

    public void upgrade()
    {
        int upgradeGold = PlayerController.instance.unitSelected.upgradeGold;

        if (PlayerController.instance.gold >= upgradeGold)
        {
            PlayerController.instance.gold -= upgradeGold;
            updateGoldText();
            PlayerController.instance.unitSelected.upgrade();
            PlayerController.instance.toUpgrade.Add(PlayerController.instance.unitSelected);
        }
    }

    public void heal()
    {
        int healGold = PlayerController.instance.unitSelected.getHealGold();

        if (PlayerController.instance.gold >= healGold)
        {
            PlayerController.instance.gold -= healGold;
            updateGoldText();
            PlayerController.instance.unitSelected.PV.RPC("heal", RpcTarget.All);
        }
    }

    [PunRPC]
    public void setIncomeText()
    {
        incomeText.text = "+" + PlayerController.instance.calculateIncome();

        //int marginalIncome = PlayerController.instance.calculateMarginalIncome();

        //if (marginalIncome > 0)
        //{
        //    marginalIncomeText.text = "+" + marginalIncome;
        //}
        //else
        //{
        //    marginalIncomeText.text = marginalIncome.ToString();
        //}
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
        goldText.text = PlayerController.instance.gold.ToString();
        shopGoldText.text = PlayerController.instance.gold.ToString();
    }

    [PunRPC]
    public void updateTimeText(string message)
    {
        timeText.text = message;
    }

    public void lost()
    {
        // turn stuff...
        cancelTurnBtn.SetActive(false);
        timeText.gameObject.SetActive(false);
        PV.RPC(nameof(setEndTurn), RpcTarget.All, PlayerController.instance.id, true);

        //display leave button
        surrenderButton.SetActive(false);
        leaveBtn.SetActive(true);

        //ask all player to recalculate income
        PV.RPC(nameof(UIManager.instance.setIncomeText), RpcTarget.All);
    }

    public int getTurnNum()
    {
        return turnNum;
    }
}
