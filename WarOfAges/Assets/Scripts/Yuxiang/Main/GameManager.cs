using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Linq;
using System.IO;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager instance;

    public PhotonView PV;

    public SortedDictionary<int, PlayerController> playerList = new();
    public List<PlayerController> allPlayersOriginal;
    public List<PlayerController> allPlayers;

    [SerializeField] int numPlayerMoved;

    [SerializeField] bool gameStarted;
    public bool turnEnded;

    public HashSet<Tile> spellTiles = new();

    private void Awake()
    {
        instance = this;
        PV = GetComponent<PhotonView>();

        if (!Config.offlineMode)
        {
            //not able to access after game begins
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }
        else
        {
            //offline mode
            PhotonNetwork.OfflineMode = true;

            //default room options
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.CustomRoomProperties = new Hashtable() {
                { "Mode", Config.defaultMode },
                { "initialTime", Config.defaultStartingTime },
                { "timeInc", Config.defaultTimeInc }
            };

            //create a room and a player
            PhotonNetwork.CreateRoom("offline", roomOptions);
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player/PlayerManager"), Vector3.zero, Quaternion.identity);
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

    public void createPlayerList()
    {
        //everyone joined
        if (playerList.Count == PhotonNetwork.CurrentRoom.PlayerCount)
        {
            //sorted list depending on actor number to assign id
            foreach (KeyValuePair<int, PlayerController> kvp in playerList)
            {
                allPlayersOriginal.Add(kvp.Value);
            }

            //this one will change
            allPlayers = new List<PlayerController>(allPlayersOriginal);
        }
    }

    public void checkStart()
    {
        //only start game once
        if (gameStarted) return;

        //master client start game once when everyone is ready
        var players = PhotonNetwork.PlayerList;
        if (players.All(p => p.CustomProperties.ContainsKey("Ready") && (bool)p.CustomProperties["Ready"]))
        {
            gameStarted = true;

            //creating random spawnLocations
            int xOffset = 6;
            int yOffset = 1;

            Tile[,] tiles = TileManager.instance.tiles;

            //all possible spawn points
            List<Vector2> spawnLocations = new()
            {
                new Vector2(xOffset, yOffset + 1),
                new Vector2(tiles.GetLength(0) - 1 - xOffset, tiles.GetLength(1) - 1 - yOffset),
                new Vector2(xOffset, tiles.GetLength(1) - 1 - yOffset),
                new Vector2(tiles.GetLength(0) - 1 - xOffset, yOffset + 1)
            };

            if (Config.sameSpawnPlaceTestMode)
            {
                //ask all player to start game
                for (int i = 0; i < allPlayers.Count; i++)
                {
                    allPlayers[i].PV.RPC("startGame", allPlayers[i].PV.Owner, i, spawnLocations[0]);
                }
            }
            else
            {
                //shuffle 
                List<Vector2> randomSpawnLocations = new List<Vector2>();
                while (spawnLocations.Count > 0)
                {
                    int index = Random.Range(0, spawnLocations.Count);
                    randomSpawnLocations.Add(spawnLocations[index]);
                    spawnLocations.RemoveAt(index);
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

        //reset movement;
        foreach (Troop troop in PlayerController.instance.allTroops)
        {
            troop.resetMovement();
        }
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
            //players move one by one
            allPlayers[numPlayerMoved].PV.RPC("troopMove", allPlayers[numPlayerMoved].PV.Owner);
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

            //different player start every turn
            allPlayers.Add(allPlayers[0]);
            allPlayers.RemoveAt(0);

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

    #region endGame

    public void leave()
    {
        StartCoroutine(nameof(leaveEnu));
    }

    public IEnumerator leaveEnu()
    {
        //disconnect before leaving
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LeaveLobby();
        PhotonNetwork.Disconnect();
        yield return new WaitUntil(() => !PhotonNetwork.IsConnected);
        Destroy(RoomManager.Instance.gameObject);
        PhotonNetwork.LoadLevel(0);
    }

    #endregion
}
