using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtraMoney : Building
{
    public override void effect()
    {
        //produce 1/10 the cost
        PlayerController.instance.gold += (sellGold / 4);
    }
}
