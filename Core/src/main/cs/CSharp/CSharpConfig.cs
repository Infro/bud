using System.Collections.Generic;
using System.Threading.Tasks;
using Bud.Build;
using NuGet;

namespace Bud.CSharp {
  public static class CSharpConfig {
    public static Task<IEnumerable<string>> GetCSharpSources(this IContext context, Key buildTarget) {
      return context.Evaluate(buildTarget / CSharpKeys.SourceFiles);
    }

    public static Framework GetTargetFramework(this IConfig config, Key buildTarget) {
      return config.Evaluate(buildTarget / CSharpKeys.TargetFramework);
    }

    public static string GetCSharpOutputAssemblyDir(this IConfig context, Key buildTarget) {
      return context.Evaluate(buildTarget / CSharpKeys.OutputAssemblyDir);
    }

    public static string GetCSharpOutputAssemblyName(this IConfig context, Key buildTarget) {
      return context.Evaluate(buildTarget / CSharpKeys.OutputAssemblyName);
    }

    public static string GetRootNamespace(this IConfig context, Key buildTarget) {
      return context.Evaluate(buildTarget / CSharpKeys.RootNamespace);
    }

    public static string GetCSharpOutputAssemblyFile(this IConfig context, Key buildTarget) {
      return context.Evaluate(buildTarget / CSharpKeys.OutputAssemblyFile);
    }

    public static IEnumerable<string> GetReferencedAssemblyPaths(this IConfig config, Key project) {
      return config.Evaluate(project / CSharpKeys.ReferencedAssemblyPaths);
    }

    public static AssemblyType GetCSharpAssemblyType(this IConfig context, Key project) {
      return context.Evaluate(project / CSharpKeys.AssemblyType);
    }

    public static Task CSharpBuild(this IContext context, Key project) {
      return context.Evaluate(project / BuildKeys.Main / CSharpKeys.CSharp / BuildKeys.Build);
    }

    public static IEnumerable<IPackageAssemblyReference> GetAssemblyReferences(this IConfig config, Key buildTarget) {
      return config.Evaluate(buildTarget / CSharpKeys.AssemblyReferences);
    }
  }
}