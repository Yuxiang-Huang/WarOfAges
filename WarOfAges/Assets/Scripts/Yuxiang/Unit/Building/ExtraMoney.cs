using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtraMoney : Building
{
    public int income()
    {
        //produce 1/8 the cost
        return (sellGold / 4);
    }
}
