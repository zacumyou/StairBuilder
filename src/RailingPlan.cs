using System;
using System.Collections.Generic;
using UnityEngine;
namespace StairBuilder {
internal sealed class RailingPlan {
 internal struct Bar {internal Vector3 A,B;}
 internal readonly List<Bar> Bars=new List<Bar>();
 internal readonly List<Vector3> Posts=new List<Vector3>();
 const float Inset=.08f;float postSpacing=1.2f;
 static bool Same(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<.000001f;
 void Post(Vector3 p){foreach(var old in Posts)if(Same(old,p))return;Posts.Add(p);}
 void Add(Vector3 a,Vector3 b,bool subdivide=false){
  if(Same(a,b))return;
  foreach(var old in Bars)if((Same(a,old.A)&&Same(b,old.B))||(Same(a,old.B)&&Same(b,old.A)))return;
  Bars.Add(new Bar{A=a,B=b});Post(a);Post(b);
  if(subdivide){int bays=Mathf.CeilToInt((b-a).magnitude/postSpacing);for(int i=1;i<bays;i++)Post(Vector3.Lerp(a,b,(float)i/bays));}
 }
 static Vector3 Port(RoutePlan.Pad pad,Vector3 direction)=>pad.Center+direction*pad.ConnectionDistance;
 // Intersection of this passage's side with the inset platform perimeter.
 internal static Vector3 Mouth(RoutePlan.Pad pad,Vector3 direction,Vector3 side){
  var right=Vector3.Cross(Vector3.up,pad.Forward);
  float x=Vector3.Dot(side,right),z=Vector3.Dot(side,pad.Forward);
  float dx=Vector3.Dot(direction,right),dz=Vector3.Dot(direction,pad.Forward);
  float distance=float.PositiveInfinity;
  if(Mathf.Abs(dx)>.00001f)distance=Mathf.Min(distance,((dx>0?1:-1)*(pad.Width*.5f-Inset)-x)/dx);
  if(Mathf.Abs(dz)>.00001f)distance=Mathf.Min(distance,((dz>0?1:-1)*(pad.Length*.5f-Inset)-z)/dz);
  // At a tight bend the two inner passage sides meet at a shared corner,
  // where the passage sides overlap. Use that common miter junction.
  foreach(var other in pad.Openings){
   float denominator=Vector3.Cross(direction,other).y;if(Mathf.Abs(denominator)<.00001f)continue;
   var boundary=side+direction*distance;
   if(Vector3.Dot(boundary,other)<=0||Mathf.Abs(Vector3.Cross(boundary,other).y)>=side.magnitude-.00001f)continue;
   foreach(int sign in new[]{-1,1}){
    var origin=Vector3.Cross(Vector3.up,other)*(side.magnitude*sign);
    float t=Vector3.Cross(origin-side,other).y/denominator;
    float u=Vector3.Cross(origin-side,direction).y/denominator;
    if(t>=0&&u>=0)distance=t;
   }
  }
  return pad.Center+side+direction*distance;
 }
 static Vector3 Anchor(RoutePlan plan,Vector3 endpoint,Vector3 intoRun,Vector3 side){
  foreach(var pad in plan.Pads)foreach(var direction in pad.Openings)
   if(Same(Port(pad,direction),endpoint)&&Vector3.Dot(direction,intoRun)>.999f)return Mouth(pad,direction,side);
  return endpoint+side;
 }
 internal RailingPlan OpenJunctions(List<RoutePlan.Pad> openings){
  if(openings.Count==0)return this;
  var bars=new List<Bar>(Bars);
  foreach(var pad in openings){
   var remaining=new List<Bar>();var right=Vector3.Cross(Vector3.up,pad.Forward);
   foreach(var bar in bars){
    var a=bar.A-pad.Center;var delta=bar.B-bar.A;float lo=0,hi=1;
    bool Slab(float start,float travel,float extent){
     if(Mathf.Abs(travel)<.000001f)return Mathf.Abs(start)<extent;
     float first=(-extent-start)/travel,last=(extent-start)/travel;
     if(first>last){float tmp=first;first=last;last=tmp;}lo=Mathf.Max(lo,first);hi=Mathf.Min(hi,last);return hi>lo+.000001f;
    }
    bool inside=Slab(Vector3.Dot(a,right),Vector3.Dot(delta,right),pad.Width*.5f-Inset)&&Slab(Vector3.Dot(a,pad.Forward),Vector3.Dot(delta,pad.Forward),pad.Length*.5f+.01f)&&Slab(a.y,delta.y,.04f);
    if(!inside){remaining.Add(bar);continue;}
    if(lo>.00001f)remaining.Add(new Bar{A=bar.A,B=Vector3.Lerp(bar.A,bar.B,lo)});
    if(hi<.99999f)remaining.Add(new Bar{A=Vector3.Lerp(bar.A,bar.B,hi),B=bar.B});
   }bars=remaining;
  }
  var result=new RailingPlan{postSpacing=postSpacing};foreach(var bar in bars)result.Add(bar.A,bar.B,true);return result;
 }
 internal static RailingPlan Build(RoutePlan plan,float width,bool continuous=false,float spacing=1.2f){
  var result=new RailingPlan{postSpacing=spacing};float half=width*.5f-Inset;
  foreach(var run in plan.Runs){
   var forward=run.B-run.A;forward.y=0;forward.Normalize();var right=Vector3.Cross(Vector3.up,forward);
   foreach(int sign in new[]{-1,1}){
    var side=right*(sign*half);
    var last=Anchor(plan,run.A,forward,side);
    if(run.Ramp||continuous){result.Add(last,Anchor(plan,run.B,-forward,side),true);continue;}
    foreach(int step in Layout.Posts(run.Steps)){
     var p=run.A+side+forward*((step+.5f)*run.Tread)+Vector3.up*((step+1)*run.Rise);
     result.Add(last,p);last=p;
    }
    result.Add(last,Anchor(plan,run.B,-forward,side));
   }
  }
  foreach(var pad in plan.Pads){
   var right=Vector3.Cross(Vector3.up,pad.Forward);
   var corners=new[]{pad.Center-right*(pad.Width*.5f-Inset)-pad.Forward*(pad.Length*.5f-Inset),pad.Center+right*(pad.Width*.5f-Inset)-pad.Forward*(pad.Length*.5f-Inset),pad.Center+right*(pad.Width*.5f-Inset)+pad.Forward*(pad.Length*.5f-Inset),pad.Center-right*(pad.Width*.5f-Inset)+pad.Forward*(pad.Length*.5f-Inset)};
   var outward=new[]{-pad.Forward,right,pad.Forward,-right};
   for(int edge=0;edge<4;edge++){
    var a=corners[edge];var b=corners[(edge+1)%4];var cuts=new List<float>{0,1};
    void Cut(float va,float vb,float target){if(Mathf.Abs(vb-va)<.000001f)return;float t=(target-va)/(vb-va);if(t>0&&t<1)cuts.Add(t);}
    foreach(var direction in pad.Openings){
     if(Vector3.Dot(outward[edge],direction)<=.001f)continue;
     Cut(Vector3.Dot(a-pad.Center,direction),Vector3.Dot(b-pad.Center,direction),0);
     float va=Vector3.Cross(a-pad.Center,direction).y,vb=Vector3.Cross(b-pad.Center,direction).y;
     Cut(va,vb,half);Cut(va,vb,-half);
    }
    cuts.Sort();
    for(int i=1;i<cuts.Count;i++){
     if(cuts[i]-cuts[i-1]<.000001f)continue;
     var p=Vector3.Lerp(a,b,(cuts[i]+cuts[i-1])*.5f);bool open=false;
     foreach(var direction in pad.Openings)
      if(Vector3.Dot(outward[edge],direction)>.001f&&Vector3.Dot(p-pad.Center,direction)>0&&Mathf.Abs(Vector3.Cross(p-pad.Center,direction).y)<half+.00001f){open=true;break;}
     if(!open)result.Add(Vector3.Lerp(a,b,cuts[i-1]),Vector3.Lerp(a,b,cuts[i]),true);
    }
   }
  }
  // Connect only coincident, opposing passage ports, never nearest free ends.
  for(int i=0;i<plan.Pads.Count;i++)for(int j=i+1;j<plan.Pads.Count;j++){
   var a=plan.Pads[i];var b=plan.Pads[j];
   foreach(var da in a.Openings)foreach(var db in b.Openings){
    if(Vector3.Dot(da,db)>-.999f||!Same(Port(a,da),Port(b,db)))continue;
    foreach(int sign in new[]{-1,1}){
     var side=Vector3.Cross(Vector3.up,da)*(half*sign);
     result.Add(Mouth(a,da,side),Mouth(b,db,side),true);
    }
   }
  }
  return result;
 }
}
}

