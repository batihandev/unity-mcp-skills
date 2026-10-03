using System;
using System.Linq;
using BatihanDev.UnityCliCommands.Analysis;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace BatihanDev.UnityCliCommands.Tests.Analysis
{
    public sealed class MaterialAnalysisContractTests
    {
        private string root;
        [SetUp] public void Setup(){root="Assets/MaterialAnalysis_"+Guid.NewGuid().ToString("N");System.IO.Directory.CreateDirectory(root);AssetDatabase.Refresh();}
        [TearDown] public void Cleanup(){AssetDatabase.DeleteAsset(root);}
        [Test] public void EquivalentSharesShaderColorQueueButIgnoresTextureIdentity()
        {
            var shaderPath=root+"/equivalence.shader";
            System.IO.File.WriteAllText(shaderPath,"Shader \"Hidden/AnalysisEquivalence/"+Guid.NewGuid().ToString("N")+"\" { Properties { _Color(\"Color\",Color)=(1,1,1,1) _MainTex(\"Texture\",2D)=\"white\"{} } SubShader { Pass { HLSLPROGRAM\n#pragma vertex vert\n#pragma fragment frag\nfloat4 vert(float4 position:POSITION):SV_POSITION{return position;}\nfloat4 frag():SV_Target{return float4(1,1,1,1);}\nENDHLSL } } }");
            AssetDatabase.ImportAsset(shaderPath,ImportAssetOptions.ForceSynchronousImport);var shader=AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);Assert.That(shader,Is.Not.Null);
            var firstTexture=new Texture2D(2,2);var secondTexture=new Texture2D(2,2);AssetDatabase.CreateAsset(firstTexture,root+"/firstTexture.asset");AssetDatabase.CreateAsset(secondTexture,root+"/secondTexture.asset");
            var a=new UnityEngine.Material(shader){renderQueue=2000};var b=new UnityEngine.Material(a);var c=new UnityEngine.Material(a){renderQueue=2500};a.SetTexture("_MainTex",firstTexture);b.SetTexture("_MainTex",secondTexture);AssetDatabase.CreateAsset(a,root+"/a.mat");AssetDatabase.CreateAsset(b,root+"/b.mat");AssetDatabase.CreateAsset(c,root+"/c.mat");
            a=AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(root+"/a.mat");b=AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(root+"/b.mat");c=AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(root+"/c.mat");Assert.That(new[]{a.renderQueue,b.renderQueue,c.renderQueue},Is.EqualTo(new[]{2000,2000,2500}));Assert.That(a.GetTexture("_MainTex"),Is.Not.EqualTo(b.GetTexture("_MainTex")));
            var r=MaterialAnalysisCommands.Equivalent();Assert.That(r.Ok,Is.True);var group=r.Result.Groups.Single(x=>x.Materials.Any(m=>m.Path==root+"/a.mat"));Assert.That(group.Materials.Select(x=>x.Path),Is.EquivalentTo(new[]{root+"/a.mat",root+"/b.mat"}));Assert.That(r.Result.Note,Does.Contain("texture"));Assert.That(r.Result.Note,Does.Contain("approximate"));
        }
        [Test] public void EquivalentSkipsWrongTypedColorAndUsesActualBaseColorWithoutSourceChanges()
        {
            var shaderPath=root+"/typedColor.shader";
            System.IO.File.WriteAllText(shaderPath,"Shader \"Hidden/AnalysisTypedColor/"+Guid.NewGuid().ToString("N")+"\" { Properties { _Color(\"Scalar\",Float)=1 _BaseColor(\"Color\",Color)=(1,1,1,1) } SubShader { Pass { HLSLPROGRAM\n#pragma vertex vert\n#pragma fragment frag\nfloat4 vert(float4 position:POSITION):SV_POSITION{return position;}\nfloat4 frag():SV_Target{return float4(1,1,1,1);}\nENDHLSL } } }");
            AssetDatabase.ImportAsset(shaderPath,ImportAssetOptions.ForceSynchronousImport);var shader=AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);Assert.That(shader,Is.Not.Null);
            var a=new UnityEngine.Material(shader){renderQueue=2000};a.SetFloat("_Color",1);a.SetColor("_BaseColor",Color.red);var b=new UnityEngine.Material(a);b.SetFloat("_Color",2);var c=new UnityEngine.Material(a);c.SetColor("_BaseColor",Color.blue);
            var paths=new[]{root+"/typedA.mat",root+"/typedB.mat",root+"/typedC.mat"};AssetDatabase.CreateAsset(a,paths[0]);AssetDatabase.CreateAsset(b,paths[1]);AssetDatabase.CreateAsset(c,paths[2]);
            Assert.That(a.HasProperty("_Color"),Is.True);Assert.That(a.HasColor("_Color"),Is.False);Assert.That(a.HasColor("_BaseColor"),Is.True);Assert.That(a.GetColor("_BaseColor"),Is.EqualTo(b.GetColor("_BaseColor")));Assert.That(a.GetColor("_BaseColor"),Is.Not.EqualTo(c.GetColor("_BaseColor")));
            var guarded=paths.Concat(new[]{shaderPath}).ToDictionary(x=>x,System.IO.File.ReadAllBytes);var r=MaterialAnalysisCommands.Equivalent();Assert.That(r.Ok,Is.True);var group=r.Result.Groups.Single(x=>x.Materials.Any(m=>m.Path==paths[0]));Assert.That(group.Materials.Select(x=>x.Path),Is.EquivalentTo(paths.Take(2)));
            foreach(var pair in guarded)Assert.That(System.IO.File.ReadAllBytes(pair.Key),Is.EqualTo(pair.Value));
        }
        [Test] public void NegativeGroupCapRefuses(){Assert.That(MaterialAnalysisCommands.Equivalent(-1).Error.Code,Is.EqualTo("ANALYSIS_INPUT_INVALID"));}
    }
}
