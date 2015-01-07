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

    public Settings ApplyTo(Settings settings, Key key) {
      return settings
        .Apply(key, BuildPlugin.Instance)
        .Apply(key, DependenciesPlugin.Instance)
        .Init(CSharpKeys.Build.In(key), ctxt => MonoCompiler.CompileProject(ctxt, key))
        .Init(CSharpKeys.SourceFiles.In(key), context => FindSources(context, key))
        .Init(CSharpKeys.AssemblyType.In(key), AssemblyType.Exe)
        .Init(CSharpKeys.CollectReferencedAssemblies.In(key), context => CollectAssembliesFromDependencies(context, key))
        .Init(CSharpKeys.OutputAssemblyDir.In(key), context => Path.Combine(context.GetOutputDir(key), ".net-4.5", "main", "debug", "bin"))
        .Init(CSharpKeys.OutputAssemblyName.In(key), context => key.Id)
        .Init(CSharpKeys.OutputAssemblyFile.In(key), context => Path.Combine(context.GetCSharpOutputAssemblyDir(key), string.Format("{0}.{1}", context.GetCSharpOutputAssemblyName(key), context.GetAssemblyFileExtension(key))))
        .AddDependencies(BuildKeys.Build.In(key), CSharpKeys.Build.In(key));
    }

    public static async Task<ImmutableList<string>> CollectAssembliesFromDependencies(IContext context, Key currentProject) {
      var projectDependencies = await CollectInternalDependencies(context, currentProject);
      var nuGetDependencies = CollectExternalDependencies(context, currentProject);
      var gacDependencies = ImmutableList.Create<string>("Facades/System.Runtime.dll");
      return projectDependencies.AddRange(nuGetDependencies).AddRange(gacDependencies);
    }

    public IEnumerable<string> FindSources(IContext context, Key key) {
      var sourceDirectory = context.GetCSharpSourceDir(key);
      if (Directory.Exists(sourceDirectory)) {
        return Directory.EnumerateFiles(sourceDirectory);
      } else {
        return ImmutableList<string>.Empty;
      }
    }

    private static async Task<ImmutableList<string>> CollectInternalDependencies(IContext context, Key currentProject) {
      var collectedAssemblies = ImmutableList.CreateBuilder<string>();
      var dependencyProjects = await context.ResolveInternalDependencies(currentProject, CSharpKeys.CSharp);
      foreach (var dependency in dependencyProjects) {
        collectedAssemblies.Add(context.GetCSharpOutputAssemblyFile(dependency.Key));
      }
      return collectedAssemblies.ToImmutable();
    }

    private static ImmutableList<string> CollectExternalDependencies(IConfig context, Key currentProject) {
      var allNuGetDependencies = context.GetNuGetResolvedPackages();
      var nuGetDependencies = context.GetExternalDependencies(currentProject, CSharpKeys.CSharp);
      var nuGetRepositoryPath = context.GetNuGetRepositoryDir();
      return nuGetDependencies
        .Select(dependency => allNuGetDependencies.GetResolvedNuGetDependency(dependency))
        .SelectMany(dependency => dependency.AssemblyPaths.Select(assemblyPath => Path.Combine(nuGetRepositoryPath, dependency.RequestedDependency.Id + "." + dependency.ResolvedVersion, assemblyPath))).ToImmutableList();
    }
  }
}

