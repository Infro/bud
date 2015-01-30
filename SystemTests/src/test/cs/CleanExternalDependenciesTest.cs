using Bud.Commander;
using Bud.Dependencies;
using Bud.Test.Assertions;
using Bud.Test.Util;
using NUnit.Framework;

namespace Bud.SystemTests {
  public class CleanExternalDependenciesTest {
    [Test]
    public void compile_MUST_produce_the_executable() {
      using (var buildCommander = TestProjects.LoadBuildCommander(this)) {
        FileAssertions.DirectoryExists(buildCommander.Evaluate(DependenciesKeys.NuGetRepositoryDir) as string);
        buildCommander.Evaluate("fetch/clean");
        FileAssertions.DirectoryDoesNotExist(buildCommander.Evaluate(DependenciesKeys.NuGetRepositoryDir) as string);
        FileAssertions.FileExists(buildCommander.Evaluate(DependenciesKeys.PersistedPackagesListFile) as string);
      }
    }
  }
}