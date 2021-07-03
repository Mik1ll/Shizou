using Shizou.Dtos;

namespace Shizou.Entities
{
    public abstract class Entity
    {
        public int Id { get; set; }

        public abstract EntityDto ToDto();
    }
}
