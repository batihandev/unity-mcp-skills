using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BatihanDev.UnityCliCommands.Foundation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BatihanDev.UnityCliCommands.Tests
{
    public sealed class PackageUnchangedRecoveryContractTests
    {
        [Test] public void LostRequestWithExactOriginalStateCanAuthorizeNoMutationInverse()
        {
            var root=Path.Combine(Path.GetTempPath(),"unity-package-unchanged-"+Guid.NewGuid().ToString("N"));
            try
            {
                const string name="com.example.unchanged-fixture";
                var source=Path.Combine(root,"Library/PackageCache",name+"@1.0.0");Directory.CreateDirectory(source);Directory.CreateDirectory(Path.Combine(root,"Packages"));Directory.CreateDirectory(Path.Combine(root,"Assets"));
                File.WriteAllText(Path.Combine(source,"package.json"),"{\"name\":\""+name+"\",\"version\":\"1.0.0\"}",new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(source,"keep.txt"),"unchanged-original\n",new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(source,"keep.txt.meta"),"fileFormatVersion: 2\nguid: 1234567890abcdef1234567890abcdef\n",new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(root,"Packages/manifest.json"),"{\"dependencies\":{\""+name+"\":\"1.0.0\"}}",new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(root,"Packages/packages-lock.json"),"{\"dependencies\":{\""+name+"\":{\"version\":\"1.0.0\",\"source\":\"registry\",\"depth\":0,\"dependencies\":{}}}}",new UTF8Encoding(false));
                var files=new JObject();foreach(var path in Directory.GetFiles(source))files[Path.GetFileName(path)]=State(path,false);
                var package=new JObject { ["name"]=name,["version"]="1.0.0",["source"]="Registry",["resolvedPath"]=source,["isDirectDependency"]=true };
                var journal=new JObject { ["schema"]="unity.package.operation@1",["project"]=root,["kind"]="embed",["name"]=name,["version"]="1.0.0",["operationId"]=Guid.NewGuid().ToString("N"),["state"]="interrupted",["requestUnresolved"]=true,["observerDetached"]=true,["editorPid"]=int.MaxValue,["nativeProcessStartTicks"]="1",["originalPackage"]=package.DeepClone(),
                    ["sourceInventory"]=new JObject { ["files"]=files,["directories"]=new JArray(".") },["destinationBefore"]=new JObject { ["files"]=new JObject(),["directories"]=new JArray() },["manifestBefore"]=State(Path.Combine(root,"Packages/manifest.json")),["lockBefore"]=State(Path.Combine(root,"Packages/packages-lock.json")) };
                var before=Directory.GetFiles(root,"*",SearchOption.AllDirectories).ToDictionary(path=>path,path=>Convert.ToBase64String(File.ReadAllBytes(path)));
                var owner=typeof(ProjectPathPolicy).Assembly.GetType("BatihanDev.UnityCliCommands.Packages.PackageAuthoringCommands");Assert.That(owner,Is.Not.Null);
                var method=owner.GetMethod("PreviewLifecycleInverse",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);Assert.That(method,Is.Not.Null,"Missing lifecycle-bound inverse planner");
                JObject plan=null;
                Assert.DoesNotThrow(()=>{try { plan=(JObject)method.Invoke(null,new object[] {root,journal,package}); } catch(TargetInvocationException exception) { throw exception.InnerException; }},"Exact original state after lifecycle barrier must have a safe no-mutation inverse.");
                Assert.That((bool)plan["observedStateInverse"],Is.True);Assert.That((bool)plan["observedNoMutation"],Is.True);Assert.That(plan["destinationAfter"]["files"].Count(),Is.Zero);Assert.That(plan["destinationAfter"]["directories"].Count(),Is.Zero);Assert.That((bool)journal["requestUnresolved"],Is.True);Assert.That(journal["nativeStatus"],Is.Null);
                foreach(var pair in before)Assert.That(Convert.ToBase64String(File.ReadAllBytes(pair.Key)),Is.EqualTo(pair.Value));
                Assert.That(Directory.Exists(Path.Combine(root,"Packages",name)),Is.False);
            }
            finally { if(Directory.Exists(root))Directory.Delete(root,true); }
        }
        private static JObject State(string path,bool bytes=true)
        {
            var content=File.ReadAllBytes(path);string hash;using(var algorithm=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(algorithm.ComputeHash(content)).Replace("-","").ToLowerInvariant();var value=new JObject { ["exists"]=true,["sha256"]=hash,["length"]=content.Length };if(bytes)value["bytesBase64"]=Convert.ToBase64String(content);return value;
        }
    }
}
