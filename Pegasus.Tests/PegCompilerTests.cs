﻿// -----------------------------------------------------------------------
// <copyright file="PegCompilerTests.cs" company="(none)">
//   Copyright © 2012 John Gietzen.  All Rights Reserved.
//   This source is subject to the MIT license.
//   Please see license.txt for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Pegasus.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Pegasus.Compiler;
    using Pegasus.Expressions;
    using Pegasus.Parser;

    public class PegCompilerTests
    {
        [Test]
        public void Compile_WithSingleSimpleRule_Succeeds()
        {
            var grammar = new Grammar(
                new[]
                {
                    new Rule("start", null, new LiteralExpression("OK")),
                },
                null,
                null);
            var compiler = new PegCompiler();

            var result = compiler.Compile(grammar);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void Compile_WithNoRules_YieldsError()
        {
            var grammar = new Grammar(Enumerable.Empty<Rule>(), null, null);
            var compiler = new PegCompiler();

            var result = compiler.Compile(grammar);

            var error = result.Errors.Single();
            Assert.That(error.ErrorNumber, Is.EqualTo("PEG0001"));
        }

        [Test]
        public void Compile_WithDuplicateDefinition_YieldsError()
        {
            var grammar = new Grammar(
                new[]
                {
                    new Rule("a", null, new LiteralExpression("a")),
                    new Rule("a", null, new LiteralExpression("b")),
                },
                null,
                null);
            var compiler = new PegCompiler();

            var result = compiler.Compile(grammar);

            var error = result.Errors.Single();
            Assert.That(error.ErrorNumber, Is.EqualTo("PEG0002"));
        }

        [Test]
        public void Compile_WithMissingRuleDefinition_YieldsError()
        {
            var grammar = new Grammar(
                new[]
                {
                    new Rule("a", null, new NameExpression("b")),
                },
                null,
                null);
            var compiler = new PegCompiler();

            var result = compiler.Compile(grammar);

            var error = result.Errors.Single();
            Assert.That(error.ErrorNumber, Is.EqualTo("PEG0003"));
        }

        [Test]
        [TestCase("a = a;")]
        [TestCase("a = '' a;")]
        [TestCase("a = ('OK' / '') a;")]
        [TestCase("a = b; b = c; c = d; d = a;")]
        [TestCase("a = b / c; b = 'OK'; c = a;")]
        [TestCase("a = !b a; b = 'OK';")]
        [TestCase("a = &b c; b = a; c = 'OK';")]
        [TestCase("a = b* a; b = 'OK';")]
        public void Compile_WithLeftRecursion_YieldsError(string subject)
        {
            var parser = new PegParser();
            var grammar = parser.Parse(subject);
            var compiler = new PegCompiler();

            var result = compiler.Compile(grammar);

            var error = result.Errors.Single();
            Assert.That(error.ErrorNumber, Is.EqualTo("PEG0004"));
        }

        [Test]
        [TestCase("namespace")]
        [TestCase("classname")]
        public void Compile_WithDuplicateSetting_YieldsError(string settingName)
        {
            var grammar = new Grammar(
                new Rule[0],
                new[]
                {
                    new KeyValuePair<string, string>(settingName, "OK"),
                    new KeyValuePair<string, string>(settingName, "OK"),
                },
                null);
            var compiler = new PegCompiler();

            var result = compiler.Compile(grammar);

            var error = result.Errors.Single();
            Assert.That(error.ErrorNumber, Is.EqualTo("PEG0005"));
        }

        [Test]
        public void Compile_WithUnrecognizedSetting_YieldsWarning()
        {
            var grammar = new Grammar(
                new Rule[0],
                new[] { new KeyValuePair<string, string>("barnacle", "OK") },
                null);
            var compiler = new PegCompiler();

            var result = compiler.Compile(grammar);

            var error = result.Errors.First();
            Assert.That(error.ErrorNumber, Is.EqualTo("PEG0006"));
        }
    }
}
