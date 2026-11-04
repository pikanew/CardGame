using UnityEngine;

public interface IContinuousPowerEffect
{
    int GetPowerChange(AbilityContext context, Card target);
}
