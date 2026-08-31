namespace TechQuestBackend.Models
{
    public class ProfessorUpload
    {
        public int UploadId { get; set; }

        public int ProfessorId { get; set; }

        public string FileName { get; set; } = "";

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public User? Professor { get; set; }

        public User? CreatedByUser { get; set; }
    }
}
