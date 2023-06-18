using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using System.IO;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] Canvas tutorialCanvas;
    [SerializeField] List<string> instructions;
    [SerializeField] TextMeshProUGUI instructionText;
    [SerializeField] int index;
    [SerializeField] GameObject forwardbtn;
    [SerializeField] GameObject backwardbtn;

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

        // read tutorial directions
        TextAsset mytxtData = (TextAsset)Resources.Load("Text/TutorialText");
        string txt = mytxtData.text;
        string[] lines = txt.Split("\n");
        foreach (string line in lines)
            instructions.Add(line);

        // setup
        index = 0;
        instructionText.text = instructions[index];
        //advance();
        UIManager.instance.timerPaused = true;
        tutorialCanvas.gameObject.SetActive(true);
    }

    void Update()
    {
        // first slide
        if (index == 0)
        {
            // advance when player spawned main base
            if (PlayerController.instance.mainBase != null)
            {
                advance();
            }
        }

        // second, third, and fourth slides
        if (index == 1 || index == 2 || index == 3)
        {
            // advance when clicked
            if (Input.GetMouseButtonDown(0))
            {
                advance();
            }
        }

        // fifth slide
        if (index == 4)
        {
            // advance when number of turn increases
            if (UIManager.instance.getTurnNum() > 1)
            {
                advance();
            }
        }

        // sixth slide
        if (index == 5)
        {
            // advance when a unit is selected to be spawned
            if (PlayerController.instance.toSpawnUnit != null)
            {
                advance();
            }
        }
    }

    void advance()
    {
        if (tutorialArrows[index] != null)
            tutorialArrows[index].SetActive(false);

        index++;
        instructionText.text = instructions[index];

        if (tutorialArrows[index] != null)
            tutorialArrows[index].SetActive(true);
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
