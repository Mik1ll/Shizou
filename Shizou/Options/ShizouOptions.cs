namespace Shizou.Options
{
    public class ShizouOptions
    {
        public const string Shizou = "Shizou";

        public ImportOptions Import { get; set; } = null!;

        public class ImportOptions
        {
        }
    }
}