using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Bud.IO;
using Bud.V1;
using Microsoft.Reactive.Testing;
using Moq;
using NUnit.Framework;
using static System.IO.Directory;
using static System.IO.Path;
using static System.Reactive.Linq.Observable;
using static Bud.BaseProjects.BuildProjects;
using static Bud.V1.Api;
using static NUnit.Framework.Assert;

namespace Bud.BaseProjects {
  public class BuildProjectsTest {
    [Test]
    public void DependenciesInput_must_be_empty_when_no_dependencies_given() {
      var projects = BuildProject("aDir", "A");
      AreEqual(new[] {Enumerable.Empty<string>()},
               projects.Get(DependenciesInput).ToList().Wait());
    }

    [Test]
    public void DependenciesInput_must_contain_output_from_dependencies() {
      var projects = Projects(BuildProject("aDir", "A")
                                .SetValue(Output, Return(new[] {"a"})),
                              BuildProject("bDir", "B")
                                .Add(Dependencies, "../A"));
      AreEqual(new[] {"a"},
               projects.Get("B"/DependenciesInput).Wait());
    }


    [Test]
    public void DependenciesInput_reobserved_when_dependencies_change() {
      var testScheduler = new TestScheduler();
      var projects = Projects(BuildProject("aDir", "A")
                                .SetValue(BuildPipelineScheduler, testScheduler)
                                .SetValue(Output, ChangingOutput(testScheduler)),
                              BuildProject("bDir", "B")
                                .SetValue(BuildPipelineScheduler, testScheduler)
                                .Add(Dependencies, "../A"));
      var bInput = projects.Get("B"/DependenciesInput).GetEnumerator();
      testScheduler.AdvanceBy(TimeSpan.FromSeconds(5).Ticks);
      IsTrue(bInput.MoveNext());
      AreEqual(new[] {"foo"}, bInput.Current);
      IsTrue(bInput.MoveNext());
      AreEqual(new[] {"bar"}, bInput.Current);
      IsFalse(bInput.MoveNext());
    }


    [Test]
    public void Sources_should_be_initially_empty()
      => IsEmpty(Sources[BuildProject("bar", "Foo")].Take(1).Wait());

    [Test]
    public void Sources_should_contain_added_files() {
      var project = SourcesSupport.AddSourceFile("A")
                                  .AddSourceFile(_ => "B");
      That(Sources[project].Take(1).Wait(),
           Is.EquivalentTo(new[] {"A", "B"}));
    }

    [Test]
    public void Sources_should_be_excluded_by_the_exclusion_filter() {
      var project = SourcesSupport.AddSourceFile("A")
                                  .AddSourceFile(_ => "B")
                                  .Add(SourceExcludeFilters, sourceFile => string.Equals("B", sourceFile));
      That(Sources[project].Take(1).Wait(),
           Is.EquivalentTo(new[] {"A"}));
    }

    [Test]
    public void Sources_should_contain_files_from_added_directories() {
      using (var tempDir = new TemporaryDirectory()) {
        var fileA = tempDir.CreateEmptyFile("A", "A.cs");
        var fileB = tempDir.CreateEmptyFile("B", "B.cs");
        var twoDirsProject = BuildProject(tempDir.Path, "foo")
          .AddSources("A")
          .AddSources("B");
        That(Sources[twoDirsProject].Take(1).Wait(),
             Is.EquivalentTo(new[] {fileA, fileB}));
      }
    }

    [Test]
    public void Sources_should_not_include_files_in_the_target_folder() {
      using (var tempDir = new TemporaryDirectory()) {
        var project = BuildProject(tempDir.Path, "foo").AddSources(fileFilter: "*.cs");
        tempDir.CreateEmptyFile(TargetDir[project], "A.cs");
        var files = Sources[project].Take(1).Wait();
        IsEmpty(files);
      }
    }

