using System;
using System.Collections.Generic;
using UnityEngine;
namespace StairBuilder {
internal sealed class SharedGeometry:IDisposable {
 internal sealed class Entry {internal Mesh Mesh;internal Visual.PickGeometry Pick;internal int Users;internal long Stamp,Bytes;}
 readonly Dictionary<Mesh,Entry> byMesh=new Dictionary<Mesh,Entry>();
 readonly Dictionary<string,Entry> entries=new Dictionary<string,Entry>();long clock;internal long Hits,Misses;
 internal object Inventory{get{long bytes=0;int active=0;foreach(var e in entries.Values){bytes+=e.Bytes;if(e.Users>0)active++;}return new{entries=entries.Count,active,unused=entries.Count-active,estimatedMeshBytes=bytes};}}
 internal Mesh Get(Visual owner,string key,Func<Mesh> create){
  if(!entries.TryGetValue(key,out var e)){var m=create();long bytes=0;for(int i=0;i<m.vertexBufferCount;i++)bytes+=(long)m.vertexCount*m.GetVertexBufferStride(i);for(int i=0;i<m.subMeshCount;i++)bytes+=m.GetIndexCount(i)*4; e=new Entry{Mesh=m,Bytes=bytes};entries.Add(key,e);byMesh.Add(m,e);Misses++;}else Hits++;
  if(owner.Leases.Add(e))e.Users++;e.Stamp=++clock;return e.Mesh;
 }
 internal Visual.PickGeometry Pick(Mesh mesh){var e=byMesh[mesh];return e.Pick??(e.Pick=new Visual.PickGeometry{Vertices=mesh.vertices,Indices=mesh.triangles,Bounds=mesh.bounds});}
 // Pick chunks own immutable arrays, not the source Mesh; completed batches no longer
 // need leases on UV/shear intermediates. Bound their unused residency as well.
 internal void Retain(Visual owner,HashSet<Mesh> retained){var release=new List<Entry>();foreach(var e in owner.Leases)if(!retained.Contains(e.Mesh))release.Add(e);foreach(var e in release){owner.Leases.Remove(e);e.Users--;}}
 internal void Trim(){long bytes=0;var unused=new List<KeyValuePair<string,Entry>>();foreach(var p in entries)if(p.Value.Users==0){bytes+=p.Value.Bytes;unused.Add(p);}unused.Sort((a,b)=>a.Value.Stamp.CompareTo(b.Value.Stamp));int count=unused.Count;foreach(var p in unused){if(bytes<=64L*1024*1024&&count<=256)break;UnityEngine.Object.Destroy(p.Value.Mesh);entries.Remove(p.Key);byMesh.Remove(p.Value.Mesh);bytes-=p.Value.Bytes;count--;}}
 public void Dispose(){foreach(var e in entries.Values)UnityEngine.Object.Destroy(e.Mesh);entries.Clear();byMesh.Clear();}
}
}
