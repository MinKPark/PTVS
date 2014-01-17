﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

extern alias analysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.PythonTools;
using Microsoft.PythonTools.Intellisense;
using Microsoft.PythonTools.Interpreter;
using Microsoft.PythonTools.Interpreter.Default;
using Microsoft.PythonTools.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudioTools.Project;
using TestUtilities;
using TestUtilities.Python;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace PythonToolsTests {
    [TestClass]
    public class CompletionDBTest {
        [ClassInitialize]
        public static void DoDeployment(TestContext context) {
            AssertListener.Initialize();
            PythonTestData.Deploy();
        }

        [TestMethod, Priority(0)]
        public void TestOpen() {
            var untested = new List<string>();

            foreach (var path in PythonPaths.Versions) {
                if (!File.Exists(path.Path)) {
                    untested.Add(path.Version.ToString());
                    continue;
                }
                Console.WriteLine(path.Path);

                Guid testId = Guid.NewGuid();
                var testDir = TestData.GetTempPath(testId.ToString());

                // run the scraper
                using (var proc = ProcessOutput.RunHiddenAndCapture(
                    path.Path,
                    TestData.GetPath("PythonScraper.py"),
                    testDir,
                    TestData.GetPath("CompletionDB")
                )) {
                    Console.WriteLine("Command: " + proc.Arguments);

                    proc.Wait();

                    // it should succeed
                    Console.WriteLine("**Stdout**");
                    foreach (var line in proc.StandardOutputLines) {
                        Console.WriteLine(line);
                    }
                    Console.WriteLine("");
                    Console.WriteLine("**Stdout**");
                    foreach (var line in proc.StandardErrorLines) {
                        Console.WriteLine(line);
                    }
                
                    Assert.AreEqual(0, proc.ExitCode, "Bad exit code: " + proc.ExitCode);
                }

                // perform some basic validation
                dynamic builtinDb = Unpickle.Load(new FileStream(Path.Combine(testDir, path.Version.Is3x() ? "builtins.idb" : "__builtin__.idb"), FileMode.Open, FileAccess.Read));
                if (path.Version.Is2x()) { // no open in 3.x
                    foreach (var overload in builtinDb["members"]["open"]["value"]["overloads"]) {
                        Assert.AreEqual("__builtin__", overload["ret_type"][0][0]);
                        Assert.AreEqual("file", overload["ret_type"][0][1]);
                    }

                    if (!path.Path.Contains("Iron")) {
                        // http://pytools.codeplex.com/workitem/799
                        var arr = (IList<object>)builtinDb["members"]["list"]["value"]["members"]["__init__"]["value"]["overloads"];
                        Assert.AreEqual(
                            "args",
                            ((dynamic)(arr[0]))["args"][1]["name"]
                        );
                    }
                }

                if (!path.Path.Contains("Iron")) {
                    dynamic itertoolsDb = Unpickle.Load(new FileStream(Path.Combine(testDir, "itertools.idb"), FileMode.Open, FileAccess.Read));
                    var tee = itertoolsDb["members"]["tee"]["value"];
                    var overloads = tee["overloads"];
                    var nArg = overloads[0]["args"][1];
                    Assert.AreEqual("n", nArg["name"]);
                    Assert.AreEqual("2", nArg["default_value"]);

                    dynamic sreDb = Unpickle.Load(new FileStream(Path.Combine(testDir, "_sre.idb"), FileMode.Open, FileAccess.Read));
                    var members = sreDb["members"];
                    Assert.IsTrue(members.ContainsKey("SRE_Pattern"));
                    Assert.IsTrue(members.ContainsKey("SRE_Match"));
                }
            }

            if (untested.Count > 0) {
                Assert.Inconclusive("Did not test with version(s) " + string.Join(", ", untested));
            }
        }

        [TestMethod, Priority(0)]
        public void TestPthFiles() {
            var outputPath = TestData.GetTempPath(randomSubPath: true);
            Console.WriteLine("Writing to: " + outputPath);

            // run the analyzer
            using (var output = ProcessOutput.RunHiddenAndCapture("Microsoft.PythonTools.Analyzer.exe",
                "/lib", TestData.GetPath(@"TestData\PathStdLib"),
                "/version", "2.7",
                "/outdir", outputPath,
                "/indir", TestData.GetPath("CompletionDB"),
                "/log", "AnalysisLog.txt")) {
                output.Wait();
                Console.WriteLine("* Stdout *");
                foreach (var line in output.StandardOutputLines) {
                    Console.WriteLine(line);
                }
                Console.WriteLine("* Stderr *");
                foreach (var line in output.StandardErrorLines) {
                    Console.WriteLine(line);
                }
                Assert.AreEqual(0, output.ExitCode);
            }

            File.Copy(TestData.GetPath(@"CompletionDB\__builtin__.idb"), Path.Combine(outputPath, "__builtin__.idb"));

            var fact = InterpreterFactoryCreator.CreateAnalysisInterpreterFactory(new Version(2, 7));
            var paths = new List<string> { outputPath };
            paths.AddRange(Directory.EnumerateDirectories(outputPath));
            var typeDb = new PythonTypeDatabase(fact, paths);
            var module = typeDb.GetModule("SomeLib");
            Assert.IsNotNull(module, "Could not import SomeLib");
            var fobMod = typeDb.GetModule("SomeLib.fob");
            Assert.IsNotNull(fobMod, "Could not import SomeLib.fob");

            var cClass = ((IPythonModule)fobMod).GetMember(null, "C");
            Assert.IsNotNull(cClass, "Could not get SomeLib.fob.C");

            Assert.AreEqual(PythonMemberType.Class, cClass.MemberType);
        }

        [TestMethod, Priority(0)]
        public void PydInPackage() {
            PythonPaths.Python27.AssertInstalled();

            var outputPath = TestData.GetTempPath(randomSubPath: true);
            Console.WriteLine("Writing to: " + outputPath);

            // run the analyzer
            using (var output = ProcessOutput.RunHiddenAndCapture("Microsoft.PythonTools.Analyzer.exe",
                "/python", PythonPaths.Python27.Path,
                "/lib", TestData.GetPath(@"TestData\PydStdLib"),
                "/version", "2.7",
                "/outdir", outputPath,
                "/indir", TestData.GetPath("CompletionDB"),
                "/log", "AnalysisLog.txt")) {
                output.Wait();
                Console.WriteLine("* Stdout *");
                foreach (var line in output.StandardOutputLines) {
                    Console.WriteLine(line);
                }
                Console.WriteLine("* Stderr *");
                foreach (var line in output.StandardErrorLines) {
                    Console.WriteLine(line);
                }
                Assert.AreEqual(0, output.ExitCode);
            }

            var fact = InterpreterFactoryCreator.CreateAnalysisInterpreterFactory(new Version(2, 7));
            var paths = new List<string> { outputPath };
            paths.AddRange(Directory.EnumerateDirectories(outputPath));
            var typeDb = new PythonTypeDatabase(fact, paths);
            var module = typeDb.GetModule("Package.winsound");
            Assert.IsNotNull(module, "Package.winsound was not analyzed");
            var package = typeDb.GetModule("Package");
            Assert.IsNotNull(package, "Could not import Package");
            var member = package.GetMember(null, "winsound");
            Assert.IsNotNull(member, "Could not get member Package.winsound");
            Assert.AreSame(module, member);

            module = typeDb.GetModule("Package._testcapi");
            Assert.IsNotNull(module, "Package._testcapi was not analyzed");
            package = typeDb.GetModule("Package");
            Assert.IsNotNull(package, "Could not import Package");
            member = package.GetMember(null, "_testcapi");
            Assert.IsNotNull(member, "Could not get member Package._testcapi");
            Assert.IsNotInstanceOfType(member, typeof(CPythonMultipleMembers));
            Assert.AreSame(module, member);

            module = typeDb.GetModule("Package.select");
            Assert.IsNotNull(module, "Package.select was not analyzed");
            package = typeDb.GetModule("Package");
            Assert.IsNotNull(package, "Could not import Package");
            member = package.GetMember(null, "select");
            Assert.IsNotNull(member, "Could not get member Package.select");
            Assert.IsInstanceOfType(member, typeof(CPythonMultipleMembers));
            var mm = (CPythonMultipleMembers)member;
            AssertUtil.ContainsExactly(mm.Members.Select(m => m.MemberType),
                PythonMemberType.Module,
                PythonMemberType.Constant,
                PythonMemberType.Class
            );
            Assert.IsNotNull(mm.Members.Contains(module));

            try {
                // Only clean up if the test passed
                Directory.Delete(outputPath, true);
            } catch { }
        }

        /// <summary>
        /// Checks that members removed or introduced in later versions show up or don't in
        /// earlier versions as appropriate.
        /// </summary>
        [TestMethod, Priority(0)]
        public void VersionedSharedDatabase() {
            var twoFive = PythonTypeDatabase.CreateDefaultTypeDatabase(new Version(2, 5));
            var twoSix = PythonTypeDatabase.CreateDefaultTypeDatabase(new Version(2, 6));
            var twoSeven = PythonTypeDatabase.CreateDefaultTypeDatabase(new Version(2, 7));
            var threeOh = PythonTypeDatabase.CreateDefaultTypeDatabase(new Version(3, 0));
            var threeOne = PythonTypeDatabase.CreateDefaultTypeDatabase(new Version(3, 1));
            var threeTwo = PythonTypeDatabase.CreateDefaultTypeDatabase(new Version(3, 2));

            // new in 2.6
            Assert.AreEqual(null, twoFive.BuiltinModule.GetAnyMember("bytearray"));
            foreach (var version in new[] { twoSix, twoSeven, threeOh, threeOne, threeTwo }) {
                Assert.AreNotEqual(version, version.BuiltinModule.GetAnyMember("bytearray"));
            }

            // new in 2.7
            Assert.AreEqual(null, twoSix.BuiltinModule.GetAnyMember("memoryview"));
            foreach (var version in new[] { twoSeven, threeOh, threeOne, threeTwo }) {
                Assert.AreNotEqual(version, version.BuiltinModule.GetAnyMember("memoryview"));
            }

            // not in 3.0
            foreach (var version in new[] { twoFive, twoSix, twoSeven }) {
                Assert.AreNotEqual(null, version.BuiltinModule.GetAnyMember("StandardError"));
            }

            foreach (var version in new[] { threeOh, threeOne, threeTwo }) {
                Assert.AreEqual(null, version.BuiltinModule.GetAnyMember("StandardError"));
            }

            // new in 3.0
            foreach (var version in new[] { twoFive, twoSix, twoSeven }) {
                Assert.AreEqual(null, version.BuiltinModule.GetAnyMember("exec"));
                Assert.AreEqual(null, version.BuiltinModule.GetAnyMember("print"));
            }

            foreach (var version in new[] { threeOh, threeOne, threeTwo }) {
                Assert.AreNotEqual(null, version.BuiltinModule.GetAnyMember("exec"));
                Assert.AreNotEqual(null, version.BuiltinModule.GetAnyMember("print"));
            }


            // new in 3.1
            foreach (var version in new[] { twoFive, twoSix, twoSeven, threeOh }) {
                Assert.AreEqual(null, version.GetModule("sys").GetMember(null, "int_info"));
            }

            foreach (var version in new[] { threeOne, threeTwo }) {
                Assert.AreNotEqual(null, version.GetModule("sys").GetMember(null, "int_info"));
            }

            // new in 3.2
            foreach (var version in new[] { twoFive, twoSix, twoSeven, threeOh, threeOne }) {
                Assert.AreEqual(null, version.GetModule("sys").GetMember(null, "setswitchinterval"));
            }

            foreach (var version in new[] { threeTwo }) {
                Assert.AreNotEqual(null, version.GetModule("sys").GetMember(null, "setswitchinterval"));
            }
        }
    }
}
