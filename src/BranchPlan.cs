using System;
using System.Collections.Generic;
using UnityEngine;
namespace StairBuilder {
internal static class BranchPlan {
 internal static Stair Create(Stair source,int point){
  var origin=source.Points[point];
  return new Stair{Width=source.Width,Style=source.Style,Solid=source.Solid,Railing=source.Railing,DeckColor=source.DeckColor,RailColor=source.RailColor,Ramp=source.Ramp,AutoLandings=true,Points=new List<StairPoint>{origin.Copy(),origin.Copy()}};
 }
 internal static Vector3 EdgeStart(Stair parent,Vector3 origin,Vector3 target){
  var direction=target-origin;direction.y=0;if(direction.sqrMagnitude<.0001f||!RoutePlan.Build(parent,out var plan,out _))return origin;direction.Normalize();
  // Follow the ray through the union of existing flat pads; never add geometry.
  float reach=0;bool changed=true;
  for(int pass=0;pass<plan.Pads.Count&&changed;pass++){
   changed=false;
   foreach(var pad in plan.Pads){
    if(Mathf.Abs(pad.Center.y-origin.y)>.02f)continue;
    var right=Vector3.Cross(Vector3.up,pad.Forward);var d=origin-pad.Center;float enter=float.NegativeInfinity,exit=float.PositiveInfinity;
    bool Slab(float position,float travel,float half){if(Mathf.Abs(travel)<.000001f)return Mathf.Abs(position)<=half+.001f;float a=(-half-position)/travel,b=(half-position)/travel;if(a>b){float t=a;a=b;b=t;}enter=Mathf.Max(enter,a);exit=Mathf.Min(exit,b);return exit>=enter;}
    if(Slab(Vector3.Dot(d,right),Vector3.Dot(direction,right),pad.Width*.5f)&&Slab(Vector3.Dot(d,pad.Forward),Vector3.Dot(direction,pad.Forward),pad.Length*.5f)&&enter<=reach+.01f&&exit>reach){reach=exit;changed=true;}
   }
  }
  return origin+direction*reach;
 }
 internal static bool OnEdge(RoutePlan.Pad pad,Vector3 p){
  if(!Contains(pad,p))return false;var d=p-pad.Center;var right=Vector3.Cross(Vector3.up,pad.Forward);
  return Mathf.Abs(Mathf.Abs(Vector3.Dot(d,right))-pad.Width*.5f)<.02f||Mathf.Abs(Mathf.Abs(Vector3.Dot(d,pad.Forward))-pad.Length*.5f)<.02f;
 }
 internal static bool EdgeContact(Stair endpointOwner,int index,Stair platform){
  if(endpointOwner==platform||(endpointOwner.Id>0&&endpointOwner.Id==platform.Id)||!RoutePlan.Build(platform,out var plan,out _))return false;
  var point=endpointOwner.Point(index);var into=endpointOwner.Point(index==0?1:index-1)-point;into.y=0;if(into.sqrMagnitude<.0001f)return false;into.Normalize();
  return plan.Pads.Exists(p=>OnEdge(p,point)&&!Contains(p,point+into*.05f));
 }
 internal static List<Vector3> Shared(Stair a,Stair b){
  var found=new List<Vector3>();
  if(a==b||(a.Id>0&&a.Id==b.Id))return found;
  for(int i=0;i<a.Points.Count;i++)for(int j=0;j<b.Points.Count;j++){
   // A branch endpoint meets an existing waypoint; ordinary crossings do not join.
   if(i!=0&&i!=a.Points.Count-1&&j!=0&&j!=b.Points.Count-1)continue;
   if((a.Point(i)-b.Point(j)).sqrMagnitude<.0001f)found.Add(a.Point(i));
  }
  foreach(int i in new[]{0,a.Points.Count-1})if(EdgeContact(a,i,b))found.Add(a.Point(i));
  foreach(int j in new[]{0,b.Points.Count-1})if(EdgeContact(b,j,a))found.Add(b.Point(j));
  return found;
 }
 internal static bool Contains(RoutePlan.Pad pad,Vector3 p){
  var d=p-pad.Center;var right=Vector3.Cross(Vector3.up,pad.Forward);
  return Mathf.Abs(d.y)<.02f&&Mathf.Abs(Vector3.Dot(d,right))<=pad.Width*.5f+.02f&&Mathf.Abs(Vector3.Dot(d,pad.Forward))<=pad.Length*.5f+.02f;
 }
 internal static List<RoutePlan.Pad> JunctionPads(Stair s,IEnumerable<Stair> neighbors){
  var pads=new List<RoutePlan.Pad>();if(neighbors==null)return pads;
  foreach(var other in neighbors){var shared=Shared(s,other);if(shared.Count==0||!RoutePlan.Build(other,out var plan,out _))continue;
   foreach(var pad in plan.Pads)if(shared.Exists(p=>Contains(pad,p)))pads.Add(pad);
   // Railing cut only: a direct sloped branch does not need a physical flat pad.
   foreach(var joint in shared)foreach(int endpoint in new[]{0,other.Points.Count-1}){
    if((other.Point(endpoint)-joint).sqrMagnitude>=.0001f)continue;
    var direction=other.Point(endpoint==0?1:endpoint-1)-joint;direction.y=0;if(direction.sqrMagnitude<.0001f)continue;direction.Normalize();
    float length=s.Width*2f;
    pads.Add(new RoutePlan.Pad{Center=joint+direction*(length*.5f-.1f),Forward=direction,Width=other.Width,Length=length+.2f,Openings=new Vector3[0]});
   }
  }return pads;
 }
}
}
