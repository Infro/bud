﻿using System.IO;
using Bud.Build;
using Bud.Test.Assertions;
using Bud.Test.Util;
using NUnit.Framework;

namespace Bud.SystemTests {
  public class BuildConfigurationProject {
    [Test]
    public void Load_MUST_produce_the_build_assembly() {
      using (var buildCommander = TestProjects.LoadBuildCommander("BuildConfigurationProject")) {
        FileAssertions.FileExists(Path.Combine(buildCommander.TemporaryDirectory.Path, BuildDirs.BudDirName, "Build.dll"));
      }
    }
  }
}