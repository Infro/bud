using System;
using NuGet.Frameworks;
using NUnit.Framework;
using static System.IO.Path;
using static Bud.NuGet.WindowsFrameworkAssemblyResolver;
using static Bud.Util.Option;
using static NUnit.Framework.Assert;

namespace Bud.NuGet {
  [Category("WindowsSpecific")]
  public class WindowsFrameworkAssemblyResolverTest {
    private static readonly Version Net2 = NuGetFramework.Parse("net2").Version;
    private static readonly Version Net35 = NuGetFramework.Parse("net35").Version;
    private static readonly Version Net45 = NuGetFramework.Parse("net45").Version;
    private static readonly Version Net452 = NuGetFramework.Parse("net452").Version;
    private static readonly Version Net46 = NuGetFramework.Parse("net46").Version;
    private static readonly Version Any = new Version();

    [Test]
    public void Default_OldFrameworkPath()
      => That(OldFrameworkPath,
              Does.EndWith(@"Windows\Microsoft.NET\Framework").IgnoreCase);

    [Test]
    public void Default_Net3PlusFrameworkPath()
      => That(Net3PlusFrameworkPath,
              Does.EndWith(@"Program Files (x86)\Reference Assemblies\Microsoft\Framework")
                  .IgnoreCase);

    [Test]
    public void Default_Net4PlusFrameworkPath()
      => That(Net4PlusFrameworkPath,
              Does.EndWith(@"Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework")
                  .IgnoreCase);

    [Test]
    public void Returns_None_for_non_existent_assemblies()
      => AreEqual(None<string>(),
                  ResolveFrameworkAssembly("Foo", Net46));

    [Test]
    public void Returns_the_facade_assembly_path_for_net46()
      => AreEqual(Some(Combine(Net4PlusFrameworkPath, "v4.6", "Facades", "System.Runtime.dll")),
                  ResolveFrameworkAssembly("System.Runtime", Net46));

    [Test]
    public void Returns_the_facade_assembly_path_for_net45()
      => AreEqual(Some(Combine(Net4PlusFrameworkPath, "v4.5", "Facades", "System.Runtime.dll")),
                  ResolveFrameworkAssembly("System.Runtime", Net45));

    [Test]
    public void Returns_the_facade_assembly_path_for_net452()
      => AreEqual(Some(Combine(Net4PlusFrameworkPath, "v4.5.2", "Facades", "System.Runtime.dll")),
                  ResolveFrameworkAssembly("System.Runtime", Net452));

    [Test]
    public void Returns_the_path_for_non_facade_assemblies()
      => AreEqual(Some(Combine(Net4PlusFrameworkPath, "v4.6", "System.Configuration.dll")),
                  ResolveFrameworkAssembly("System.Configuration", Net46));

    [Test]
    public void Returns_the_path_for_net35_assemblies()
      => AreEqual(Some(Combine(Net3PlusFrameworkPath, "v3.5", "System.Core.dll")),
                  ResolveFrameworkAssembly("System.Core", Net35));

    [Test]
    public void Returns_the_windows_dir_based_paths_for_net35_assemblies()
      => AreEqual(Some(Combine(OldFrameworkPath, "v3.5", "Microsoft.Data.Entity.Build.Tasks.dll")),
                  ResolveFrameworkAssembly("Microsoft.Data.Entity.Build.Tasks", Net35));

    [Test]
    public void Falls_back_to_net30_for_assemblies_that_do_not_exist_in_net35()
      => AreEqual(Some(Combine(Net3PlusFrameworkPath, "v3.0", "System.ServiceModel.dll")),
                  ResolveFrameworkAssembly("System.ServiceModel", Net35));

    [Test]
    public void Falls_back_to_net35_for_assemblies_that_do_not_exist_in_net4plus()
      => AreEqual(Some(Combine(OldFrameworkPath, "v3.5", "Microsoft.Data.Entity.Build.Tasks.dll")),
                  ResolveFrameworkAssembly("Microsoft.Data.Entity.Build.Tasks", Net35));

    [Test]
    public void Returns_the_path_for_net20_assemblies()
      => AreEqual(Some(Combine(OldFrameworkPath, "v2.0.50727", "System.dll")),
                  ResolveFrameworkAssembly("System", Net2));

    [Test]
    public void Finds_a_reference_when_given_any_version()
      => IsTrue(ResolveFrameworkAssembly("System", Any).HasValue);

    [Test]
    public void Finds_a_reference_when_assembly_is_in_an_assembly_ex_dir()
      => IsTrue(ResolveFrameworkAssembly("Microsoft.VisualStudio.QualityTools.UnitTestFramework", Net45)
                  .HasValue);
  }
}