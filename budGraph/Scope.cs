using System.IO;
using System.Collections.Immutable;
using Bud.SettingsConstruction;
using Bud.SettingsConstruction.Ops;
using System.Text;

namespace Bud {
  public class Scope {
    public static readonly Scope Global = new Scope("Global", null);
    public readonly Scope Parent;
    public readonly string Id;
    private readonly int Depth;
    private readonly int Hash;

    public Scope(string id) : this(id, Global) {
    }

    protected Scope(string id, Scope parent) {
      Parent = parent ?? this;
      Id = id;
      Depth = parent == null ? 1 : (parent.Depth + 1);
      unchecked {
        Hash = (parent == null ? -1498327287 : parent.Hash) ^ Id.GetHashCode();
      }
    }

    public bool IsGlobal {
      get { return ReferenceEquals(this, Global); }
    }

    public Scope In(Scope parent) {
      if (Parent.Equals(parent)) {
        return this;
      }
      return new Scope(Id, parent);
    }

    public bool Equals(Scope otherScope) {
      if (ReferenceEquals(this, otherScope)) {
        return true;
      }
      if (Depth != otherScope.Depth) {
        return false;
      }
      return Id.Equals(otherScope.Id) && Parent.Equals(otherScope.Parent);
    }

    public override bool Equals(object other) {
      if (other == null) {
        return false;
      }
      if (!(other is Scope)) {
        return false;
      }
      return Equals((Scope)other);
    }

    public override int GetHashCode() {
      return Hash;
    }

    public override string ToString() {
      return IsGlobal ? Id : AppendAsString(new StringBuilder()).ToString();
    }

    private StringBuilder AppendAsString(StringBuilder stringBuilder) {
      return IsGlobal ? stringBuilder.Append(Id) : Parent.AppendAsString(stringBuilder).Append(':').Append(Id);
    }
  }
}
