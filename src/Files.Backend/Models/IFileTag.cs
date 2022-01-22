namespace Files.Backend.Models
{
    public interface IFileTag
    {
        public string TagName { get; set; }
        public string Uid { get; set; }
        public string ColorString { get; set; }
    }
}
