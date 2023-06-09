using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using System.IO;
using Unity.VisualScripting;
using Photon.Realtime;
using static Photon.Pun.UtilityScripts.TabViewManager;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    public Canvas tutorialCanvas;
    [SerializeField] List<string> instructions;
    [SerializeField] TextMeshProUGUI instructionText;
    [SerializeField] int index;
    [SerializeField] GameObject forwardbtn;
    [SerializeField] GameObject backwardbtn;

    [SerializeField] List<GameObject> tutorialFilters;
    [SerializeField] List<GameObject> tutorialArrows;

    private void Start()
    {
        // destroy if not tutorial
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Tutorial") ||
            !(bool)PhotonNetwork.CurrentRoom.CustomProperties["Tutorial"])
        {
            tutorialCanvas.gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        instance = this;

        // read tutorial directions
        TextAsset mytxtData = (TextAsset)Resources.Load("Text/TutorialText");
        string txt = mytxtData.text;
        string[] lines = txt.Split("\n");
        foreach (string line in lines)
            instructions.Add(line);

        // setup
        index = 0;
        instructionText.text = instructions[index*2];
        UIManager.instance.timerPaused = true;
        tutorialCanvas.gameObject.SetActive(true);

        // hide all arrows
        foreach (GameObject arrow in tutorialArrows)
            if (arrow != null)
                arrow.SetActive(false);

        // hide all filters
        foreach (GameObject filter in tutorialFilters)
            if (filter != null)
                filter.SetActive(false);
    }

    void Update()
    {
        if (index == 0)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        else if (index == 1)
        {
            // advance when base is placed
            if (PlayerController.instance.mainBase != null)
            {
                advance();
            }
        }

        else if (index == 2 || index == 3 || index == 4)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        else if (index == 5)
        {
            // advance when it is the second turn
            if (UIManager.instance.getTurnNum() > 1)
            {
                advance();
            }
        }

        else if (index == 6 || index == 7)
        {   
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        else if (index == 8)
        {
            // advance when a spawnButton is selected
            if (PlayerController.instance.toSpawnUnit != null)
            {
                advance();
            }
        }

        else if (index == 9)
        {
            // advance when a unit is spawned
            if (PlayerController.instance.spawnList.Count != 0)
            {
                advance();
            }
        }

        else if (index == 10)
        {
            // advance when the spawnButton is deselected
            if (PlayerController.instance.toSpawnUnit == null)
            {
                advance();
            }
        }

        else if (index == 11)
        {   
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        else if (index == 12)
        {
            // advance when it is the third turn
            if (UIManager.instance.getTurnNum() > 2)
            {
                advance();
            }
        }

        else if (index == 13)
        {   
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        else if (index == 14)
        {
            // advance when troop has an arrow
            if (PlayerController.instance.allTroops[0].arrow != null)
            {
                advance();
            }
        }

        else if (index == 15)
        {   
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        else if (index == 16)
        {
            // advance when it is the fourth turn
            if (UIManager.instance.getTurnNum() > 3)
            {
                advance();
            }
        }

        else if (index > 16 && index < 23)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        else if (index == 23)
        {
            // advance when a unit is unlocked
            if (SpawnManager.instance.keys == 0)
            {
                advance();
            }
        }

        else if (index > 23 && index < 29)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        else if (index == 29)
        {
            // end of tutorial when clicked
            if (Input.GetMouseButtonDown(0))
            {
                tutorialCanvas.gameObject.SetActive(false);
            }
        }
    }

    void advance()
    {
        if (tutorialArrows[index] != null)
            tutorialArrows[index].SetActive(false);
        //if (tutorialFilters[index] != null)
        //    tutorialFilters[index].SetActive(false);
        index++;
        instructionText.text = instructions[index * 2];

        if (tutorialArrows[index] != null)
            tutorialArrows[index].SetActive(true);
        //if (tutorialFilters[index] != null)
        //    tutorialFilters[index].SetActive(true);
    }

    public void endTutorial()
    {
        Destroy(RoomManager.Instance.gameObject);
        PhotonNetwork.LoadLevel(0);
    }
}
