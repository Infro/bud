﻿namespace Bud {
  public interface IPlugin {
    Settings ApplyTo(Settings settings, Key project);
  }
}