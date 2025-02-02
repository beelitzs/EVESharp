namespace EVESharp.PythonTypes.Types.Primitives
{
    public class PyBool : PyDataType
    {
        protected bool Equals(PyBool other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return Equals((PyBool) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public bool Value { get; }

        public PyBool(bool value)
        {
            this.Value = value;
        }

        public static bool operator ==(PyBool obj, bool value)
        {
            if (ReferenceEquals(null, obj) == true) return false;

            return obj.Value == value;
        }

        public static bool operator ==(PyBool obj, PyBool value)
        {
            if (ReferenceEquals(obj, value)) return true;
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(null, value)) return false;

            return obj.Equals(value);
        }

        public static bool operator !=(PyBool obj, PyBool value)
        {
            return !(obj == value);
        }

        public static bool operator !=(PyBool obj, bool value)
        {
            return !(obj == value);
        }

        public static bool operator true(PyBool obj)
        {
            return obj.Value;
        }

        public static bool operator false(PyBool obj)
        {
            return !obj.Value;
        }

        public static implicit operator bool(PyBool obj)
        {
            return obj.Value;
        }

        public static implicit operator PyBool(bool value)
        {
            return new PyBool(value);
        }

        public static implicit operator PyInteger(PyBool obj)
        {
            return new PyInteger(obj.Value == true ? 1 : 0);
        }
    }
}