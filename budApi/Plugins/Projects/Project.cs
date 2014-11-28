using System.IO;
using System.Collections.Immutable;
using Bud.SettingsConstruction;
using Bud.SettingsConstruction.Ops;

namespace Bud.Plugins {

  public class Project {
    public readonly string Id;
    public readonly string BaseDir;

    public Project(string id, string baseDir) {
      this.BaseDir = baseDir;
      this.Id = id;
    }

    public override int GetHashCode() {
      return Id.GetHashCode();
    }

    public override bool Equals(object otherProject) {
      return otherProject.GetType().Equals(GetType()) && Id.Equals(((Project)otherProject).Id);
    }

    public override string ToString() {
      return string.Format("Project({0}, {1})", Id, BaseDir);
    }
  }

}

