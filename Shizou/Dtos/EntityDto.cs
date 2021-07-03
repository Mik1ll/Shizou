using Shizou.Entities;

namespace Shizou.Dtos
{
    public abstract class EntityDto
    {
        public int Id { get; set; }

        public abstract Entity ToEntity();
    }
}
