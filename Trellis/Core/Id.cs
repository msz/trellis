using System;

namespace Trellis.Core
{
    public class Id : IEquatable<Id>
    {
        private string idVal;
        private Id() { }
        public bool Equals(Id other)
        {
            return other.idVal.Equals(idVal);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Id;
            if (other == null)
                throw new ArgumentException("Can only compare other Id", "obj");
            return idVal.Equals(other.idVal);   
        }

        public override int GetHashCode()
        {
            return idVal.GetHashCode();
        }

        public static implicit operator string(Id id)
        {
            return id.idVal;
        }

        public static implicit operator Id(string id)
        {
            return new Id() { idVal = id };
        }

        public static implicit operator int(Id id)
        {
            return Convert.ToInt32(id.idVal);
        }

        public static implicit operator Id(int id)
        {
            return new Id { idVal = id.ToString() };
        }

        public static implicit operator long(Id id)
        {
            return Convert.ToInt64(id.idVal);
        }

        public static implicit operator Id(long id)
        {
            return new Id { idVal = id.ToString() };
        }

        public override string ToString()
        {
            return string.Format("Id<{0}>", idVal);
        }
    }
}
