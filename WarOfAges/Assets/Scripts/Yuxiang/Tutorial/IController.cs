using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public interface IController
{
    PhotonView PV { get; }

    public void startFirstIndicator(bool status);

    public void startGame(int newID, Vector2 spawnLocation);

    public void checkDeath();
}
