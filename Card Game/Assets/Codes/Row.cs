using UnityEngine;

public class Row : MonoBehaviour
{
    public int P1Sum { get; private set; }
    public int P2Sum {get; private set;}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddP1Sum(int amount) { P1Sum += amount; }
    public void AddP2Sum(int amount) { P2Sum += amount; }

}
