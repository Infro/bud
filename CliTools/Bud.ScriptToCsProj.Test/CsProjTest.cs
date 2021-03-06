﻿using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Bud.ScriptToCsProj {
  public class CsProjTest {
    [Test]
    public void Generate_produces_an_empty_csproj_file()
      => Assert.AreEqual(GetResourceContent("Bud.ScriptToCsProj.SampleCsProjs.Empty.csproj"),
                  CsProj.Generate());

    [Test]
    public void Generate_imports()
      => Assert.AreEqual(GetResourceContent("Bud.ScriptToCsProj.SampleCsProjs.MsBuildImports.csproj"),
                  CsProj.Generate(CsProj.Import("blah", "bleh")));

    [Test]
    public void Generate_property_group()
      => Assert.AreEqual(GetResourceContent("Bud.ScriptToCsProj.SampleCsProjs.PropertyGroup.csproj"),
                  CsProj.Generate(
                    CsProj.PropertyGroup("ya",
                                         CsProj.Property("ConfigurationName", "42"),
                                         CsProj.Property("DebugType", "9001", "nah"))));

    private static string GetResourceContent(string embeddedResourceName) {
      var manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedResourceName);
      if (manifestResourceStream == null) {
        throw new Exception($"Could not find the embedded resource '{embeddedResourceName}'.");
      }
      using (var expectedContent = manifestResourceStream) {
        using (var streamReader = new StreamReader(expectedContent)) {
          return streamReader.ReadToEnd().Replace("\r\n", "\n");
        }
      }
    }
  }
}