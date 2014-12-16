using Bud.Plugins.CSharp.Compiler;
using Bud.Plugins.Projects;
using System.IO;
using Bud.Util;
using Bud.Plugins.Build;
using Bud.Plugins.Dependencies;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Bud.Plugins.CSharp {

  public class CSharpPlugin : IPlugin {
    public static readonly CSharpPlugin Instance = new CSharpPlugin();

    private CSharpPlugin() {
    }

    public Settings ApplyTo(Settings settings, Scope scope) {
      return settings
        .Add(BuildPlugin.Instance)
        .Add(DependenciesPlugin.Instance)
        .InitOrKeep(CSharpKeys.Build.In(scope), ctxt => MonoCompiler.CompileProject(ctxt, scope))
        .InitOrKeep(CSharpKeys.SourceFiles.In(scope), context => FindSources(context, scope))
        .InitOrKeep(CSharpKeys.AssemblyType.In(scope), AssemblyType.Exe)
        .InitOrKeep(CSharpKeys.CollectReferencedAssemblies.In(scope), context => CollectAssembliesFromDependencies(context, scope))
        .InitOrKeep(CSharpKeys.OutputAssemblyDir.In(scope), context => Path.Combine(context.GetOutputDir(scope), ".net-4.5", "main", "debug", "bin"))
        .InitOrKeep(CSharpKeys.OutputAssemblyName.In(scope), context => scope.Id)
        .InitOrKeep(CSharpKeys.OutputAssemblyFile.In(scope), context => Path.Combine(context.GetCSharpOutputAssemblyDir(scope), string.Format("{0}.{1}", context.GetCSharpOutputAssemblyName(scope), context.GetAssemblyFileExtension(scope))))
        .AddDependencies(BuildKeys.Build.In(scope), CSharpKeys.Build.In(scope));
    }

    public static async Task<ImmutableList<string>> CollectAssembliesFromDependencies(EvaluationContext context, Scope currentProject) {
      var collectedAssemblies = ImmutableList<string>.Empty;
      var resolvedDependencies = await context.ResolveDependencies(currentProject);
      foreach (var resolvedDependency in resolvedDependencies) {
        var resolvedScopeDependency = resolvedDependency as ResolvedScopeDependency;
        if (resolvedScopeDependency != null && context.Exists(CSharpKeys.OutputAssemblyFile.In(resolvedScopeDependency.Dependency))) {
          collectedAssemblies = collectedAssemblies.Add(context.GetCSharpOutputAssemblyFile(resolvedScopeDependency.Dependency));
        }
      }
      return collectedAssemblies;
    }

    public IEnumerable<string> FindSources(EvaluationContext context, Scope scope) {
      var sourceDirectory = context.GetCSharpSourceDir(scope);
      if (Directory.Exists(sourceDirectory)) {
        return Directory.EnumerateFiles(sourceDirectory);
      } else {
        return ImmutableList<string>.Empty;
      }
    }
  }
}
