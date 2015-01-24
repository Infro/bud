﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Bud.Util;

namespace Bud.Plugins.Build {
  public abstract class BuildTargetPlugin : IPlugin {
    protected readonly Key Scope;
    protected readonly Key Language;
    private readonly IEnumerable<IPlugin> plugins;

    protected BuildTargetPlugin(Key scope, Key language, IEnumerable<IPlugin> plugins) {
      Scope = scope;
      Language = language;
      this.plugins = plugins ?? ImmutableList<IPlugin>.Empty;
    }

    public Settings ApplyTo(Settings settings, Key project) {
      var buildTargetKey = BuildTargetKey(project);
      return settings
        .In(project,
            BuildKeys.Test.Init(TaskUtils.NoOpTask)
        ).In(buildTargetKey,
             BuildDirsKeys.BaseDir.Init(context => Path.Combine(context.GetBaseDir(project), "src", Scope.Id, Language.Id)),
             BuildDirsKeys.OutputDir.Init(context => Path.Combine(context.GetOutputDir(project), Scope.Id, Language.Id)),
             BuildKeys.Build.Init(InvokeCompilerTaskImpl)
        ).In(Key.Global,
             BuildKeys.Test.Init(TaskUtils.NoOpTask),
             BuildKeys.Test.DependsOn(BuildKeys.Test.In(project)),
             BuildKeys.Build.Init(TaskUtils.NoOpTask),
             BuildKeys.Build.DependsOn(BuildKeys.Build.In(Scope)),
             BuildKeys.Build.In(Scope).Init(TaskUtils.NoOpTask),
             BuildKeys.Build.In(Scope).DependsOn(BuildKeys.Build.In(buildTargetKey))
        )
        .Apply(buildTargetKey, PluginUtils.Create((existingsettings, buildTarget) => ApplyTo(existingsettings, buildTarget, project)))
        .Apply(buildTargetKey, plugins);
    }

    protected abstract Settings ApplyTo(Settings existingsettings, Key project, Key buildTarget);

    protected abstract Task<Unit> InvokeCompilerTaskImpl(IContext arg, Key buildKey);

    protected Key BuildTargetKey(Key project) {
      return BuildUtils.BuildTargetKey(project, Scope, Language);
    }
  }
}