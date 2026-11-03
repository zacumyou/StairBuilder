using System;
using System.Collections.Generic;
using UnityEngine;
namespace StairBuilder {
// Derived visual data: no new saved entities or changes to pedestrian topology.
internal static class SupportPlan {
 internal const float MaxSpacing=8f;
 internal static bool RemovedWithLastPoint(int key,int lastPoint){
  if(key>=1000000)return ((key-1000000)/1000)/16==lastPoint-1;
  int pad=key/1000;
  if(pad<100)return pad>=lastPoint-1;
  return pad<200?pad-100==lastPoint-1:(pad-200)/16==lastPoint-1;
 }
 internal sealed class Pier {internal int Key;internal Vector3 Top,Forward,Up;internal Vector3 ShaftTop=>Top-Up*.40f;internal float Width,Bottom;}
 // Match the nearest flight's underside, excluding the riser above its slope line.
 internal static float PadThickness(RoutePlan route,RoutePlan.Pad pad,int style){
  float rise=.17f,best=float.PositiveInfinity;bool nearestRamp=false;
  foreach(var run in route.Runs){

   foreach(var end in new[]{run.A,run.B}){
    var v=end-pad.Center;float along=Mathf.Max(0,Mathf.Abs(Vector3.Dot(v,pad.Forward))-pad.Length*.5f);
    float across=Mathf.Max(0,Mathf.Abs(Vector3.Dot(v,Vector3.Cross(Vector3.up,pad.Forward)))-pad.Width*.5f);
    float distance=along*along+across*across+v.y*v.y;
    if(distance<best){best=distance;rise=run.Rise;nearestRamp=run.Ramp;}
   }
  }
  if(nearestRamp)return style==1||style==3?.50f:.32f;
  return (style==1||style==3?.33f:.15f)*rise/.17f;
 }
 internal static List<Pier> Build(RoutePlan route,float width,Func<Vector3,float> terrain,ISet<int> hidden=null,bool steel=false,int style=0,bool rampDeck=false){
  var result=new List<Pier>();
  float extra=style==1||style==3?.18f:0;
  bool ramp=rampDeck;
  float spacing=steel?4f:MaxSpacing;float minimumSpacing=steel?1.2f:Mathf.Max(2.5f,width*.85f);
  void Add(int key,Vector3 top,Vector3 forward,float bearingWidth,bool landing=false){
   var up=Vector3.Cross(forward.normalized,Vector3.Cross(Vector3.up,forward).normalized);
   var center=top-up*.40f;
   // Sample the full shaft footprint so its lower edge stays buried on slopes.
   float ground=terrain(center);
   foreach(float x in new[]{-.30f,.30f})foreach(float z in new[]{-.30f,.30f})
    ground=Mathf.Min(ground,terrain(center+new Vector3(x,0,z)));
   if(top.y-ground<.55f)return; // Deck already rests on the ground.
   foreach(var old in result){
    var delta=old.Top-top;
    // Compare the occupied height intervals, not only the top elevations.
    // Two nearby columns at different deck heights can still overlap below.
    if(new Vector2(delta.x,delta.z).magnitude<(landing?.7f:minimumSpacing)&&
       Mathf.Min(old.Top.y,top.y)>Mathf.Max(old.Bottom,ground-.20f))return;
   }
   result.Add(new Pier{Key=key,Top=top,Forward=forward,Up=up,Width=bearingWidth,Bottom=ground-.20f});
  }
  // Corners occur first in RoutePlan: they take priority over nearby span supports.
  for(int padIndex=0;padIndex<route.Pads.Count;padIndex++){
   var pad=route.Pads[padIndex];
   // Use the same slab depth as the renderer, including the surface clearance.
   void At(int slot,float distance){
    Add(pad.SupportId*1000+slot,pad.Center+pad.Forward*distance+Vector3.up*(steel?-.335f:-(ramp?.32f+extra:PadThickness(route,pad,style))-.003f+.015f),
     pad.Forward,Mathf.Min(width,pad.Width)*.8f,slot==0);
   }
   At(0,0);
   int half=Mathf.CeilToInt(pad.Length*.5f/spacing);
   for(int i=1;i<=half;i++){
    float d=Mathf.Min(i*spacing,pad.Length*.5f-.4f);
    if(d>.8f){At(i*2-1,-d);At(i*2,d);}
   }
  }
  for(int runIndex=0;runIndex<route.Runs.Count;runIndex++){
   var run=route.Runs[runIndex];
   var delta=run.B-run.A;var horizontal=delta;horizontal.y=0;
   float length=horizontal.magnitude;var forward=horizontal/length;
   int bays=Math.Max(1,Mathf.CeilToInt(length/spacing));
   // Even a single inclined flight gets a central support when elevated.
   for(int i=0;i<bays;i++){
    float t=(i+.5f)/bays;
    Add(1000000+run.SupportId*1000+i,Vector3.Lerp(run.A,run.B,t)+(steel?Vector3.up*(-.32f/(horizontal.magnitude/delta.magnitude)-.015f):run.Ramp?(Quaternion.LookRotation(delta)*Vector3.up)*(-.32f-extra+.015f)-Vector3.up*.003f:Vector3.up*(-(.15f+extra)*run.Rise/.17f+.015f)),
     delta,width*.8f);
   }
  }
  // Remove after spacing resolution so a deleted pier is not immediately replaced
  // by a candidate that used to be suppressed for being too close.
  if(hidden!=null)result.RemoveAll(p=>hidden.Contains(p.Key));
  return result;
 }
}
}


