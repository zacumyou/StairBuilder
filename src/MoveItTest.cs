using System;
using System.Linq;
using System.Reflection;
using Game.Tools;
using UnityEngine;
namespace StairBuilder {
public partial class StairSystem {
 string moveItTestReport="";
 object GizmoTestSession(){var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("GizmoXYZ.GizmoSession")).FirstOrDefault(t=>t!=null);return type==null?null:World.GetExistingSystemManaged(type);}
 object MoveItTestState(){var g=GizmoTestSession();const BindingFlags f=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;return new{report=moveItTestReport,selected=moveItSelected,captured=MoveItCaptured,held=GizmoHeld,gizmoPresent=g!=null,gizmoHeld=g?.GetType().GetProperty("Held",f)?.GetValue(g),gizmoTranslationOnly=g?.GetType().GetProperty("TranslationOnly",f)?.GetValue(g)};}
 void MoveItTest(string action,int target){
  if(Stairs.Count!=benchmarkIds.Count||target<0||target>=Stairs.Count)throw new InvalidOperationException("Owned fixture required");
  var stair=Stairs[target];const BindingFlags f=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  if(action=="width-memory-moveit-activate"){
   if(Draft!=null)throw new InvalidOperationException("Draft exists");
   var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("MoveIt.Tool.MIT")??a.GetType("MoveIt.Tool.MoveItToolSystem")).FirstOrDefault(t=>t!=null);
   var tool=type==null?null:World.GetExistingSystemManaged(type) as ToolBaseSystem;if(tool==null)throw new InvalidOperationException("Move It not loaded");
   World.GetOrCreateSystemManaged<ToolSystem>().activeTool=tool;MoveItBridge.Observe(tool);moveItTestReport="activated";
  }else if(action=="width-memory-moveit-check"){
   var tool=World.GetOrCreateSystemManaged<ToolSystem>().activeTool;if(!MoveItBridge.Idle(tool))throw new InvalidOperationException("Move It single mode not idle");
   var original=stair.Copy();var view=visuals[stair.Id];var delta=new Vector3(8,1.25f,-6);int total=Stairs.Count;
   if(!BeginMoveIt(stair.Id))throw new InvalidOperationException("Begin failed");
   for(int i=0;i<64;i++)MoveItOffset(delta*(i/63f));
   if(!ReferenceEquals(view,visuals[stair.Id])||!SameRoute(stair,original)||stair.Area!=original.Area||Draft!=null)throw new InvalidOperationException("Drag changed committed data");
   FinishMoveIt(false);if(!SameRoute(stair,original))throw new InvalidOperationException("Cancel changed original");
   BeginMoveIt(stair.Id);MoveItOffset(delta);if(!FinishMoveIt(true))throw new InvalidOperationException("Commit failed");
   var moved=Stairs.Find(s=>s.Id==original.Id);if(total!=Stairs.Count||moved.Width!=original.Width||moved.Style!=original.Style||moved.DeckColor!=original.DeckColor||moved.RailColor!=original.RailColor||!paths.Alive(moved))throw new InvalidOperationException("Move lost design/path data");
   for(int i=0;i<moved.Points.Count;i++)if((moved.Point(i)-original.Point(i)-delta).sqrMagnitude>.00001f)throw new InvalidOperationException("Point displacement mismatch");
   VerifyPick(moved);moveItTestReport="PASS: 64 outline updates, cancel, whole move, path rebuild, picking";
  }else if(action=="width-memory-moveit-gizmo"||action=="width-memory-moveit-copy"){
   moveItSelected=stair.Id;if(!MoveItToGizmo(action.EndsWith("copy",StringComparison.Ordinal)))throw new InvalidOperationException("Gizmo handoff failed");moveItTestReport="gizmo handed off";
  }else if(action=="width-memory-moveit-gizmo-cancel"||action=="width-memory-moveit-gizmo-confirm"){
   var g=GizmoTestSession();if(g==null||!(bool)g.GetType().GetProperty("Held",f).GetValue(g))throw new InvalidOperationException("Gizmo did not acquire bridge");
   if(action.EndsWith("confirm",StringComparison.Ordinal)&&!(bool)g.GetType().GetProperty("ConfirmReady",f).GetValue(g))throw new InvalidOperationException("Gizmo not ready");
   g.GetType().GetMethod("Command",f).Invoke(g,new object[]{action.EndsWith("confirm",StringComparison.Ordinal)?"confirm":"cancel"});moveItTestReport="Gizmo UI command dispatched";
  }else throw new ArgumentException(action);
 }
}
}
