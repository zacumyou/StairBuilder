using System;
using System.Linq;
using System.Reflection;
using Game.Tools;
using Unity.Jobs;
namespace StairBuilder {
// No assembly reference to Move It or Gizmo XYZ. Unknown contracts leave their tools untouched.
internal static class MoveItBridge {
 static Type hooked;static object harmony;static Type harmonyType;static bool failed;
 static PropertyInfo state,manipulating,selection,count,marquee,copying,deleting;
 internal static bool Active(ToolBaseSystem tool)=>tool!=null&&tool.Enabled&&(tool.GetType().FullName=="MoveIt.Tool.MIT"||tool.GetType().FullName=="MoveIt.Tool.MoveItToolSystem");
 internal static bool Idle(ToolBaseSystem tool){
  if(!Active(tool)||tool.GetType()!=hooked||failed)return false;
  var selected=selection.GetValue(tool);
  return selected!=null&&state.GetValue(tool)?.ToString()=="Default"&&!(bool)manipulating.GetValue(tool)&&!(bool)marquee.GetValue(tool)&&!(copying?.GetValue(tool) is bool c&&c)&&!(deleting?.GetValue(tool) is bool d&&d)&&Convert.ToInt32(count.GetValue(selected))==0;
 }
 internal static void Observe(ToolBaseSystem tool){
  if(!Active(tool)||hooked!=null||failed)return;
  try{
   var type=tool.GetType();const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
   state=type.GetProperty("MITState",flags);manipulating=type.GetProperty("IsManipulating",flags);selection=type.GetProperty("Selection",flags);count=selection?.PropertyType.GetProperty("Count");marquee=type.GetProperty("UseMarquee",flags);copying=type.GetProperty("Copying",flags);deleting=type.GetProperty("Deleting",flags);
   var update=type.GetMethod("OnUpdate",flags,null,new[]{typeof(JobHandle)},null);
   if(state==null||manipulating?.PropertyType!=typeof(bool)||count==null||marquee?.PropertyType!=typeof(bool)||update==null)throw new MissingMemberException("Move It single-selection contract");
   var a=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x=>x.GetName().Name=="0Harmony");if(a==null)throw new MissingMemberException("Harmony unavailable");
   harmonyType=a.GetType("HarmonyLib.Harmony",true);harmony=Activator.CreateInstance(harmonyType,"zacum.StairBuilder.MoveIt");
   var hm=a.GetType("HarmonyLib.HarmonyMethod",true);var prefix=Activator.CreateInstance(hm,typeof(MoveItBridge).GetMethod(nameof(Prefix),BindingFlags.Static|BindingFlags.NonPublic));hm.GetField("priority").SetValue(prefix,800);
   harmonyType.GetMethods().Single(m=>m.Name=="Patch"&&m.GetParameters().Length==5).Invoke(harmony,new object[]{update,prefix,null,null,null});hooked=type;
   Mod.Log.Info("moveit.connected "+type.FullName+" "+type.Assembly.GetName().Version);
  }catch(Exception e){failed=true;Mod.Log.Warn("Move It integration unavailable: "+e.Message);}
 }
 static bool Prefix(ToolBaseSystem __instance,JobHandle __0,ref JobHandle __result){
  var s=__instance.World.GetExistingSystemManaged<StairSystem>();if(s==null||!s.Ready)return true;
  try{
   if(__instance.World.GetExistingSystemManaged<ToolSystem>()?.activeTool!=__instance)return true;
   if(!Idle(__instance)){s.ResetMoveIt();return true;}
   __0.Complete();if(!s.MoveItInput())return true;__result=__0;return false;
  }catch(Exception e){bool captured=s.MoveItCaptured;s.ResetMoveIt();failed=true;Mod.Log.Error(e,"moveit.failed");__result=__0;return !captured;}
 }
 internal static void Dispose(){if(harmony!=null)try{harmonyType.GetMethod("UnpatchAll",new[]{typeof(string)})?.Invoke(harmony,new object[]{"zacum.StairBuilder.MoveIt"});}catch(Exception e){Mod.Log.Warn(e.Message);}harmony=null;hooked=null;failed=false;}
}
}
