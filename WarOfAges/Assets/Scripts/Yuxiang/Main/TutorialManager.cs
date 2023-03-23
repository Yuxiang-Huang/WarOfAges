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
            instructions.Add(line);


        tutorialCanvas.gameObject.SetActive(true);
        UIManager.instance.IntroText.SetActive(false);

        //first direction
        instructionText.text = instructions[0];
        backwardbtn.SetActive(false);
        forwardbtn.SetActive(true);

        //StartCoroutine(nameof(firstSlide));
    }

    //public IEnumerator firstSlide()
    //{
    //    yield return new WaitForSeconds(1f);

    //    //player spawned main base
    //    if (PlayerController.instance.mainBase != null)
    //    {
    //        //first instruction
    //        instructions[0].SetActive(true);
    //        backwardbtn.SetActive(false);
    //        forwardbtn.SetActive(true);
    //    }
    //    else
    //    {
    //        StartCoroutine(nameof(firstSlide));
    //    }
    //}

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
}
