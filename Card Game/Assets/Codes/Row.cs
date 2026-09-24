using UnityEngine;

public class Row : MonoBehaviour
{
    public int Player1Sum { get; private set; }
    public int Player2Sum {get; private set;}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddP1Sum(int amount) { Player1Sum += amount; }
    public void AddP2Sum(int amount) { Player2Sum += amount; }

}
