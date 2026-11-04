using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Backend")]
    [SerializeField]
    private string apiUrl =
        "https://localhost:7091/api/Auth/login";

    [Serializable]
    private class LoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    private class LoginResponse
    {
        public string message;
        public int userId;
        public string username;
        public string token;
    }

    public void Login()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Please enter your email and password.";
            return;
        }

        StartCoroutine(SendLoginRequest(email, password));
    }

    private IEnumerator SendLoginRequest(string email, string password)
    {
        loginButton.interactable = false;
        statusText.text = "Logging in...";

        LoginRequest loginData = new LoginRequest
        {
            email = email,
            password = password
        };

        string json = JsonUtility.ToJson(loginData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                statusText.text = "Login failed: " + request.error;
                loginButton.interactable = true;
                yield break;
            }

            LoginResponse response =
                JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

            if (response == null || string.IsNullOrEmpty(response.token))
            {
                statusText.text = "Invalid server response.";
                loginButton.interactable = true;
                yield break;
            }

            AuthSession.Login(
                response.token,
                response.userId,
                response.username
            );

            statusText.text = "Welcome, " + response.username;

            SceneManager.LoadScene("MainMenu");
        }
    }
}