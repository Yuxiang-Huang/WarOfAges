using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtraMoney : Building
{
    public override void effect()
    {
        PlayerController.instance.gold += age + Config.ageIncomeOffset;
    }
}
