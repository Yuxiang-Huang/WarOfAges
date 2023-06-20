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
        // Welcome! Begin your game by placing your <b>base</b> on a visible green <b>tile</b> on the map. Each tile can either be water (blue) or land (green), but only land tiles can generate <b>income</b>.
        if (index == 0)
        {
            // advance when player spawned main base
            if (PlayerController.instance.mainBase != null)
            {
                advance();
            }
        }

        // The highlighted tiles are your <b>territory</b>. You will learn how to expand your territory later.
        // <br><br>Click anywhere to continue.

        // Here is the player info tab, where you can view info about each player.For now, know that the hexagon symbol shows how many tiles are your territory and the coin symbol shows how many<b>gold</b> you have.
        // <br><br>Click anywhere to continue.

        // Here you can also see the amount of Gold you have. The second number shows the income you will get after this <b>turn</b>.
        // <br><br>Click anywhere to continue.
        if (index == 1 || index == 2 || index == 3)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        // Now end your turn by clicking the End Turn button. Notice that your gold increases.
        if (index == 4)
        {
            // advance when it is the second turn
            if (UIManager.instance.getTurnNum() > 1)
            {
                advance();
            }
        }

        // Here is the shop. You can view info about each <b>unit</b> by hovering or holding on its image. Let's learn how to spawn a unit now. First, you select a unit in the shop. Most units are either a <b>troop</b> or <b>building</b>, but there are <i>exceptions</i>…
        if (index == 5)
        {
            // advance when a spawnButton is selected
            if (PlayerController.instance.toSpawnUnit != null)
            {
                advance();
            }
        }

        // This unit is a troop. You can view its info in the <b>unit info bar</b> on the right. You can spawn troops only on territory adjacent to a building. Your base is a building. Let's spawn your first troop!
        if (index == 6)
        {
            // advance when a unit is spawned
            if (PlayerController.instance.spawnList.Count != 0)
            {
                advance();
            }
        }

        // Let's deselect the unit in the shop by clicking anywhere that is not a spawnable tile.
        if (index == 7)
        {
            // advance when the spawnButton is deselected
            if (PlayerController.instance.toSpawnUnit == null)
            {
                advance();
            }
        }

        // Now let's click on the unit you just put on the spawn list.
        if (index == 8)
        {
            // advance when a spawninfo is selected
            if (PlayerController.instance.spawnInfoSelected != null)
            {
                advance();
            }
        }

        // You can see its info in the unit info bar. You can choose to despawn it and you will get all the gold used to spawn this unit back.
        // <br><br>Click anywhere to continue.
        if (index == 9)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        // Put a unit in the spawn list.
        if (index == 10)
        {
            // advance when a unit is put on spawn list
            if (PlayerController.instance.spawnList.Count != 0)
            {
                advance();
            }
        }

        // Units will be spawned after the turn ends. Let's end your turn by clicking the End Turn button.
        if (index == 11)
        {
            // advance when it is the third turn
            if (UIManager.instance.getTurnNum() > 2)
            {
                advance();
            }
        }

        // Remember the player info tab? The soldier icon shows how many troops you have and the house icon shows how many buliding you have.
        // <br><br>Click anywhere to continue.
        if (index == 12)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        // Let's select the troop you just spawned.
        if (index == 13)
        {
            // advance when a troop is selected
            if (PlayerController.instance.unitSelected != null)
            {
                advance();
            }
        }

        // Again, you can see its info in the unit info bar. To move your troop now, simply click on any tile you want it to move to.
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

        // The red arrow shows the tile this troop will move to after the turn ends. End the turn by clicking the End Turn Button or the turn will automatically end after the time runs out.
        if (index == 15)
        {
            // advance when it is the fourth turn
            if (UIManager.instance.getTurnNum() > 3)
            {
                advance();
            }
        }

        // The troop will continue to move to its destination tile, which you set last turn. To prevent a troop from moving next turn, you can simply set its destination tile to the tile it is on.<br><br>Click anywhere to continue.
        if (index == 16)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        if (index > 16)
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
