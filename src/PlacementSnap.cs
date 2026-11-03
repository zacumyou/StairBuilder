using System.Collections.Generic;
using UnityEngine;
namespace StairBuilder {
internal static class PlacementSnap {
 internal struct Line { internal Vector3 Origin,Direction; internal bool Active; }
 internal sealed class Result { internal Vector3 Position; internal readonly List<Line> Lines=new List<Line>(); }
 internal static Result Solve(Stair stair,int index,Vector3 raw,bool directions,bool alignment,bool rightAngle,float radius){
  var r=new Result{Position=raw};if(index<2||index>=stair.Points.Count)return r;
  var last=stair.Point(index-1);var axis=last-stair.Point(index-2);axis.y=0;if(axis.sqrMagnitude<.0001f)return r;axis.Normalize();
  var perpendicular=Vector3.Cross(Vector3.up,axis);radius=Mathf.Clamp(radius,.1f,4f);
  if(rightAngle)r.Position=PointSnap.RightAngle(stair,index,raw);
  var probe=r.Position;
  void Add(Vector3 origin,Vector3 direction,bool force=false){origin.y=raw.y;var projected=origin+direction*Vector3.Dot(probe-origin,direction);if(!force&&(projected-probe).sqrMagnitude>radius*radius*9)return;
   foreach(var line in r.Lines)if(Mathf.Abs(Vector3.Dot(line.Direction,direction))>.9999f&&Vector3.Cross(origin-line.Origin,direction).sqrMagnitude<.0001f)return;
   r.Lines.Add(new Line{Origin=origin,Direction=direction});}
  if(directions||rightAngle){if(!rightAngle)Add(last,axis);Add(last,perpendicular,rightAngle);}
  var first=stair.Point(1)-stair.Point(0);first.y=0;if(first.sqrMagnitude<.0001f)first=axis;first.Normalize();
  if(alignment)for(int i=0;i<stair.Points.Count;i++){if(i==index)continue;Add(stair.Point(i),first);Add(stair.Point(i),Vector3.Cross(Vector3.up,first));}
  float best=-1;int aBest=-1,bBest=-1;
  void Offer(Vector3 p,int a,int b){float distance=(p-probe).magnitude;if(distance>radius)return;if(rightAngle&&Mathf.Abs(Vector3.Dot(p-last,axis))>.001f)return;
   float quality=(b>=0?2:1)*(1-distance/radius);if(quality<=best)return;best=quality;r.Position=p;aBest=a;bBest=b;}
  for(int i=0;i<r.Lines.Count;i++){var a=r.Lines[i];Offer(a.Origin+a.Direction*Vector3.Dot(probe-a.Origin,a.Direction),i,-1);
   for(int j=0;j<i;j++){var b=r.Lines[j];float den=a.Direction.x*b.Direction.z-a.Direction.z*b.Direction.x;if(Mathf.Abs(den)<.0001f)continue;var d=b.Origin-a.Origin;float t=(d.x*b.Direction.z-d.z*b.Direction.x)/den;Offer(a.Origin+a.Direction*t,i,j);}}
  r.Position=new Vector3(r.Position.x,raw.y,r.Position.z);
  for(int i=0;i<r.Lines.Count;i++){var line=r.Lines[i];line.Active=i==aBest||i==bBest;r.Lines[i]=line;}
  return r;
 }
}
}
