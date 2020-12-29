using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.Entities
{
    public class ImportFolder : Entity
    {
        [Required]
        public string Location { get; set; } = null!;

    }
}
