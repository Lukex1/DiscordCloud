namespace DiscordCloud.Core.Models
{
    public class AggregatedFile
    {
        public string FileName { get; set; }
        public DateTime CreationDate { get; set; }
        public double FileSize { get; set; }
        public List<string> Urls { get; set; }
    }
}
