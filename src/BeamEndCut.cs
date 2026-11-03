using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
namespace StairBuilder {
internal static class BeamEndCut {
 // Cut rotated I-beam ends vertically so landing joins have no triangular gaps.
 // Packed side/top shading stays intact; rigid end plates cover the changed end normals.
 internal static Mesh Create(Mesh source,float slope){
  if(source.GetVertexAttributeFormat(VertexAttribute.Position)!=VertexAttributeFormat.Float32)throw new InvalidOperationException("Expected Float32 beam positions");
  int stream=source.GetVertexAttributeStream(VertexAttribute.Position),offset=source.GetVertexAttributeOffset(VertexAttribute.Position),stride=source.GetVertexBufferStride(stream);
  byte[] data;using(var read=Mesh.AcquireReadOnlyMeshData(source)){data=read[0].GetVertexData<byte>(stream).ToArray();}
  for(int i=0;i<source.vertexCount;i++){int at=i*stride+offset;float y=BitConverter.ToSingle(data,at+4),z=BitConverter.ToSingle(data,at+8);Buffer.BlockCopy(BitConverter.GetBytes(z-y*slope),0,data,at+8,4);}
  var mesh=UnityEngine.Object.Instantiate(source);mesh.SetVertexBufferData(data,0,0,data.Length,stream,MeshUpdateFlags.DontValidateIndices);mesh.RecalculateBounds();return mesh;
 }
}
}
