using System;

namespace Shizou.Entities
{
    public class Entity
    {
        public long Id { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is not Entity other) return false;
            if (ReferenceEquals(this, other))
                return true;
            if (GetType() != other.GetType())
                return false;
            return Id == other.Id;
        }

        public static bool operator ==(Entity? a, Entity? b)
        {
            if (a is null && b is null)
                return true;
            if (a is null || b is null)
                return false;
            return a.Equals(b);
        }

        public static bool operator !=(Entity? a, Entity? b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GetType(), Id);
        }
    }
}