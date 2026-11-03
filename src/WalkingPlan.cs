using System.Collections.Generic;
using UnityEngine;
namespace StairBuilder {
// Merge only collinear, equal-width, coplanar strips. Native area triangulation
// operates in XZ and may flip diagonals across non-planar landing boundaries.
internal sealed class WalkingPlan {
 internal sealed class Strip {internal Vector3 A,B;internal float Width;}
 internal readonly List<Strip> Areas=new List<Strip>();
 internal readonly List<Vector3> Junctions=new List<Vector3>();
 internal void Add(Vector3 a,Vector3 b,float width){Areas.Add(new Strip{A=a,B=b,Width=width});}
 internal static WalkingPlan Build(RoutePlan plan,float width){
  var result=new WalkingPlan();
  foreach(var run in plan.Runs){var p=WalkProfile.Run(run);result.Add(p[0],p[1],width);}
  foreach(var pad in plan.Pads){var p=WalkProfile.Pad(pad);result.Add(p[0],p[2],pad.Width);
   // Turning waypoints remain connection anchors across inset corridor joins.
   // Automatic rest centers do not connect separate areas and need no marker.
   if(pad.SupportId<100&&Vector3.Dot(pad.Openings[0],pad.Openings[1])>-.9999f)result.Junctions.Add(p[1]);
  }
  result.Merge();return result;
 }
 internal void Merge(){
  bool changed;
  do{changed=false;for(int i=0;i<Areas.Count&&!changed;i++)for(int j=i+1;j<Areas.Count;j++){
   if(!Join(Areas[i],Areas[j],out var merged))continue;
   Areas[i]=merged;Areas.RemoveAt(j);changed=true;break;
  }}while(changed);
 }
 static bool Join(Strip a,Strip b,out Strip merged){
  merged=null;if(Mathf.Abs(a.Width-b.Width)>.001f)return false;
  var delta=a.B-a.A;var axis=delta;axis.y=0;float length=axis.magnitude;if(length<.001f)return false;axis/=length;
  var right=Vector3.Cross(Vector3.up,axis);float slope=delta.y/length;
  float Project(Vector3 p)=>Vector3.Dot(p-a.A,axis);
  bool OnLine(Vector3 p)=>Mathf.Abs(Vector3.Dot(p-a.A,right))<.001f&&Mathf.Abs(p.y-a.A.y-slope*Project(p))<.001f;
  if(!OnLine(b.A)||!OnLine(b.B))return false;
  float x=Project(b.A),y=Project(b.B),lo=Mathf.Min(x,y),hi=Mathf.Max(x,y);
  // No tolerance expansion: never fill a real gap between disconnected paths.
  if(lo>length+.00001f||hi<-.00001f)return false;
  float start=Mathf.Min(0,lo),end=Mathf.Max(length,hi);
  merged=new Strip{A=a.A+axis*start+Vector3.up*(slope*start),B=a.A+axis*end+Vector3.up*(slope*end),Width=a.Width};return true;
 }
}
}
