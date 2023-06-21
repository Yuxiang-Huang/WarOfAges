using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Linq;
using System.IO;

public class DefaultGameManager : GameManager
{
    public PhotonView PV;

    [SerializeField] int numPlayerMoved;

    [SerializeField] bool gameStarted;
    public bool turnEnded;

    private void Awake()
    {
        instance = this;
        PV = GetComponent<PhotonView>();

        // destroy if offline mode
        if (Config.offlineMode)
        {
            //offline mode
            PhotonNetwork.OfflineMode = true;

            //default room options
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.CustomRoomProperties = new Hashtable() {
                { "Mode", Config.defaultMode },
                { "initialTime", Config.defaultStartingTime },
                { "timeInc", Config.defaultTimeInc },
                { "mapRadius", Config.defaultMapRadius },
                { "Tutorial", Config.tutorialMode}
            };

            //create a room and a player
            PhotonNetwork.CreateRoom("Tutorial", roomOptions);
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player/PlayerManager"), Vector3.zero, Quaternion.identity);

            // destroy
            Destroy(gameObject);
        }
        // also destroy if tutorial
        else if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Tutorial") &&
            (bool)PhotonNetwork.CurrentRoom.CustomProperties["Tutorial"])
        {
            // destroy
            Destroy(gameObject);
        }
        else
        {
            //not able to access after game begins
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }
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

    public override void createPlayerList()
    {
        //everyone joined
        if (playerList.Count == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            //sorted list depending on actor number to assign id
            foreach (KeyValuePair<int, IController> kvp in playerList)
            {
                allPlayersOriginal.Add(kvp.Value);
            }

            //this one will change
            allPlayers = new List<IController>(allPlayersOriginal);
        }
    }

    public override void checkStart()
    {
        //only start game once
        if (gameStarted) return;

        //master client start game once when everyone is ready
        var players = PhotonNetwork.PlayerList;
        if (players.All(p => p.CustomProperties.ContainsKey("Ready") && (bool)p.CustomProperties["Ready"]))
        {
            gameStarted = true;

            Tile[,] tiles = TileManager.instance.tiles;

            if (Config.sameSpawnPlaceTestMode)
            {
                //ask all player to start game in same spot
                for (int i = 0; i < allPlayers.Count; i++)
                {
                    allPlayers[i].PV.RPC("startGame", allPlayers[i].PV.Owner, i, TileManager.instance.spawnLocations[0]);
                }
            }
            else
            {
                //shuffle to get spawn position
                List<Vector2> randomSpawnLocations = new List<Vector2>();
                while (TileManager.instance.spawnLocations.Count > 0)
                {
                    int index = Random.Range(0, TileManager.instance.spawnLocations.Count);
                    randomSpawnLocations.Add(TileManager.instance.spawnLocations[index]);
                    TileManager.instance.spawnLocations.RemoveAt(index);
                }

                //ask all player to start game
                for (int i = 0; i < allPlayers.Count; i++)
                {
                    allPlayers[i].PV.RPC("startGame", allPlayers[i].PV.Owner, i, randomSpawnLocations[i]);
                }
            }
        }
    }

    #endregion

    #region Start Turn

    [PunRPC]
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
        //stop action of player
        PlayerController.instance.stop();

        //ask master client to count player
        Hashtable playerProperties = new Hashtable();
        playerProperties.Add("EndTurn", true);
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
    }

    public override void checkEndTurn()
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
        foreach (PlayerController player in allPlayersOriginal)
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
            foreach (PlayerController player in allPlayersOriginal)
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
            foreach (PlayerController player in allPlayers)
            {
                player.PV.RPC(nameof(player.fillInfoTab), player.PV.Owner);
            }

            //next turn
            PV.RPC(nameof(startTurn), RpcTarget.AllViaServer);
        }
    }

    #endregion
}
