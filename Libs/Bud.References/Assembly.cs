namespace Bud.References {
  public class Assembly {
    public string Name { get; }
    public string Path { get; }

    public Assembly(string name, string path) {
      Name = name;
      Path = path;
    }

    public static Assembly FromPath(string path)
      => new Assembly(System.IO.Path.GetFileNameWithoutExtension(path), path);

    protected bool Equals(Assembly other)
      => string.Equals(Name, other.Name) && string.Equals(Path, other.Path);

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) {
        return false;
      }
      if (ReferenceEquals(this, obj)) {
        return true;
      }
      if (obj.GetType() != GetType()) {
        return false;
      }
      return Equals((Assembly) obj);
    }

    public override int GetHashCode() {
      unchecked {
        return (Name.GetHashCode()*397) ^ Path.GetHashCode();
      }
    }

    public override string ToString() => $"Assembly(Name: {Name}, Path: {Path})";
  }
}