using System.Globalization;
using UnityEngine;
namespace StairBuilder {
public partial class StairSystem {
 internal bool DraggingWidth;bool widthHover;Vector3 widthAnchor,widthOut,widthGrab;Plane widthPlane;float widthBefore;RoutePlan widthDragPlan;Stair widthOriginal;
 internal void ClearWidthHover(){if(!DraggingWidth)widthHover=false;}
 internal string WidthHandle {
  get {
   if(!Open||Draft==null||!HaveB||MovingPoint>=0||GizmoHeld||!widthHover||Camera.main==null)return "";
   var camera=Camera.main;var p=camera.WorldToViewportPoint(widthAnchor);if(p.z<=0)return "";
   var a=camera.WorldToScreenPoint(widthAnchor);var b=camera.WorldToScreenPoint(widthAnchor+widthOut);
   float angle=-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg;
   return string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3:F1},{4}",p.x*100,(1-p.y)*100,angle,Draft.Width,DraggingWidth?1:0);
  }
 }
 internal bool SetWidth(float value){
  if(!Layout.Finite(value))return false;value=WidthPolicy.Snap(value);
  if(Draft==null){if(Mathf.Abs(Width-value)>.001f){Width=value;PlayStairSound(5);}return true;}
  if(Mathf.Abs(value-Draft.Width)<.001f)return true;
  // Keep the last usable preview if a wider landing would swallow a short segment.
  var candidate=Draft.Copy();candidate.Width=value;
  if(DraggingWidth&&widthOriginal!=null){var shift=widthOut*((value-widthBefore)*.5f);for(int i=0;i<candidate.Points.Count;i++){candidate.Points[i]=widthOriginal.Points[i].Copy();candidate.Points[i].Ground+=shift;}}
  RoutePlan plan=null;if(HaveA&&HaveB&&!RoutePlan.Build(candidate,out plan,out _))return false;
  Width=Draft.Width=value;if(DraggingWidth)Draft.Points=candidate.Points;SelectedSupport=-1;
  PlayStairSound(5,DraggingWidth);if(DraggingWidth)widthDragPlan=plan;else Rebuild();return true;
 }
 internal void HoverWidth(Vector2 screen){
  ClearWidthHover();if(DraggingWidth||Draft==null||!HaveA||!HaveB||MovingPoint>=0||GizmoHeld||Camera.main==null||HoverPoint>=0)return;
  float best=16f*16f;var camera=Camera.main;
  for(int i=1;i<Draft.Points.Count;i++){
   var a=Draft.Point(i-1);var b=Draft.Point(i);var forward=b-a;forward.y=0;if(forward.sqrMagnitude<.01f)continue;
   var right=Vector3.Cross(Vector3.up,forward.normalized);
   foreach(float side in new[]{-1f,1f}){
    var offset=right*(side*Draft.Width*.5f)+Vector3.up*.04f;
    var start=Vector3.Lerp(a,b,.08f)+offset;var end=Vector3.Lerp(a,b,.92f)+offset;
    var pa=camera.WorldToScreenPoint(start);var pb=camera.WorldToScreenPoint(end);if(pa.z<=0||pb.z<=0)continue;
    Vector2 sa=new Vector2(pa.x,pa.y),sb=new Vector2(pb.x,pb.y),d=sb-sa;if(d.sqrMagnitude<1)continue;
    float t=Mathf.Clamp01(Vector2.Dot(screen-sa,d)/d.sqrMagnitude);float distance=(screen-(sa+d*t)).sqrMagnitude;
    if(distance<best){best=distance;widthHover=true;widthAnchor=Vector3.Lerp(start,end,t);widthOut=right*side;}
   }
  }
 }
 internal bool BeginWidthDrag(Ray ray){
  if(!widthHover||Draft==null||!HaveB||MovingPoint>=0||GizmoHeld||!previewReady)return false;
  widthPlane=new Plane(Vector3.up,widthAnchor);if(!widthPlane.Raycast(ray,out float hit)||Mathf.Abs(ray.direction.y)<.05f)return false;
  widthGrab=ray.GetPoint(hit);widthBefore=Draft.Width;widthOriginal=Draft.Copy();RoutePlan.Build(Draft,out widthDragPlan,out _);DraggingWidth=true;preview.Root.SetActive(false);dragCandidate=-1;HoverPoint=HoverSupport=-1;PlayStairSound(5);return true;
 }
 internal void UpdateWidthDrag(Ray ray,bool held){
  if(!DraggingWidth)return;
  if(widthPlane.Raycast(ray,out float hit)&&Mathf.Abs(ray.direction.y)>=.05f){
   float next=WidthPolicy.Drag(widthBefore,Vector3.Dot(ray.GetPoint(hit)-widthGrab,widthOut));float old=Draft.Width;
   if(SetWidth(next))widthAnchor+=widthOut*(Draft.Width-old);
  }
  if(!held)EndWidthDrag(false);
 }
 internal bool EndWidthDrag(bool cancel){
  if(!DraggingWidth)return false;DraggingWidth=false;widthHover=false;widthDragPlan=null;
  bool changed=Draft!=null&&Mathf.Abs(Draft.Width-widthBefore)>.001f;
  if(cancel&&Draft!=null){PlayStairSound(4);Width=Draft.Width=widthBefore;Draft.Points=widthOriginal.Points.ConvertAll(p=>p.Copy());}
  if(!cancel&&changed){
   try{RebaseWidthGround();PlayStairSound(1);Rebuild();}catch{Width=Draft.Width=widthBefore;Draft.Points=widthOriginal.Points.ConvertAll(p=>p.Copy());previewReady=preview!=null;if(preview!=null)preview.Root.SetActive(true);throw;}
  }else if(preview!=null)preview.Root.SetActive(true);
  widthOriginal=null;return true;
 }
 void RebaseWidthGround(){var terrain=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();for(int i=0;i<Draft.Points.Count;i++){var world=Draft.Point(i);float ground=Game.Simulation.TerrainUtils.SampleHeight(ref terrain,(Unity.Mathematics.float3)world);Draft.Points[i].AtHeight(new Vector3(world.x,ground,world.z),world.y);}}
}
}
