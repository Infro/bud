using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive;
using System.Reactive.Concurrency;
using Bud.Configuration;
using Bud.Cs;
using Bud.IO;
using Bud.NuGet;
using Bud.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Bud.BaseProjects.BareProjects;
using static Bud.BaseProjects.BuildProjects;
using static Bud.NuGet.PackageReferencesProjects;

namespace Bud.V1 {
  /// <summary>
  ///   Defines the core concepts of every build in Bud.
  ///   <para>
  ///     Every build has an ID and a directory.
  ///   </para>
  ///   <para>
  ///     In addition, every build has three observable streams: input, build, and output.
  ///     The input is piped (unmodified) through to the build and then frurther
  ///     through to output.
  ///   </para>
  ///   <para>
  ///     The build is defined entirely through keys defined in this class. For example,
  ///     the input, build, and output are defined with keys <see cref="Api.Input" />,
  ///     <see cref="Api.Build" />, and <see cref="Conf.Out" />. One can customise these through
  ///     the <see cref="Conf" /> API (such as the <see cref="Conf.Modify{T}" /> method).
  ///   </para>
  /// </summary>
  public static class Api {
    #region Project Grouping

    public static Conf Project(string scope, params IConfBuilder[] confs)
      => Conf.Group(scope, confs);

    public static Conf Projects(params IConfBuilder[] confs)
      => Conf.Group((IEnumerable<IConfBuilder>) confs);

    public static Conf Projects(IEnumerable<IConfBuilder> confs)
      => Conf.Group(confs);

    #endregion

    #region Build Support

    /// <summary>
    ///   Input is an observable stream of collections of files. Whenever input
    ///   files change, a new observation is made in this input stream.
    /// </summary>
    public static readonly Key<IObservable<IEnumerable<string>>> Input = nameof(Input);

    /// <summary>
    ///   By default, build produces a single empty output.
    /// </summary>
    public static readonly Key<IObservable<IEnumerable<string>>> Build = nameof(Build);

    /// <summary>
    ///   By default, output forwards the result of the build without modifications.
    /// </summary>
    public static readonly Key<IObservable<IEnumerable<string>>> Output = nameof(Output);

    #endregion

    #region Dependencies Support

    /// <summary>
    ///   A list of keys (paths) to other builds. For example, say we defined two projects
    ///   <c>A</c> and <c>B</c>. To make <c>B</c> depend on <c>A</c>, one would add the
    ///   <c>../A</c> to the list of <see cref="Dependencies" />.
    /// </summary>
    public static readonly Key<IImmutableSet<string>> Dependencies = nameof(Dependencies);

    /// <summary>
    ///   This observable stream contains aggregated output from all dependencies.
    /// </summary>
    public static readonly Key<IObservable<IEnumerable<string>>> DependenciesOutput = nameof(DependenciesOutput);

    #endregion

    #region Build Pipeline Scheduling Support

    /// <summary>
    ///   By default the entire build pipeline (input, sources, build, and output) are
    ///   scheduled on the same scheduler and the same thread (i.e.: the build pipeline
    ///   is single threaded). The build pipeline is also asynchronous. For example,
    ///   compilers can run each in their own thread and produce output when they finish.
    ///   The output is collected in the build pipeline's thread.
    /// </summary>
    /// <remarks>
    ///   You should never need to override this outside of testing. In all honesty, this
    ///   key is mostly meant for testing.
    /// </remarks>
    public static readonly Key<IScheduler> BuildPipelineScheduler = nameof(BuildPipelineScheduler);

    #endregion

    #region Sources Support

    /// <summary>
    ///   By default, the build has no sources. Add them through
    ///   <see cref="AddSources" /> or <see cref="AddSourceFiles" />.
    /// </summary>
    public static readonly Key<IObservable<IImmutableList<string>>> Sources = nameof(Sources);

