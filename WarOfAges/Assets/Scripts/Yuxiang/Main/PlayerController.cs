using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : Controller
{
    public static PlayerController instance;

    private void Awake()
    {
        PV = GetComponent<PhotonView>();

        //keep track of all players
        GameManager.instance.playerList.Add(PV.OwnerActorNr, this);
        GameManager.instance.createPlayerList();

        if (!PV.IsMine) return;
        instance = this;

        //master client in charge making grid
        if (PhotonNetwork.IsMasterClient)
        {
            TileManager.instance.startMakeGrid();
        }

        if (Config.moreMoneyTestMode)
        {
            gold = 40000000;
        }
        else
        {
            gold = 40;
        }
    }

    [PunRPC]
    public override void startGame(int newID, Vector2 spawnLocation)
    {
        //assign id
        id = newID;

        PV.RPC(nameof(startGame_all), RpcTarget.AllViaServer, newID);

        mode = "start";

        //reveal starting territory
        Tile[,] tiles = TileManager.instance.tiles;

        Tile root = tiles[(int)spawnLocation.x, (int)spawnLocation.y];

        root.setDark(false);

        foreach (Tile neighbor in root.neighbors)
        {
            neighbor.setDark(false);
        }

        foreach (Tile neighbor in root.neighbors2)
        {
            neighbor.setDark(false);
        }
    }

    public override void stop()
    {
        UIManager.instance.hideInfoTab();

        if (mode == "spawn")
        {
            //set color of the spawn button selected back to white
            SpawnManager.instance.resetSpawnButtonImage();

            //clear gray
            foreach (Tile tile in visibleTiles)
            {
                if (tile != null)
                    tile.setGray(false);
            }
        }

        else if (mode == "move")
        {
            //set color of the selected unit back to white and deselect
            if (unitSelected != null)
            {
                unitSelected.setImage(Color.white);
                unitSelected = null;
            }
        }

        //clear selection
        if (unitSelected != null)
        {
            unitSelected.setImage(Color.white);
            unitSelected = null;
        }

        if (spawnInfoSelected != null)
        {
            spawnInfoSelected.setSpawnImageColor(Color.white);
            spawnInfoSelected = null;
        }

        mode = "select";
        turnEnded = true;
    }
}