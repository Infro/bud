using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using Bud.IO;
using Bud.Util;
using Bud.V1;
using static System.IO.Directory;
using static System.IO.Path;
using static Bud.BaseProjects.BuildProjects;
using static Bud.NuGet.NuGetPackageReferencesReader;
using static Bud.V1.Api;

namespace Bud.NuGet {
  internal static class PackageReferencesProjects {
    private static readonly Conf PackageReferencesProjectSettings = Conf
      .Empty
      .Add(SourcesSupport)
      .AddSourceFile(c => PackagesConfigFile[c])
      .Init(AssemblyResolver, new NuGetAssemblyResolver())
      .Init(PackageDownloader, new NuGetPackageDownloader())
      .Init(ReferencedPackages, ReadReferencedPackagesFromSources)
      .Init(PackagesConfigFile, c => Combine(ProjectDir[c], "packages.config"))
      .Init(ResolvedAssemblies, ResolveAssemblies);

    private static IObservable<IImmutableList<PackageReference>>
      ReadReferencedPackagesFromSources(IConf c)
      => Sources[c].Select(LoadReferences)
                   .Select(ImmutableList.ToImmutableList);

    internal static Conf CreatePackageReferencesProject(string projectId,
                                                        Option<string> projectDir = default(Option<string>),
                                                        Option<string> baseDir = default(Option<string>))
      => BareProject(projectId, projectDir, baseDir)
        .Add(PackageReferencesProjectSettings);

    internal static IObservable<IImmutableSet<string>> ResolveAssemblies(IConf c)
      => ReferencedPackages[c].Select(packageReferences => {
        var buildDir = BuildDir[c];
        var resolvedAssembliesFile = Combine(buildDir, "resolved_assemblies");
        CreateDirectory(buildDir);
        var hash = PackageReference.GetHash(packageReferences);
        var resolvedAssemblies = HashBasedCaching.GetLinesOrCache(
          resolvedAssembliesFile,
          hash,
          () => DownloadAndResolvePackages(c, packageReferences));
        return resolvedAssemblies.ToImmutableHashSet();
      });

    private static IEnumerable<string> DownloadAndResolvePackages(IConf c, IReadOnlyCollection<PackageReference> packageReferences) {
      var packagesDir = Combine(BuildDir[c], "cache");
      CreateDirectory(packagesDir);
      if (packageReferences.Count == 0) {
        return Enumerable.Empty<string>();
      }
      if (!PackageDownloader[c].DownloadPackages(packageReferences, packagesDir)) {
        throw new Exception($"Could not download packages: {string.Join(", ", packageReferences)}");
      }
      return AssemblyResolver[c]
        .FindAssembly(packageReferences, packagesDir, BuildDir[c]);
    }
  }
}