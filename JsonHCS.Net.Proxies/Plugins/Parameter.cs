using System;
using System.Collections.Generic;
using System.Text;

namespace JsonHCSNet.Proxies.Plugins
{
    public struct Parameter : IEquatable<Parameter>
    {
        public string Name;
        public SourceType Type;
        public object Value;

        public Parameter(string name, SourceType type, object val) : this()
        {
            Name = name;
            this.Type = type;
            this.Value = val;
        }

        public override bool Equals(Object obj)
        {
            return obj is Parameter && this == (Parameter)obj;
        }

        public override int GetHashCode()
        {
            return (Name.GetHashCode() << 8) ^ (Type.GetHashCode() << 4) ^ (Value?.GetHashCode() ?? 0);
        }

        public static bool operator ==(Parameter x, Parameter y)
        {
            return x.Name == y.Name && x.Type == y.Type && x.Value == y.Value;
        }

        public static bool operator !=(Parameter x, Parameter y)
        {
            return !(x == y);
        }

        public bool Equals(Parameter other)
        {
            return this == other;
        }
    }

    public enum SourceType { Body, Form, Header, Query, Route, Services }
}
