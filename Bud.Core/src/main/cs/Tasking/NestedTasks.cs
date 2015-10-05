using System.Threading.Tasks;

namespace Bud.Tasking {
  internal class NestedTasks : ITasks {
    private readonly string prefix;
    private readonly ITasks tasks;

    public NestedTasks(string prefix, ITasks tasks) {
      this.prefix = prefix + "/";
      this.tasks = tasks;
    }

    public Task<T> Get<T>(Key<T> taskName) => tasks.Get<T>(prefix + taskName);
    public Task Get(Key taskName) => tasks.Get(prefix + taskName);
  }
}