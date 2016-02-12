using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reactive.Linq;
using Bud.IO;
using Bud.Util;
using Bud.V1;
using static System.IO.Path;
using static System.Linq.Enumerable;
using static Bud.V1.Api;

namespace Bud.NuGet {
  internal static class NuGetPublishing {
    internal static Conf NuGetPublishingSupport
      = Conf.Empty
            .Init(PackageMetadata, DefaultPackageMetadata)
            .Init(PackageOutputDir, c => Combine(BuildDir[c], PackageOutputDirName))
            .Init(Publish, DefaultPublish)
            .Init(PackageBaseDir, _ => Directory.GetCurrentDirectory())
            .InitValue(PublishApiKey, Option.None<string>())
            .InitValue(Packager, new NuGetPackager())
            .InitValue(Publisher, new NuGetPublisher())
            .Init(Package, DefaultPackage)
            .Init(PackageFiles, DefaultPackageFiles);

    private static NuGetPackageMetadata DefaultPackageMetadata(IConf c)
      => new NuGetPackageMetadata(Environment.UserName,
                                  ProjectId[c],
                                  ImmutableDictionary<string, string>.Empty);

    private static IObservable<string> DefaultPackage(IConf c)
      => PackageFiles[c].CombineLatest(GetReferencedPackages(c),
                                       (packageFiles, referencedPackages) => Pack(c, packageFiles, referencedPackages));

    private static IObservable<IImmutableList<PackageReference>> GetReferencedPackages(IConf c)
      => c.TryGet(ReferencedPackages)
          .GetOrElse(Observable.Return(ImmutableList<PackageReference>.Empty));

    private static string Pack(IConf c,
                               IEnumerable<PackageFile> packageFiles,
                               IEnumerable<PackageReference> referencedPackages) {
      return Packager[c].Pack(
        PackageOutputDir[c],
        PackageBaseDir[c],
        ProjectId[c],
        Api.Version[c],
        packageFiles,
        PackageDependencies(c).Concat(referencedPackages.Select(r => new PackageDependency(r.Id, r.Version.ToString()))),
        PackageMetadata[c]);
    }

    private static IObservable<IEnumerable<PackageFile>> DefaultPackageFiles(IConf c)
      => c.TryGet(Output)
          .GetOrElse(Observable.Return(Empty<string>()))
          .Select(files => files.Select(ToContentFiles));

    private static IObservable<bool> DefaultPublish(IConf c)
      => Package[c]
        .Select(package => Publisher[c].Publish(package,
                                                PublishUrl[c],
                                                PublishApiKey[c]));

    private static PackageFile ToContentFiles(string file)
      => new PackageFile(file, $"content/{GetFileName(file)}");

    private static IEnumerable<PackageDependency> PackageDependencies(IConf c)
      => Dependencies[c].Select(dependency => ToPackageDependency(c, dependency));

    private static PackageDependency ToPackageDependency(IConf c, string dependency)
      => new PackageDependency(c.Get(dependency/ProjectId),
                               c.Get(dependency/Api.Version));
  }
}