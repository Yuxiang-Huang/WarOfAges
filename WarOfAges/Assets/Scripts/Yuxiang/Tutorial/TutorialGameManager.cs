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

        #region turns

        //start turn
        else if (changedProps.ContainsKey("Spawned")) checkSpawn();

        else if (changedProps.ContainsKey("Moved")) checkMove();

        else if (changedProps.ContainsKey("Attacked")) checkAttack();

        else if (changedProps.ContainsKey("CheckedDeath")) checkDeath();

        #endregion
    }

    #region Begin Game

    public override void checkStart()
    {
        player = PlayerController.instance;
        bot = BotController.instance;

        allPlayersOriginal = new List<IController>();
        allPlayersOriginal.Add(player);
        allPlayersOriginal.Add(bot);
        allPlayers = new List<IController>(allPlayersOriginal);

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
        UIManager.instance.startTurnUI();

        //reset all vars
        numPlayerMoved = 0;
        Hashtable playerProperties = new Hashtable();

        //don't reset if lost
        if (!PlayerController.instance.lost)
            playerProperties.Add("EndTurn", false);

        playerProperties.Add("Spawned", false);
        playerProperties.Add("Attacked", false);
        playerProperties.Add("Finished", false);
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);

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
        UIManager.instance.PV.RPC(nameof(UIManager.instance.updateTimeText), RpcTarget.All, "Take Turns...");
        UIManager.instance.PV.RPC(nameof(UIManager.instance.turnPhase), RpcTarget.All);

        //all players spawn
        foreach (IController player in allPlayersOriginal)
        {
            player.PV.RPC("spawn", player.PV.Owner);
        }
    }

    public override void checkSpawn()
    {
        //everyone is ready
        var players = PhotonNetwork.CurrentRoom.Players;
        if (players.All(p => p.Value.CustomProperties.ContainsKey("Spawned") && (bool)p.Value.CustomProperties["Spawned"]))
        {
            // edge case of 0 player left
            if (allPlayers.Count == 0)
            {
                UIManager.instance.PV.RPC(nameof(UIManager.instance.updateTimeText), RpcTarget.All, "Combating...");

                StartCoroutine(nameof(delayAttack));
            }
            else
            {
                // players move one by one
                allPlayers[numPlayerMoved].PV.RPC("troopMove", allPlayers[numPlayerMoved].PV.Owner);
            }
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
            allPlayers[numPlayerMoved].PV.RPC("troopMove", allPlayers[numPlayerMoved].PV.Owner);
        }
    }

    public override IEnumerator delayAttack()
    {
        yield return new WaitForSeconds(1f);

        //all players attack
        foreach (IController player in allPlayersOriginal)
        {
            player.PV.RPC(nameof(player.attack), player.PV.Owner);
        }
    }

    public override void checkAttack()
    {
        //everyone is ready
        var players = PhotonNetwork.CurrentRoom.Players;
        if (players.All(p => p.Value.CustomProperties.ContainsKey("Attacked") && (bool)p.Value.CustomProperties["Attacked"]))
        {
            //all players check death
            foreach (IController player in allPlayersOriginal)
            {
                player.PV.RPC(nameof(player.checkDeath), player.PV.Owner);
            }
        }
    }

    public override void checkDeath()
    {
        //end turn once
        if (!turnEnded) return;

        //everyone is ready
        var players = PhotonNetwork.CurrentRoom.Players;
        if (players.All(p => p.Value.CustomProperties.ContainsKey("CheckedDeath") && (bool)p.Value.CustomProperties["CheckedDeath"]))
        {
            turnEnded = false;

            if (allPlayers.Count > 0)
            {
                //different player start every turn
                allPlayers[0].startFirstIndicator(false);
                allPlayers.Add(allPlayers[0]);
                allPlayers.RemoveAt(0);
                allPlayers[0].startFirstIndicator(true);
            }

            //ask every playercontroller owner to update their info
            foreach (IController player in allPlayers)
            {
                player.PV.RPC(nameof(player.fillInfoTab), player.PV.Owner);
            }

            //next turn
            startTurn();
        }
    }

    #endregion

    #region endGame

    public override void surrender()
    {
        PlayerController.instance.mainBase.sell();
        PlayerController.instance.toSell.Add(PlayerController.instance.mainBase);
    }

    public override void leave()
    {
        StartCoroutine(nameof(leaveEnu));
    }

    public override IEnumerator leaveEnu()
    {
        //disconnect before leaving
        PhotonNetwork.LeaveRoom();

        // can't leave lobby if tutorial
        if (TutorialManager.instance == null)
            PhotonNetwork.LeaveLobby();

        PhotonNetwork.Disconnect();
        yield return new WaitUntil(() => !PhotonNetwork.IsConnected);
        Destroy(RoomManager.Instance.gameObject);
        PhotonNetwork.LoadLevel(0);
    }

    #endregion
}
