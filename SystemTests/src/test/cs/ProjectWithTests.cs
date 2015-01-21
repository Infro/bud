using Bud.Plugins.Build;
using Bud.Test.Assertions;
using Bud.Test.Util;
using NUnit.Framework;

namespace Bud.SystemTests {
  public class ProjectWithTests {
    [Test]
    public void compile_MUST_produce_the_main_and_test_libraries() {
      using (var buildCommander = TestProjects.LoadBuildCommander("ProjectWithTests")) {
        buildCommander.Evaluate("test/build");
        FileAssertions.AssertFilesExist(new[] {
          SystemTestUtils.OutputAssemblyPath(buildCommander, "A", BuildKeys.Main, "A.dll"),
          SystemTestUtils.OutputAssemblyPath(buildCommander, "A", BuildKeys.Test, "A.Test.dll")
        });
      }
    }
  }
}