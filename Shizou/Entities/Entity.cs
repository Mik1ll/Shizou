namespace Shizou.Entities
{
    public class Entity
    {
        public long Id { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is Entity other)
            {
                if (ReferenceEquals(this, other))
                    return true;
                if (GetType() != other.GetType())
                    return false;
                if (Id == 0 || other.Id == 0)
                    return false;
                return Id == other.Id;
            }
            else
                return false;
        }

        public static bool operator ==(Entity a, Entity b)
        {
            if (a is null && b is null)
                return true;
            if (a is null || b is null)
                return false;
            return a.Equals(b);
        }

        public static bool operator !=(Entity a, Entity b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(GetType(), Id);
        }
    }
}