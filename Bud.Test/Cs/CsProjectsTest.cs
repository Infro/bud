using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Bud.IO;
using Bud.NuGet;
using Bud.V1;
using Microsoft.CodeAnalysis;
using Microsoft.Reactive.Testing;
using Moq;
using NuGet.Frameworks;
using NuGet.Versioning;
using NUnit.Framework;
using static System.IO.Path;
using static Bud.V1.Api;
using static NUnit.Framework.Assert;
using Contains = NUnit.Framework.Contains;

namespace Bud.Cs {
  public class CsProjectsTest {
    [Test]
    public void Assembly_name_must_use_the_project_id() {
      using (var tempDir = new TemporaryDirectory()) {
        AreEqual("Foo.dll",
                 CsLib(tempDir.Path, "Foo").Get(AssemblyName));
      }
    }

    [Test]
    public void CSharp_sources_must_be_listed() {
      using (var tempDir = new TemporaryDirectory()) {
        var cSharpProject = CsLib(tempDir.Path, "Foo");
        var sourceFile = tempDir.CreateEmptyFile("TestMainClass.cs");
        That(Sources[cSharpProject].Take(1).Wait(),
             Contains.Item(sourceFile));
      }
    }

    [Test]
    public void CSharp_sources_in_nested_directories_must_be_listed() {
      using (var tempDir = new TemporaryDirectory()) {
        var cSharpProject = CsLib(tempDir.Path, "Foo");
        var sourceFile = tempDir.CreateEmptyFile("Bud", "TestMainClass.cs");
        That(Sources[cSharpProject].Take(1).Wait(),
             Contains.Item(sourceFile));
      }
    }

    [Test]
    public void Non_csharp_files_must_not_be_listed() {
      using (var tempDir = new TemporaryDirectory()) {
        var cSharpProject = CsLib(tempDir.Path, "Foo");
        var textFile = tempDir.CreateEmptyFile("Bud", "TextFile.txt");
        That(Sources[cSharpProject].Take(1).Wait(),
             Is.Not.Contains(textFile));
      }
    }

    [Test]
    public void Compiler_is_invoked_with_input_sources() {
      var cSharpCompiler = new Mock<Func<CompileInput, CompileOutput>>(MockBehavior.Strict);
      cSharpCompiler.Setup(self => self(CompileInputTestUtils.ToCompileInput("A.cs", null, null)))
                    .Returns(EmptyCompileOutput());
      var projectA = CsLib("foo", "A")
        .SetValue(Compiler, cSharpCompiler.Object)
        .Clear(Input)
        .Add(Input, "A.cs");
      Compile[projectA].Take(1).Wait();
      cSharpCompiler.VerifyAll();
    }

    [Test]
    public void Compiler_is_invoked_with_assembly_references() {
      var cSharpCompiler = new Mock<Func<CompileInput, CompileOutput>>(MockBehavior.Strict);
      cSharpCompiler.Setup(self => self(CompileInputTestUtils.ToCompileInput(null, null, "A.dll")))
                    .Returns(EmptyCompileOutput());
      var projectA = CsLib("foo", "A")
        .SetValue(Compiler, cSharpCompiler.Object)
        .Clear(Input)
        .Add(AssemblyReferences, "A.dll");
      Compile[projectA].Take(1).Wait();
      cSharpCompiler.VerifyAll();
    }

    [Test]
    public void Compiler_invoked_with_dependencies() {
      var cSharpCompiler = new Mock<Func<CompileInput, CompileOutput>>(MockBehavior.Strict);
      cSharpCompiler.Setup(self => self(CompileInputTestUtils.ToCompileInput(null, EmptyCompileOutput(42), null)))
                    .Returns(EmptyCompileOutput());
      var projectA = Projects(ProjectAOutputsFooDll(42),
                              ProjectWithDependencies("B", "../A")
                                .SetValue(Compiler, cSharpCompiler.Object));
      projectA.Get("B"/Compile).Take(1).Wait();
      cSharpCompiler.VerifyAll();
    }

