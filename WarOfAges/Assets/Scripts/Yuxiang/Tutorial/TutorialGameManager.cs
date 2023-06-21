using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TutorialGameManager : MonoBehaviourPunCallbacks
{
    public List<PlayerController> allPlayersOriginal;
    public List<PlayerController> allPlayers;

    [SerializeField] int numPlayerMoved;

    [SerializeField] bool gameStarted;
    public bool turnEnded;

    public HashSet<Tile> spellTiles = new();

    public HashSet<IUnit> unsellableUnits = new();

    void Start()
    {
        // destroy if not offline
        if (! PhotonNetwork.OfflineMode)
        {
            Destroy(gameObject);
            return;
        }

        allPlayersOriginal = new List<PlayerController>();
        allPlayersOriginal.Add(PlayerController.instance);

        //this one will change
        allPlayers = new List<PlayerController>(allPlayersOriginal);
    }

    //called when any player is ready
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        //start
        if (changedProps.ContainsKey("Ready")) checkStart();

        #region turns

        //start turn
        else if (changedProps.ContainsKey("EndTurn")) checkEndTurn();

        else if (changedProps.ContainsKey("Spawned")) checkSpawn();

        else if (changedProps.ContainsKey("Moved")) checkMove();

        else if (changedProps.ContainsKey("Attacked")) checkAttack();

        else if (changedProps.ContainsKey("CheckedDeath")) checkDeath();

        #endregion
    }

    #region Begin Game

    public void checkStart()
    {
        // no need to check because only one player
        Tile[,] tiles = TileManager.instance.tiles;

        if (Config.sameSpawnPlaceTestMode)
        {
            //ask all player to start game
            for (int i = 0; i < allPlayers.Count; i++)
            {
                allPlayers[i].PV.RPC("startGame", allPlayers[i].PV.Owner, i, TileManager.instance.spawnLocations[0]);
            }
        }
        else
        {
            // tutorial mode
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Tutorial") &&
            (bool)PhotonNetwork.CurrentRoom.CustomProperties["Tutorial"])
            {
                allPlayers[0].PV.RPC("startGame", allPlayers[0].PV.Owner, 0, TileManager.instance.spawnLocations[0]);
                return;
            }
        }
    }

    #endregion

    #region Start Turn

    [PunRPC]
    public void startTurn()
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

    [PunRPC]
    public void endTurn()
    {
        //stop action of player
        PlayerController.instance.stop();

        UIManager.instance.endTurnUI();

        if (PhotonNetwork.OfflineMode)
        {
            UIManager.instance.PV.RPC(nameof(UIManager.instance.turnPhase), RpcTarget.All);

            //in case player quit
            if (allPlayers.Count > 0)
                allPlayers[0].spawn();
        }
        else
        {
            //ask master client to count player
            Hashtable playerProperties = new Hashtable();
            playerProperties.Add("EndTurn", true);
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }

    public void cancelEndTurn()
    {
        PlayerController.instance.turnEnded = false;

        UIManager.instance.cancelEndTurnUI();

        //revert endturn property
        Hashtable playerProperties = new Hashtable();
        playerProperties.Add("EndTurn", false);
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
    }

    #endregion

    #region TakeTurn

    public void checkEndTurn()
    {
        //edge case of cancel when just ended
        if (turnEnded) return;

        //everyone is ready
        var players = PhotonNetwork.CurrentRoom.Players;
        if (players.All(p => p.Value.CustomProperties.ContainsKey("EndTurn") && (bool)p.Value.CustomProperties["EndTurn"]))
        {
            turnEnded = true;

            UIManager.instance.PV.RPC(nameof(UIManager.instance.updateTimeText), RpcTarget.All, "Take Turns...");
            UIManager.instance.PV.RPC(nameof(UIManager.instance.turnPhase), RpcTarget.All);

            //all players spawn
            foreach (PlayerController player in allPlayersOriginal)
            {
                player.PV.RPC("spawn", player.PV.Owner);
            }
        }
    }

    public void checkSpawn()
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

    public void checkMove()
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

    public IEnumerator delayAttack()
    {
        yield return new WaitForSeconds(1f);

        //all players attack
        foreach (PlayerController player in allPlayersOriginal)
        {
            player.PV.RPC(nameof(player.attack), player.PV.Owner);
        }
    }

    public void checkAttack()
    {
        //everyone is ready
        var players = PhotonNetwork.CurrentRoom.Players;
        if (players.All(p => p.Value.CustomProperties.ContainsKey("Attacked") && (bool)p.Value.CustomProperties["Attacked"]))
        {
            //all players check death
            foreach (PlayerController player in allPlayersOriginal)
            {
                player.PV.RPC(nameof(player.checkDeath), player.PV.Owner);
            }
        }
    }

    public void checkDeath()
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
            foreach (PlayerController player in allPlayers)
            {
                player.PV.RPC(nameof(player.fillInfoTab), player.PV.Owner);
            }

            //next turn
            //PV.RPC(nameof(startTurn), RpcTarget.AllViaServer);
        }
    }

    #endregion

    #region endGame

    public void surrender()
    {
        PlayerController.instance.mainBase.sell();
        PlayerController.instance.toSell.Add(PlayerController.instance.mainBase);
    }

    public void leave()
    {
        StartCoroutine(nameof(leaveEnu));
    }

    public IEnumerator leaveEnu()
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
