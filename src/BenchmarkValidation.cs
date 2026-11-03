using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace StairBuilder {
public partial class StairSystem {
 readonly List<Entity> surfaceObjects=new List<Entity>();
 object SurfaceObjectState(){var result=new List<object>();foreach(var e in surfaceObjects){bool alive=EntityManager.Exists(e);Vector3 position=alive&&EntityManager.HasComponent<Game.Objects.Transform>(e)?(Vector3)EntityManager.GetComponentData<Game.Objects.Transform>(e).m_Position:Vector3.zero;result.Add(new{index=e.Index,version=e.Version,alive,position=new[]{position.x,position.y,position.z}});}return result;}

 void VerifyResidencyPick(Visual before,Visual after){
  var box=before.InstanceBounds;
  foreach(bool surface in new[]{false,true})foreach(var direction in new[]{Vector3.down,Vector3.right,Vector3.forward,new Vector3(.2f,-1,.3f).normalized}){
   var ray=new Ray(before.Origin+box.center-direction*(box.size.magnitude+10),direction);
   bool a=before.Hit(ray,out var da,out _,out var na,surface),b=after.Hit(ray,out var db,out _,out var nb,surface);
   if(a!=b||(a&&(Mathf.Abs(da-db)>.003f||Vector3.Dot(na,nb)<.999f)))throw new InvalidOperationException("Residency changed exact center/edge picking");
  }
 }
 void VerifyPick(Stair stair){
  var v=visuals[stair.Id];var box=v.InstanceBounds;
  foreach(bool surfaces in new[]{false,true})foreach(var direction in new[]{Vector3.down,Vector3.right,Vector3.forward,new Vector3(.2f,-1,.3f).normalized}){
   // Avoid testing exactly on a shared triangle edge: world-space float rounding
   // can turn that degenerate case into a different triangle hit. Residency parity
   // separately checks the unshifted center rays before/after rebuilding.
   var ray=new Ray(v.Origin+box.center+new Vector3(.013f,.017f,.019f)-direction*(box.size.magnitude+10),direction);
   bool actual=v.Hit(ray,out var distance,out _,out var normal,surfaces);float expected=float.PositiveInfinity;
   foreach(var chunk in v.PickChunks){
    if(chunk.SurfaceOnly&&!surfaces)continue;
    var matrix=Matrix4x4.TRS(v.Origin,v.Rotation,Vector3.one)*chunk.Inverse.inverse;var vertices=chunk.Geometry.Vertices;var indices=chunk.Geometry.Indices;
    for(int i=0;i<indices.Length;i+=3){
     var a=matrix.MultiplyPoint3x4(vertices[indices[i]]);var e=matrix.MultiplyPoint3x4(vertices[indices[i+1]])-a;var f=matrix.MultiplyPoint3x4(vertices[indices[i+2]])-a;
     var p=Vector3.Cross(ray.direction,f);float det=Vector3.Dot(e,p);if(Mathf.Abs(det)<1e-9f)continue;
     var s=ray.origin-a;float u=Vector3.Dot(s,p)/det;if(u<-.00001f||u>1.00001f)continue;var q=Vector3.Cross(s,e);float w=Vector3.Dot(ray.direction,q)/det;if(w<-.00001f||w+u>1.00001f)continue;
     float d=Vector3.Dot(f,q)/det;if(d>0&&d<expected)expected=d;
    }
   }
   if(actual!=!float.IsPositiveInfinity(expected)||(actual&&(Mathf.Abs(distance-expected)>.003f||Mathf.Abs(normal.magnitude-1)>.001f)))throw new InvalidOperationException("Pick mismatch stair "+stair.Id+" surface="+surfaces+" ray="+ray+" coarse="+v.Coarse+" expected="+expected+" actual="+distance);
   verifiedRays++;
  }
 }
 object ProbeSurface(){
  var prefabs=World.GetOrCreateSystemManaged<PrefabSystem>();var em=EntityManager;var candidates=new List<string>();ObjectPrefab prop=null,decal=null;
  using(var query=em.CreateEntityQuery(ComponentType.ReadOnly<PrefabData>(),ComponentType.ReadOnly<PlaceableObjectData>(),ComponentType.ReadOnly<ObjectData>()))
  using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var entity in entities){
   if(!prefabs.TryGetPrefab<ObjectPrefab>(entity,out var prefab)||!SurfaceBridge.EligiblePrefab(em,entity))continue;
   if(prefab.name.IndexOf("Bench",StringComparison.OrdinalIgnoreCase)>=0&&prop==null)prop=prefab;
   if(prefab.name.IndexOf("Decal",StringComparison.OrdinalIgnoreCase)>=0){if(candidates.Count<25)candidates.Add(prefab.name);if(decal==null)decal=prefab;}
  }
  if(prop==null||decal==null)throw new InvalidOperationException("No eligible bench/decal prefab; candidates="+string.Join(",",candidates));
  var reports=new List<object>();var stair=Stairs[Math.Min(3,Stairs.Count-1)];RoutePlan.Build(stair,out var plan,out _);var center=plan.Pads.Count>0?plan.Pads[0].Center:stair.A+Vector3.forward*.45f;var ray=new Ray(center+Vector3.up*30,Vector3.down);
  if(!SurfaceHit(ray,out var distance,out var normal))throw new InvalidOperationException("No stair surface at probe");
  var hit=ray.GetPoint(distance);
  surfaceProbe=new{hit=new[]{hit.x,hit.y,hit.z},reports,candidates,receiverMask=StairRenderer.SurfaceReceiverMask};
  foreach(var prefab in new[]{prop,decal}){
   if(prefab==null)continue;
   ray=new Ray(center+Vector3.right*(prefab==prop?-.8f:.8f)+Vector3.up*30,Vector3.down);
   if(!SurfaceHit(ray,out distance,out normal))throw new InvalidOperationException("Missing landing surface");hit=ray.GetPoint(distance);
   var cp=SurfaceBridge.Probe(this,prefab,ray,true);var entity=prefabs.GetEntity(prefab);var placement=em.GetComponentData<PlaceableObjectData>(entity);
   float expected=hit.y+placement.m_PlacementOffset.y;
   if(Mathf.Abs(cp.m_Position.y-expected)>.02f)throw new InvalidOperationException("Native snap height mismatch "+prefab.name+" expected="+expected+" actual="+cp.m_Position.y);
   var disabled=SurfaceBridge.Probe(this,prefab,ray,false);
   if(math.distancesq(disabled.m_HitPosition,cp.m_HitPosition)<.0001f)throw new InvalidOperationException("Disabled surface snap still hit stair");
   // Real native object archetype consumes the placement result, without faking a stair owner.
   var obj=em.CreateEntity(em.GetComponentData<ObjectData>(entity).m_Archetype);
   void Set<T>(T data) where T:unmanaged,IComponentData {if(em.HasComponent<T>(obj))em.SetComponentData(obj,data);else em.AddComponentData(obj,data);}
   Set(new PrefabRef(entity));Set(new Game.Objects.Transform(cp.m_Position,cp.m_Rotation));Set(new Game.Objects.Elevation(cp.m_Elevation,0));Set(new Game.Common.PseudoRandomSeed((ushort)1));Set(new Game.Common.Updated());
   surfaceObjects.Add(obj);
   reports.Add(new{hit=new[]{hit.x,hit.y,hit.z},prefab=prefab.name,index=obj.Index,version=obj.Version,expectedY=expected,actualY=cp.m_Position.y,elevation=cp.m_Elevation,normal=new[]{normal.x,normal.y,normal.z},rotation=new[]{cp.m_Rotation.value.x,cp.m_Rotation.value.y,cp.m_Rotation.value.z,cp.m_Rotation.value.w}});
  }
  string groundSurface="";
  using(var query=em.CreateEntityQuery(ComponentType.ReadOnly<PrefabData>(),ComponentType.ReadOnly<SurfaceData>(),ComponentType.ReadOnly<AreaData>()))
  using(var entities=query.ToEntityArray(Allocator.Temp))foreach(var entity in entities){
   if(!prefabs.TryGetPrefab<PrefabBase>(entity,out var prefab)||prefab.name.IndexOf("Pavement",StringComparison.OrdinalIgnoreCase)<0)continue;
   if(!prefab.TryGet<RenderedArea>(out var rendered)||rendered.m_DecalLayerMask!=Game.Rendering.DecalLayers.Terrain)continue;
   var area=em.CreateEntity(em.GetComponentData<AreaData>(entity).m_Archetype);em.SetComponentData(area,new PrefabRef(entity));em.SetComponentData(area,new Game.Areas.Area(Game.Areas.AreaFlags.Complete));
   var nodes=em.GetBuffer<Game.Areas.Node>(area);nodes.Clear();var terrain=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
   foreach(var offset in new[]{new Vector3(-3,0,-2),new Vector3(3,0,-2),new Vector3(3,0,10),new Vector3(-3,0,10)}){var p=stair.A+offset;p.y=Game.Simulation.TerrainUtils.SampleHeight(ref terrain,(float3)p);nodes.Add(new Game.Areas.Node((float3)p,0));}
   if(!em.HasComponent<Game.Common.Updated>(area))em.AddComponent<Game.Common.Updated>(area);groundSurface=prefab.name;break;
  }
  return new{groundSurface,hit=new[]{hit.x,hit.y,hit.z},reports,candidates,receiverMask=StairRenderer.SurfaceReceiverMask};
 }
}
}
