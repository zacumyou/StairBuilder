using Colossal.Mathematics;
using Game.Rendering;
using UnityEngine;

namespace StairBuilder {
internal static class StairGuide {
 // Width drag draws a handful of edges, without prefab/mesh/material work.
 internal static void DrawWidth(OverlayRenderSystem overlay,RoutePlan plan,float width){
  if(plan==null)return;var buffer=overlay.GetBuffer(out var dependency);dependency.Complete();
  var color=new Color(.3f,.9f,1f,.95f);
  void Outline(Vector3 start,Vector3 end,float span){
   start+=Vector3.up*.05f;end+=Vector3.up*.05f;var forward=end-start;forward.y=0;if(forward.sqrMagnitude<.000001f)return;
   var half=Vector3.Cross(Vector3.up,forward.normalized)*(span*.5f);
   buffer.DrawLine(color,new Line3.Segment(start-half,end-half),.06f,true);
   buffer.DrawLine(color,new Line3.Segment(start+half,end+half),.06f,true);
   buffer.DrawLine(color,new Line3.Segment(start-half,start+half),.04f,true);
   buffer.DrawLine(color,new Line3.Segment(end-half,end+half),.04f,true);
  }
  foreach(var run in plan.Runs)Outline(run.A,run.B,width);
  foreach(var pad in plan.Pads)Outline(pad.Center-pad.Forward*pad.Length*.5f,pad.Center+pad.Forward*pad.Length*.5f,pad.Width);
  overlay.AddBufferWriter(default);
 }
 internal static void Snap(OverlayRenderSystem overlay,PlacementSnap.Result result,float height){
  var b=overlay.GetBuffer(out var dependency);dependency.Complete();
  foreach(var line in result.Lines){var center=line.Origin+line.Direction*Vector3.Dot(result.Position-line.Origin,line.Direction);center.y=height+.08f;
   var color=line.Active?new Color(.5f,1f,.65f,.95f):new Color(.65f,.85f,1f,.35f);
   float extent=Mathf.Clamp((center-line.Origin).magnitude+4,8,80);
   b.DrawDashedLine(color,new Line3.Segment(center-line.Direction*extent,center+line.Direction*extent),line.Active?.055f:.025f,.65f,.35f);
   if(line.Active)b.DrawCircle(color,new Vector3(result.Position.x,height+.08f,result.Position.z),.25f);
  }
  overlay.AddBufferWriter(default);
 }
 internal static void Support(OverlayRenderSystem overlay,SupportPlan.Pier pier,Color color){
  var buffer=overlay.GetBuffer(out var dependency);dependency.Complete();
  var top=pier.ShaftTop;var bottom=new Vector3(top.x,pier.Bottom,top.z);
  var corners=new[]{new Vector3(-.32f,0,-.32f),new Vector3(.32f,0,-.32f),new Vector3(.32f,0,.32f),new Vector3(-.32f,0,.32f)};
  for(int i=0;i<4;i++){
   int next=(i+1)%4;
   // Native DrawLine rejects segments with zero XZ length, even camera-facing.
   buffer.DrawCustomMesh(color,(top+bottom)*.5f+corners[i],top.y-bottom.y,.0225f,OverlayRenderSystem.CustomMeshType.Cylinder);
   buffer.DrawLine(color,new Line3.Segment(top+corners[i],top+corners[next]),.045f,true);
   buffer.DrawLine(color,new Line3.Segment(bottom+corners[i],bottom+corners[next]),.045f,true);
  }
  var rotation=Quaternion.LookRotation(pier.Forward);
  var right=rotation*Vector3.right*(pier.Width*.5f+.04f);var forward=rotation*Vector3.forward*.36f;
  var edge=new[]{pier.Top-right-forward,pier.Top+right-forward,pier.Top+right+forward,pier.Top-right+forward};
  for(int i=0;i<4;i++)buffer.DrawLine(color,new Line3.Segment(edge[i],edge[(i+1)%4]),.06f,true);
  overlay.AddBufferWriter(default);
 }
 // Drawn each tool frame, independent of prefab loading and preview mesh creation.
 internal static void Draw(OverlayRenderSystem overlay,Stair stair,bool hasMesh,Color? overrideColor=null){
  bool valid=RoutePlan.Build(stair,out var plan,out _);
  var color=!valid?new Color(1f,.22f,.16f,.85f):hasMesh?new Color(.2f,.85f,.95f,.75f):new Color(1f,.65f,.2f,.8f);
  if(overrideColor.HasValue)color=overrideColor.Value;
  if(valid&&plan.Gentle)color=new Color(1f,.65f,.2f,.85f);
  if(valid){foreach(var run in plan.Runs)Segment(overlay,run.A,run.B,stair.Width,color);foreach(var pad in plan.Pads)Segment(overlay,pad.Center-pad.Forward*(pad.Length*.5f),pad.Center+pad.Forward*(pad.Length*.5f),pad.Width,color);}
  else for(int i=1;i<stair.Points.Count;i++)Segment(overlay,stair.Point(i-1),stair.Point(i),stair.Width,color);
 }
 static void Segment(OverlayRenderSystem overlay,Vector3 startPoint,Vector3 endPoint,float width,Color color){
  var a=startPoint+Vector3.up*.035f;var b=endPoint+Vector3.up*.035f;
  var forward=new Vector3(b.x-a.x,0,b.z-a.z);
  var side=forward.sqrMagnitude>.0001f?Vector3.Cross(Vector3.up,forward.normalized):Vector3.right;
  var half=side*(width*.5f);
  var buffer=overlay.GetBuffer(out var dependency);dependency.Complete();
  void Line(Vector3 start,Vector3 end,float width){
   if(new Vector2(end.x-start.x,end.z-start.z).sqrMagnitude<.000001f)return;
   buffer.DrawLine(color,new Line3.Segment(start,end),width,true);
  }
  // A translucent ribbon communicates width; the outline remains visible on top.
  if(forward.sqrMagnitude>.0001f){
   var fill=color;fill.a=.10f;
   buffer.DrawLine(fill,new Line3.Segment(a,b),width);
   Line(a-half,b-half,.045f);Line(a+half,b+half,.045f);
   buffer.DrawDashedLine(color,new Line3.Segment(a,b),.035f,.35f,.20f);
   int divisions=Mathf.Clamp(Mathf.CeilToInt(forward.magnitude/.6f),1,64);
   for(int i=1;i<divisions;i++){
    var p=Vector3.Lerp(a,b,(float)i/divisions);Line(p-half,p+half,.018f);
   }
  }
  Line(a-half,a+half,.055f);Line(b-half,b+half,.055f);
  buffer.DrawCircle(color,a,.16f);buffer.DrawCircle(color,b,.16f);
  // Main-thread writes are complete before handing the buffer back to rendering.
  overlay.AddBufferWriter(default);
 }
}
}

