using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
namespace StairBuilder {
// Translation-only batching preserves the game's packed shading attributes verbatim.
internal static class MeshBatch {
 internal sealed class Part {internal Mesh Mesh;internal Material[] Materials;internal Vector3 Position,Scale;internal Quaternion Rotation;internal bool Rail;}
 internal static List<Renderer> Build(Visual owner,List<Part> input){var output=new List<Renderer>();foreach(var step in BuildSteps(owner,input,output)){}return output;}
 sealed class SourceData {internal byte[][] Buffers;internal int[][] Indices;}
 internal static IEnumerable<int> BuildSteps(Visual owner,List<Part> input,List<Renderer> output){
  var sources=new Dictionary<Mesh,SourceData>();
  SourceData Read(Mesh mesh){
   if(sources.TryGetValue(mesh,out var found))return found;
   var data=new SourceData{Buffers=new byte[mesh.vertexBufferCount][],Indices=new int[mesh.subMeshCount][]};
   using(var read=Mesh.AcquireReadOnlyMeshData(mesh))for(int j=0;j<data.Buffers.Length;j++)data.Buffers[j]=read[0].GetVertexData<byte>(j).ToArray();
   for(int j=0;j<data.Indices.Length;j++)data.Indices[j]=mesh.GetIndices(j);
   sources.Add(mesh,data);return data;
  }
  var groups=new Dictionary<string,List<Part>>();int work=0;
  foreach(var r in input){
   if(++work%32==0)yield return 0;
   var mesh=r.Mesh;var mats=r.Materials;bool transparent=false;
   foreach(var m in mats)if(m==null||m.renderQueue>=3000)transparent=true;
   bool planarGlass=transparent;foreach(var m in mats)if(m==null||(!m.name.Contains("CSKRStairGlassPanel")&&!m.name.Contains("CSKRStairGlassWall")))planarGlass=false;
   if((transparent&&!planarGlass)||mesh.GetVertexAttributeFormat(VertexAttribute.Position)!=VertexAttributeFormat.Float32||mesh.GetVertexAttributeDimension(VertexAttribute.Position)!=3){output.Add(Create(owner,r,r.Mesh));continue;}
   string key=r.Rail?"rail:":"deck:";foreach(var m in mats)key+=m.GetInstanceID()+":";
   var q=r.Rotation;var s=r.Scale;var p=r.Position;
   // Only parallel, coplanar, disjoint authored glass panels share a renderer.
   // Curved roofs and other transparency retain individual sorting.
   if(planarGlass)key+="plane:"+Vector3.Dot(q*Vector3.right,p).ToString("R",CultureInfo.InvariantCulture)+":";
   foreach(float f in new[]{q.x,q.y,q.z,q.w,s.x,s.y,s.z})key+=f.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":";
   key+=Mathf.FloorToInt(p.x/32)+":"+Mathf.FloorToInt(p.y/32)+":"+Mathf.FloorToInt(p.z/32);
   foreach(var a in mesh.GetVertexAttributes())key+="/"+a.attribute+":"+a.format+":"+a.dimension+":"+a.stream;
   if(!groups.TryGetValue(key,out var g)){g=new List<Part>();groups.Add(key,g);}g.Add(r);
  }
  foreach(var g in groups.Values){yield return 0;if(g.Count<2){output.Add(Create(owner,g[0],g[0].Mesh));continue;}
   var first=g[0];var inverseKey=Matrix4x4.TRS(first.Position,first.Rotation,first.Scale).inverse;
   var key=new StringBuilder("batch:");
   foreach(var part in g){key.Append(part.Mesh.GetInstanceID()).Append(':');var delta=inverseKey.MultiplyPoint3x4(part.Position);for(int axis=0;axis<3;axis++)key.Append(delta[axis].ToString("R",CultureInfo.InvariantCulture)).Append(':');}
   var cached=owner.Cache.Get(owner,key.ToString(),()=>{
   var source=first.Mesh;int count=0;foreach(var r in g)count+=r.Mesh.vertexCount;
   var merged=new Mesh{name="Stair packed batch",indexFormat=IndexFormat.UInt32};
   merged.SetVertexBufferParams(count,source.GetVertexAttributes());
   int posStream=source.GetVertexAttributeStream(VertexAttribute.Position),posOffset=source.GetVertexAttributeOffset(VertexAttribute.Position);
   var indices=new List<int>[source.subMeshCount];for(int j=0;j<indices.Length;j++)indices[j]=new List<int>();
   var buffers=new byte[source.vertexBufferCount][];for(int j=0;j<buffers.Length;j++)buffers[j]=new byte[count*source.GetVertexBufferStride(j)];
   int offset=0;var inverse=Matrix4x4.TRS(first.Position,first.Rotation,first.Scale).inverse;
   foreach(var r in g){var m=r.Mesh;var delta=inverse.MultiplyPoint3x4(r.Position);
    var data=Read(m);for(int stream=0;stream<buffers.Length;stream++){
     var sourceBytes=data.Buffers[stream];var bytes=buffers[stream];int stride=m.GetVertexBufferStride(stream),begin=offset*stride;
     Buffer.BlockCopy(sourceBytes,0,bytes,begin,sourceBytes.Length);
     if(stream==posStream)for(int v=0;v<m.vertexCount;v++)for(int axis=0;axis<3;axis++){int at=begin+v*stride+posOffset+axis*4;var value=new FloatBits{Value=BitConverter.ToSingle(bytes,at)+delta[axis]};bytes[at]=(byte)value.Bits;bytes[at+1]=(byte)(value.Bits>>8);bytes[at+2]=(byte)(value.Bits>>16);bytes[at+3]=(byte)(value.Bits>>24);}
    }
    for(int j=0;j<indices.Length;j++)foreach(int i in data.Indices[j])indices[j].Add(i+offset);offset+=m.vertexCount;
   }
   for(int j=0;j<buffers.Length;j++)merged.SetVertexBufferData(buffers[j],0,0,buffers[j].Length,j,MeshUpdateFlags.DontRecalculateBounds);
   merged.subMeshCount=indices.Length;for(int j=0;j<indices.Length;j++)merged.SetTriangles(indices[j],j);merged.RecalculateBounds();
   merged.UploadMeshData(true);return merged;
   });output.Add(Create(owner,first,cached));
  }
  owner.RendererCount+=output.Count;
 }
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
 struct FloatBits {[System.Runtime.InteropServices.FieldOffset(0)]internal float Value;[System.Runtime.InteropServices.FieldOffset(0)]internal uint Bits;}
 static Renderer Create(Visual owner,Part part,Mesh mesh){
  var go=new GameObject("Stair render group");go.transform.SetParent(owner.Root.transform,false);go.transform.localPosition=part.Position;go.transform.localRotation=part.Rotation;go.transform.localScale=part.Scale;
  go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=part.Materials;renderer.renderingLayerMask=(renderer.renderingLayerMask&0xffff00ffu)|(StairRenderer.SurfaceReceiverMask<<8);renderer.shadowCastingMode=ShadowCastingMode.On;owner.Colors.Add(renderer,part.Materials,part.Rail);return renderer;
 }
}
}