    /// <summary>
    ///   A descriptor of where to fetch source files from and how to
    ///   watch for their changes.
    ///   <para>
    ///     By default, these sources are passed through <see cref="SourceExcludeFilters" />
    ///     and then passed on to <see cref="Sources" />.
    ///   </para>
    /// </summary>
    public static readonly Key<IImmutableList<FileWatcher>> SourceIncludes = nameof(SourceIncludes);

    /// <summary>
    ///   These filters are applied on the <see cref="Sources" /> stream
    ///   before it is passed to <see cref="SourceProcessors" />.
    /// </summary>
    public static readonly Key<IImmutableList<Func<string, bool>>> SourceExcludeFilters = nameof(SourceExcludeFilters);

    /// <summary>
    ///   How long to wait after a file change has been noticed before triggering
    ///   a build (i.e.: producing an observation). For example, <see cref="ProcessedSources" />
    ///   are guarded with this calming period.
    /// </summary>
    public static readonly Key<TimeSpan> WatchedFilesCalmingPeriod = nameof(WatchedFilesCalmingPeriod);

    /// <summary>
    ///   This observatory is used when watching source file changes (see <see cref="Sources" />).
    /// </summary>
    /// <remarks>
    ///   You should never need to override this outside of testing. In all honesty, this
    ///   key is mostly meant for testing.
    /// </remarks>
    public static readonly Key<IFilesObservatory> FilesObservatory = nameof(FilesObservatory);

    /// <summary>
    ///   Adds an individual source file to the project.
    /// </summary>
    public static Conf AddSourceFile(this Conf c, string absolutePath)
      => c.Add(SourceIncludes,
               conf => FilesObservatory[conf].WatchFiles(absolutePath));

    /// <summary>
    ///   Adds an individual source file to the project.
    /// </summary>
    public static Conf AddSourceFile(this Conf c, Func<IConf, string> absolutePath)
      => c.Add(SourceIncludes,
               conf => FilesObservatory[conf].WatchFiles(absolutePath(conf)));

    #endregion

    #region Source Processing Support

    /// <summary>
    ///   A stream of <see cref="Sources" /> after they have been processed
    ///   by <see cref="SourceProcessors" />.
    /// </summary>
    public static readonly Key<IObservable<IImmutableList<string>>> ProcessedSources = nameof(ProcessedSources);

    /// <summary>
    ///   <see cref="Sources" /> are passed through source processors in order.
    ///   Their output is then piped through <c>ProcessedSources</c>.
    /// </summary>
    public static readonly Key<IImmutableList<IInputProcessor>> SourceProcessors = nameof(SourceProcessors);

    #endregion

    #region Publishing Support

    public const string PackageOutputDirName = "package";

    /// <summary>
    ///   Publishes a project to a distribution endpoint. For example,
    ///   projects like <see cref="CsLibrary(string, string)" /> are published
    ///   to a NuGet repository.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Returns a stream of booleans which indicate whether the publication
    ///     was successful.
    ///   </para>
    /// </remarks>
    public static readonly Key<IObservable<bool>> Publish = nameof(Publish);

    public static Key<IPublisher> Publisher = nameof(Publisher);

    /// <summary>
    ///   The repository URL to which to publish the package. The default
    ///   is NuGet's main repository.
    /// </summary>
    public static Key<Option<string>> PublishUrl = nameof(PublishUrl);

    /// <summary>
    ///   The API key to use when publishing with NuGet.
    /// </summary>
    public static Key<Option<string>> PublishApiKey = nameof(PublishApiKey);

    /// <summary>
    ///   Creates a package and returns the path to the created package.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Returns a stream of paths to the created package.
    ///   </para>
    /// </remarks>
    public static readonly Key<IObservable<string>> Package = nameof(Package);

    public static Key<IPackager> Packager = nameof(Packager);

    /// <summary>
    ///   The directory relative to which NuGet should look for
    ///   <see cref="PackageFiles" /> that have relative source paths.
    ///   By default it is the current working directory.
    /// </summary>
    public static Key<string> PackageBaseDir = nameof(PackageBaseDir);

