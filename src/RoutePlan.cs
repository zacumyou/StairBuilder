using System;
using System.Collections.Generic;
using UnityEngine;
namespace StairBuilder {
internal sealed class RoutePlan {
 internal sealed class Run {internal Vector3 A,B;internal bool Ramp;internal int Steps,SupportId;internal float Rise,Tread;}
 internal sealed class Pad {
  internal int SupportId;internal Vector3 Center,Forward;internal float Width,Length,ConnectionDistance;internal Vector3[] Openings;
  internal bool IsOpening(Vector3 p,Vector3 outward,float passageWidth){
   foreach(var dir in Openings){
    // A passage cuts the edge it crosses, never the parallel side rails.
    if(Vector3.Dot(outward,dir)<=.001f)continue;
    var v=p-Center;
    if(Vector3.Dot(v,dir)>0&&Mathf.Abs(Vector3.Cross(v,dir).y)<passageWidth*.5f+.04f)return true;
   }
   return false;
  }
 }
 internal readonly List<Run> Runs=new List<Run>();internal readonly List<Pad> Pads=new List<Pad>();
 internal bool Gentle;
 internal int Steps;internal float Rise,Tread;
 internal static bool Build(Stair s,out RoutePlan plan,out string error){
  plan=new RoutePlan();error="range";if(s.Points.Count<2||s.Points.Count>32||!WidthPolicy.Valid(s.Width))return false;
  var directions=new Vector3[s.Points.Count-1];
  for(int i=0;i<s.Points.Count;i++){var p=s.Point(i);if(!Layout.Finite(p.x)||!Layout.Finite(p.y)||!Layout.Finite(p.z))return false;}
  for(int i=0;i<directions.Length;i++){var v=s.Point(i+1)-s.Point(i);v.y=0;if(v.magnitude<.6f||v.magnitude>100)return false;directions[i]=v.normalized;}
  for(int i=1;i<s.Points.Count-1;i++){
   var before=directions[i-1];var after=directions[i];float dot=Vector3.Dot(before,after);
   if(dot<-.5f){error="turn";return false;}
   float extent=s.Width*(Mathf.Abs(dot)+Mathf.Abs(Vector3.Dot(Vector3.Cross(Vector3.up,before),after)));
   plan.Pads.Add(new Pad{SupportId=i,Center=s.Point(i),Forward=before,ConnectionDistance=s.Width*.5f,Width=Mathf.Max(s.Width,extent),Length=Mathf.Max(s.Width,extent),Openings=new[]{-before,after}});
  }
  for(int segment=0;segment<directions.Length;segment++){
   var a=s.Point(segment);var b=s.Point(segment+1);var direction=directions[segment];
   if(segment>0)a+=direction*(s.Width*.5f);
   if(segment<directions.Length-1)b-=direction*(s.Width*.5f);
   float length=Vector3.Dot(b-a,direction),height=Mathf.Abs(b.y-a.y);
   if(length<.6f){error="landingSpace";return false;}
   if(height<.001f){plan.Pads.Add(new Pad{SupportId=100+segment,Center=(a+b)*.5f,Forward=direction,ConnectionDistance=length*.5f,Width=s.Width,Length=length,Openings=new[]{-direction,direction}});continue;}
   if(s.Ramp){
    int flights=s.AutoLandings?Math.Max(1,Mathf.CeilToInt(length/9f)):1;
    float travel=length-(flights-1)*1.5f;
    if(travel<=0||height/travel>1f/12f+.00001f){error="rampSlope";return false;}
    var origin=a.y<=b.y?a:b;var finish=a.y<=b.y?b:a;var along=finish-origin;along.y=0;along.Normalize();
    for(int k=0;k<flights;k++){
     var from=origin+along*(k*(travel/flights+1.5f))+Vector3.up*(height*k/flights);
     var to=from+along*(travel/flights)+Vector3.up*(height/flights);
     plan.Runs.Add(new Run{Ramp=true,SupportId=segment*16+k,A=from,B=to});
     if(k<flights-1)plan.Pads.Add(new Pad{SupportId=200+segment*16+k+1,Center=to+along*.75f,Forward=along,ConnectionDistance=.75f,Width=s.Width,Length=1.5f,Openings=new[]{-along,along}});
    }
    continue;
   }
   int count=Math.Max(1,(int)Math.Round(height/.17f));int rests=s.AutoLandings?(count-1)/15:0;
   float runLength=length-rests*1.5f;
   if(!Layout.Solve(runLength,height,s.Width,out int n,out float rise,out float tread,out error)){
    int candidate=Math.Max(1,(int)Math.Round(height/.17f));
    if(height<=30&&length<=100&&height/length<=.17f/.60f&&(height<.12f||runLength/candidate>.60f)){
     plan.Runs.Add(new Run{Ramp=true,SupportId=segment*16,A=a.y<=b.y?a:b,B=a.y<=b.y?b:a});
     plan.Gentle=true;continue;
    }
    return false;
   }
   var low=a.y<=b.y?a:b;var high=a.y<=b.y?b:a;var forward=high-low;forward.y=0;forward.Normalize();
   int completed=0;var cursor=low;
   while(completed<n){
    int steps=s.AutoLandings?Math.Min(15,n-completed):n;
    var end=cursor+forward*(steps*tread)+Vector3.up*(steps*rise);
    plan.Runs.Add(new Run{SupportId=segment*16+completed/15,A=cursor,B=end,Steps=steps,Rise=rise,Tread=tread});completed+=steps;cursor=end;
    if(completed<n){plan.Pads.Add(new Pad{SupportId=200+segment*16+completed/15,Center=cursor+forward*.75f,Forward=forward,ConnectionDistance=.75f,Width=s.Width,Length=1.5f,Openings=new[]{-forward,forward}});cursor+=forward*1.5f;}
   }
   plan.Steps+=n;plan.Rise+=rise*n;plan.Tread+=tread*n;
  }
  if(plan.Steps>720){error="range";return false;}
  if(plan.Steps>0){plan.Rise/=plan.Steps;plan.Tread/=plan.Steps;}error="";return true;
 }
}
}
