namespace Bud.Configuration {
  public abstract class ConfigTransform<T> : IConfigTransform {
    protected ConfigTransform(string key) {
      Key = key;
    }

    public string Key { get; }
    public abstract ConfigDefinition<T> Modify(ConfigDefinition<T> configDefinition);
    public abstract ConfigDefinition<T> ToConfigDefinition();
    public ConfigTransform<T> Nest(string parentKey) => new NestConfig<T>(parentKey, this);
    IConfigDefinition IConfigTransform.ToConfigDefinition() => ToConfigDefinition();
    public IConfigDefinition Modify(IConfigDefinition configDefinition) => Modify((ConfigDefinition<T>) configDefinition);
    IConfigTransform IConfigTransform.Nest(string parentKey) => Nest(parentKey);
  }
}