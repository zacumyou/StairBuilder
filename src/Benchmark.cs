using System;
using System.Collections.Generic;
using UnityEngine;
namespace StairBuilder {
public partial class StairSystem {
 bool benchmarkStress;int stressPoints,stressWidths;bool benchmarkWood;bool benchmarkColors;int colorIndex=-1,colorVerified,inspectIndex=-1;
 bool benchmarkMixed;int verifyIndex=-1,verifiedRays;object surfaceProbe;bool surfaceProbePending;
 bool benchmarkEditPending,benchmarkArmed,benchmarkRunning,benchmarkPreviousLogging;int benchmarkTarget,benchmarkMade;float benchmarkX,benchmarkZ;string benchmarkError="";readonly HashSet<int> benchmarkIds=new HashSet<int>();
 // Narrow optional CityBridge adapter. Requests enqueue work; ToolUpdate owns creation.
 public object StairBenchmark(string action,int target,float x,float z){
  if(!Ready)throw new InvalidOperationException("City not ready");
  if(action.StartsWith("width-memory-",StringComparison.Ordinal)){QueueWidthTest(action,target,x);}else if(action=="width-stress"){if(!benchmarkArmed||benchmarkMade!=0||benchmarkRunning||Draft!=null)throw new InvalidOperationException("Empty isolated test required");benchmarkStress=true;}else if(action=="width-resume"){if(benchmarkArmed||Draft!=null)throw new InvalidOperationException("Resume requires inactive adapter and no draft");benchmarkArmed=true;benchmarkPreviousLogging=Mod.Options.PerformanceLogging;benchmarkX=x;benchmarkZ=z;foreach(var stair in Stairs)benchmarkIds.Add(stair.Id);}else if(action.StartsWith("width-template-",StringComparison.Ordinal)){QueueWidthTest(action,target,x);}else if(action=="width-fixture"||action=="width-set"||action=="width-commit"||action=="width-cancel"||action=="width-edit"){QueueWidthTest(action,target,x);}else if(action=="arm-unsaved-test-city"){
   if(Stairs.Count!=0||Draft!=null||benchmarkArmed)throw new InvalidOperationException("Requires an empty staircase set and no draft");
   benchmarkPreviousLogging=Mod.Options.PerformanceLogging;Mod.Options.PerformanceLogging=true;benchmarkArmed=true;benchmarkX=x;benchmarkZ=z;benchmarkMade=0;benchmarkIds.Clear();benchmarkError="";
  }else if(action=="mixed"){
   if(!benchmarkArmed||benchmarkMade!=0||benchmarkRunning)throw new InvalidOperationException("Choose mixture before population");benchmarkMixed=true;
  }else if(action=="wood"){if(!benchmarkArmed||benchmarkMade!=0||benchmarkRunning)throw new InvalidOperationException("Choose wood before population");benchmarkWood=true;}else if(action=="colors"){
   if(!benchmarkArmed||benchmarkMade!=0||benchmarkRunning)throw new InvalidOperationException("Choose colors before population");benchmarkColors=true;
  }else if(action=="inspect"||action=="color-probe"){
   if(!benchmarkArmed||benchmarkRunning||Draft!=null||Stairs.Count==0||colorIndex>=0)throw new InvalidOperationException("Requires completed isolated benchmark and no draft");
   if(action=="inspect"){if(target<0||target>=Stairs.Count)throw new ArgumentOutOfRangeException("target");inspectIndex=target;}else{colorIndex=0;colorVerified=0;}
  }else if(action=="populate"){
   if(!benchmarkArmed||benchmarkRunning||Stairs.Count!=benchmarkIds.Count||Draft!=null)throw new InvalidOperationException("Benchmark not isolated or already running");
   if(target<=benchmarkMade||target>1000)throw new ArgumentOutOfRangeException("target");
   benchmarkTarget=target;benchmarkRunning=true;benchmarkError="";
  }else if(action=="render-mode"){InstanceRenderer.SetMode(target);
  }else if(action=="capture"){
   var folder=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"Low","Colossal Order","Cities Skylines II","ModsData","StairBuilder","Diagnostics");System.IO.Directory.CreateDirectory(folder);ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,"benchmark.png"));
  }else if(action=="edit-probe"){
   if(!benchmarkArmed||benchmarkRunning||Draft!=null||Stairs.Count==0)throw new InvalidOperationException("Requires completed isolated benchmark");
   benchmarkEditPending=true;
  }else if(action=="verify-picking"||action=="surface-probe"){
   if(!benchmarkArmed||benchmarkRunning||Draft!=null||Stairs.Count==0)throw new InvalidOperationException("Requires completed isolated benchmark");
   if(action=="verify-picking"){verifyIndex=0;verifiedRays=0;}else {if(surfaceProbe!=null||surfaceProbePending)throw new InvalidOperationException("Surface probe already submitted");surfaceProbePending=true;}
  }else if(action=="stop"){benchmarkRunning=false;}
  else if(action!="state")throw new ArgumentException("Unknown benchmark action");
  var combinationsSeen=new HashSet<string>();foreach(var stair in Stairs)combinationsSeen.Add(stair.Style+":"+stair.Railing+":"+stair.Width);
  var uniquePick=new HashSet<Visual.PickGeometry>();long pickBytes=0;int chunks=0;foreach(var v in visuals.Values)foreach(var c in v.PickChunks){chunks++;if(uniquePick.Add(c.Geometry))pickBytes+=(long)c.Geometry.Vertices.Length*12+(long)c.Geometry.Indices.Length*4;}
  int renderers=0,meshes=0,vertices=0;foreach(var v in visuals.Values){renderers+=v.RendererCount;meshes+=v.Meshes.Count;vertices+=v.PickVertexCount;}
  var sample=target>=0&&target<Stairs.Count?Stairs[target]:null;
  return new{adapterVersion=10,moveIt=MoveItTestState(),PreviewReuses,streaming=StreamingState(),templateTestRequest=TemplateTestRequest,templateTestReport=TemplateTestReport,templates=TemplatesJson,groupPlacement=GroupPlacement,version=typeof(Mod).Assembly.GetName().Version.ToString(),benchmarkStress,stressPoints,stressWidths,widthTestPending=widthTestAction!=null,benchmarkColors,colorIndex,colorVerified,inspectIndex,ui=new{Open,HaveA,HaveB,MovingPoint,ActivePoint,CanCommit,Status,draft=Draft==null?null:new{Draft.Id,Draft.Style,Draft.Width,Draft.Railing,Draft.DeckColor,Draft.RailColor},previewColors=preview?.Colors.State},sample=sample==null?null:new{sample.Id,sample.Style,sample.Width,sample.Railing,sample.DeckColor,sample.RailColor,area=sample.Area.ToString(),accessA=sample.AccessA.ToString(),accessB=sample.AccessB.ToString(),colors=visuals.TryGetValue(sample.Id,out var sampleVisual)?sampleVisual.Colors.State:null},mixed=benchmarkMixed,combinations=combinationsSeen.Count,pickSharedSources=uniquePick.Count,pickSourceBytes=pickBytes,pickChunks=chunks,editPending=benchmarkEditPending,verifyIndex,verifiedRays,surfaceProbePending,surfaceProbe,surfaceObjects=SurfaceObjectState(),cache=renderer?.CacheStats,instanceError=InstanceRenderer.Error,instanceDrawCalls=InstanceRenderer.DrawCalls,activeInstances=InstanceRenderer.InstanceCount,armed=benchmarkArmed,running=benchmarkRunning,target=benchmarkTarget,created=benchmarkMade,stairs=Stairs.Count,renderers,meshes,vertices,error=benchmarkError,originX=benchmarkX,originZ=benchmarkZ};
 }
 void BenchmarkTick(){
  if(widthTestAction!=null){WidthTestTick();return;}
  if(inspectIndex>=0){int index=inspectIndex;inspectIndex=-1;Open=true;Activate();SelectStair(Stairs[index].Id);return;}
  if(colorIndex>=0){try{
   var saved=Stairs[colorIndex];int dc=saved.DeckColor,rc=saved.RailColor;Open=true;Activate();SelectStair(saved.Id);var before=preview;
   Command("deckColor:14710866");Command("railColor:7190760");if(preview!=before)throw new InvalidOperationException("Color edit rebuilt geometry");preview.Colors.Verify();
   if(saved.DeckColor!=dc||saved.RailColor!=rc)throw new InvalidOperationException("Draft mutated saved colors");
   Command("deckColor:-1");Command("railColor:-1");preview.Colors.Verify();Cancel();
   if(visuals[saved.Id].Root.activeSelf!=true)throw new InvalidOperationException("Cancel did not restore original");visuals[saved.Id].Colors.Verify();
   colorVerified++;colorIndex++;if(colorIndex>=Math.Min(140,Stairs.Count)){colorIndex=-1;Command("toggle");}
  }catch(Exception e){colorIndex=-1;benchmarkError=e.ToString();Cancel();Mod.Log.Error(e,"Color probe failed");}return;}

  if(verifyIndex>=0){try{VerifyPick(Stairs[verifyIndex]);verifyIndex++;if(verifyIndex>=Math.Min(140,Stairs.Count))verifyIndex=-1;}catch(Exception e){verifyIndex=-1;benchmarkError=e.ToString();Mod.Log.Error(e,"Pick verification failed");}return;}
  if(surfaceProbePending){surfaceProbePending=false;try{surfaceProbe=ProbeSurface();}catch(Exception e){benchmarkError=e.ToString();Mod.Log.Error(e,"Surface probe failed");}return;}
  if(benchmarkEditPending){benchmarkEditPending=false;try{SelectStair(Stairs[0].Id);BeginPointMove(1);Draft.Points[1].Offset+=.25f;Rebuild();if(preview!=null)throw new InvalidOperationException("Moving preview unexpectedly assembled");MovingPoint=-1;HaveB=true;Rebuild();if(!CanCommit)throw new InvalidOperationException("Edit preview invalid");Commit();}catch(Exception e){benchmarkError=e.ToString();Mod.Log.Error(e,"Edit probe failed");}return;}
  if(!benchmarkRunning)return;
  try{
   if(Stairs.Count!=benchmarkIds.Count||Draft!=null)throw new InvalidOperationException("User state changed; stopping benchmark");
   int i=benchmarkMade;var a=new Vector3(benchmarkX+(i%20)*8,0,benchmarkZ+(i/20)*14);var b=a+Vector3.forward*8;
   var data=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
   a.y=Game.Simulation.TerrainUtils.SampleHeight(ref data,(Unity.Mathematics.float3)a);b.y=Game.Simulation.TerrainUtils.SampleHeight(ref data,(Unity.Mathematics.float3)b);
   int combination=i%140;
   Draft=benchmarkMixed?new Stair{Width=1+combination%4,Style=combination/28,Railing=(combination/4)%7-1}:new Stair{Width=2,Style=1};if(benchmarkWood){Draft.Style=5;Draft.Width=1+i%4;Draft.Railing=-1;}Draft.Points[0].AtHeight(a,a.y+1);Draft.Points[1].AtHeight(b,a.y+4.4f);
   if(benchmarkColors){int[] colors={0xf3f1e7,0xaab2b6,0x414a52,0xe37852,0xe8bd62,0x78b99b,0x6db8e8,0xb699d1};Draft.DeckColor=colors[i%colors.Length];Draft.RailColor=colors[(i+3)%colors.Length];}
   HaveA=HaveB=true;MovingPoint=-1;ActivePoint=1;
   if(benchmarkStress)StressDraft(i);
   Rebuild();if(!CanCommit)throw new InvalidOperationException("Template preview failed: "+Status);
   int id=nextId;Commit();if(!visuals.ContainsKey(id))throw new InvalidOperationException("Commit did not create expected stair");
   benchmarkIds.Add(id);benchmarkMade++;if(benchmarkMade>=benchmarkTarget)benchmarkRunning=false;
  }catch(Exception e){benchmarkRunning=false;benchmarkError=e.GetType().Name+": "+e.Message;Cancel();Mod.Log.Error(e,"Benchmark stopped");}
 }
 void ResetBenchmark(){TemplateTestRequest=TemplateTestReport="";templateTestPrefix=null;templateTestOriginal.Clear();benchmarkStress=false;stressPoints=stressWidths=0;widthTestAction=null;benchmarkWood=false;benchmarkColors=false;colorIndex=inspectIndex=-1;colorVerified=0;surfaceObjects.Clear();verifyIndex=-1;surfaceProbePending=false;surfaceProbe=null;if(benchmarkArmed)Mod.Options.PerformanceLogging=benchmarkPreviousLogging;benchmarkMixed=benchmarkEditPending=benchmarkRunning=benchmarkArmed=false;benchmarkMade=benchmarkTarget=0;benchmarkIds.Clear();benchmarkError="";}
}
}