    [Test]
    public void Input_should_initially_observe_a_single_empty_inout()
      => AreEqual(new[] {Enumerable.Empty<string>()},
                  Input[BuildProject("bar", "Foo")].ToList().Wait());

    [Test]
    public void Input_contains_the_added_file() {
      var buildProject = BuildProject("foo", "Foo")
        .Add(SourceIncludes, c => FilesObservatory[c].WatchFiles("foo/bar"));
      AreEqual(new[] {"foo/bar"},
               Input[buildProject].Take(1).Wait());
    }

    [Test]
    public void Source_processor_changes_source_input() {
      var fileProcessor = new Mock<IInputProcessor>(MockBehavior.Strict);
      var expectedOutputFiles = new[] {"foo"};
      fileProcessor.Setup(self => self.Process(It.IsAny<IObservable<IEnumerable<string>>>()))
                   .Returns(Return(expectedOutputFiles));
      var actualOutputFiles = BuildProject("FooDir", "Foo")
        .Add(SourceProcessors, fileProcessor.Object)
        .Get(ProcessedSources)
        .Wait();
      fileProcessor.VerifyAll();
      AreEqual(expectedOutputFiles, actualOutputFiles);
    }

    [Test]
    public void Source_processors_must_be_invoked_on_the_build_pipeline_thread() {
      int inputThreadId = 0;
      var fileProcessor = new ThreadIdRecordingInputProcessor();
      BuildProject("fooDir", "A")
        .Add(SourceIncludes, new FileWatcher(Enumerable.Empty<string>(), Create<string>(observer => {
          Task.Run(() => {
            inputThreadId = Thread.CurrentThread.ManagedThreadId;
            observer.OnNext("A.cs");
            observer.OnCompleted();
          });
          return new CompositeDisposable();
        })))
        .Add(SourceProcessors, fileProcessor)
        .Get(ProcessedSources).Wait();
      AreNotEqual(0, fileProcessor.InvocationThreadId);
      AreNotEqual(inputThreadId, fileProcessor.InvocationThreadId);
    }

    [Test]
    public void Default_input_contains_processed_sources() {
      var projects = BuildProject("bDir", "B")
        .Add(SourceIncludes, new FileWatcher("b"))
        .Add(SourceProcessors, new FooAppenderInputProcessor());
      AreEqual(new[] {"bfoo"},
               projects.Get(Input).Wait());
    }

    [Test]
    public void Clean_deletes_non_empty_target_folders() {
      using (var tmpDir = new TemporaryDirectory()) {
        tmpDir.CreateEmptyFile("target", "foo.txt");
        tmpDir.CreateEmptyFile("target", "dir", "bar.txt");
        BuildProject(Combine(tmpDir.Path), "A").Get(Clean);
        IsFalse(Exists(Combine(tmpDir.Path, "target")));
      }
    }

    [Test]
    public void Clean_does_nothing_when_the_target_folder_does_not_exist() {
      using (var tmpDir = new TemporaryDirectory()) {
        BuildProject(Combine(tmpDir.Path), "A").Get(Clean);
        IsFalse(Exists(Combine(tmpDir.Path, "target")));
      }
    }


    private static IObservable<string[]> ChangingOutput(IScheduler scheduler)
      => Return(new[] {"foo"}).Delay(TimeSpan.FromSeconds(1), scheduler)
                              .Concat(Return(new[] {"bar"})
                                        .Delay(TimeSpan.FromSeconds(1), scheduler));

    private class FooAppenderInputProcessor : IInputProcessor {
      public IObservable<IEnumerable<string>> Process(IObservable<IEnumerable<string>> sources)
        => sources.Select(io => io.Select(file => file + "foo"));
    }

    public class ThreadIdRecordingInputProcessor : IInputProcessor {
      public int InvocationThreadId { get; private set; }

      public IObservable<IEnumerable<string>> Process(IObservable<IEnumerable<string>> sources) {
        InvocationThreadId = Thread.CurrentThread.ManagedThreadId;
        return sources;
      }
    }
  }
}