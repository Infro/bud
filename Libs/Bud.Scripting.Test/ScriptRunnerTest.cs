﻿using System;
using System.IO;
using NUnit.Framework;

namespace Bud.Scripting {
  [Category("IntegrationTest")]
  public class ScriptRunnerTest {
    [Test]
    public void RunScript_runs_the_script_in_the_current_directory() {
      using (var dir = new TmpDir()) {
        var outputDir = dir.CreateDir("output-dir");
        var fooExpected = dir.CreateFile("42 1337", "output-dir", "foo.expected");
        var script = dir.CreateFileFromResource("Bud.Scripting.TestScripts.CreateFooFile.cs",
                                                "CreateFooFile.cs");
        ScriptRunner.Run(new[] {"1337"}, script, outputDir);
        FileAssert.AreEqual(fooExpected, Path.Combine(outputDir, "foo"));
      }
    }

    [Test]
    public void RunScript_shows_an_informative_exception_message_on_compiler_error() {
      using (var dir = new TmpDir()) {
        var script = dir.CreateFileFromResource("Bud.Scripting.TestScripts.UsingLinqWithoutReference.cs",
                                                "UsingLinqWithoutReference.cs");
        var exception = Assert.Throws<Exception>(() => ScriptRunner.Run(new[] {""}, script, dir.Path));
        Assert.That(exception.Message, Contains.Substring("Linq"));
      }
    }

    [Test]
    public void RunScript_runs_the_script_when_reference_present() {
      using (var dir = new TmpDir()) {
        var fooExpected = dir.CreateFile("FOO BAR", "foo.expected");
        var script = dir.CreateFileFromResource("Bud.Scripting.TestScripts.UsingLinqWithReference.cs",
                                                "UsingLinqWithReference.cs");
        ScriptRunner.Run(new[] {"foo", "bar"}, script, dir.Path);
        FileAssert.AreEqual(fooExpected, Path.Combine(dir.Path, "foo"));
      }
    }
  }
}