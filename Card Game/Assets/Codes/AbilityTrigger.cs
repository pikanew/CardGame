using UnityEngine;
using System;


[CreateAssetMenu(fileName = "AbilityTrigger", menuName = "Scriptable Objects/AbilityTrigger")]
public abstract class AbilityTrigger : ScriptableObject
{
    public abstract Action Register(AbilityInstance ability, Board board);
}
