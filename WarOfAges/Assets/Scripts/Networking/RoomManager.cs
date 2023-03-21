using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.IO;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;
using Unity.VisualScripting;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    [SerializeField] TextMeshProUGUI mapSettingText;
    [SerializeField] TMP_InputField initialTimeInput;
    [SerializeField] TMP_InputField timeIncInput;

    void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        //Debug.Log(scene.buildIndex);

        if (scene.buildIndex == 1)// game scene
        {
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player/PlayerManager"), Vector3.zero, Quaternion.identity);
        }
    }

    //change map setting
    public void changeMapSetting()
    {
        string mode = (string)PhotonNetwork.CurrentRoom.CustomProperties["Mode"];

        Hashtable hash = new Hashtable();

        if (mode == "Water")
        {
            mapSettingText.text = "Water: Off";
            hash.Add("Mode", "Land");
        }
        else
        {
            mapSettingText.text = "Water: On";
            hash.Add("Mode", "Water");
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
    }

    public bool validSetting()
    {
        Hashtable hash = new Hashtable();

        bool res = true;

        //make sure integers are inputed
        if (int.TryParse(initialTimeInput.text, out int num))
        {
            hash.Add("initialTime", num);
        }
        else
        {
            //default value
            if (initialTimeInput.text == "")
            {
                hash.Add("initialTime", Config.defaultStartingTime);
            }
            else
            {
                initialTimeInput.text = "";
                res = false;
            }
        }

        if (int.TryParse(timeIncInput.text, out int num2))
        {
            hash.Add("timeInc", num2);
        }
        else
        {
            //default value
            if (timeIncInput.text == "")
            {
                hash.Add("timeInc", Config.defaultTimeInc);
            }
            else
            {
                timeIncInput.text = "";
                res = false;
            }
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

        return res;
    }

    //when room host change
    public void updateBtn()
    {
        //map setting
        bool hasWater = (string)PhotonNetwork.CurrentRoom.CustomProperties["Mode"] == "Water";

        //update text
        if (hasWater)
        {
            mapSettingText.text = "Water: On";
        }
        else
        {
            mapSettingText.text = "Water: Off";
        }
    }
}
