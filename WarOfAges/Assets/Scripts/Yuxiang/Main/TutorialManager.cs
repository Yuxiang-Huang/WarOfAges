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

    [SerializeField] Canvas tutorialCanvas;
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
        if (index != 0)
            advance();
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

        if (index == 1)
        {
            // advance when base is placed
            if (PlayerController.instance.mainBase != null)
            {
                advance();
            }
        }

        if (index == 2 || index == 3 || index == 4)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        if (index == 5)
        {
            // advance when it is the second turn
            if (UIManager.instance.getTurnNum() > 1)
            {
                advance();
            }
        }

        if (index == 6 || index == 7)
        {   
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        if (index == 8)
        {
            // advance when a spawnButton is selected
            if (PlayerController.instance.toSpawnUnit != null)
            {
                advance();
            }
        }

        if (index == 9)
        {
            // advance when a unit is spawned
            if (PlayerController.instance.spawnList.Count != 0)
            {
                advance();
            }
        }

        if (index == 10)
        {
            // advance when the spawnButton is deselected
            if (PlayerController.instance.toSpawnUnit == null)
            {
                advance();
            }
        }

        if (index == 11)
        {   
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        if (index == 12)
        {
            // advance when it is the third turn
            if (UIManager.instance.getTurnNum() > 2)
            {
                advance();
            }
        }

        if (index == 13)
        {   
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        if (index == 14)
        {
            // advance when troop has an arrow
            if (PlayerController.instance.allTroops[0].arrow != null)
            {
                advance();
                // timer on
                UIManager.instance.timerPaused = false;
            }
        }

        if (index == 15)
        {   
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        if (index == 16)
        {
            // advance when it is the fourth turn
            if (UIManager.instance.getTurnNum() > 3)
            {
                advance();
            }
        }

        if (index > 16 && index <= 20)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        if (index == 21)
        {
            // advance when it is the fourth turn
            if (UIManager.instance.getTurnNum() > 3)
            {
                advance();
            }
        }

        if (index == 22)
        {
            // advance when it is the fourth turn
            if (UIManager.instance.getTurnNum() > 3)
            {
                advance();
            }
        }

        if (index == 28)
        {
            // exit when clicked
            if (Input.GetMouseButtonDown(0))
            {
                endTutorial();
            }
        }
        else if (index > 22)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }
    }
    void advance()
    {
        if (tutorialArrows[index] != null) 
            tutorialArrows[index].SetActive(false);
        if (tutorialFilters[index] != null)
            tutorialFilters[index].SetActive(false);
        index++;
        instructionText.text = instructions[index*2];

        if (tutorialArrows[index] != null)
            tutorialArrows[index].SetActive(true);
        if (tutorialFilters[index] != null)
            tutorialFilters[index].SetActive(true);
    }

    #region old buttons

    public void forward()
    {
        if (index == 0)
        {
            backwardbtn.SetActive(true);
        }

        index++;
        instructionText.text = instructions[index];

        if (index == instructions.Count - 1)
        {
            forwardbtn.SetActive(false);
        }
    }

    public void backward()
    {
        if (index == instructions.Count - 1)
        {
            forwardbtn.SetActive(true);
        }

        index--;
        instructionText.text = instructions[index];

        if (index == 0)
        {
            backwardbtn.SetActive(false);
        }
    }

    public void endTutorial()
    {
        Destroy(RoomManager.Instance.gameObject);
        PhotonNetwork.LoadLevel(0);
    }

    #endregion
}
