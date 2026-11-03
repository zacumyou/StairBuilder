using System;
using System.Collections.Generic;
using Game.Common;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace StairBuilder {
internal sealed class NativePaths {
 readonly EntityManager em;readonly PrefabSystem prefabs;Entity walking,access,roofLight;bool warnedRoofLight;
 internal NativePaths(EntityManager em,PrefabSystem prefabs){this.em=em;this.prefabs=prefabs;}
 internal bool Available{
  get{
   if(em.Exists(walking)&&em.Exists(access))return true;
   walking=access=Entity.Null;
   using(var q=em.CreateEntityQuery(ComponentType.ReadOnly<PrefabData>()))
   using(var entities=q.ToEntityArray(Allocator.Temp)){
    foreach(var e in entities){
     if(!em.HasComponent<AreaData>(e)&&!em.HasComponent<ObjectData>(e))continue;
     if(!prefabs.TryGetPrefab<PrefabBase>(e,out var p)||p==null)continue;
     if(p.name=="RoofLight01"&&em.HasComponent<ObjectData>(e))roofLight=e;
     if(p.name=="Walking Area"&&em.HasComponent<AreaData>(e))walking=e;
     if(p.name=="Pedestrian Access Location"&&em.HasComponent<ObjectData>(e)&&em.HasComponent<RouteConnectionData>(e))access=e;
    }
   }return walking!=Entity.Null&&access!=Entity.Null;
  }
 }
 void Set<T>(Entity e,T value) where T:unmanaged,IComponentData {if(em.HasComponent<T>(e))em.SetComponentData(e,value);else em.AddComponentData(e,value);}
 void Updated(Entity e){if(!em.HasComponent<Updated>(e))em.AddComponent<Updated>(e);}
 Entity Marker(Vector3 position,float elevation,Quaternion rotation){
  var e=em.CreateEntity(em.GetComponentData<ObjectData>(access).m_Archetype);
  Set(e,new PrefabRef(access));Set(e,new Game.Objects.Transform(position,new quaternion(rotation.x,rotation.y,rotation.z,rotation.w)));
  Set(e,new Game.Objects.Elevation(elevation,0));
  if(em.HasComponent<Game.Common.PseudoRandomSeed>(e))Set(e,new Game.Common.PseudoRandomSeed((ushort)1));
  Updated(e);return e;
 }
 // Native prefab archetypes include Created/Updated. Changes run in ToolUpdate,
 // never from UI callbacks, then native geometry/search/path systems consume them.
 internal void Create(Stair s,IEnumerable<Stair> neighbors=null){
 using(var perf=PerformanceLog.Begin("native.create",s.Id,false)){try{
  if(!Available)throw new InvalidOperationException("Walking Area / Pedestrian Access Location prefabs unavailable");
  if(!RoutePlan.Build(s,out var plan,out var error))throw new InvalidOperationException(error);
  s.Area=s.AccessA=s.AccessB=Entity.Null;s.ExtraPaths=new List<Entity>();
  try{
   var start=s.A+Vector3.up*WalkProfile.Clearance;var end=s.B+Vector3.up*WalkProfile.Clearance;
   var seen=new List<Vector3>{start,end};
   var first=s.Point(1)-s.A;first.y=0;var last=s.B-s.Point(s.Points.Count-2);last.y=0;
   s.AccessA=Marker(start,s.OffsetA+WalkProfile.Clearance,Quaternion.LookRotation(first));s.AccessB=Marker(end,s.OffsetB+WalkProfile.Clearance,Quaternion.LookRotation(-last));
   void Access(Vector3 p,Vector3 forward){foreach(var old in seen)if((old-p).sqrMagnitude<.0001f)return;seen.Add(p);var terrain=em.World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();float elevation=p.y-Game.Simulation.TerrainUtils.SampleHeight(ref terrain,(float3)p);s.ExtraPaths.Add(Marker(p,elevation,Quaternion.LookRotation(forward)));}
   void Area(Vector3 a,Vector3 b,float width){
    var forward=b-a;forward.y=0;forward.Normalize();var right=Vector3.Cross(Vector3.up,forward)*(width*.5f-.30f);
    var entity=em.CreateEntity(em.GetComponentData<AreaData>(walking).m_Archetype);
    if(s.Area==Entity.Null)s.Area=entity;else s.ExtraPaths.Add(entity);
    Set(entity,new PrefabRef(walking));Set(entity,new Game.Areas.Area(Game.Areas.AreaFlags.Complete));
    var nodes=em.GetBuffer<Game.Areas.Node>(entity);nodes.Clear();
    var outline=new[]{a-right,a+right,b+right,b-right};
    foreach(var p in outline)nodes.Add(new Game.Areas.Node((float3)p,0));
    Updated(entity);Access(a,forward);Access(b,-forward);
   }
   var layout=WalkingPlan.Build(plan,s.Width);
   // Bridge the existing platform's 30cm side inset with walking data only.
   if(neighbors!=null)foreach(int endpoint in new[]{0,s.Points.Count-1}){
    bool contact=false;foreach(var other in neighbors)if(BranchPlan.EdgeContact(s,endpoint,other)){contact=true;break;}
    if(contact){var p=s.Point(endpoint)+Vector3.up*WalkProfile.Clearance;var direction=s.Point(endpoint==0?1:endpoint-1)-s.Point(endpoint);direction.y=0;direction.Normalize();layout.Add(p-direction*.35f,p,s.Width);}
   }
   layout.Merge();
   foreach(var region in layout.Areas)Area(region.A,region.B,region.Width);
   foreach(var point in layout.Junctions)Access(point,first.normalized);
   if(s.Style==3){
    if(roofLight!=Entity.Null&&em.Exists(roofLight)){
     var lightPositions=new List<Vector3>();
     void Light(Vector3 a,Vector3 b){
      var delta=b-a;if(delta.magnitude<3f)return;var rotation=Quaternion.LookRotation(delta);
      var position=(a+b)*.5f+Vector3.up*2.6f+(rotation*Vector3.up)*.12f;
      foreach(var old in lightPositions)if((old-position).sqrMagnitude<1f)return;
      var light=em.CreateEntity(em.GetComponentData<ObjectData>(roofLight).m_Archetype);s.ExtraPaths.Add(light);
      Set(light,new PrefabRef(roofLight));Set(light,new Game.Objects.Transform(position,new quaternion(rotation.x,rotation.y,rotation.z,rotation.w)));
      var terrain=em.World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
      Set(light,new Game.Objects.Elevation(position.y-Game.Simulation.TerrainUtils.SampleHeight(ref terrain,(float3)position),0));
      if(em.HasComponent<Game.Common.PseudoRandomSeed>(light))Set(light,new Game.Common.PseudoRandomSeed((ushort)1));
      Updated(light);lightPositions.Add(position);
     }
     foreach(var run in plan.Runs)Light(run.A,run.B);
     foreach(var pad in plan.Pads)Light(pad.Center-pad.Forward*pad.Length*.5f,pad.Center+pad.Forward*pad.Length*.5f);
    }else if(!warnedRoofLight){warnedRoofLight=true;Mod.Log.Warn("RoofLight01 prefab unavailable; tunnel ceiling lights skipped");}
   }
   PerformanceLog.Event("native.areas",s.Id,layout.Areas.Count);PerformanceLog.Event("native.access",s.Id,seen.Count);PerformanceLog.Event("native.references",s.Id,3+s.ExtraPaths.Count);
   Mod.Log.Info("Walking topology stair="+s.Id+" areas="+layout.Areas.Count+" locations="+seen.Count);
   // Independent placed entities; never make marker-prefab children.
  }catch{Delete(s);throw;}
 
 }catch{perf?.Fail();throw;}}
 }
 internal string Describe(Stair s)=>"area="+State(s.Area)+", accessA="+State(s.AccessA)+", accessB="+State(s.AccessB);
 string State(Entity e)=>e==Entity.Null?"null":!em.Exists(e)?"gone":em.HasComponent<Deleted>(e)?"deleted":"alive";
 internal bool Alive(Stair s){if(s==null||!Alive(s.Area)||!Alive(s.AccessA)||!Alive(s.AccessB))return false;foreach(var e in s.ExtraPaths)if(!Alive(e))return false;return true;}
 bool Alive(Entity e)=>e!=Entity.Null&&em.Exists(e)&&!em.HasComponent<Deleted>(e);
 internal void Delete(Stair s){
 using(var perf=PerformanceLog.Begin("native.delete",s.Id,false)){try{foreach(var e in new[]{s.AccessB,s.Area,s.AccessA})if(Alive(e))em.AddComponent<Deleted>(e);foreach(var e in s.ExtraPaths)if(Alive(e))em.AddComponent<Deleted>(e);
 }catch{perf?.Fail();throw;}}
 }
 internal string Connection(Stair s){
  if(!Alive(s))return "missing";
  bool Connected(Entity e){
   if(!em.HasComponent<Game.Routes.AccessLane>(e)||!em.HasComponent<Game.Routes.RouteLane>(e))return false;
   var a=em.GetComponentData<Game.Routes.AccessLane>(e);var r=em.GetComponentData<Game.Routes.RouteLane>(e);
   bool InArea(Entity lane){
    for(int i=0;i<8&&Alive(lane);i++){
     if(lane==s.Area||s.ExtraPaths.Contains(lane))return true;
     if(!em.HasComponent<Owner>(lane))break;
     lane=em.GetComponentData<Owner>(lane).m_Owner;
    }return false;
   }
   return Alive(a.m_Lane)&&Alive(r.m_EndLane)&&InArea(a.m_Lane)!=InArea(r.m_EndLane);
  }
  return Connected(s.AccessA)&&Connected(s.AccessB)?"linksFound":"linksPending";
 }
}
}

