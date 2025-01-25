using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Int", menuName = "Variable/Primitives/Int")]
public class IntVariable : Variable<int>
{
    public static IntVariable operator ++(IntVariable a)
    {
        a.Value++;
        return a;
    }
    public static IntVariable operator --(IntVariable a) 
    {
        a.Value--;
        return a;
    }    

    /// <summary>
    /// Mostly useful for being assigned to UnityEvents in the editor
    /// </summary>
    /// <param name="value"></param>
    public void Modify(int value)
    {
        Value += value;
    }
}
