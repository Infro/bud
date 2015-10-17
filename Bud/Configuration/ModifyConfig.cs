using System;

namespace Bud.Configuration {
  public class ModifyConfig<T> : ConfigTransform<T> {
    public ModifyConfig(Key<T> key, Func<IConf, T, T> valueFactory) : this(null, key, valueFactory) {}

    private ModifyConfig(string prefix, Key<T> key, Func<IConf, T, T> valueFactory)
      : base(prefix == null ? key : new Key<T>(prefix + key)) {
      Prefix = prefix;
      ValueFactory = valueFactory;
    }

    public string Prefix { get; }
    private Func<IConf, T, T> ValueFactory { get; }

    public override ConfigDefinition<T> Modify(ConfigDefinition<T> configDefinition)
      => Prefix == null ?
        new ConfigDefinition<T>(configs => ValueFactory(configs, configDefinition.Invoke(configs))) :
        new ConfigDefinition<T>(configs => ValueFactory(new NestedConf(Prefix, configs), configDefinition.Invoke(configs)));

    public override ConfigTransform<T> Nest(string parentKey) => new ModifyConfig<T>(parentKey, Key, ValueFactory);

    public override ConfigDefinition<T> ToConfigDefinition() {
      throw new ConfigDefinitionException(Key, typeof(T), $"Could not modify the configuration '{Key}' of type '{typeof(T)}'. No previous definition exists.");
    }
  }
}