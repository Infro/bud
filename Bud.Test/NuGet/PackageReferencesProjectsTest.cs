using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using Bud.IO;
using Bud.V1;
using Moq;
using NuGet.Frameworks;
using NuGet.Versioning;
using NUnit.Framework;
using static System.IO.File;
using static System.IO.Path;
using static Bud.NuGet.NuGetPackageReferencesReader;
using static Bud.NuGet.PackageConfigTestUtils;
using static Bud.V1.Api;
using static NUnit.Framework.Assert;

namespace Bud.NuGet {
  public class PackageReferencesProjectsTest {
    [Test]
    public void Packages_config_file_is_at_the_root_by_default()
      => That(PackagesConfigFile[TestProject()],
              Is.EqualTo(Combine("a", "packages.config")));

    [Test]
    public void Assemblies_is_initially_empty()
      => That(ResolvedAssemblies[TestProject()].Take(1).ToEnumerable(),
              Has.Exactly(1).Empty);

    [Test]
    [Category("IntegrationTest")]
    public void Assemblies_are_resolved_from_the_packages_config_file() {
      using (var tmpDir = new TemporaryDirectory()) {
        var packageConfigFile = CreatePackagesConfigFile(tmpDir);
        var expectedAssemblies = ImmutableList.Create("Foo.dll");
        var resolver = MockPackageResolver(packageConfigFile, expectedAssemblies);
        var project = TestProject(tmpDir.Path)
          .SetValue(AssemblyResolver, resolver.Object);

        var actualAssemblies = ResolvedAssemblies[project].Take(1).ToEnumerable();

        That(actualAssemblies, Has.Exactly(1).EqualTo(expectedAssemblies));
        resolver.VerifyAll();
      }
    }

    [Test]
    [Category("IntegrationTest")]
    public void Assemblies_are_stored_in_the_target_folder() {
      using (var tmpDir = new TemporaryDirectory()) {
        var packageConfigFile = CreatePackagesConfigFile(tmpDir);
        var resolvedAssemblies = ImmutableList.Create("Foo.dll", "Bar.dll");
        var resolver = MockPackageResolver(packageConfigFile, resolvedAssemblies);
        var project = TestProject(tmpDir.Path)
          .SetValue(AssemblyResolver, resolver.Object)
          .ToCompiled();

        ("A"/ResolvedAssemblies)[project].Take(1).Wait();

        That(ReadResolvedAssembliesCache(project),
             Is.EquivalentTo(resolvedAssemblies));
      }
    }

    [Test]
    public void Assemblies_are_loaded_from_cache() {
      using (var tmpDir = new TemporaryDirectory()) {
        CreatePackagesConfigFile(tmpDir);
        var resolver = new Mock<IAssemblyResolver>(MockBehavior.Strict);
        var project = TestProject(tmpDir.Path)
          .SetValue(AssemblyResolver, resolver.Object)
          .ToCompiled();
        tmpDir.CreateFile(
          "4D-31-2B-41-83-A6-87-D8-FC-8C-92-C7-F3-CE-60-E9\nMoo.dll\nZoo.dll",
          ("A"/BuildDir)[project], "resolved_assemblies");

        ("A"/ResolvedAssemblies)[project].Take(1).Wait();

        That(ReadResolvedAssembliesCache(project),
             Is.EquivalentTo(new[] {"Moo.dll", "Zoo.dll"}));
      }
    }

    [Test]
    public void ReferencedPackages_lists_package_references_read_from() {
      using (var tmpDir = new TemporaryDirectory()) {
        CreatePackagesConfigFile(tmpDir);
        var project = TestProject(tmpDir.Path);
        That(project.Get(ReferencedPackages).Take(1).Wait(),
             Is.EqualTo(new[] {
               new PackageReference("Urbas.Example.Foo",
                                    NuGetVersion.Parse("1.0.1"),
                                    NuGetFramework.Parse("net46"))
             }));
      }
    }

    private static Conf TestProject(string baseDir = "a")
      => PackageReferencesProject(baseDir, "A");

    private static Mock<IAssemblyResolver> MockPackageResolver(string packageConfigFile,
                                                              IEnumerable<string> assemblies) {
      var resolver = new Mock<IAssemblyResolver>(MockBehavior.Strict);
      var packageReferences = LoadReferences(packageConfigFile);
      resolver.Setup(self => self.FindAssembly(packageReferences,
                                          It.IsAny<string>(),
                                          It.IsAny<string>()))
              .Returns(assemblies.ToImmutableHashSet());
      return resolver;
    }

    private static IEnumerable<string> ReadResolvedAssembliesCache(IConf project)
      => ReadAllLines(Combine(("A"/BuildDir)[project], "resolved_assemblies")).Skip(1);
  }
}