    public static Key<string> PackageOutputDir = nameof(PackageOutputDir);

    public static Key<IObservable<IEnumerable<PackageFile>>> PackageFiles = nameof(PackageFiles);

    public static Key<NuGetPackageMetadata> PackageMetadata = nameof(PackageMetadata);

    public static Conf NuGetPublishingProject(string projectDir, string projectId)
      => BareProject(projectDir, projectId)
        .Add(NuGetPublishing.NuGetPublishingSupport);

    #endregion

    #region Bare Project

    public const string BuildDirName = "build";

    public const string DefaultVersion = "0.0.1";

    /// <summary>
    ///   The build's identifier. This identifier is used in <see cref="Dependencies" />.
    /// </summary>
    public static readonly Key<string> ProjectId = nameof(ProjectId);

    /// <summary>
    ///   The build's directory. Ideally, all the sources of this build
    ///   should be located within this directory.
    /// </summary>
    public static readonly Key<string> ProjectDir = nameof(ProjectDir);

    /// <summary>
    ///   The directory where all outputs and generated files are placed.
    ///   This directory is by default deleted through the <see cref="Clean" />
    ///   command.
    /// </summary>
    public static readonly Key<string> BuildDir = nameof(BuildDir);

    /// <summary>
    ///   By default, deletes the entire <see cref="BuildDir" />
    /// </summary>
    public static readonly Key<Unit> Clean = nameof(Clean);

    /// <summary>
    ///   The version of the project. By default, it's <see cref="DefaultVersion" />.
    /// </summary>
    public static Key<string> Version = nameof(Version);

    /// <param name="projectDir">see <see cref="ProjectDir" /></param>
    /// <param name="projectId">see <see cref="ProjectId" /></param>
    public static Conf BareProject(string projectDir, string projectId)
      => CreateBareProject(projectDir, projectId);

    #endregion

    #region Build Project

    /// <summary>
    ///   Returns a list of files to package. These file will end up in
    ///   the zip archive at <see cref="DistributionZipPath"/> produced by
    ///   <see cref="DistributionZip"/>.
    /// </summary>
    public static readonly Key<IObservable<IImmutableList<PackageFile>>> FilesToDistribute = nameof(FilesToDistribute);

    /// <summary>
    ///   The path where <see cref="DistributionZip"/> should place the
    ///   zip archive.
    /// </summary>
    public static readonly Key<string> DistributionZipPath = nameof(DistributionZipPath);

    /// <summary>
    ///   Creates a zip archive that contains all that is needed for the
    ///   distribution of the project. It returns the path to the created
    ///   archive.
    /// </summary>
    public static readonly Key<IObservable<string>> DistributionZip = nameof(DistributionZip);

    /// <param name="projectDir">see <see cref="ProjectDir" /></param>
    /// <param name="projectId">see <see cref="ProjectId" /></param>
    public static Conf BuildProject(string projectDir, string projectId)
      => CreateBuildProject(projectDir, projectId);

    /// <summary>
    ///   Adds files found in <paramref name="subDir" /> to <see cref="Sources" />.
    /// </summary>
    /// <param name="c">the project to which to add sources.</param>
    /// <param name="subDir">a directory relative to <see cref="ProjectDir" />.</param>
    /// <param name="fileFilter">
    ///   a wildcard-based filter of files to collect
    ///   from <paramref name="subDir" />.
    /// </param>
    /// <param name="includeSubdirs">
    ///   indicates whether files in sub-directories
    ///   of <paramref name="subDir" /> should be included.
    /// </param>
    /// <returns>the modified project</returns>
    public static Conf AddSources(this Conf c, string subDir = null, string fileFilter = "*", bool includeSubdirs = true)
      => AddSourcesImpl(c, subDir, fileFilter, includeSubdirs);

