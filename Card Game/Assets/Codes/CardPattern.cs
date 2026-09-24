using UnityEngine;

[System.Serializable]
public class CardPattern
{
    public PatternRow[] rows = new PatternRow[5];

    public int GetValue(int row, int column)
    {
        return rows[row].values[column];
    }
}
