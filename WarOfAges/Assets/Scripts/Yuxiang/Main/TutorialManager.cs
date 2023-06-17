using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using System.IO;
using System.Text.RegularExpressions;

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
        //destroy if not tutorial
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Tutorial") ||
            !(bool)PhotonNetwork.CurrentRoom.CustomProperties["Tutorial"])
        {
            tutorialCanvas.gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        //read tutorial directions
        TextAsset mytxtData = (TextAsset)Resources.Load("Text/TutorialText");
        string txt = mytxtData.text;

        string[] lines = txt.Split("\n");

        foreach (string line in lines)
            instructions.Add(Regex.Unescape(line));


        tutorialCanvas.gameObject.SetActive(true);

        //first direction
        instructionText.text = instructions[0];

        UIManager.instance.timerPaused = true;
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

        // second slide
        if (index == 1)
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
