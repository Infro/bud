using System;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using NUnit.Framework;
using static Bud.References.WindowsFrameworkReferenceResolver;

namespace Bud.References {
  [Category("WindowsSpecific")]
  [Category("IntegrationTest")]
  [Category("AppVeyorIgnore")]
  public class WindowsResolverTest {
    private static readonly Version Net2 = NuGetFramework.Parse("net2").Version;
    private static readonly Version Net35 = NuGetFramework.Parse("net35").Version;
    private static readonly Version Net45 = NuGetFramework.Parse("net45").Version;
    private static readonly Version Net452 = NuGetFramework.Parse("net452").Version;
    private static readonly Version Net46 = NuGetFramework.Parse("net46").Version;
    private static readonly Version Any = new Version();

    [Test]
    public void Default_OldFrameworkPath()
      => Assert.That(OldFrameworkPath,
              Does.EndWith(@"Windows\Microsoft.NET\Framework").IgnoreCase);

    [Test]
    public void Default_Net3PlusFrameworkPath()
      => Assert.That(Net3PlusFrameworkPath,
              Does.EndWith(@"Program Files (x86)\Reference Assemblies\Microsoft\Framework")
                  .IgnoreCase);

    [Test]
    public void Default_Net4PlusFrameworkPath()
      => Assert.That(Net4PlusFrameworkPath,
              Does.EndWith(@"Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework")
                  .IgnoreCase);

    [Test]
    public void Returns_None_for_non_existent_assemblies()
      => Assert.AreEqual(Option.None<string>(),
                  ResolveFrameworkAssembly("Foo", Net46));

    [Test]
    public void Returns_the_facade_assembly_path_for_net46()
      => Assert.AreEqual(Option.Some(Path.Combine(Net4PlusFrameworkPath, "v4.6", "Facades", "System.Runtime.dll")),
                  ResolveFrameworkAssembly("System.Runtime", Net46));

    [Test]
    public void Returns_the_facade_assembly_path_for_net45()
      => Assert.AreEqual(Option.Some(Path.Combine(Net4PlusFrameworkPath, "v4.5", "Facades", "System.Runtime.dll")),
                  ResolveFrameworkAssembly("System.Runtime", Net45));

    [Test]
    public void Returns_the_facade_assembly_path_for_net452()
      => Assert.AreEqual(Option.Some(Path.Combine(Net4PlusFrameworkPath, "v4.5.2", "Facades", "System.Runtime.dll")),
                  ResolveFrameworkAssembly("System.Runtime", Net452));

    [Test]
    public void Returns_the_path_for_non_facade_assemblies()
      => Assert.AreEqual(Option.Some(Path.Combine(Net4PlusFrameworkPath, "v4.6", "System.Configuration.dll")),
                  ResolveFrameworkAssembly("System.Configuration", Net46));

    [Test]
    public void Returns_the_path_for_net35_assemblies()
      => Assert.AreEqual(Option.Some(Path.Combine(Net3PlusFrameworkPath, "v3.5", "System.Core.dll")),
                  ResolveFrameworkAssembly("System.Core", Net35));

    [Test]
    public void Returns_the_windows_dir_based_paths_for_net35_assemblies()
      => Assert.AreEqual(Option.Some(Path.Combine(OldFrameworkPath, "v3.5", "Microsoft.Data.Entity.Build.Tasks.dll")),
                  ResolveFrameworkAssembly("Microsoft.Data.Entity.Build.Tasks", Net35));

    [Test]
    public void Falls_back_to_net30_for_assemblies_that_do_not_exist_in_net35()
      => Assert.AreEqual(Option.Some(Path.Combine(Net3PlusFrameworkPath, "v3.0", "System.ServiceModel.dll")),
                  ResolveFrameworkAssembly("System.ServiceModel", Net35));

    [Test]
    public void Falls_back_to_net35_for_assemblies_that_do_not_exist_in_net4plus()
      => Assert.AreEqual(Option.Some(Path.Combine(OldFrameworkPath, "v3.5", "Microsoft.Data.Entity.Build.Tasks.dll")),
                  ResolveFrameworkAssembly("Microsoft.Data.Entity.Build.Tasks", Net35));

