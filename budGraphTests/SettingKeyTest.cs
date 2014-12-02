﻿using System;
using NUnit.Framework;

namespace Bud {
  public class ScopeTest {

    private Scope settingKeyA = new Scope("A");
    private ConfigKey<string> configKeyA = new ConfigKey<string>("A");
    private TaskKey<int> taskKeyA = new TaskKey<int>("A");
    private Scope settingKeyB = new Scope("B");
    private ConfigKey<string> configKeyB = new ConfigKey<string>("B");
    private TaskKey<int> taskKeyB = new TaskKey<int>("B");
    private Scope scopedScopeA = new Scope("A").In(new Scope("C"));
    private ConfigKey<string> scopedConfigKeyA = new ConfigKey<string>("A").In(new Scope("C"));
    private TaskKey<int> scopedTaskKeyA = new TaskKey<int>("A").In(new Scope("C"));

    [Test]
    public void Equals_MUST_return_true_WHEN_the_keys_have_the_same_id_and_same_scope() {
      Assert.AreEqual(settingKeyA, configKeyA);
      Assert.AreEqual(settingKeyA, taskKeyA);
      Assert.AreEqual(configKeyA, taskKeyA);
      Assert.AreEqual(configKeyA, settingKeyA);
      Assert.AreEqual(taskKeyA, settingKeyA);
      Assert.AreEqual(taskKeyA, configKeyA);
      Assert.AreEqual(settingKeyA, settingKeyA);
      Assert.AreEqual(configKeyA, configKeyA);
      Assert.AreEqual(taskKeyA, taskKeyA);
    }

    [Test]
    public void GetHashCode_MUST_return_the_same_values_FOR_keys_with_the_same_id_and_same_scope() {
      Assert.AreEqual(settingKeyA.GetHashCode(), configKeyA.GetHashCode());
      Assert.AreEqual(settingKeyA.GetHashCode(), taskKeyA.GetHashCode());
    }

    [Test]
    public void Equals_MUST_return_false_WHEN_the_keys_have_a_different_id() {
      Assert.AreNotEqual(settingKeyA, configKeyB);
      Assert.AreNotEqual(settingKeyA, taskKeyB);
      Assert.AreNotEqual(configKeyA, taskKeyB);
      Assert.AreNotEqual(configKeyB, settingKeyA);
      Assert.AreNotEqual(taskKeyB, settingKeyA);
      Assert.AreNotEqual(taskKeyB, configKeyA);
      Assert.AreNotEqual(settingKeyA, settingKeyB);
      Assert.AreNotEqual(configKeyA, configKeyB);
      Assert.AreNotEqual(taskKeyA, taskKeyB);
      Assert.AreNotEqual(settingKeyB, settingKeyA);
      Assert.AreNotEqual(configKeyB, configKeyA);
      Assert.AreNotEqual(taskKeyB, taskKeyA);
      Assert.AreNotEqual(configKeyA, settingKeyB);
      Assert.AreNotEqual(taskKeyA, settingKeyB);
      Assert.AreNotEqual(settingKeyB, configKeyA);
      Assert.AreNotEqual(settingKeyB, taskKeyA);
    }

    [Test]
    public void Equals_MUST_return_different_has_codes_WHEN_the_keys_have_a_different_ids() {
      Assert.AreNotEqual(settingKeyA.GetHashCode(), settingKeyB.GetHashCode());
      Assert.AreNotEqual(configKeyA.GetHashCode(), configKeyB.GetHashCode());
      Assert.AreNotEqual(taskKeyA.GetHashCode(), taskKeyB.GetHashCode());
    }

    [Test]
    public void Equals_MUST_return_false_WHEN_the_keys_have_a_different_scope() {
      Assert.AreNotEqual(settingKeyA, scopedConfigKeyA);
      Assert.AreNotEqual(settingKeyA, scopedTaskKeyA);
      Assert.AreNotEqual(configKeyA, scopedTaskKeyA);
      Assert.AreNotEqual(scopedConfigKeyA, settingKeyA);
      Assert.AreNotEqual(scopedTaskKeyA, settingKeyA);
      Assert.AreNotEqual(scopedTaskKeyA, configKeyA);
      Assert.AreNotEqual(settingKeyA, scopedScopeA);
      Assert.AreNotEqual(configKeyA, scopedConfigKeyA);
      Assert.AreNotEqual(taskKeyA, scopedTaskKeyA);
      Assert.AreNotEqual(scopedScopeA, settingKeyA);
      Assert.AreNotEqual(scopedConfigKeyA, configKeyA);
      Assert.AreNotEqual(scopedTaskKeyA, taskKeyA);
      Assert.AreNotEqual(configKeyA, scopedScopeA);
      Assert.AreNotEqual(taskKeyA, scopedScopeA);
      Assert.AreNotEqual(scopedScopeA, configKeyA);
      Assert.AreNotEqual(scopedScopeA, taskKeyA);
    }

    [Test]
    public void Equals_MUST_return_different_has_codes_WHEN_the_keys_have_a_different_scope() {
      Assert.AreNotEqual(settingKeyA.GetHashCode(), scopedScopeA.GetHashCode());
      Assert.AreNotEqual(configKeyA.GetHashCode(), scopedConfigKeyA.GetHashCode());
      Assert.AreNotEqual(taskKeyA.GetHashCode(), scopedTaskKeyA.GetHashCode());
    }

    [Test]
    public void ToString_MUST_return_the_id_of_the_key_WHEN_its_scope_is_global() {
      Assert.AreEqual("Global:A", settingKeyA.ToString());
    }

    [Test]
    public void ToString_MUST_return_the_id_and_the_ids_of_scopes_WHEN_the_key_is_nested_in_multiple_scopes() {
      var deeplyNestedKey = taskKeyA
        .In(new TaskKey<uint>("E").In(new Scope("D").In(new Scope("foo").In(new ConfigKey<bool>("C").In(configKeyB)))));
      Assert.AreEqual("Global:B:C:foo:D:E:A", deeplyNestedKey.ToString());
    }

    [Test]
    public void Equals_MUST_return_true_WHEN_two_key_instances_have_the_same_id_and_scope() {
      var scopedConfigKeyAClone = new ConfigKey<string>("A").In(new Scope("C"));
      Assert.AreEqual(scopedConfigKeyA.GetHashCode(), scopedConfigKeyAClone.GetHashCode());
      Assert.AreEqual(scopedConfigKeyA, scopedConfigKeyAClone);
    }

  }
}
