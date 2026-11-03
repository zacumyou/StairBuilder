using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
namespace StairBuilder {
internal static class ScaledModuleUV {
 // Clone only when scaling a full-tile material. Never reinterpret packed normals/tangents.
 internal static Mesh Create(Mesh source,Vector3 scale,Vector2 phase=default){
  var vertices=source.vertices;var uv=source.uv;var indices=source.triangles;
  if(uv.Length!=vertices.Length)throw new InvalidOperationException("Missing module UV0");
  var factors=new Vector2[uv.Length];var weights=new int[uv.Length];
  for(int i=0;i<indices.Length;i+=3){int a=indices[i],b=indices[i+1],c=indices[i+2];
   var e1=vertices[b]-vertices[a];var e2=vertices[c]-vertices[a];var d1=uv[b]-uv[a];var d2=uv[c]-uv[a];float det=d1.x*d2.y-d1.y*d2.x;if(Mathf.Abs(det)<1e-12f)continue;
   var u=(e1*d2.y-e2*d1.y)/det;var v=(e2*d1.x-e1*d2.x)/det;
   if(u.sqrMagnitude<1e-12f||v.sqrMagnitude<1e-12f)continue;
   var factor=new Vector2(Vector3.Scale(u,scale).magnitude/u.magnitude,Vector3.Scale(v,scale).magnitude/v.magnitude);
   factors[a]+=factor;factors[b]+=factor;factors[c]+=factor;weights[a]++;weights[b]++;weights[c]++;
  }
  var format=source.GetVertexAttributeFormat(VertexAttribute.TexCoord0);
  if(format!=VertexAttributeFormat.Float32&&format!=VertexAttributeFormat.Float16)throw new InvalidOperationException("Unsupported module UV format "+format);
  int stream=source.GetVertexAttributeStream(VertexAttribute.TexCoord0),offset=source.GetVertexAttributeOffset(VertexAttribute.TexCoord0),stride=source.GetVertexBufferStride(stream);
  byte[] bytes;using(var data=Mesh.AcquireReadOnlyMeshData(source)){bytes=data[0].GetVertexData<byte>(stream).ToArray();}
  for(int i=0;i<uv.Length;i++)if(weights[i]>0){var factor=factors[i]/weights[i];var p=Vector2.Scale(uv[i]-Vector2.one*.5f,factor)+Vector2.one*.5f+phase;
   for(int axis=0;axis<2;axis++){int at=i*stride+offset+axis*(format==VertexAttributeFormat.Float32?4:2);var value=format==VertexAttributeFormat.Float32?BitConverter.GetBytes(p[axis]):BitConverter.GetBytes(new half(p[axis]).value);Buffer.BlockCopy(value,0,bytes,at,value.Length);}}
  var mesh=UnityEngine.Object.Instantiate(source);mesh.name=source.name+" metric UV";
  mesh.SetVertexBufferData(bytes,0,0,bytes.Length,stream,MeshUpdateFlags.DontRecalculateBounds|MeshUpdateFlags.DontValidateIndices);return mesh;
 }
}
}
