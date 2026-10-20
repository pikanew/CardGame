using UnityEngine;

public class Player : MonoBehaviour
{
    public Deck Deck { get; private set; }
    [SerializeField] private int num;

    private Hand hand;

    public int Num => num;
    public Hand Hand => hand;


    public void Initialize(int playerNum)
    {
        num = playerNum;
        hand = new Hand();
        Deck = new Deck();
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
