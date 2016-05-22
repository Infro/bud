using System;
using System.Threading.Tasks;
using Bud.V1;
using Moq;
using NUnit.Framework;
using static Bud.Option;

namespace Bud.Configuration {
  [Category("AppVeyorIgnore")]
  public class ConfCacheTest {
    private Mock<Func<Key<int>, Option<int>>> intFunc;
    private Mock<Func<Key<object>, Option<object>>> objectFunc;
    private ConfCache cache;

    [SetUp]
    public void SetUp() {
      intFunc = new Mock<Func<Key<int>, Option<int>>>();
      intFunc.Setup(self => self("foo")).Returns(42);
      objectFunc = new Mock<Func<Key<object>, Option<object>>>();
      objectFunc.Setup(self => self("bar")).Returns<Key<object>>(k => new object());
      objectFunc.Setup(self => self("undefined")).Returns(None<object>());
      objectFunc.Setup(self => self("defined")).Returns(Some<object>("42"));
      cache = new ConfCache();
    }

    [Test]
    public void Delegate_to_wrapped_conf() {
      cache.TryGet("foo", intFunc.Object);
      intFunc.Verify(self => self("foo"));
    }

    [Test]
    public void Return_the_value_returned_by_wrapped_conf()
      => Assert.AreEqual(42, cache.TryGet("foo", intFunc.Object).Value);

    [Test]
    public void Invokes_wrapped_conf_only_once() {
      cache.TryGet("bar", intFunc.Object);
      cache.TryGet("bar", intFunc.Object);
      intFunc.Verify(self => self("bar"), Times.Once);
    }

    [Test]
    public void Always_returns_the_same_value()
      => Assert.AreSame(cache.TryGet("bar", objectFunc.Object).Value,
                        cache.TryGet("bar", objectFunc.Object).Value);

    [Test]
    public void TryGet_returns_an_empty_optional_when_the_fallback_does_not_define_the_key()
      => Assert.IsFalse(cache.TryGet("undefined", objectFunc.Object)
                                   .HasValue);

    [Test]
    public void TryGet_returns_an_optional_with_a_value_when_the_key_is_defined_by_the_fallback_function()
      => Assert.IsTrue(cache.TryGet("defined", objectFunc.Object)
                                  .HasValue);

    [Test]
    public void TryGet_returns_an_optional_containing_the_value_returned_by_the_fallback_function()
      => Assert.AreEqual("42",
                         cache.TryGet("defined", objectFunc.Object)
                                    .Value);

    [Test]
    public void Nested_concurrent_access_to_different_keys_must_not_deadlock() {
      var result = cache.TryGet<int>("A", key => {
        var task = Task.Run(() => cache.TryGet<int>("B", _ => 1).Value);
        task.Wait();
        return 42 + task.Result;
      });
      Assert.AreEqual(43, result.Value);
    }
  }
}