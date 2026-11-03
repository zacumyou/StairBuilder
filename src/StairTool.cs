using Game.Input;
using Game.Prefabs;
using Game.Tools;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.InputSystem;
namespace StairBuilder {
public partial class StairTool:ToolBaseSystem {
 public override string toolID=>"Stair Builder";
 public override PrefabBase GetPrefab()=>null;
 public override bool TrySetPrefab(PrefabBase prefab)=>false;
 public override void InitializeRaycast(){base.InitializeRaycast();m_ToolRaycastSystem.typeMask=Game.Common.TypeMask.Terrain;
  var s=World.GetOrCreateSystemManaged<StairSystem>();
  if(s.Draft!=null&&s.MovingPoint>=0){
   if(s.SnapObjects)m_ToolRaycastSystem.typeMask|=Game.Common.TypeMask.StaticObjects;
   if(s.SnapNetworks)m_ToolRaycastSystem.typeMask|=Game.Common.TypeMask.Net;
   m_ToolRaycastSystem.collisionMask=Game.Common.CollisionMask.OnGround|Game.Common.CollisionMask.Overground;
  }
 }
 protected override JobHandle OnUpdate(JobHandle deps){
  deps.Complete();applyMode=ApplyMode.None;var s=World.GetOrCreateSystemManaged<StairSystem>();var input=InputManager.instance;
  bool enabled=!s.GizmoHeld&&s.Ready&&s.Open&&Application.isFocused&&!input.hasInputFieldFocus&&!input.overlayActive;
  foreach(var key in new[]{"Confirm","Cancel","Raise","Lower"})Mod.Options.GetAction(key).shouldBeEnabled=enabled;
  if(!enabled){s.AbortDrag();return default;}
  try{
   if(Mod.Options.GetAction("Cancel").WasPressedThisFrame()||(input.controlOverWorld&&Mouse.current?.rightButton.wasPressedThisFrame==true)){if(!s.EndWidthDrag(true))s.Enqueue("cancel");return default;}
   if(!s.DraggingWidth&&Mod.Options.GetAction("Raise").WasPressedThisFrame())s.Adjust(1);
   if(!s.DraggingWidth&&Mod.Options.GetAction("Lower").WasPressedThisFrame())s.Adjust(-1);
   if(!s.DraggingWidth&&Mod.Options.GetAction("Confirm").WasPressedThisFrame()){s.Enqueue("commit");return default;}
   if(Mouse.current==null)return default;
   if(s.DraggingWidth){if(Camera.main!=null){var mouse=Mouse.current.position.ReadValue();s.UpdateWidthDrag(Camera.main.ScreenPointToRay(new Vector3(mouse.x,mouse.y,0)),Mouse.current.leftButton.isPressed);}return default;}
   s.PollPointDrag(Mouse.current.position.ReadValue(),Mouse.current.leftButton.isPressed);
   if(!input.controlOverWorld){s.ClearWidthHover();if(s.DraggingPoint&&!Mouse.current.leftButton.isPressed)s.AbortDrag();return default;}
   bool click=Mouse.current.leftButton.wasPressedThisFrame;
   if(s.MovingPoint>=0){if(GetRaycastResult(out Unity.Entities.Entity entity,out Game.Common.RaycastHit hit)){
    Vector3 point=hit.m_HitPosition;bool surface=s.SnapObjects&&EntityManager.HasComponent<Game.Objects.Transform>(entity);
    if(!surface&&s.SnapNetworks)surface=RoadEdge(ref point);
    s.Cursor(point,s.DraggingPoint?Mouse.current.leftButton.wasReleasedThisFrame:click,surface);
   }else if(s.DraggingPoint&&Mouse.current.leftButton.wasReleasedThisFrame)s.AbortDrag();}
   else if(Camera.main!=null){var p=Mouse.current.position.ReadValue();var ray=Camera.main.ScreenPointToRay(new Vector3(p.x,p.y,0));s.Hover(ray,p);if(click){if(s.BeginWidthDrag(ray))return default;s.Pick(ray);if(s.HoverPoint>=0)s.ArmPointDrag(s.HoverPoint);}}
  }catch(System.Exception e){s.Status=e is ImportedAssetException?"assets":"error";Mod.Log.Error(e,"Stair tool failed");}
  finally{s.DrawGuide();}
  return default;
 }
 protected override void OnStopRunning(){foreach(var key in new[]{"Confirm","Cancel","Raise","Lower"})Mod.Options.GetAction(key).shouldBeEnabled=false;World.GetOrCreateSystemManaged<StairSystem>().ToolStopped();base.OnStopRunning();}
}
}
