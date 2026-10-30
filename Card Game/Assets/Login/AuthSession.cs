using UnityEngine;

public static class AuthSession
{
    public static string Token { get; private set; }
    public static int UserId { get; private set; }
    public static string Username { get; private set; }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public static void Login(string token, int userId, string username)
    {
        Token = token;
        UserId = userId;
        Username = username;
    }

    public static void Logout()
    {
        Token = null;
        UserId = 0;
        Username = null;
    } 
}