    [Test]
    public void Compiler_reinvoked_when_input_changes() {
      var testScheduler = new TestScheduler();
      var compiler = NoOpCompiler();
      var compilation = ProjectAWithUpdatingSources(testScheduler, compiler.Object)
        .Get(Output).GetEnumerator();
      testScheduler.AdvanceBy(TimeSpan.FromSeconds(5).Ticks);
      while (compilation.MoveNext()) {}
      compiler.Verify(self => self(It.IsAny<CompileInput>()), Times.Exactly(3));
    }

    [Test]
    public void Referenced_packages_must_be_added_to_the_list_of_assembly_references() {
      var project = CsLib("Foo")
        .Clear("Packages"/ResolvedAssemblies)
        .Add("Packages"/ResolvedAssemblies, "Bar.dll");
      That(AssemblyReferences[project].ToEnumerable(),
           Has.Exactly(1).EqualTo(new[] {"Bar.dll"}));
    }

    [Test]
    public void Referenced_packages_project_must_reside_in_the_packages_folder()
      => AreEqual(Combine(Directory.GetCurrentDirectory(), "Foo", "packages"),
                  CsLib("Foo").Get("Packages"/ProjectDir));

    [Test]
    public void Packages_config_file_must_be_read_from_the_root()
      => AreEqual(Combine(Directory.GetCurrentDirectory(), "Foo", "packages.config"),
                  CsLib("Foo").Get("Packages"/PackagesConfigFile));

    [Test]
    public void CSharp_files_in_the_packages_folder_must_not_be_listed() {
      using (var tempDir = new TemporaryDirectory()) {
        var csFile = tempDir.CreateEmptyFile("packages", "A.cs");
        That(Sources[CsLib(tempDir.Path, "Foo")].Take(1).Wait(),
             Is.Not.Contains(csFile));
      }
    }

    [Test]
    public void CSharp_files_in_the_obj_folder_must_not_be_listed() {
      using (var tempDir = new TemporaryDirectory()) {
        var csFile = tempDir.CreateEmptyFile("obj", "A.cs");
        That(Sources[CsLib(tempDir.Path, "Foo")].Take(1).Wait(),
             Is.Not.Contains(csFile));
      }
    }

    [Test]
    public void CSharp_files_in_the_bin_folder_must_not_be_listed() {
      using (var tempDir = new TemporaryDirectory()) {
        var csFile = tempDir.CreateEmptyFile("bin", "A.cs");
        That(Sources[CsLib(tempDir.Path, "Foo")].Take(1).Wait(),
             Is.Not.Contains(csFile));
      }
    }

    [Test]
    public void CSharp_files_in_the_target_folder_must_not_be_listed() {
      using (var tempDir = new TemporaryDirectory()) {
        var csFile = tempDir.CreateEmptyFile("build", "A.cs");
        That(Sources[CsLib(tempDir.Path, "Foo")].Take(1).Wait(),
             Is.Not.Contains(csFile));
      }
    }

    [Test]
    public void Package_must_contain_the_dll_of_the_CsLibrary_project() {
      var packager = new Mock<IPackager>(MockBehavior.Strict);
      var projects = Projects(CsLib("aDir", "A")
                                .SetValue(ProjectVersion, "4.2.0"),
                              CsLib("bDir", "B")
                                .Clear(Output).Add(Output, "B.dll")
                                .SetValue(Packager, packager.Object)
                                .Add(Dependencies, "../A"));
      packager.Setup(s => s.Pack(projects.Get("B"/PackageOutputDir),
                                 Directory.GetCurrentDirectory(),
                                 "B",
                                 DefaultVersion,
                                 new[] {new PackageFile("B.dll", "lib/B.dll")},
                                 new[] {new PackageDependency("A", "4.2.0")},
                                 It.IsAny<NuGetPackageMetadata>()))
              .Returns("B.nupkg");
      projects.Get("B"/Package).Take(1).Wait();
      packager.VerifyAll();
    }

    [Test]
    public void Package_must_contain_references_to_own_packages() {
      var packager = new Mock<IPackager>(MockBehavior.Strict);
      var projects = Projects(CsLib("bDir", "B")
                                .Clear(Output).Add(Output, "B.dll")
                                .SetValue(Packager, packager.Object)
                                .Clear("Packages"/ReferencedPackages)
                                .Add("Packages"/ReferencedPackages, new PackageReference("Foo", NuGetVersion.Parse("2.4.1"), NuGetFramework.Parse("net35"))));
      packager.Setup(s => s.Pack(projects.Get("B"/PackageOutputDir),
                                 Directory.GetCurrentDirectory(),
                                 "B",
                                 DefaultVersion,
                                 new[] {new PackageFile("B.dll", "lib/B.dll")},
                                 new[] {new PackageDependency("Foo", "2.4.1")},
                                 It.IsAny<NuGetPackageMetadata>()))
              .Returns("B.nupkg");
      projects.Get("B"/Package).Take(1).Wait();
      packager.VerifyAll();
    }

