using System;
using System.Linq;
using System.Reflection;
using Game.Input;
using Game.SceneFlow;
using Game.Tools;
using UnityEngine;
using UnityEngine.InputSystem;
namespace StairBuilder {
// The normal Better Bulldozer mode extends this native tool. Never intercept its
// separate sub-element/vehicle tools or a native demolition already in progress.
internal static class BulldozerBridge {
 static object harmony;static Type harmonyType;
 static FieldInfo nativeState,blocked;static BulldozeToolSystem blockedTool;
 internal static void Install(Mod owner){
  try{
   nativeState=typeof(BulldozeToolSystem).GetField("m_State",BindingFlags.Instance|BindingFlags.NonPublic);
   blocked=typeof(BulldozeToolSystem).GetField("m_ApplyBlocked",BindingFlags.Instance|BindingFlags.NonPublic);
   var update=typeof(BulldozeToolSystem).GetMethod("OnUpdate",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(Unity.Jobs.JobHandle)},null);
   if(nativeState==null||blocked?.FieldType!=typeof(bool)||update==null||!nativeState.FieldType.IsEnum||Enum.GetName(nativeState.FieldType,0)!="Default")throw new MissingMemberException("Bulldozer input contract changed");
   var assembly=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name=="0Harmony");
   if(assembly==null){
    if(!GameManager.instance.modManager.TryGetExecutableAsset(owner,out var asset))throw new InvalidOperationException("Mod path unavailable");
    assembly=Assembly.LoadFrom(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(asset.path),"0Harmony.dll"));
   }
   harmonyType=assembly.GetType("HarmonyLib.Harmony",true);harmony=Activator.CreateInstance(harmonyType,"zacum.StairBuilder.Bulldozer");
   var prefix=Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod",true),typeof(BulldozerBridge).GetMethod(nameof(Prefix),BindingFlags.Static|BindingFlags.NonPublic));
   var postfix=Activator.CreateInstance(assembly.GetType("HarmonyLib.HarmonyMethod",true),typeof(BulldozerBridge).GetMethod(nameof(Postfix),BindingFlags.Static|BindingFlags.NonPublic));
   harmonyType.GetMethods().Single(m=>m.Name=="Patch"&&m.GetParameters().Length==5).Invoke(harmony,new object[]{update,prefix,postfix,null,null});
   BulldozerLog.Event("bulldozer.connected","Native normal bulldozer / Better Bulldozer normal mode");
  }catch(Exception e){BulldozerLog.Failure("bulldozer.unavailable",e);Dispose();}
 }
 static void Prefix(BulldozeToolSystem __instance){
  var state=__instance.World.GetExistingSystemManaged<StairSystem>();if(state==null||!state.Ready)return;
  try{
   if(__instance.World.GetExistingSystemManaged<ToolSystem>()?.activeTool!=__instance)return;
   if(Convert.ToInt32(nativeState.GetValue(__instance))!=0){state.ResetBulldozer();return;}
   // Block native input BEFORE mutating our own data. The native update still
   // runs and clears its previews and release latch normally.
   if(blockedTool==__instance)blocked.SetValue(__instance,true);
   state.HandleBulldozer(__instance,()=>{blocked.SetValue(__instance,true);blockedTool=__instance;},(bool)blocked.GetValue(__instance));
  }catch(Exception e){if(state.BulldozerCaptured)blocked.SetValue(__instance,true);BulldozerLog.Failure("bulldozer.failed",e);}
 }
 static void Postfix(BulldozeToolSystem __instance){
  // A shape may have no native entity behind it, so the native apply action
  // can be disabled and never report release. Clear only our own latch.
  if(blockedTool==__instance&&(Mouse.current?.leftButton.isPressed!=true||!Application.isFocused)){
   blocked.SetValue(__instance,false);blockedTool=null;
  }
 }
 internal static void Dispose(){
  if(blockedTool!=null){blocked.SetValue(blockedTool,false);blockedTool=null;}if(harmony!=null){try{harmonyType.GetMethod("UnpatchAll",new[]{typeof(string)})?.Invoke(harmony,new object[]{"zacum.StairBuilder.Bulldozer"});}catch(Exception e){BulldozerLog.Failure("bulldozer.unpatch.failed",e);}harmony=null;}}
}
internal static class BulldozerLog {internal static void Event(string key,string text)=>Mod.Log.Info(key+" "+text);internal static void Failure(string key,Exception e)=>Mod.Log.Error(e,key);}
public partial class StairSystem {
 readonly BulldozerGesture bulldozerGesture=new BulldozerGesture();
 internal bool BulldozerCaptured=>bulldozerGesture.Captured;
 internal void ResetBulldozer()=>bulldozerGesture.Reset();
 static Type bbType;static PropertyInfo bbTarget,bbFilters,bbMode;
 bool PropsAllowed(){
  if(bbType==null){foreach(var a in AppDomain.CurrentDomain.GetAssemblies()){bbType=a.GetType("Better_Bulldozer.Systems.BetterBulldozerUISystem");if(bbType!=null)break;}if(bbType==null)return true;bbTarget=bbType.GetProperty("SelectedRaycastTarget");bbFilters=bbType.GetProperty("SelectedVanillaFilters");bbMode=bbType.GetProperty("ActiveSelectionMode");}
  var ui=World.GetExistingSystemManaged(bbType);if(ui==null)return true;
  if(bbTarget==null||bbFilters==null)return false;
  return bbTarget.GetValue(ui).ToString()=="Vanilla"&&bbMode?.GetValue(ui).ToString()=="Single"&&(Convert.ToInt32(bbFilters.GetValue(ui))&32)!=0;
 }
 internal void RemoveStair(int id){
  var s=Stairs.Find(x=>x.Id==id);if(s==null)return;
  if(Draft?.Id==id)Cancel();paths.Delete(s);Stairs.Remove(s);missingPaths.Remove(id);
  if(visuals.TryGetValue(id,out var visual)){visual.Dispose();visuals.Remove(id);}RefreshJunctions(s,null);Mod.Log.Info("Deleted whole stair "+id);
 }
 internal void HandleBulldozer(BulldozeToolSystem tool,Action block,bool nativeBlocked){
  var input=InputManager.instance;var mouse=Mouse.current;var camera=Camera.main;
  bool available=Application.isFocused&&!input.hasInputFieldFocus&&!input.overlayActive&&input.controlOverWorld&&mouse!=null&&camera!=null&&PropsAllowed();
  int id=-1;
  if(available){
   var pos=mouse.position.ReadValue();var ray=camera.ScreenPointToRay(new Vector3(pos.x,pos.y,0));float nearest=float.PositiveInfinity;
   foreach(var pair in visuals)if(pair.Value.Hit(ray,out var distance)&&distance<nearest){nearest=distance;id=pair.Key;}
   if(id>=0&&World.GetOrCreateSystemManaged<ToolRaycastSystem>().GetRaycastResult(out var hit)){
    Vector3 p=hit.m_Hit.m_HitPosition;if(Vector3.Dot(p-ray.origin,ray.direction)+.1f<nearest)id=-1;
   }
   if(id>=0)StairGuide.Draw(World.GetOrCreateSystemManaged<Game.Rendering.OverlayRenderSystem>(),Stairs.Find(x=>x.Id==id),true,new Color(1,.2f,.15f,.8f));
  }
  if(bulldozerGesture.Step(id,available,mouse?.leftButton.wasPressedThisFrame==true,mouse?.leftButton.isPressed==true,mouse?.leftButton.wasReleasedThisFrame==true,nativeBlocked,out int remove)){
   block();if(remove>=0)RemoveStair(remove);
  }
 }
}
}
