namespace TechQuestBackend.Models
{
    /// <summary>
    /// Maps to the existing PostgreSQL Student_profile table.
    ///
    /// Student_profile:
    /// - Student_id
    /// - User_id
    /// - Total_coins
    /// - Total_points
    /// </summary>
    public class StudentProfile
    {
        public int StudentId { get; set; }

        public int UserId { get; set; }

        public int TotalCoins { get; set; }

        public int TotalPoints { get; set; }
    }
}
