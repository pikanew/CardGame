using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class AccountAccessManager : MonoBehaviour
{
    private const string BaseUrl =
        "https://localhost:7091/api/Auth";

    [Header("Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject forgotPasswordPanel;
    [SerializeField] private GameObject resetPasswordPanel;

    [Header("Registration")]
    [SerializeField] private TMP_InputField registerUsername;
    [SerializeField] private TMP_InputField registerEmail;
    [SerializeField] private TMP_InputField registerPassword;
    [SerializeField] private TMP_InputField registerConfirmPassword;

    [Header("Forgot Password")]
    [SerializeField] private TMP_InputField forgotEmail;

    [Header("Reset Password")]
    [SerializeField] private TMP_InputField resetToken;
    [SerializeField] private TMP_InputField resetNewPassword;
    [SerializeField] private TMP_InputField resetConfirmPassword;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private bool busy;

    [Serializable]
    private class RegisterRequest
    {
        public string username;
        public string email;
        public string password;
    }

    [Serializable]
    private class ForgotPasswordRequest
    {
        public string email;
    }

    [Serializable]
    private class ResetPasswordRequest
    {
        public string token;
        public string newPassword;
    }

    private void Start()
    {
        ShowLogin();
    }

    private void ShowPanel(GameObject panel)
    {
        if (busy)
            return;

        loginPanel.SetActive(panel == loginPanel);
        registerPanel.SetActive(panel == registerPanel);
        forgotPasswordPanel.SetActive(panel == forgotPasswordPanel);
        resetPasswordPanel.SetActive(panel == resetPasswordPanel);

        statusText.text = "";
    }

    public void ShowLogin()
    {
        ShowPanel(loginPanel);
    }

    public void ShowRegister()
    {
        ShowPanel(registerPanel);
    }

    public void ShowForgotPassword()
    {
        ShowPanel(forgotPasswordPanel);
    }

    public void ShowResetPassword()
    {
        ShowPanel(resetPasswordPanel);
    }

    // ---------------- REGISTRATION ----------------

    public void Register()
    {
        if (busy)
            return;

        string username = registerUsername.text.Trim();
        string email = registerEmail.text.Trim();
        string password = registerPassword.text;
        string confirmation = registerConfirmPassword.text;

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrEmpty(password))
        {
            statusText.text = "Please fill in all fields.";
            return;
        }

        if (password != confirmation)
        {
            statusText.text = "Passwords do not match.";
            return;
        }

        RegisterRequest data = new RegisterRequest
        {
            username = username,
            email = email,
            password = password
        };

        StartCoroutine(SendRegistration(data));
    }

    private IEnumerator SendRegistration(RegisterRequest data)
    {
        statusText.text = "Creating account...";

        bool succeeded = false;
        string error = "";

        yield return StartCoroutine(
            PostRequest(
                "/register",
                JsonUtility.ToJson(data),
                (success, response) =>
                {
                    succeeded = success;
                    error = response;
                }
            )
        );

        if (!succeeded)
        {
            statusText.text = "Registration failed.";
            Debug.LogError(error);
            yield break;
        }

        ShowLogin();

        statusText.text =
            "Registration successful! Check your email " +
            "and confirm your account before logging in.";
    }

    // ---------------- FORGOT PASSWORD ----------------

    public void ForgotPassword()
    {
        if (busy)
            return;

        string email = forgotEmail.text.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            statusText.text = "Enter your email address.";
            return;
        }

        ForgotPasswordRequest data = new ForgotPasswordRequest
        {
            email = email
        };

        StartCoroutine(SendForgotPassword(data));
    }

    private IEnumerator SendForgotPassword(
        ForgotPasswordRequest data)
    {
        statusText.text = "Sending reset email...";

        bool succeeded = false;
        string error = "";

        yield return StartCoroutine(
            PostRequest(
                "/forgot-password",
                JsonUtility.ToJson(data),
                (success, response) =>
                {
                    succeeded = success;
                    error = response;
                }
            )
        );

        if (!succeeded)
        {
            statusText.text = "Request failed.";
            Debug.LogError(error);
            yield break;
        }

        ShowResetPassword();

        statusText.text =
            "If the account exists, a reset email was sent. " +
            "Paste the reset link from the email below.";
    }

    // ---------------- RESET PASSWORD ----------------

    public void ResetPassword()
    {
        if (busy)
            return;

        string token = ExtractToken(resetToken.text);

        string newPassword = resetNewPassword.text;
        string confirmation = resetConfirmPassword.text;

        if (string.IsNullOrWhiteSpace(token) ||
            string.IsNullOrEmpty(newPassword))
        {
            statusText.text = "Enter the reset link and new password.";
            return;
        }

        if (newPassword != confirmation)
        {
            statusText.text = "Passwords do not match.";
            return;
        }

        ResetPasswordRequest data = new ResetPasswordRequest
        {
            token = token,
            newPassword = newPassword
        };

        StartCoroutine(SendResetPassword(data));
    }

    private IEnumerator SendResetPassword(
        ResetPasswordRequest data)
    {
        statusText.text = "Resetting password...";

        bool succeeded = false;
        string error = "";

        yield return StartCoroutine(
            PostRequest(
                "/reset-password",
                JsonUtility.ToJson(data),
                (success, response) =>
                {
                    succeeded = success;
                    error = response;
                }
            )
        );

        if (!succeeded)
        {
            statusText.text = "Password reset failed.";
            Debug.LogError(error);
            yield break;
        }

        ShowLogin();

        statusText.text =
            "Password reset successfully. You can now log in.";
    }

    // Accept either a token or the entire email link.
    private string ExtractToken(string input)
    {
        string value = input.Trim();

        int index = value.IndexOf(
            "token=",
            StringComparison.OrdinalIgnoreCase
        );

        if (index >= 0)
        {
            value = value.Substring(index + 6);

            int end = value.IndexOfAny(new[] { '&', '#' });

            if (end >= 0)
                value = value.Substring(0, end);

            value = Uri.UnescapeDataString(value);
        }

        return value;
    }

    // ---------------- HTTP ----------------

    private IEnumerator PostRequest(
        string endpoint,
        string json,
        Action<bool, string> onComplete)
    {
        busy = true;

        using (UnityWebRequest request =
            new UnityWebRequest(BaseUrl + endpoint, "POST"))
        {
            request.uploadHandler =
                new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(json)
                );

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            yield return request.SendWebRequest();

            bool success =
                request.result ==
                UnityWebRequest.Result.Success;

            onComplete?.Invoke(
                success,
                request.downloadHandler.text
            );
        }

        busy = false;
    }
}