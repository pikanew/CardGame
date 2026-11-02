public static class RankedMatchSession
{
    public static string RoomCode { get; private set; }
    public static int PlayerNumber { get; private set; }

    public static bool HasMatch =>
        !string.IsNullOrEmpty(RoomCode);

    public static void SetMatch(string roomCode, int playerNumber)
    {
        RoomCode = roomCode;
        PlayerNumber = playerNumber;
    }

    public static void Clear()
    {
        RoomCode = null;
        PlayerNumber = 0;
    }
}