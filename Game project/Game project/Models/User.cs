namespace Game_project.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; } = false;

        public string? EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmationTokenExpiresAt { get; set; }

        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        public PlayerStats? Stats { get; set; }
    }
}