    [Test]
    public void Returns_the_path_for_net20_assemblies()
      => Assert.AreEqual(Option.Some(Path.Combine(OldFrameworkPath, "v2.0.50727", "System.dll")),
                  ResolveFrameworkAssembly("System", Net2));

    [Test]
    public void Finds_a_reference_when_given_any_version()
      => Assert.IsTrue(ResolveFrameworkAssembly("System", Any).HasValue);

    [Test]
    public void Finds_a_reference_when_assembly_is_in_an_assembly_ex_dir()
      => Assert.IsTrue(ResolveFrameworkAssembly("Microsoft.VisualStudio.QualityTools.UnitTestFramework", Net45)
                  .HasValue);

    [Test]
    public void IsFrameworkAssembly_returns_true()
      => Assert.IsTrue(IsFrameworkAssembly("System.dll"));

    [Test]
    public void IsFrameworkAssembly_returns_false()
      => Assert.IsFalse(IsFrameworkAssembly("ThisIsSparta.dll"));

    [Test]
    public void IsFrameworkAssembly_returns_true_for_linq()
      => Assert.IsTrue(IsFrameworkAssembly("System.Linq.dll"));

    [Test]
    public void FrameworkDirs_lists_all_supported_folders()
      => Assert.That(FrameworkDirs,
              Is.SupersetOf(new[] {
                new FrameworkDir(new Version(4, 6, 1), Path.Combine(Net4PlusFrameworkPath, "v4.6.1", "Facades")),
                new FrameworkDir(new Version(4, 6, 1), Path.Combine(Net4PlusFrameworkPath, "v4.6.1")),
                new FrameworkDir(new Version(4, 6), Path.Combine(Net4PlusFrameworkPath, "v4.6", "Facades")),
                new FrameworkDir(new Version(4, 6), Path.Combine(Net4PlusFrameworkPath, "v4.6")),
                new FrameworkDir(new Version(4, 5, 2), Path.Combine(Net4PlusFrameworkPath, "v4.5.2", "Facades")),
                new FrameworkDir(new Version(4, 5, 2), Path.Combine(Net4PlusFrameworkPath, "v4.5.2")),
                new FrameworkDir(new Version(4, 5, 1), Path.Combine(Net4PlusFrameworkPath, "v4.5.1", "Facades")),
                new FrameworkDir(new Version(4, 5, 1), Path.Combine(Net4PlusFrameworkPath, "v4.5.1")),
                new FrameworkDir(new Version(4, 5), Path.Combine(Net4PlusFrameworkPath, "v4.5", "Facades")),
                new FrameworkDir(new Version(4, 5), Path.Combine(Net4PlusFrameworkPath, "v4.5")),
                new FrameworkDir(new Version(4, 0), Path.Combine(Net4PlusFrameworkPath, "v4.0")),
                new FrameworkDir(new Version(3, 5), Path.Combine(Net4PlusFrameworkPath, "v3.5")),
                new FrameworkDir(new Version(3, 5), Path.Combine(Net3PlusFrameworkPath, "v3.5")),
                new FrameworkDir(new Version(3, 5), Path.Combine(OldFrameworkPath, "v3.5")),
                new FrameworkDir(new Version(3, 0), Path.Combine(Net3PlusFrameworkPath, "v3.0")),
                new FrameworkDir(new Version(3, 0), Path.Combine(OldFrameworkPath, "v3.0")),
                new FrameworkDir(new Version(2, 0), Path.Combine(OldFrameworkPath, "v2.0.50727")),
              }));

    [Test]
    public void FrameworkDirs_is_ordered()
      => Assert.That(FrameworkDirs.Take(FrameworkDirs.Count - 1)
                           .Zip(FrameworkDirs.Skip(1),
                                (f1, f2) => f1.Version >= f2.Version),
              Has.All.True);
  }
}