using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private int num;

    public int Num => num;

    public void Initialize(int playerNum)
    {
        num = playerNum;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