    [Test]
    public void CsApp_produces_an_executable()
      => AreEqual("Foo.exe", CsApp("Foo").Get(AssemblyName));

    [Test]
    [Category("IntegrationTest")]
    public void DistributionZip_contains_assembly_references() {
      using (var tmpDir = new TemporaryDirectory()) {
        var distZip = CsApp(tmpDir.Path, "A")
          .Clear(Output)
          .Add(AssemblyReferences, tmpDir.CreateEmptyFile("AssRef.dll"))
          .Get(DistributionArchive).Take(1).Wait();
        ZipTestUtils.IsInZip(distZip, "AssRef.dll");
      }
    }

    [Test]
    [Category("IntegrationTest")]
    public void DistributionZip_does_not_contain_framework_assembly_references() {
      using (var tmpDir = new TemporaryDirectory()) {
        var project = CsApp(tmpDir.Path, "A")
          .Clear(Output)
          .Add(AssemblyReferences, tmpDir.CreateEmptyFile("System.Runtime.dll"));
        var distZip = project.Get(DistributionArchive).Take(1).Wait();
        ZipTestUtils.IsNotInZip(distZip, "System.Runtime.dll");
      }
    }

    [Test]
    [Category("IntegrationTest")]
    public void Projects_must_reference_the_mscorlib_assembly() {
      using (var tmpDir = new TemporaryDirectory()) {
        tmpDir.CreateFile(
          "public class App {public static void Main(string[] args) => System.Console.WriteLine(\"Hello World!\");}",
          "App.cs");
        var project = CsProjects.CsLib(tmpDir.Path, "Foo");
        var compileOutput = project.Get(Compile).Take(1).Wait();
        IsTrue(compileOutput.Success);
      }
    }

    private static Conf ProjectAOutputsFooDll(long initialTimestamp)
      => EmptyCSharpProject("A")
        .SetValue(Compiler, input => EmptyCompileOutput(initialTimestamp++));

    private static Conf ProjectWithDependencies(string projectId,
                                                params string[] dependencies)
      => EmptyCSharpProject(projectId)
        .Add(Dependencies, dependencies);

    private static Conf EmptyCSharpProject(string projectId)
      => CsLib(projectId, projectId)
        .Clear(SourceIncludes)
        .Clear(AssemblyReferences);

    private static CompileOutput EmptyCompileOutput(long timestamp = 0L)
      => new CompileOutput(Enumerable.Empty<Diagnostic>(),
                           TimeSpan.FromMilliseconds(123),
                           "Foo.dll",
                           true,
                           timestamp,
                           null);

    private static Mock<Func<CompileInput, CompileOutput>> NoOpCompiler() {
      var compiler = new Mock<Func<CompileInput, CompileOutput>>();
      compiler.Setup(self => self(It.IsAny<CompileInput>()))
              .Returns(EmptyCompileOutput());
      return compiler;
    }

    private static FileWatcher FileADelayedUpdates(IScheduler testScheduler) {
      var changes = Observable.Return("A.cs").Delay(TimeSpan.FromSeconds(1),
                                                    testScheduler)
                              .Concat(Observable.Return("A.cs")
                                                .Delay(TimeSpan.FromSeconds(1),
                                                       testScheduler));
      IEnumerable<string> files = new[] {"A.cs"};
      return new FileWatcher(files, changes);
    }

    private static Conf
      ProjectAWithUpdatingSources(IScheduler testScheduler,
                                  Func<CompileInput, CompileOutput> compiler)
      => CsLib("a", "A")
        .SetValue(BuildPipelineScheduler, testScheduler)
        .Clear(SourceIncludes)
        .Add(SourceIncludes, FileADelayedUpdates(testScheduler))
        .Clear(AssemblyReferences)
        .SetValue(Compiler, compiler);
  }
}