using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
namespace StairBuilder {
internal static class CurbMiter {
 // Source forward is -Z; independent end shears meet at a shared curb miter.
 // Keep the imported packed shading while trimming the two adjoining ends.
 internal static Mesh Create(Mesh source,float start,float end){
  if(source.GetVertexAttributeFormat(VertexAttribute.Position)!=VertexAttributeFormat.Float32)throw new InvalidOperationException("Expected Float32 beam positions");
  int stream=source.GetVertexAttributeStream(VertexAttribute.Position),offset=source.GetVertexAttributeOffset(VertexAttribute.Position),stride=source.GetVertexBufferStride(stream);
  byte[] data;using(var read=Mesh.AcquireReadOnlyMeshData(source)){data=read[0].GetVertexData<byte>(stream).ToArray();}
  var bounds=source.bounds;float length=Mathf.Max(.0001f,bounds.size.z);
  for(int i=0;i<source.vertexCount;i++){int at=i*stride+offset;float x=BitConverter.ToSingle(data,at),z=BitConverter.ToSingle(data,at+8);float t=Mathf.Clamp01((bounds.max.z-z)/length);float slope=Mathf.Lerp(start,end,t);Buffer.BlockCopy(BitConverter.GetBytes(z+x*slope),0,data,at+8,4);}
  var mesh=UnityEngine.Object.Instantiate(source);mesh.SetVertexBufferData(data,0,0,data.Length,stream,MeshUpdateFlags.DontValidateIndices);mesh.RecalculateBounds();return mesh;
 }
}
}
