using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shizou.Entities
{
    public class ImportFolder : Entity
    {
        public string Location { get; set; } = null!;

    }
}
