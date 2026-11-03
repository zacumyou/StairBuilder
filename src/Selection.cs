using Game.Input;
using Game.Tools;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
namespace StairBuilder {
public partial class StairSystem {
 internal int DetailId;int detailPressed;Vector2 detailPressPosition;bool detailDragged;
 internal Stair DetailStair=>Stairs.Find(s=>s.Id==DetailId);
 void HandleDefaultSelection(){
  var tools=World.GetOrCreateSystemManaged<ToolSystem>();
  if(Open||!(tools.activeTool is DefaultToolSystem)){DetailId=0;detailPressed=0;return;}
  var input=InputManager.instance;var mouse=Mouse.current;var camera=Camera.main;
  if(!Application.isFocused||input.hasInputFieldFocus||input.overlayActive||mouse==null||camera==null){detailPressed=0;return;}
  if(Keyboard.current?.escapeKey.wasPressedThisFrame==true){DetailId=0;detailPressed=0;return;}
  var keyboard=Keyboard.current;
  if(DetailId>0&&keyboard!=null&&(keyboard.leftCtrlKey.isPressed||keyboard.rightCtrlKey.isPressed)&&!keyboard.shiftKey.isPressed&&!keyboard.altKey.isPressed){
   if(keyboard.tKey.wasPressedThisFrame&&SelectedStairToGizmo(false))return;
   if(keyboard.dKey.wasPressedThisFrame&&SelectedStairToGizmo(true))return;
  }
  int hover=0;
  if(input.controlOverWorld){
   var pos=mouse.position.ReadValue();var ray=camera.ScreenPointToRay(new Vector3(pos.x,pos.y,0));float nearest=float.PositiveInfinity;
   foreach(var pair in visuals)if(pair.Value.Hit(ray,out float d)&&d<nearest){nearest=d;hover=pair.Key;}
   if(hover>0&&World.GetOrCreateSystemManaged<ToolRaycastSystem>().GetRaycastResult(out var hit)){
    Vector3 p=hit.m_Hit.m_HitPosition;if(Vector3.Dot(p-ray.origin,ray.direction)+.1f<nearest)hover=0;
   }
   if(mouse.leftButton.wasPressedThisFrame){detailPressed=hover;detailPressPosition=pos;detailDragged=false;}
   if((pos-detailPressPosition).sqrMagnitude>36)detailDragged=true;
   if(mouse.leftButton.wasReleasedThisFrame){if(!detailDragged)DetailId=detailPressed==hover?hover:0;detailPressed=0;}
  }else detailPressed=0;
  if(DetailId>0){
   // Keep the underlying road/building's native selection panel from competing.
   var info=World.GetOrCreateSystemManaged<Game.UI.InGame.SelectedInfoUISystem>();if(info.selectedEntity!=Entity.Null)info.SetSelection(Entity.Null);
  }
  var shown=Stairs.Find(s=>s.Id==(hover>0?hover:DetailId));
  if(shown!=null)StairGuide.Draw(World.GetOrCreateSystemManaged<Game.Rendering.OverlayRenderSystem>(),shown,true,shown.Id==DetailId?new Color(.25f,.9f,1f,.85f):new Color(.8f,.95f,1f,.55f));
 }
}
}
