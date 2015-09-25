using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Bud.Tasking {
  public class Tasks {
    public static readonly Tasks Empty = new Tasks(ImmutableDictionary<string, ITaskDefinition>.Empty);
    private ImmutableDictionary<string, ITaskDefinition> TaskDefinitions { get; }

    private Tasks(ImmutableDictionary<string, ITaskDefinition> taskDefinitions) {
      TaskDefinitions = taskDefinitions;
    }

    public Tasks Set<T>(string taskName, Func<ITasker, Task<T>> task) {
      ITaskDefinition previousTaskDefinition;
      if (TaskDefinitions.TryGetValue(taskName, out previousTaskDefinition)) {
        AssertTaskTypeNotOverridden<T>(taskName, previousTaskDefinition);
      }
      return new Tasks(TaskDefinitions.SetItem(taskName, new TaskDefinition<T>(task)));
    }

    public Tasks Modify<T>(string taskName, Func<ITasker, Task<T>, Task<T>> task) {
      ITaskDefinition previousTaskDefinition;
      if (TaskDefinitions.TryGetValue(taskName, out previousTaskDefinition)) {
        AssertTaskTypeNotOverridden<T>(taskName, previousTaskDefinition);
        return new Tasks(TaskDefinitions.SetItem(taskName, new TaskDefinition<T>(((TaskDefinition<T>) previousTaskDefinition).Task, task)));
      }
      throw new TaskUndefinedException($"Could not modify the task '{taskName}'. The task is not defined yet.");
    }

    public bool TryGetTask(string taskName, out ITaskDefinition task) {
      ITaskDefinition taskDefinition;
      if (TaskDefinitions.TryGetValue(taskName, out taskDefinition)) {
        task = taskDefinition;
        return true;
      }
      task = null;
      return false;
    }

    private static void AssertTaskTypeNotOverridden<T>(string taskName, ITaskDefinition previousTaskDefinition) {
      if (previousTaskDefinition.ReturnType != typeof(T)) {
        throw new TaskTypeOverrideException($"Could not redefine the type of task '{taskName}' from '{previousTaskDefinition.ReturnType}' to '{typeof(T)}'. Redefinition of task types is not allowed.");
      }
    }

    private class TaskDefinition<T> : ITaskDefinition {
      public Type ReturnType => typeof(T);
      public Func<ITasker, Task<T>> Task { get; }
      Func<ITasker, Task> ITaskDefinition.Task => Task;

      public TaskDefinition(Func<ITasker, Task<T>> originalTask, Func<ITasker, Task<T>, Task<T>> modifierTask) {
        Task = context => modifierTask(context, originalTask(context));
      }

      public TaskDefinition(Func<ITasker, Task<T>> originalTask) {
        Task = originalTask;
      }
    }
  }
}