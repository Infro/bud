using System;
using System.Collections.Immutable;

namespace Bud.SettingsConstruction.Ops {
  public class EnsureConfigInitialized<T> : Setting {
    public Func<BuildConfiguration, T> InitialValue;

    public EnsureConfigInitialized(ConfigKey<T> key, T initialValue) : this(key, b => initialValue) {}

    public EnsureConfigInitialized(ConfigKey<T> key, Func<BuildConfiguration, T> initialValue) : base(key) {
      this.InitialValue = initialValue;
    }

    public override void ApplyTo(ImmutableDictionary<ISettingKey, IValueDefinition>.Builder buildConfigurationBuilder) {
      if (!buildConfigurationBuilder.ContainsKey(Key)) {
        buildConfigurationBuilder[Key] = new ConfigDefinition<T>(InitialValue);
      }
    }
  }
}
