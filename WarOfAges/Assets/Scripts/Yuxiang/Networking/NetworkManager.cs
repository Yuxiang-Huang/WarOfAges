using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.IO;
using Unity.VisualScripting;

public class NetworkManager: MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    [SerializeField] TMP_InputField roomNameInput;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomListPrefab;

    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject playerListPrefab;

    [SerializeField] GameObject startGameButton;
    [SerializeField] GameObject settingButtons;

    private static Dictionary<string, RoomInfo> fullRoomList = new Dictionary<string, RoomInfo>();

    void Awake()
    {
        Instance = this;

        roomNameInput.text = "Room " + Random.Range(0, 1000).ToString("0000");
    }

    public void Connect()
    {
        ScreenManager.Instance.DisplayScreen("Loading");
        Debug.Log("Connecting to Master");
        PhotonNetwork.ConnectUsingSettings();
    }

    public void Disconnect()
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnConnectedToMaster()
    {
        if (PhotonNetwork.OfflineMode) return;

        Debug.Log("Joined Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        ScreenManager.Instance.DisplayScreen("Play");
        Debug.Log("Joined Lobby");
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text))
            return;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 6;

        //default setting
        roomOptions.CustomRoomProperties = new Hashtable() {
            { "Mode", Config.defaultMode }
        };

        PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);

        ScreenManager.Instance.DisplayScreen("Loading");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Room Creation Failed: " + errorText;
        ScreenManager.Instance.DisplayScreen("Error");
    }

    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);
        ScreenManager.Instance.DisplayScreen("Loading");
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.OfflineMode) return;

        ScreenManager.Instance.DisplayScreen("Room");
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        //clear player list
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        //create players
        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(playerListPrefab, playerListContent)
            .GetComponent<PlayerListItem>().SetUp(players[i]);
        }

        //Start Game and Settings Button only visible for the host
        settingButtons.SetActive(PhotonNetwork.IsMasterClient);
        startGameButton.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 2);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        //Updates Start Game and Settings Button only visible for the host
        startGameButton.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 2);
        settingButtons.SetActive(PhotonNetwork.IsMasterClient);
        RoomManager.Instance.updateBtn();
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        ScreenManager.Instance.DisplayScreen("Loading");
    }

    public override void OnLeftRoom()
    {
        ScreenManager.Instance.DisplayScreen("Loading");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        //clear list
        foreach (Transform transform in roomListContent)
        {
            Destroy(transform.gameObject);
        }

        //make room
        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo info = roomList[i];

            if (info.RemovedFromList)
            {
                fullRoomList.Remove(info.Name);
            }
            else
            {
                fullRoomList[info.Name] = info;
            }
        }
        foreach (KeyValuePair<string, RoomInfo> entry in fullRoomList)
        {
            Instantiate(roomListPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(fullRoomList[entry.Key]);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(playerListPrefab, playerListContent)
            .GetComponent<PlayerListItem>().SetUp(newPlayer);

        //update start game button depend on number of player
        startGameButton.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 2);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        //upate start game button depend on number of player
        startGameButton.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 2);
    }

    public void StartGame()
    {
        if (RoomManager.Instance.validSetting())
        {
            PhotonNetwork.LoadLevel(1);
        }
    }

    public void StartTutorial()
    {
        ScreenManager.Instance.DisplayScreen("Loading");
        StartCoroutine(nameof(StartTutorialEnu));
    }

    public IEnumerator StartTutorialEnu()
    {
        //offline mode
        PhotonNetwork.Disconnect();
        yield return new WaitUntil(() => !PhotonNetwork.IsConnected);
        PhotonNetwork.OfflineMode = true;

        //default room options
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.CustomRoomProperties = new Hashtable() {
                { "Mode", Config.defaultMode },
                { "initialTime", Config.defaultStartingTime },
                { "timeInc", Config.defaultTimeInc },
                { "mapRadius", Config.defaultMapRadius },
                { "Tutorial", true}
            };

        //create a room
        PhotonNetwork.CreateRoom("Tutorial", roomOptions);

        PhotonNetwork.LoadLevel(1);
    }
}
