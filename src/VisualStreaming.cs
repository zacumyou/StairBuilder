using System;
using System.Collections.Generic;
using UnityEngine;
namespace StairBuilder {
public partial class StairSystem {
 readonly List<int> streamNear=new List<int>(),streamFar=new List<int>();
 readonly Plane[] streamPlanes=new Plane[6];float nextStreamScan,nextCacheTrim;
 IEnumerator<int> streamJob;Visual streamResult,streamOriginal;int streamId,streamRevision;bool streamCoarse;
 readonly List<int> restoreVisuals=new List<int>();
 int streamPromoted,streamReleased,streamSlices;double streamMaxMs,streamMaxSliceMs;System.Diagnostics.Stopwatch streamTimer;string streamError="";
 internal static bool StreamingEnabled=true;
 static Bounds WorldBounds(Visual v){var b=v.InstanceBounds;return new Bounds(v.Origin+v.Rotation*b.center,b.size);}
 bool NeedsDetail(Visual v,Camera camera,bool retain)=>NeedsDetail(WorldBounds(v),camera,retain);
 static Bounds DesignBounds(Stair stair){var b=new Bounds(stair.A,Vector3.zero);for(int i=1;i<stair.Points.Count;i++)b.Encapsulate(stair.Point(i));b.Expand(stair.Width+2);return b;}
 bool NeedsDetail(Bounds b,Camera camera,bool retain){
  float d=Mathf.Sqrt(b.SqrDistance(camera.transform.position));
  if(d<(retain?220:160))return true;
  if(!GeometryUtility.TestPlanesAABB(streamPlanes,b))return false;
  float size=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
  float denominator=camera.orthographic?2*camera.orthographicSize:2*Mathf.Max(1,Vector3.Distance(camera.transform.position,b.center))*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
  return size*QualitySettings.lodBias/denominator>(retain?.020f:.030f);
 }
 void StreamVisuals(){
  if(!StreamingEnabled||streamError.Length!=0)return;
  if(streamJob!=null){AdvanceVisualJob();return;}
  if(restoreVisuals.Count>0){
   int restoreId=restoreVisuals[0];restoreVisuals.RemoveAt(0);var saved=Stairs.Find(x=>x.Id==restoreId);
   if(saved!=null&&!visuals.ContainsKey(restoreId)){var view=Camera.main;bool restoreCoarse=true;if(view!=null){GeometryUtility.CalculateFrustumPlanes(view,streamPlanes);restoreCoarse=!NeedsDetail(DesignBounds(saved),view,false);}StartVisualJob(saved,restoreCoarse,null);AdvanceVisualJob();}return;
  }
  if(renderer==null)return;
  var camera=Camera.main;if(camera==null)return;float now=UnityEngine.Time.unscaledTime;
  if(now>=nextCacheTrim){nextCacheTrim=now+1;renderer.TrimCache();}
  if(now>=nextStreamScan){
   nextStreamScan=now+.25f;GeometryUtility.CalculateFrustumPlanes(camera,streamPlanes);
   streamNear.Clear();streamFar.Clear();var near=new List<KeyValuePair<float,int>>();
   foreach(var s in Stairs){if(!visuals.TryGetValue(s.Id,out var v)||Draft?.Id==s.Id||!v.Root.activeSelf)continue;
    if(NeedsDetail(v,camera,!v.Coarse)){v.DetailUntil=now+8;if(v.Coarse)near.Add(new KeyValuePair<float,int>(WorldBounds(v).SqrDistance(camera.transform.position),s.Id));}
    else if(!v.Coarse&&now>=v.DetailUntil)streamFar.Add(s.Id);
   }
   near.Sort((a,b)=>a.Key.CompareTo(b.Key));foreach(var p in near)streamNear.Add(p.Value);
  }
  // Cooperative work slices retain the old visual until the replacement is complete.
  // A single native upload/cache operation can still exceed the requested slice budget.
  var queue=streamNear.Count>0?streamNear:streamFar;if(queue.Count==0)return;
  int id=queue[0];queue.RemoveAt(0);var stair=Stairs.Find(s=>s.Id==id);
  if(stair==null||Draft?.Id==id||!visuals.TryGetValue(id,out var old)||!old.Root.activeSelf)return;
  bool coarse=!NeedsDetail(old,camera,!old.Coarse);if(coarse==old.Coarse||coarse&&now<old.DetailUntil)return;
  StartVisualJob(stair,coarse,old);AdvanceVisualJob();
 }
 void StartVisualJob(Stair stair,bool coarse,Visual old){
  streamRevision=geometryRevision;streamId=stair.Id;streamCoarse=coarse;streamOriginal=old;streamResult=null;streamTimer=System.Diagnostics.Stopwatch.StartNew();
  // Snapshot design and neighbor list. Only this job owns its unfinished hidden root.
  streamJob=Renderer.BuildSteps(stair.Copy(),new List<Stair>(Stairs),coarse,old,v=>streamResult=v).GetEnumerator();
 }
 void CancelVisualJob(){streamJob?.Dispose();streamJob=null;streamResult?.Dispose();streamResult=null;streamOriginal=null;}
 void AdvanceVisualJob(){
  if(streamJob==null)return;
  if(streamRevision!=geometryRevision||!Stairs.Exists(x=>x.Id==streamId)||Draft?.Id==streamId||
     (streamOriginal==null&&visuals.ContainsKey(streamId))||
     (streamOriginal!=null&&(!visuals.TryGetValue(streamId,out var current)||!ReferenceEquals(current,streamOriginal)))){if(streamOriginal==null&&Stairs.Exists(x=>x.Id==streamId)&&!visuals.ContainsKey(streamId))restoreVisuals.Add(streamId);CancelVisualJob();return;}
  var slice=System.Diagnostics.Stopwatch.StartNew();
  try{
   bool more=true;int steps=0;
   using(PerformanceLog.Begin("visual.slice",streamId))while(more&&steps++<128&&slice.Elapsed.TotalMilliseconds<3)more=streamJob.MoveNext();
   if(!more){
    streamJob.Dispose();streamJob=null;
    if(streamResult==null)throw new InvalidOperationException("Visual job completed without a result");
    visuals[streamId]=streamResult;streamResult=null;streamOriginal?.Dispose();streamOriginal=null;
    if(streamCoarse)streamReleased++;else streamPromoted++;
    streamMaxMs=Math.Max(streamMaxMs,streamTimer.Elapsed.TotalMilliseconds);
   }
  }catch(Exception e){CancelVisualJob();streamError=e.ToString();Mod.Log.Error(e,"Stair visual preparation stopped; existing visual preserved");}
  streamSlices++;streamMaxSliceMs=Math.Max(streamMaxSliceMs,slice.Elapsed.TotalMilliseconds);
 }
 void BeginVisualRestore(){
  CancelVisualJob();restoreVisuals.Clear();var view=Camera.main;var order=new List<KeyValuePair<float,int>>();
  if(view!=null)GeometryUtility.CalculateFrustumPlanes(view,streamPlanes);
  foreach(var s in Stairs)if(!visuals.ContainsKey(s.Id)){var bounds=DesignBounds(s);float priority=view==null?0:bounds.SqrDistance(view.transform.position);if(view!=null&&!NeedsDetail(bounds,view,false))priority+=1e12f;order.Add(new KeyValuePair<float,int>(priority,s.Id));}
  order.Sort((a,b)=>{int distance=a.Key.CompareTo(b.Key);return distance!=0?distance:a.Value.CompareTo(b.Value);});foreach(var item in order)restoreVisuals.Add(item.Value);
 }

 object StreamingState(){int detail=0,coarse=0;foreach(var v in visuals.Values){if(v.Coarse)coarse++;else detail++;}return new{enabled=StreamingEnabled,detail,coarse,building=streamJob!=null,restoring=restoreVisuals.Count,slices=streamSlices,maxSliceMs=streamMaxSliceMs,pendingNear=streamNear.Count,pendingFar=streamFar.Count,promoted=streamPromoted,released=streamReleased,maxBuildMs=streamMaxMs,error=streamError};}
 void ResetStreaming(){CancelVisualJob();restoreVisuals.Clear();streamSlices=0;streamMaxSliceMs=0;streamNear.Clear();streamFar.Clear();nextStreamScan=nextCacheTrim=0;streamPromoted=streamReleased=0;streamMaxMs=0;streamError="";StreamingEnabled=true;}
}
}


