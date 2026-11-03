using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
namespace StairBuilder {
internal static class PanelShear {
 // Native source forward is -Z (assembler applies180deg). Broad glass faces
 // are X-normal, unchanged by this Y/Z shear; packed shading bytes stay intact.
 internal static Mesh Create(Mesh source,float rise){
  if(source.GetVertexAttributeFormat(VertexAttribute.Position)!=VertexAttributeFormat.Float32)throw new InvalidOperationException("Expected Float32 positions");
  int stream=source.GetVertexAttributeStream(VertexAttribute.Position),offset=source.GetVertexAttributeOffset(VertexAttribute.Position),stride=source.GetVertexBufferStride(stream);
  byte[] data;using(var read=Mesh.AcquireReadOnlyMeshData(source)){data=read[0].GetVertexData<byte>(stream).ToArray();}
  for(int i=0;i<source.vertexCount;i++){int at=i*stride+offset;float y=BitConverter.ToSingle(data,at+4),z=BitConverter.ToSingle(data,at+8);Buffer.BlockCopy(BitConverter.GetBytes(y+z*rise),0,data,at+4,4);}
  var mesh=UnityEngine.Object.Instantiate(source);mesh.SetVertexBufferData(data,0,0,data.Length,stream,MeshUpdateFlags.DontValidateIndices);mesh.RecalculateBounds();return mesh;
 }
}
}
