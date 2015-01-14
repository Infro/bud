using Bud.Test.Assertions;
using Bud.Test.Util;
using NUnit.Framework;

namespace Bud.SystemTests {
  public class ExternalDependenciesNoVersion {
    [Test]
    public void compile_MUST_produce_the_executable() {
      using (var buildCommander = TestProjects.LoadBuildCommander(this)) {
//        buildCommander.Evaluate("fetch");
//        Process.Start(buildCommander.TemporaryDirectory.Path);
        buildCommander.Evaluate("build");
        FileAssertions.AssertFileExists(SystemTestUtils.OutputAssemblyPath(buildCommander, "Foo.exe"));
      }
    }
  }
}