using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] Canvas tutorialCanvas;
    [SerializeField] List<GameObject> instructions;
    [SerializeField] int index;
    [SerializeField] GameObject forwardbtn;
    [SerializeField] GameObject backwardbtn;

    void Awake()
    {
        //destroy if not tutorial
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Tutorial") ||
            !(bool)PhotonNetwork.CurrentRoom.CustomProperties["Tutorial"])
        {
            tutorialCanvas.gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        tutorialCanvas.gameObject.SetActive(true);
        backwardbtn.SetActive(false);
        forwardbtn.SetActive(true);
        foreach(GameObject text in instructions)
        {
            text.SetActive(false);
        }
    }

    public void forward()
    {
        if (index == 0)
        {
            backwardbtn.SetActive(true);
        }

        instructions[index].gameObject.SetActive(false);
        index++;
        instructions[index].gameObject.SetActive(true);

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

        instructions[index].gameObject.SetActive(false);
        index--;
        instructions[index].gameObject.SetActive(true);

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
