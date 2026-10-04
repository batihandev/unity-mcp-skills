using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BatihanDev.UnityCliCommands.Probing;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BatihanDev.UnityCliCommands.Tests.Foundation
{
    public sealed class ProbeCompilationContractTests
    {
        private string root;
        [SetUp] public void SetUp() { root = Path.Combine(Path.GetTempPath(), "cli-compile-contract-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); }
        [TearDown] public void TearDown() { if (Directory.Exists(root)) Directory.Delete(root, true); }
        [Test] public async Task IdenticalTypeNamesCompileIndependentlyWithoutLoadingAssemblies()
        {
            var before = LoadedProbeAssemblies();
            var result = await Run("class SameName {}", "class SameName {}");
            Assert.That(result["items"].Count(), Is.EqualTo(2));
            foreach (var row in result["items"])
            {
                Assert.That((bool)row["result"]["success"], Is.True);
                Assert.That(row["result"]["assemblyName"].Type, Is.EqualTo(JTokenType.Null));
                Assert.That((int)row["result"]["executeMs"], Is.Zero);
            }
            Assert.That(LoadedProbeAssemblies(), Is.EqualTo(before));
        }
        [Test] public async Task CrossFileDependencyFailsAndOtherSourceStillCompiles()
        {
            var result = await Run("class Depends { OtherProbe missing; }", "class OtherProbe {}");
            Assert.That((bool)result["items"][0]["result"]["success"], Is.False);
            Assert.That(result["items"][0]["result"]["diagnostics"].ToString(), Does.Contain("CS0246"));
            Assert.That((bool)result["items"][1]["result"]["success"], Is.True);
        }
        [Test] public async Task ProbeEntryAndInitializerNeverExecute()
        {
            var marker = Path.Combine(root, "must-not-exist.txt").Replace("\\", "\\\\");
            var result = await Run("class NeverRun { static NeverRun(){System.IO.File.WriteAllText(\"" + marker + "\",\"bad\");} public static void Run(){throw new System.Exception(\"must not run\");} }");
            Assert.That((bool)result["items"][0]["result"]["success"], Is.True);
            Assert.That(File.Exists(Path.Combine(root, "must-not-exist.txt")), Is.False);
        }
        [Test] public void ChangedManifestIsRejectedBeforeCompilation()
        {
            var path = Path.Combine(root,"manifest.json");File.WriteAllText(path,"{}");
            Assert.ThrowsAsync<ArgumentException>(async () => await ProbeCompilationCommands.Compile(path,new string('0',64)));
        }
        [Test] public async Task RelativeSourcePathIsRefusedWithoutCompilingDifferentBytes()
        {
            var manifest=Path.Combine(root,"manifest.json");
            var source=new JObject { ["id"]="0",["path"]="relative.cs",["sha256"]=new string('0',64) };
            File.WriteAllText(manifest,new JObject { ["schemaVersion"]=1,["sources"]=new JArray(source) }.ToString());
            var result=await ProbeCompilationCommands.Compile(manifest,Hash(File.ReadAllBytes(manifest)));
            Assert.That((bool)result["items"][0]["result"]["success"],Is.False);
            Assert.That((string)result["items"][0]["result"]["errorDetails"],Does.Contain("absolute"));
        }
        private async Task<JObject> Run(params string[] contents)
        {
            var sources = new JArray();
            for (var index=0;index<contents.Length;++index)
            {
                var path=Path.Combine(root,index+".cs");File.WriteAllText(path,contents[index]);
                sources.Add(new JObject { ["id"]=index.ToString(),["path"]=path,["sha256"]=Hash(File.ReadAllBytes(path)) });
            }
            var manifest=Path.Combine(root,"manifest.json");File.WriteAllText(manifest,new JObject { ["schemaVersion"]=1,["sources"]=sources }.ToString());
            return await ProbeCompilationCommands.Compile(manifest,Hash(File.ReadAllBytes(manifest)));
        }
        private static int LoadedProbeAssemblies() => AppDomain.CurrentDomain.GetAssemblies().Count(a=>a.GetName().Name.StartsWith("PipelineRunScript_",StringComparison.Ordinal));
        private static string Hash(byte[] bytes) { using(var hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    }
}
