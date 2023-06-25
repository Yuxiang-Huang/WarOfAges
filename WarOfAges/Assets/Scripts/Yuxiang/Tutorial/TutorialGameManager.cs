using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TutorialGameManager : GameManager
{
    [SerializeField] PlayerController player;
    [SerializeField] BotController bot;

    [SerializeField] int numPlayerMoved;

    [SerializeField] bool gameStarted;
    public bool turnEnded;

    void Start()
    {
        // destroy if not offline
        if (! PhotonNetwork.OfflineMode)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    //called when any player is ready
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        //start
        if (changedProps.ContainsKey("Ready")) checkStart();
    }

    #region Begin Game

    public override void checkStart()
    {
        player = PlayerController.instance;
        bot = BotController.instance;

        allPlayersOriginal = new List<Controller>();
        allPlayersOriginal.Add(player);
        allPlayersOriginal.Add(bot);
        allPlayers = new List<Controller>(allPlayersOriginal);

        // no need to check because only one player
        if (Config.sameSpawnPlaceTestMode)
        {
            player.startGame(0, TileManager.instance.spawnLocations[0]);
            bot.startGame(1, TileManager.instance.spawnLocations[0]);
        }
        else
        {
            player.startGame(0, TileManager.instance.spawnLocations[0]);
            bot.startGame(1, TileManager.instance.spawnLocations[Random.Range(1, 6)]);
        }
    }

    #endregion

    #region Start Turn

    public override void startTurn()
    {
        bot.takeActions();

        UIManager.instance.startTurnUI();

        //reset all vars
        numPlayerMoved = 0;

        //skip if lost
        if (PlayerController.instance.lost) return;

        PlayerController.instance.turnEnded = false;
    }

    #endregion

    #region TakeTurn

    public override void endTurn()
    {
        // stop action of player
        PlayerController.instance.stop();

        // just end turn
        checkEndTurn();
    }

    public override void checkEndTurn()
    {
        // only one player so no need to check
        UIManager.instance.updateTimeText("Take Turns...");
        UIManager.instance.turnPhase();

        //all players spawn
        foreach (Controller player in allPlayersOriginal)
        {
            player.spawn();
        }

        checkSpawn();
    }

    public override void checkSpawn()
    {
        // edge case of 0 player left
        if (allPlayers.Count == 0)
        {
            UIManager.instance.PV.RPC(nameof(UIManager.instance.updateTimeText), RpcTarget.All, "Combating...");

            checkAttack();
        }
        else
        {
            // players move one by one
            allPlayers[numPlayerMoved].troopMove();
            checkMove();
        }
    }

    public override void checkMove()
    {
        numPlayerMoved++;

        //all players moved
        if (numPlayerMoved == allPlayers.Count)
        {
            UIManager.instance.PV.RPC(nameof(UIManager.instance.updateTimeText), RpcTarget.All, "Combating...");

            StartCoroutine(nameof(delayAttack));
        }
        else
        {
            //next player move
            allPlayers[numPlayerMoved].troopMove();
            checkMove();
        }
    }

    public override IEnumerator delayAttack()
    {
        yield return new WaitForEndOfFrame();
        //yield return new WaitForSeconds(1f);

        //all players attack
        foreach (Controller player in allPlayersOriginal)
        {
            player.attack();
        }

        checkAttack();
    }

    public override void checkAttack()
    { 
        //all players check death
        foreach (Controller player in allPlayersOriginal)
        {
            player.checkDeath();
        }

        checkDeath();
    }

    public override void checkDeath()
    {
        if (allPlayers.Count > 0)
        {
            //different player start every turn
            allPlayers[0].startFirstIndicator(false);
            allPlayers.Add(allPlayers[0]);
            allPlayers.RemoveAt(0);
            allPlayers[0].startFirstIndicator(true);
        }

        // first time initilization for bot
        if (UIManager.instance.getTurnNum() == 0)
        {
            bot.playerUIManager.PV.RPC("initilize", RpcTarget.All, "Bot", 1);
        }

        //ask every playercontroller owner to update their info
        foreach (Controller player in allPlayers)
        {
            player.fillInfoTab();
        }

        //next turn
        startTurn();
    }

    #endregion
}
