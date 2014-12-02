﻿using System;
using System.IO;
using Bud.Util;

namespace Bud {
  public static class BuildPlugin {
    public static readonly TaskKey<Unit> Build = new TaskKey<Unit>("Build");
    public static readonly TaskKey<Unit> Clean = new TaskKey<Unit>("Clean");

    public static Settings AddBuildSupport(this Settings existingSettings) {
      return existingSettings
        .InitOrKeep(Clean, TaskUtils.NoOpTask)
        .InitOrKeep(Build, TaskUtils.NoOpTask);
    }
  }
}
