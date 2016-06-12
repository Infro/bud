using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Bud.Building;
using Bud.References;
using Newtonsoft.Json;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Repositories;
using NuGet.Versioning;
using static Bud.References.AssemblyAggregator;
using FrameworkAssemblyReference = Bud.References.FrameworkAssemblyReference;

namespace Bud.NuGet {
  public class NuGetReferenceResolver : INuGetReferenceResolver {
    public ResolvedReferences Resolve(IEnumerable<PackageReference> packageReferences, string outputDir) {
      var packageReferencesList = packageReferences as IList<PackageReference> ?? packageReferences.ToList();
      if (packageReferencesList.Count == 0) {
        return ResolvedReferences.Empty;
      }
      var packagesConfigFile = PackageReference.WritePackagesConfigXml(packageReferencesList, Path.Combine(outputDir, "packages.config"));
      var nugetV2RepoDir = Path.Combine(outputDir, "packages");
      var nugetV3RepoDir = Path.Combine(outputDir, "pacakges-v3");
      HashBasedBuilder.Build(RestorePackages, packagesConfigFile, nugetV2RepoDir);
      var assemblyPathsJsonFile = HashBasedBuilder.Build((inputfile, outputfile) => ResolveAssemblies(packageReferencesList, nugetV2RepoDir, nugetV3RepoDir, outputfile),
                                                         packagesConfigFile,
                                                         Path.Combine(outputDir, "nuget_assemblies.json"));
      var readAllText = File.ReadAllText(assemblyPathsJsonFile);
      return JsonConvert.DeserializeObject<ResolvedReferences>(readAllText);
    }

    private static void RestorePackages(string packagesConfigFile, string packagesDir) {
      BatchExec.RunQuietlyThrow("nuget",
                                $"restore {packagesConfigFile} -PackagesDirectory {packagesDir}");
    }

    private static void ResolveAssemblies(IEnumerable<PackageReference> packageReferences, string packagesDir, string indexDir, string outputNuGetReferencesJsonFile) {
      var nugetRepo = CreateNuGetV3Repo(packagesDir, indexDir);
      var resolvedReferences = FindAssemblies(packageReferences, packagesDir, nugetRepo);
      var assembliesListJson = JsonConvert.SerializeObject(resolvedReferences, Formatting.Indented);
      File.WriteAllText(outputNuGetReferencesJsonFile, assembliesListJson);
    }


    public static ResolvedReferences FindAssemblies(IEnumerable<PackageReference> packageReferences,
                                                    string packagesCacheDir,
                                                    NuGetv3LocalRepository localRepository) {
      var frameworkAssemblies = new List<FrameworkAssemblyReference>();
      var assemblies = new List<ResolvedAssembly>();
      foreach (var packageReference in packageReferences) {
        var packageInfo = FindBestMatch(localRepository, packageReference);
        var nuspec = new NuspecReader(XDocument.Load(packageInfo.ManifestPath));

        frameworkAssemblies.AddRange(FindAssemblyFrameworkVersions(packageReference.Framework, nuspec.GetFrameworkReferenceGroups()));

        using (var fileStream = File.OpenRead(packageInfo.ZipPath)) {
          AddAssembliesFromPackage(packagesCacheDir,
                                   fileStream,
                                   assemblies,
                                   frameworkAssemblies,
                                   nuspec.GetId(),
                                   nuspec.GetVersion(),
                                   packageReference.Framework);
        }
      }
      return new ResolvedReferences(assemblies,
                                    AggregateByFrameworkVersion(frameworkAssemblies).ToList());
    }

    private static NuGetv3LocalRepository CreateNuGetV3Repo(string nugetV2RepoDir, string nugetV3RepoDir) {
      BatchExec.RunQuietlyThrow("nuget", $"init {nugetV2RepoDir} {nugetV3RepoDir}");
      return new NuGetv3LocalRepository(nugetV3RepoDir, true);
    }

    private static void AddAssembliesFromPackage(string packagesCacheDir,
                                                 Stream fileStream,
                                                 List<ResolvedAssembly> assemblies,
                                                 ICollection<FrameworkAssemblyReference> frameworkAssemblies,
                                                 string packageId,
                                                 NuGetVersion packageVersion,
                                                 NuGetFramework targetFramework) {
      var nupkg = new PackageArchiveReader(fileStream, false);
      var frameworkSpecificGroup = nupkg.GetReferenceItems()
                                        .GetNearest(targetFramework);
      var referenceItems = frameworkSpecificGroup?.Items ?? Enumerable.Empty<string>();
      var referenceItemsList = referenceItems as IList<string> ?? referenceItems.ToList();
      if (referenceItemsList.Count > 0) {
        assemblies.AddRange(referenceItemsList.Select(pathInPackage => Path.Combine(packagesCacheDir, $"{packageId}.{packageVersion}", pathInPackage))
                                              .Select(ResolvedAssembly.ToAssemblyPath)
                                              .ToList());
      } else {
        if (frameworkSpecificGroup != null) {
          frameworkAssemblies.Add(new FrameworkAssemblyReference(nupkg.GetIdentity().Id,
                                                               frameworkSpecificGroup.TargetFramework.Version));
        }
      }
    }

    private static IEnumerable<FrameworkAssemblyReference>
      FindAssemblyFrameworkVersions(NuGetFramework targetFramework,
                                    IEnumerable<FrameworkSpecificGroup> frameworkGroups) {
      var group = frameworkGroups.GetNearest(targetFramework);
      if (group == null) {
        return Enumerable.Empty<FrameworkAssemblyReference>();
      }
      var frameworkVersion = group.TargetFramework.Version;
      return group.Items
                  .Select(assemblyName => ToAssemblyFrameworkVersion(assemblyName, frameworkVersion));
    }

    private static FrameworkAssemblyReference ToAssemblyFrameworkVersion(string assemblyName, Version frameworkVersion) {
      return new FrameworkAssemblyReference(assemblyName,
                                          frameworkVersion);
    }

    private static LocalPackageInfo
      FindBestMatch(NuGetv3LocalRepository packageRepository,
                    PackageReference packageReference)
      => packageRepository
        .FindPackagesById(packageReference.Id)
        .FindBestMatch(new VersionRange(packageReference.Version), info => info?.Version);
  }
}