﻿using System;
using System.Collections.Immutable;

namespace Bud.Plugins.Build {
  public static class BuildDirsKeys {
    public static readonly ConfigKey<string> BaseDir = new ConfigKey<string>("baseDir");
    public static readonly ConfigKey<string> BudDir = new ConfigKey<string>("budDir");
    public static readonly ConfigKey<string> OutputDir = new ConfigKey<string>("outputDir");
    public static readonly ConfigKey<string> BuildConfigCacheDir = new ConfigKey<string>("buildConfigCacheDir");
    public static readonly ConfigKey<string> PersistentBuildConfigDir = new ConfigKey<string>("persistentBuildConfigDir");
    public static readonly TaskKey<Unit> Clean = new TaskKey<Unit>("clean");
  }
}
