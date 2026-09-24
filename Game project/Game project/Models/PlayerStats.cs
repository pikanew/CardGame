namespace Game_project.Models
{
    public class PlayerStats
    {
        public int Id { get; set; }

        public int Score { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Draws { get; set; } = 0;

        public int UserId { get; set; }

        public User User { get; set; } = null!;
    }
}
