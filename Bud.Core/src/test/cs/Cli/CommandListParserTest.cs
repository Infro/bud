﻿using System.Collections.Generic;
using System.Linq;
using Bud.Cli.CommandListParser;
using NUnit.Framework;

namespace Bud.Cli {
  public class CommandListParserTest {
    [Test]
    public void ToCommandList_MUST_return_an_empty_list_WHEN_given_null() {
      Assert.IsEmpty(ToCommandList(null));
    }

    [Test]
    public void ToCommandList_MUST_return_a_key_command_WHEN_given_a_single_key() {
      AssertCommandsAreEqual(new KeyCommand("foo"),
                             ToCommandList(new[] {"foo"}).First());
    }

    [Test]
    public void ToCommandList_MUST_return_a_macro_command_WHEN_given_a_single_macro() {
      AssertCommandsAreEqual(new MacroCommand("foo"),
                             ToCommandList(new[] {"@foo"}).First());
    }

    [Test]
    public void ToCommandList_MUST_return_a_macro_command_with_parameters_WHEN_given_macro_parameters() {
      AssertCommandsAreEqual(new MacroCommand("foo", "par1", "par2"),
                             ToCommandList(new[] {"@foo", "par1", "par2"}).First());
    }

    [Test]
    public void ToCommandList_MUST_return_a_list_of_key_commands_WHEN_given_only_keys() {
      AssertCommandsAreEqual(new[] {new KeyCommand("foo"), new KeyCommand("bar")},
                             ToCommandList(new[] {"foo", "bar"}));
    }

    [Test]
    public void ToCommandList_MUST_return_a_list_of_macro_commands_WHEN_given_only_macros() {
      AssertCommandsAreEqual(new[] {new MacroCommand("foo"), new MacroCommand("bar")},
                             ToCommandList(new[] {"@foo", "@bar"}));
    }

    [Test]
    public void ToCommandList_MUST_return_a_separated_key_command_after_a_macro_WHEN_given_only_macros() {
      AssertCommandsAreEqual(new Command[] {new MacroCommand("foo"), new KeyCommand("bar")},
                             ToCommandList(new[] {"@foo", "@", "bar"}));
    }

    [Test]
    public void ExtractOptionsAndCommands_MUST_produce_empty_arrays_WHEN_given_an_empty_array() {
      string[] options;
      string[] commands;
      ExtractOptionsAndCommands(new string[] {}, out options, out commands);
      Assert.IsEmpty(options);
      Assert.IsEmpty(commands);
    }

    [Test]
    public void ExtractOptionsAndCommands_MUST_produce_some_options_and_empty_commands_WHEN_given_options_only() {
      string[] options;
      string[] commands;
      var commandLineArgs = new[] {"-a", "--dsa", "some option"};
      ExtractOptionsAndCommands(commandLineArgs, out options, out commands);
      Assert.AreSame(commandLineArgs, options);
      Assert.IsEmpty(commands);
    }

    [Test]
    public void ExtractOptionsAndCommands_MUST_produce_some_options_and_some_commands_WHEN_some_options_are_separated__from_some_commands() {
      string[] options;
      string[] commands;
      var expectedOptions = new[] {"-a", "--dsa", "some option"};
      var expectedCommands = new[] {"@command1", "--foo", "-b"};
      ExtractOptionsAndCommands(expectedOptions.Concat(expectedCommands).ToArray(),
                                out options,
                                out commands);
      Assert.AreEqual(expectedOptions, options);
      Assert.AreEqual(expectedCommands, commands);
    }

    [Test]
    public void ParseCommandLine_returns_an_empty_list_WHEN_given_an_empty_string() {
      Assert.IsEmpty(ParseCommandLine(string.Empty));
    }

    [Test]
    public void ParseCommandLine_returns_a_single_command() {
      AssertCommandsAreEqual(new[] {new KeyCommand("foo")},
                             ParseCommandLine("foo"));
    }

    [Test]
    public void ParseCommandLine_returns_two_commands() {
      AssertCommandsAreEqual(new Command[] {new KeyCommand("foo"), new MacroCommand("bar") },
                             ParseCommandLine("foo @bar"));
    }

    [Test]
    public void ParseCommandLine_passes_arguments_to_macros() {
      AssertCommandsAreEqual(new Command[] {new MacroCommand("bar", "a", "-b") },
                             ParseCommandLine("@bar a -b"));
    }

    private void AssertCommandsAreEqual(IEnumerable<Command> expectedCommands,
                                        IEnumerable<Command> actualCommands) {
      Assert.AreEqual(expectedCommands.Count(), actualCommands.Count(), "Number of commands does not match.");
      expectedCommands.Zip(actualCommands, (expectedCommand, actualCommand) => new {expectedCommand, actualCommand})
                      .All(commandPairs => AssertCommandsAreEqual(commandPairs.expectedCommand, commandPairs.actualCommand));
    }

    private bool AssertCommandsAreEqual(Command expectedCommand, Command actualCommand) {
      if (expectedCommand is MacroCommand) {
        AssertCommandsAreEqual((MacroCommand) expectedCommand, (MacroCommand) actualCommand);
      } else {
        Assert.AreEqual(((KeyCommand) expectedCommand).Name, ((KeyCommand) actualCommand).Name);
      }
      return true;
    }

    private static void AssertCommandsAreEqual(MacroCommand expectedCommand, MacroCommand actualCommand) {
      Assert.AreEqual(expectedCommand.Name, actualCommand.Name);
      Assert.AreEqual(expectedCommand.Parameters, actualCommand.Parameters);
    }
  }
}