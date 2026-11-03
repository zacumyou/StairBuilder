using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace StairBuilder {
public partial class StairTool {
 struct RoadQuery:INativeQuadTreeIterator<Entity,QuadTreeBoundsXZ>{public Bounds2 Bounds;public NativeList<Entity> Items;public bool Intersect(QuadTreeBoundsXZ b)=>MathUtils.Intersect(Bounds,b.m_Bounds.xz);public void Iterate(QuadTreeBoundsXZ b,Entity e){if(Intersect(b))Items.Add(e);}}
 bool RoadEdge(ref Vector3 point){
  float3 raw=point;float best=Mod.Options.SnapRadius;float3 target=raw;bool found=false;
  void Curve(Bezier4x3 curve){float distance=MathUtils.Distance(curve.xz,raw.xz,out float t);var p=MathUtils.Position(curve,t);
   if(distance>best||math.abs(p.y-raw.y)>2)return;best=distance;target=p;found=true;}
  using(var items=new NativeList<Entity>(Allocator.Temp)){
   float range=Mod.Options.SnapRadius+1;var query=new RoadQuery{Bounds=new Bounds2(raw.xz-range,raw.xz+range),Items=items};
   var tree=World.GetOrCreateSystemManaged<Game.Net.SearchSystem>().GetNetSearchTree(true,out var job);job.Complete();tree.Iterate(ref query);
   foreach(var entity in items){if(!EntityManager.HasComponent<Game.Net.Road>(entity)||EntityManager.HasComponent<Deleted>(entity)||EntityManager.HasComponent<Game.Tools.Temp>(entity))continue;
    if(EntityManager.HasComponent<Game.Net.EdgeGeometry>(entity)){var g=EntityManager.GetComponentData<Game.Net.EdgeGeometry>(entity);Curve(g.m_Start.m_Left);Curve(g.m_Start.m_Right);Curve(g.m_End.m_Left);Curve(g.m_End.m_Right);}
   }
  }
  if(found)point=target;return found;
 }
}
}
