using System;
using System.Linq;
using System.Reflection;
using Game.Input;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
namespace StairBuilder {
// Supplies geometry to the native placement pipeline, without inventing an ECS owner.
internal static class SurfaceBridge {
 const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Event(string name,string detail)=>Mod.Log.Info(name+": "+detail);
 static void Failure(string name,Exception e)=>Mod.Log.Error(e,name);
 static Ray? probeRay;static ObjectToolSystem probeTool;
 static object harmony;static Type harmonyType;static FieldInfo points,onMask,offMask;static bool failed,logged;
 sealed class SnapState {internal Snap On,Off;internal float3 Hit;internal bool Changed;}
 internal static void Install(Mod owner){try{
  points=typeof(ObjectToolSystem).GetField("m_ControlPoints",Instance);onMask=typeof(ToolBaseSystem).GetField("m_SnapOnMask",Instance);offMask=typeof(ToolBaseSystem).GetField("m_SnapOffMask",Instance);
  var ray=typeof(ToolBaseSystem).GetMethod("GetRaycastResult",Instance,null,new[]{typeof(ControlPoint).MakeByRefType(),typeof(bool).MakeByRefType()},null);
  var raySingle=typeof(ToolBaseSystem).GetMethod("GetRaycastResult",Instance,null,new[]{typeof(ControlPoint).MakeByRefType()},null);
  var snap=typeof(ObjectToolSystem).GetMethod("SnapControlPoint",Instance);
  if(points?.FieldType!=typeof(NativeList<ControlPoint>)||onMask==null||offMask==null||ray==null||raySingle==null||snap==null)throw new MissingMemberException("Object surface placement contract changed");
  var assembly=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name=="0Harmony");
  if(assembly==null){if(!GameManager.instance.modManager.TryGetExecutableAsset(owner,out var asset))throw new InvalidOperationException("Mod path unavailable");assembly=Assembly.LoadFrom(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(asset.path),"0Harmony.dll"));}
  harmonyType=assembly.GetType("HarmonyLib.Harmony",true);harmony=Activator.CreateInstance(harmonyType,"zacum.StairBuilder.Surface");
  object Method(string name)=>Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod",true),typeof(SurfaceBridge).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic));
  var patch=harmonyType.GetMethods().Single(m=>m.Name=="Patch"&&m.GetParameters().Length==5);
  patch.Invoke(harmony,new object[]{ray,null,Method(nameof(Raycast)),null,null});
  patch.Invoke(harmony,new object[]{raySingle,null,Method(nameof(Raycast)),null,null});
  patch.Invoke(harmony,new object[]{snap,Method(nameof(BeforeSnap)),Method(nameof(AfterSnap)),null,Method(nameof(FinishSnap))});
  failed=false;logged=false;Event("surface.connected","Native Create placement; ObjectSurface toggle; standalone props/decals");
 }catch(Exception e){Failure("surface.unavailable",e);Dispose();}}
 static bool Eligible(ToolBaseSystem tool,out ObjectToolSystem objects,out Snap snap){
  objects=tool as ObjectToolSystem;snap=Snap.None;
  if(failed||objects==null||objects.actualMode!=ObjectToolSystem.Mode.Create||objects.prefab==null||(probeTool!=tool&&tool.World.GetExistingSystemManaged<ToolSystem>()?.activeTool!=tool))return false;
  var input=InputManager.instance;if(probeTool!=tool&&(!Application.isFocused||input.hasInputFieldFocus||input.overlayActive||!input.controlOverWorld))return false;
  snap=ToolBaseSystem.GetActualSnap(tool.selectedSnap,(Snap)onMask.GetValue(tool),(Snap)offMask.GetValue(tool));if((snap&Snap.ObjectSurface)==0)return false;
  var prefab=tool.World.GetExistingSystemManaged<PrefabSystem>().GetEntity(objects.prefab);
  return EligiblePrefab(tool.EntityManager,prefab);
 }
 internal static bool EligiblePrefab(EntityManager em,Entity prefab){
  if(!em.HasComponent<PlaceableObjectData>(prefab)||em.HasComponent<BuildingData>(prefab)||em.HasComponent<TreeData>(prefab)||em.HasComponent<NetObjectData>(prefab)||em.HasComponent<MovingObjectData>(prefab)||em.HasComponent<AssetStampData>(prefab))return false;
  if(em.HasBuffer<SubNet>(prefab)&&em.GetBuffer<SubNet>(prefab,true).Length>0)return false;
  if(em.HasBuffer<Game.Prefabs.SubLane>(prefab)&&em.GetBuffer<Game.Prefabs.SubLane>(prefab,true).Length>0)return false;
  var flags=em.GetComponentData<PlaceableObjectData>(prefab).m_Flags;
  return (flags&(Game.Objects.PlacementFlags.Floating|Game.Objects.PlacementFlags.Hovering|Game.Objects.PlacementFlags.Shoreline|Game.Objects.PlacementFlags.RoadSide|Game.Objects.PlacementFlags.RoadEdge|Game.Objects.PlacementFlags.RoadNode|Game.Objects.PlacementFlags.NetObject|Game.Objects.PlacementFlags.Attached))==0;
 }

 static bool Hit(ToolBaseSystem tool,out float3 position,out float3 normal){
  position=normal=default;var camera=Camera.main;var mouse=Mouse.current;if(camera==null||mouse==null)return false;
  var state=tool.World.GetExistingSystemManaged<StairSystem>();if(state==null||!state.Ready)return false;
  var cursor=mouse.position.ReadValue();var ray=probeTool==tool&&probeRay.HasValue?probeRay.Value:camera.ScreenPointToRay(new Vector3(cursor.x,cursor.y,0));
  if(!state.SurfaceHit(ray,out float distance,out var n))return false;
  if(probeTool!=tool&&tool.World.GetExistingSystemManaged<ToolRaycastSystem>().GetRaycastResult(out var native)&&Vector3.Dot((Vector3)native.m_Hit.m_HitPosition-ray.origin,ray.direction)+.02f<distance)return false;
  position=ray.GetPoint(distance);normal=n;return true;
 }
 static void Raycast(ToolBaseSystem __instance,ref ControlPoint __0,ref bool __result){try{
  if(!Eligible(__instance,out _,out _)||!Hit(__instance,out var p,out var n))return;
  var camera=Camera.main;if(probeTool!=__instance&&__result&&__0.m_OriginalEntity==Entity.Null&&camera!=null&&Vector3.Distance(camera.transform.position,(Vector3)__0.m_HitPosition)+.02f<Vector3.Distance(camera.transform.position,(Vector3)p))return;
  __0=new ControlPoint{m_Position=p,m_HitPosition=p,m_HitDirection=n,m_Rotation=quaternion.identity,m_OriginalEntity=Entity.Null,m_ElementIndex=new int2(-1)};__result=true;
 }catch(Exception e){Stop(e);}}
 static void BeforeSnap(ObjectToolSystem __instance,JobHandle inputDeps,out SnapState __state){
  __state=null;try{
   if(!Eligible(__instance,out _,out var snap)||!Hit(__instance,out var hit,out var normal))return;
   inputDeps.Complete();var list=(NativeList<ControlPoint>)points.GetValue(__instance);if(list.Length==0)return;var cp=list[list.Length-1];
   if(cp.m_OriginalEntity!=Entity.Null||math.distancesq(cp.m_HitPosition,hit)>.0001f||math.distancesq(cp.m_HitDirection,normal)>.0001f)return;
   __state=new SnapState{On=(Snap)onMask.GetValue(__instance),Off=(Snap)offMask.GetValue(__instance),Hit=hit,Changed=true};
   // Native job still owns placement offsets, hanging bounds, upright and rotation.
   // Other snap candidates must not pull this surface hit to a nearby road/building.
   onMask.SetValue(__instance,snap&Snap.Upright);offMask.SetValue(__instance,~(snap&Snap.Upright));
  }catch(Exception e){Restore(__instance,__state);Stop(e);}
 }
 static void Restore(ObjectToolSystem tool,SnapState state){if(state?.Changed==true){onMask.SetValue(tool,state.On);offMask.SetValue(tool,state.Off);state.Changed=false;}}
 static void AfterSnap(ObjectToolSystem __instance,JobHandle __result,SnapState __state){
  Restore(__instance,__state);if(__state==null||failed)return;
  try{
   __result.Complete();var list=(NativeList<ControlPoint>)points.GetValue(__instance);if(list.Length==0)return;
   var data=__instance.World.GetExistingSystemManaged<TerrainSystem>().GetHeightData();var cp=list[list.Length-1];
   float delta=__state.Hit.y-TerrainUtils.SampleHeight(ref data,cp.m_Position);
   cp.m_Position.y+=delta;cp.m_Elevation+=delta;list[list.Length-1]=cp;
   if(!logged){logged=true;Event("surface.first-hit","Native prop/decal preview aligned to committed shape");}
  }catch(Exception e){Stop(e);}
 }
 static Exception FinishSnap(ObjectToolSystem __instance,SnapState __state,Exception __exception){Restore(__instance,__state);return __exception;}
 static void Stop(Exception e){if(!failed)Failure("surface.failed",e);failed=true;}
 internal static ControlPoint Probe(StairSystem state,ObjectPrefab prefab,Ray ray,bool enabled){
  if(failed||harmony==null)throw new InvalidOperationException("Surface hook unavailable");
  var tool=state.World.GetOrCreateSystemManaged<ObjectToolSystem>();
  if(state.World.GetExistingSystemManaged<ToolSystem>().activeTool==tool)throw new InvalidOperationException("Object tool is busy");
  state.EntityManager.CompleteAllTrackedJobs();
  var list=tool.GetControlPoints(out var dependency);dependency.Complete();if(list.Length!=0)throw new InvalidOperationException("Object tool has unfinished points");
  var current=typeof(ObjectToolSystem).GetField("m_Prefab",Instance);var selected=typeof(ObjectToolSystem).GetField("m_SelectedPrefab",Instance);
  var rotation=typeof(ObjectToolSystem).GetField("m_Rotation",Instance);var rotationRef=rotation.GetValue(tool);var rotationValue=rotationRef.GetType().GetProperty("Value");var oldRotation=rotationValue.GetValue(rotationRef);
  var oldCurrent=current.GetValue(tool);var oldSelected=selected.GetValue(tool);var oldOn=onMask.GetValue(tool);var oldOff=offMask.GetValue(tool);var oldSnap=tool.selectedSnap;
  try{
   current.SetValue(tool,prefab);selected.SetValue(tool,prefab);onMask.SetValue(tool,Snap.ObjectSurface|Snap.Upright);offMask.SetValue(tool,Snap.All);tool.selectedSnap=enabled?Snap.ObjectSurface:Snap.None;
   probeTool=tool;probeRay=ray;
   object[] args={default(ControlPoint)};
   var get=typeof(ToolBaseSystem).GetMethod("GetRaycastResult",Instance,null,new[]{typeof(ControlPoint).MakeByRefType()},null);
   bool hit=(bool)get.Invoke(tool,args);if(!enabled)return (ControlPoint)args[0];if(!hit)throw new InvalidOperationException("Surface ray did not hit "+prefab.name+" eligible="+EligiblePrefab(state.EntityManager,state.World.GetExistingSystemManaged<PrefabSystem>().GetEntity(prefab))+" mode="+tool.actualMode);
   list.Add((ControlPoint)args[0]);
   var snap=typeof(ObjectToolSystem).GetMethod("SnapControlPoint",Instance);
   ((JobHandle)snap.Invoke(tool,new object[]{default(JobHandle)})).Complete();return list[list.Length-1];
  }finally{probeRay=null;probeTool=null;list.Clear();rotationValue.SetValue(rotationRef,oldRotation);current.SetValue(tool,oldCurrent);selected.SetValue(tool,oldSelected);onMask.SetValue(tool,oldOn);offMask.SetValue(tool,oldOff);tool.selectedSnap=oldSnap;}
 }
 internal static void Dispose(){if(harmony!=null){try{harmonyType.GetMethod("UnpatchAll",new[]{typeof(string)})?.Invoke(harmony,new object[]{"zacum.StairBuilder.Surface"});}catch(Exception e){Failure("surface.unpatch.failed",e);}harmony=null;}}
}
public partial class StairSystem {
 internal bool SurfaceHit(Ray ray,out float distance,out Vector3 normal){
  distance=float.PositiveInfinity;normal=default;
  foreach(var visual in visuals.Values){if(visual.Root==null||!visual.Root.activeInHierarchy)continue;if(visual.Hit(ray,out var d,out _,out var n,true)&&d<distance){distance=d;normal=n;}}
  return !float.IsPositiveInfinity(distance);
 }
}
}

