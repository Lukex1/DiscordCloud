namespace DiscordCloud.Models
{
    public class FileModel
    {
        public string FileName { get; set; }
        public DateTime CreationDate { get; set; }
        public double FileSize { get; set; }
        public List<string> Urls { get; set; }
    }
}