    /// <summary>
    ///   Adds individual source files to the project.
    /// </summary>
    public static Conf AddSourceFiles(this Conf c, params string[] relativeFilePaths)
      => AddSourceFilesImpl(c, relativeFilePaths);

    /// <summary>
    ///   Removes the given list of subdirectories from sources.
    /// </summary>
    public static Conf ExcludeSourceDirs(this Conf c, params string[] subDirs)
      => c.ExcludeSourceDirs(_ => subDirs);

    /// <summary>
    ///   Removes the given subdirectory from sources.
    /// </summary>
    public static Conf ExcludeSourceDir(this Conf c, Func<IConf, string> subDir)
      => c.ExcludeSourceDirs(conf => new[] {subDir(conf)});

    /// <summary>
    ///   Removes the given list of subdirectories from sources.
    /// </summary>
    public static Conf ExcludeSourceDirs(this Conf c, Func<IConf, IEnumerable<string>> subDirs)
      => ExcludeSourceDirsImpl(c, subDirs);

    #endregion

    #region CSharp Projects

    public static readonly Key<IObservable<CompileOutput>> Compile = nameof(Compile);
    public static readonly Key<Func<CompileInput, CompileOutput>> Compiler = nameof(Compiler);
    public static readonly Key<IObservable<IImmutableList<string>>> AssemblyReferences = nameof(AssemblyReferences);
    public static readonly Key<string> AssemblyName = nameof(AssemblyName);
    public static readonly Key<CSharpCompilationOptions> CsCompilationOptions = nameof(CsCompilationOptions);
    public static readonly Key<IImmutableList<ResourceDescription>> EmbeddedResources = nameof(EmbeddedResources);

    /// <summary>
    ///   Configures a C# library project named <paramref name="projectId" /> and placed in the
    ///   directory with the same name. The project's directory will be placed  in the current
    ///   working directory.
    /// </summary>
    public static Conf CsLibrary(string projectId)
      => CsLibrary(projectId, projectId);

    /// <summary>
    ///   Similar to <see cref="CsLibrary(string)"/> but places the project in the specified
    ///   folder.
    /// </summary>
    public static Conf CsLibrary(string projectDir, string projectId)
      => CsProjects.CsLibrary(projectDir, projectId);

    /// <summary>
    ///   Similar to <see cref="CsLibrary(string)"/> but produces a console application instead
    ///   of a library.
    /// </summary>
    public static Conf CsApp(string projectId)
      => CsApp(projectId, projectId);

    /// <summary>
    ///   Similar to <see cref="CsLibrary(string, string)"/> but produces a console application instead
    ///   of a library.
    /// </summary>
    public static Conf CsApp(string projectDir, string projectId)
      => CsProjects.CsApp(projectDir, projectId);

    public static Conf EmbedResource(this Conf conf, string path, string nameInAssembly)
      => CsProjects.EmbedResourceImpl(conf, path, nameInAssembly);

    #endregion

    #region Package Reference Projects

    /// <summary>
    ///   The path to the <c>packages.config</c> file. By default, it is placed directly
    ///   under the <see cref="ProjectDir" />.
    /// </summary>
    public static Key<string> PackagesConfigFile = nameof(PackagesConfigFile);

    /// <summary>
    ///   A list of paths to assemblies. These paths are resolved from NuGet
    ///   package references.
    /// </summary>
    public static Key<IObservable<IImmutableList<PackageReference>>> ReferencedPackages = nameof(ReferencedPackages);

    /// <summary>
    ///   A list of paths to assemblies. These paths are resolved from NuGet
    ///   package references.
    /// </summary>
    public static Key<IObservable<IImmutableSet<string>>> ResolvedAssemblies = nameof(ResolvedAssemblies);

    public static Key<IAssemblyResolver> AssemblyResolver = nameof(AssemblyResolver);

    public static Conf PackageReferencesProject(string dir, string projectId)
      => CreatePackageReferencesProject(dir, projectId);

    #endregion
  }
}