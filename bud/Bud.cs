using System;
using Bud.Plugin.CSharp;

namespace Bud {
  public static class Bud {

    public static BuildConfiguration Load(string path) {
      return new BuildConfiguration(path);
    }

    public static void Evaluate(BuildConfiguration buildConfiguration, string key) {
      if ("compile".Equals(key)) {
        CSharpPlugin.Compile(buildConfiguration);
      } else {
        DefaultBuildPlugin.Clean(buildConfiguration);
      }
    }

  }
}

