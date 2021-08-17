using Shizou.Dtos;

namespace Shizou.Entities
{
    public abstract class Entity
    {
        public virtual int Id { get; set; }

        public abstract EntityDto ToDto();
    }
}
