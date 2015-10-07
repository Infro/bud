using System;

namespace Bud.Configuration {
  public interface IConfigDefinition {
    Type ValueType { get; }
    object Invoke(IConfigs configs);
  }
}