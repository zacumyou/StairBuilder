using System;
using Game.Input;
using Game.Tools;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
namespace StairBuilder {
public partial class StairSystem {
 int moveItSelected;Stair moveItSource,moveItPreview;Vector3 moveItGrab;Vector2 moveItPress;Plane moveItPlane;bool moveItDragged;
 internal bool MoveItCaptured=>moveItSource!=null;
 internal void ResetMoveIt(){moveItSelected=0;moveItSource=moveItPreview=null;moveItDragged=false;}
 void ObserveMoveIt(){var tool=World.GetOrCreateSystemManaged<ToolSystem>().activeTool;MoveItBridge.Observe(tool);if(!MoveItBridge.Active(tool))ResetMoveIt();}
 internal bool BeginMoveIt(int id){
  if(Draft!=null||MoveItCaptured)return false;var s=Stairs.Find(x=>x.Id==id);if(s==null)return false;
  moveItSelected=id;moveItSource=s.Copy();moveItPreview=s.Copy();moveItDragged=false;return true;
 }
 internal void MoveItOffset(Vector3 delta){
  if(!MoveItCaptured||!math.all(math.isfinite((float3)delta))||delta.sqrMagnitude>100000000)return;
  // No mesh, material, native entity or terrain lookup during pointer movement.
  for(int i=0;i<moveItSource.Points.Count;i++)moveItPreview.Points[i].Ground=moveItSource.Points[i].Ground+delta;
 }
 internal bool FinishMoveIt(bool apply){
  if(!MoveItCaptured)return false;var original=moveItSource;var desired=moveItPreview;moveItSource=moveItPreview=null;
  if(!apply||(desired.A-original.A).sqrMagnitude<.000001f)return true;
  if(Draft!=null||!Stairs.Exists(s=>s.Id==original.Id))return false;
  var terrain=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
  for(int i=0;i<desired.Points.Count;i++){var p=desired.Point(i);var ground=p;ground.y=Game.Simulation.TerrainUtils.SampleHeight(ref terrain,(float3)p);desired.Points[i].AtHeight(ground,p.y);}
  try{
   Draft=desired;HaveA=HaveB=true;MovingPoint=-1;Rebuild();if(!CanCommit)return false;Commit();return Draft==null;
  }finally{if(Draft!=null)Cancel();}
 }
 internal bool MoveItToGizmo(bool copy){
  if(MoveItCaptured||Draft!=null||moveItSelected<=0)return false;
  // Check a live Gizmo system, not merely a DLL/subscription.
  var type=System.Array.Find(AppDomain.CurrentDomain.GetAssemblies(),a=>a.GetName().Name=="GizmoXYZ")?.GetType("GizmoXYZ.GizmoSession");
  if(type==null||World.GetExistingSystemManaged(type)==null)return false;
  int id=moveItSelected;Open=true;Activate();SelectStair(id);BeginGizmo(!copy);return GizmoHeld;
 }
 internal bool MoveItInput(){
  var input=InputManager.instance;var mouse=Mouse.current;var keyboard=Keyboard.current;var camera=Camera.main;
  if(!Application.isFocused||input.hasInputFieldFocus||input.overlayActive||mouse==null||camera==null){bool held=MoveItCaptured;ResetMoveIt();return held;}
  if(Draft!=null){ResetMoveIt();return false;}
  if(keyboard?.escapeKey.wasPressedThisFrame==true||mouse.rightButton.wasPressedThisFrame){bool ours=MoveItCaptured||moveItSelected>0;ResetMoveIt();return ours;}
  if(moveItSelected>0&&!MoveItCaptured&&keyboard!=null&&(keyboard.leftCtrlKey.isPressed||keyboard.rightCtrlKey.isPressed)){
   if(keyboard.tKey.wasPressedThisFrame&&MoveItToGizmo(false))return true;
   if(keyboard.dKey.wasPressedThisFrame&&MoveItToGizmo(true))return true;
  }
  if(!MoveItCaptured&&keyboard!=null&&(keyboard.leftShiftKey.isPressed||keyboard.rightShiftKey.isPressed)){moveItSelected=0;return false;}
  var pos=mouse.position.ReadValue();var ray=camera.ScreenPointToRay(new Vector3(pos.x,pos.y,0));
  if(MoveItCaptured){
   if((pos-moveItPress).sqrMagnitude>36)moveItDragged=true;
   if(moveItDragged&&input.controlOverWorld&&Mathf.Abs(ray.direction.y)>.05f&&moveItPlane.Raycast(ray,out float d))MoveItOffset(ray.GetPoint(d)-moveItGrab);
   if(moveItPreview!=null)StairGuide.Draw(World.GetOrCreateSystemManaged<Game.Rendering.OverlayRenderSystem>(),moveItPreview,true,new Color(.25f,.9f,1,.9f));
   if(!mouse.leftButton.isPressed)FinishMoveIt(input.controlOverWorld&&moveItDragged);
   return true;
  }
  int hover=0;float nearest=float.PositiveInfinity;
  if(input.controlOverWorld){
   foreach(var pair in visuals)if(pair.Value.Hit(ray,out float d)&&d<nearest){nearest=d;hover=pair.Key;}
   if(hover>0&&World.GetOrCreateSystemManaged<ToolRaycastSystem>().GetRaycastResult(out var hit)&&Vector3.Dot((Vector3)hit.m_Hit.m_HitPosition-ray.origin,ray.direction)+.1f<nearest)hover=0;
   if(mouse.leftButton.wasPressedThisFrame){
    if(hover>0&&Mathf.Abs(ray.direction.y)>.05f){moveItPlane=new Plane(Vector3.up,ray.GetPoint(nearest));if(moveItPlane.Raycast(ray,out float d)&&BeginMoveIt(hover)){moveItGrab=ray.GetPoint(d);moveItPress=pos;PlayStairSound(5);return true;}}
    moveItSelected=0;
   }
  }
  var shown=Stairs.Find(s=>s.Id==(hover>0?hover:moveItSelected));if(shown!=null)StairGuide.Draw(World.GetOrCreateSystemManaged<Game.Rendering.OverlayRenderSystem>(),shown,true,new Color(.25f,.9f,1,.8f));
  return false;
 }
}
}
