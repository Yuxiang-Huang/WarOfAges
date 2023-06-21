using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Linq;
using System.IO;
using Unity.VisualScripting;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager instance;

    public SortedDictionary<int, Controller> playerList = new();

    public List<Controller> allPlayersOriginal = new List<Controller>();
    public List<Controller> allPlayers = new List<Controller>();

    public HashSet<Tile> spellTiles = new();

    public HashSet<IUnit> unsellableUnits = new();

    #region Begin Game

    public virtual void createPlayerList()
    {

    }

    public virtual void checkStart()
    {

    }

    #endregion

    #region Start Turn

    [PunRPC]
    public virtual void startTurn()
    {

    }

    //call by endTurn in UIManager
    [PunRPC]
    public virtual void endTurn()
    {
        
    }

    //call by cancelEndTurn in UIManager
    public void cancelEndTurn()
    {
        PlayerController.instance.turnEnded = false;

        //revert endturn property
        Hashtable playerProperties = new Hashtable();
        playerProperties.Add("EndTurn", false);
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
    }

    #endregion

    #region TakeTurn

    public virtual void checkEndTurn()
    {
        
    }

    public virtual void checkSpawn()
    {

    }

    public virtual void checkMove()
    {
        
    }

    public virtual IEnumerator delayAttack()
    {
        yield return new WaitForEndOfFrame();
    }

    public virtual void checkAttack()
    {
        
    }

    public virtual void checkDeath()
    {
        
    }

    #endregion
}
