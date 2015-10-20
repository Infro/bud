﻿using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Bud;
using Bud.Compilation;

public class BudBuild : IBuild {
  public Conf Init(string dir)
    => BudProject.Add(BudTestProject)
                 .Add(CompilationDependency("budTest", "bud"));

  private static readonly Conf BudProject = CSharp.CSharpProject("Bud", "bud").Add(BudDependencies()).In("bud");

  private static readonly Conf BudTestProject = CSharp.CSharpProject("Bud.Test", "budTest").Add(BudTestDependencies()).In("budTest");

  public static Conf CompilationDependency(string dependentProject, string dependencyProject)
    => Conf.Empty.Modify(dependentProject / CSharp.AssemblyReferences, (configs, assemblyReferences) => {
      var dependencyCompilation = (dependencyProject / CSharp.Compilation)[configs];
      var dependency = dependencyCompilation.ToEnumerable().First().ToAssemblyReference();
      return assemblyReferences.Add(dependency);
    });

  private static Conf BudDependencies()
    => Conf.Empty.Set(CSharp.AssemblyReferences, c => new Assemblies(Path.Combine(Build.ProjectDir[c], "../packages/Microsoft.CodeAnalysis.Common.1.1.0-beta1-20150812-01/lib/net45/Microsoft.CodeAnalysis.dll"), Path.Combine(Build.ProjectDir[c], "../packages/Microsoft.CodeAnalysis.CSharp.1.1.0-beta1-20150812-01/lib/net45/Microsoft.CodeAnalysis.CSharp.dll"), Path.Combine(Build.ProjectDir[c], "../packages/Microsoft.Web.Xdt.2.1.0/lib/net40/Microsoft.Web.XmlTransform.dll"), Path.Combine(Build.ProjectDir[c], "../packages/NuGet.Core.2.8.6/lib/net40-Client/NuGet.Core.dll"), Path.Combine(Build.ProjectDir[c], "../packages/System.Collections.Immutable.1.1.38-beta-23225/lib/dotnet/System.Collections.Immutable.dll"), Path.Combine(Build.ProjectDir[c], "../packages/Rx-Core.2.2.5/lib/net45/System.Reactive.Core.dll"), Path.Combine(Build.ProjectDir[c], "../packages/Rx-Interfaces.2.2.5/lib/net45/System.Reactive.Interfaces.dll"), Path.Combine(Build.ProjectDir[c], "../packages/Rx-Linq.2.2.5/lib/net45/System.Reactive.Linq.dll"), Path.Combine(Build.ProjectDir[c], "../packages/Rx-PlatformServices.2.2.5/lib/net45/System.Reactive.PlatformServices.dll"), Path.Combine(Build.ProjectDir[c], "../packages/System.Reflection.Metadata.1.1.0-alpha-00009/lib/dotnet/System.Reflection.Metadata.dll"), "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Collections.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Diagnostics.Debug.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Globalization.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.IO.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Linq.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Reflection.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Reflection.Extensions.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Reflection.Primitives.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Resources.ResourceManager.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Runtime.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Runtime.Extensions.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Runtime.InteropServices.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Text.Encoding.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Text.Encoding.Extensions.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Threading.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/Facades/System.Threading.Tasks.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/mscorlib.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/System.dll", "C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.6/System.Core.dll"));

  private static Conf BudTestDependencies()
    => BudDependencies().Modify(CSharp.AssemblyReferences, (c, references) => references.ExpandWith(new Assemblies(Path.Combine(Build.ProjectDir[c], "../packages/NUnit.2.6.4/lib/nunit.framework.dll"), Path.Combine(Build.ProjectDir[c], "../packages/Moq.4.2.1507.0118/lib/net40/Moq.dll"))));
}