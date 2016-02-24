﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bud.Cs;
using Bud.Reactive;
using Bud.Util;
using Bud.V1;
using static System.IO.Directory;
using static System.IO.Path;
using static Bud.Cli.BuildScriptCompilation;
using static Bud.Util.Option;

namespace Bud.Cli {
  public class BuildTool {
    public static void Main(string[] args)
      => ExecuteBuild(GetCurrentDirectory(), args);

    private static void ExecuteBuild(string baseDir, IEnumerable<string> args) {
      var buildScriptPath = Combine(baseDir, "Build.cs");
      var compileOutput = CompileBuildScript(baseDir, buildScriptPath);
      if (compileOutput.Success) {
        var buildDefinition = LoadBuildDefinition(compileOutput.AssemblyPath);
        foreach (var command in args) {
          ExecuteCommand(buildDefinition, command);
        }
      } else {
        PrintCompilationErrors(compileOutput);
      }
    }

    internal static Option<object> ExecuteCommand(IConf buildDefinition, string command) {
      var optionalValue = buildDefinition.TryGet<object>(command);
      if (!optionalValue.HasValue) {
        return None<object>();
      }
      var optionalResults = ObservableResults.TryCollect(optionalValue.Value);
      if (optionalResults.HasValue) {
        return optionalResults.Value as object;
      }
      var task = optionalValue.Value as Task;
      if (task != null) {
        return TaskResults.Await(task);
      }
      return optionalValue.Value;
    }

    private static void PrintCompilationErrors(CompileOutput compilationOutput) {
      Console.WriteLine("Could not compile the build configuration.");
      foreach (var diagnostic in compilationOutput.Diagnostics) {
        Console.WriteLine(diagnostic);
      }
    }

    private static IConf LoadBuildDefinition(string assemblyPath) {
      var assembly = Assembly.LoadFile(assemblyPath);
      var buildDefinitionType = assembly
        .GetExportedTypes()
        .First(typeof(IBuild).IsAssignableFrom);
      var buildDefinition = buildDefinitionType
        .GetConstructor(Type.EmptyTypes)
        .Invoke(new object[] {});
      var buildConf = ((IBuild) buildDefinition)
        .Init();
      return buildConf.ToCompiled();
    }
  }
}