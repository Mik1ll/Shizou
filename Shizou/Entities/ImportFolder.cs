using System.ComponentModel.DataAnnotations;

namespace Shizou.Entities
{
    public class ImportFolder : Entity
    {
        [Required]
        public string Location { get; set; } = null!;
    }
}