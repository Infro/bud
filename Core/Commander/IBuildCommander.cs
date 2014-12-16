using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Bud.Plugins.Build;
using Bud.Plugins.BuildLoading;

namespace Bud.Commander {
  public interface IBuildCommander : IDisposable {
    string Evaluate(string command);
  }
}