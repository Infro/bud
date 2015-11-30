using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Bud.IO;
using Bud.Reactive;
using static System.IO.Path;
using static Bud.Conf;

namespace Bud {
  public static class Builds {
    public static readonly Key<IObservable<InOut>> Input = nameof(Input);
    public static readonly Key<IObservable<InOut>> Build = nameof(Build);
    public static readonly Key<IObservable<InOut>> Output = nameof(Output);
    public static readonly Key<Files> Sources = nameof(Sources);
    public static readonly Key<string> ProjectId = nameof(ProjectId);
    public static readonly Key<string> ProjectDir = nameof(ProjectDir);
    public static readonly Key<IEnumerable<string>> Dependencies = nameof(Dependencies);
    public static readonly Key<IFilesObservatory> FilesObservatory = nameof(FilesObservatory);
    public static readonly Key<IScheduler> BuildPipelineScheduler = nameof(BuildPipelineScheduler);
    public static readonly Key<IObservable<InOut>> ProcessedSources = nameof(ProcessedSources);
    public static readonly Key<IEnumerable<IFilesProcessor>> SourceProcessors = nameof(SourceProcessors);
    public static readonly Key<TimeSpan> InputCalmingPeriod = nameof(InputCalmingPeriod);
    private static readonly Lazy<EventLoopScheduler> DefauBuildPipelineScheduler = new Lazy<EventLoopScheduler>(() => new EventLoopScheduler());

    public static Conf Project(string projectDir, string projectId)
      => Group(projectId)
        .InitValue(ProjectDir, projectDir)
        .InitValue(ProjectId, projectId)
        .InitValue(Sources, Files.Empty)
        .Init(Input, DefaultInput)
        .Init(Build, c => Input[c])
        .Init(Output, c => Build[c])
        .InitValue(Dependencies, Enumerable.Empty<string>())
        .Init(BuildPipelineScheduler, _ => DefauBuildPipelineScheduler.Value)
        .Init(ProcessedSources, ProcessSources)
        .InitValue(InputCalmingPeriod, TimeSpan.FromMilliseconds(300))
        .InitValue(SourceProcessors, ImmutableList<IFilesProcessor>.Empty)
        .Init(FilesObservatory, _ => new LocalFilesObservatory());

    private static IObservable<InOut> DefaultInput(IConf conf)
      => Dependencies[conf].Select(dependency => (dependency / Output)[conf])
                           .Concat(new[] {ProcessedSources[conf]})
                           .CombineLatest(InOut.Merge);

    public static Conf SourceDir(string subDir = null, string fileFilter = "*", bool includeSubdirs = true) {
      return Empty.Modify(Sources, (conf, sources) => {
        var sourceDir = subDir == null ? ProjectDir[conf] : Combine(ProjectDir[conf], subDir);
        var newSources = FilesObservatory[conf].ObserveDir(sourceDir, fileFilter, includeSubdirs);
        return sources.ExpandWith(newSources);
      });
    }

    public static Conf SourceFiles(params string[] relativeFilePaths)
      => Empty.Modify(Sources, (conf, existingSources) => {
        var projectDir = ProjectDir[conf];
        var absolutePaths = relativeFilePaths.Select(relativeFilePath => Combine(projectDir, relativeFilePath));
        var newSources = FilesObservatory[conf].ObserveFiles(absolutePaths);
        return existingSources.ExpandWith(newSources);
      });

    public static Conf ExcludeSourceDirs(params string[] subDirs)
      => Empty.Modify(Sources, (conf, previousFiles) => {
        var forbiddenDirs = subDirs.Select(s => Combine(ProjectDir[conf], s));
        return previousFiles.WithFilter(file => !forbiddenDirs.Any(file.StartsWith));
      });

    private static IObservable<InOut> ProcessSources(IConf project)
      => SourceProcessors[project]
        .Aggregate(ObservedSources(project),
                   (sources, processor) => processor.Process(sources));

    private static IObservable<InOut> ObservedSources(IConf c)
      => Sources[c].Watch()
                   .ObserveOn(BuildPipelineScheduler[c])
                   .CalmAfterFirst(InputCalmingPeriod[c], BuildPipelineScheduler[c])
                   .Select(InOut.Create);

    public static Conf AddSourceProcessor(this Conf project, Func<IConf, IFilesProcessor> fileProcessorFactory)
      => project.Modify(SourceProcessors, (conf, processors) => processors.Concat(new[] {fileProcessorFactory(conf)}));
  }
}