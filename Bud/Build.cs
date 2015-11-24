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
  public static class Build {
    public static readonly Key<Files> Sources = nameof(Sources);
    public static readonly Key<string> ProjectId = nameof(ProjectId);
    public static readonly Key<string> ProjectDir = nameof(ProjectDir);
    public static readonly Key<IEnumerable<string>> Dependencies = nameof(Dependencies);
    public static readonly Key<IFilesObservatory> FilesObservatory = nameof(FilesObservatory);
    public static readonly Key<IScheduler> BuildPipelineScheduler = nameof(BuildPipelineScheduler);
    public static readonly Key<IObservable<ImmutableArray<Timestamped<string>>>> ProcessedSources = nameof(ProcessedSources);
    public static readonly Key<ImmutableList<IFilesProcessor>> SourceProcessors = nameof(SourceProcessors);
    public static readonly Key<TimeSpan> InputCalmingPeriod = nameof(InputCalmingPeriod);
    private static readonly Lazy<EventLoopScheduler> DefauBuildPipelineScheduler = new Lazy<EventLoopScheduler>(() => new EventLoopScheduler());

    public static Conf Project(string projectDir, string projectId)
      => Group(projectId)
        .InitValue(ProjectDir, projectDir)
        .InitValue(ProjectId, projectId)
        .InitValue(Sources, Files.Empty)
        .InitValue(Dependencies, Enumerable.Empty<string>())
        .Init(BuildPipelineScheduler, _ => DefauBuildPipelineScheduler.Value)
        .Init(ProcessedSources, ProcessSources)
        .InitValue(InputCalmingPeriod, TimeSpan.FromMilliseconds(300))
        .InitValue(SourceProcessors, ImmutableList<IFilesProcessor>.Empty)
        .Init(FilesObservatory, _ => new LocalFilesObservatory());

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

    private static IObservable<ImmutableArray<Timestamped<string>>> ProcessSources(IConf project)
      => SourceProcessors[project]
        .Aggregate(CollectTimestampedSources(project),
                   (sources, processor) => processor.Process(sources));

    private static IObservable<ImmutableArray<Timestamped<string>>> CollectTimestampedSources(IConf c)
      => Sources[c].Watch()
                   .ObserveOn(BuildPipelineScheduler[c])
                   .CalmAfterFirst(InputCalmingPeriod[c], BuildPipelineScheduler[c])
                   .Select(Files.ToTimestampedFiles);

    public static Conf AddSourceProcessor(this Conf project, Func<IConf, IFilesProcessor> fileProcessorFactory)
      => project.Modify(SourceProcessors, (conf, processors) => processors.Add(fileProcessorFactory(conf)));
  }
}