using System;
using System.Globalization;
using UnityEngine;
namespace StairBuilder {
public partial class StairSystem {
 string widthTestAction;int widthTestTarget;float widthTestValue;
 void QueueWidthTest(string action,int target,float value){
  if(!benchmarkArmed||benchmarkRunning||widthTestAction!=null)throw new InvalidOperationException("Isolated idle test required");
  widthTestAction=action;widthTestTarget=target;widthTestValue=value;
 }
 void WidthTestTick(){
  if(widthTestAction==null)return;var action=widthTestAction;widthTestAction=null;
  try{
   if(action.StartsWith("width-memory-moveit-",StringComparison.Ordinal)){MoveItTest(action,widthTestTarget);}
   else if(action=="width-memory-detail"){
    if(widthTestTarget<0||widthTestTarget>=Stairs.Count||!benchmarkIds.Contains(Stairs[widthTestTarget].Id))throw new InvalidOperationException("Owned test stair required");
    int id=Stairs[widthTestTarget].Id;Cancel();Open=false;World.GetOrCreateSystemManaged<Game.Tools.ToolSystem>().activeTool=World.GetOrCreateSystemManaged<Game.Tools.DefaultToolSystem>();DetailId=id;
   }
   else if(action=="width-memory-restore"){
    if(Draft!=null||Stairs.Count!=benchmarkIds.Count)throw new InvalidOperationException("Exclusive idle test required");
    ClearVisuals();BeginVisualRestore();
   }else if(action=="width-memory-cold"){
    if(Draft!=null||widthTestTarget<0||widthTestTarget>=Stairs.Count)throw new InvalidOperationException("Idle test stair required");
    var stair=Stairs[widthTestTarget];var old=visuals[stair.Id];var next=Renderer.Build(stair,Stairs,true);
    try{VerifyResidencyPick(old,next);}catch{next.Dispose();throw;}
    visuals[stair.Id]=next;old.Dispose();VerifyPick(stair);Renderer.TrimCache();
   }else if(action=="width-memory-delete"){
    if(Draft!=null||Stairs.Count!=benchmarkIds.Count)throw new InvalidOperationException("Exclusive benchmark required");
    int count=Math.Min(Math.Max(0,widthTestTarget),Stairs.Count);
    for(int i=0;i<count;i++){int id=Stairs[Stairs.Count-1].Id;if(!benchmarkIds.Contains(id))throw new InvalidOperationException("Not a test stair");RemoveStair(id);benchmarkIds.Remove(id);}
    benchmarkMade=Stairs.Count;Renderer.TrimCache();
   }else if(action=="width-memory-stream")StreamingEnabled=widthTestTarget!=0;
   else if(action.StartsWith("width-template-",StringComparison.Ordinal)){TemplateTestTick(action,widthTestTarget,widthTestValue);}else if(action=="width-fixture"){
    if(Draft!=null)throw new InvalidOperationException("Draft exists");
    Open=true;Activate();var a=new Vector3(benchmarkX+Stairs.Count*15,0,benchmarkZ);var data=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();a.y=Game.Simulation.TerrainUtils.SampleHeight(ref data,(Unity.Mathematics.float3)a);
    Draft=new Stair{Width=2,Style=Math.Max(0,Math.Min(5,widthTestTarget)),Ramp=widthTestValue==1};
    Draft.Points[0].AtHeight(a,a.y+1);Draft.Points[1].AtHeight(a+Vector3.forward*30,a.y+(Draft.Ramp?2.5f:4));
    if(widthTestValue==2){var p=new StairPoint();p.AtHeight(a+Vector3.forward*30+Vector3.right*30,a.y+7);Draft.Points.Add(p);}
    HaveA=HaveB=true;MovingPoint=-1;ActivePoint=1;Rebuild();
   }else if(action=="width-set")Command("width:"+widthTestValue.ToString(CultureInfo.InvariantCulture));
   else if(action=="width-commit"){int id=nextId;Command("commit");if(visuals.ContainsKey(id))benchmarkIds.Add(id);}
   else if(action=="width-cancel")Cancel();
   else if(action=="width-edit"){Open=true;Activate();SelectStair(Stairs[widthTestTarget].Id);}
  }catch(Exception e){benchmarkError=e.ToString();Mod.Log.Error(e,"Width test failed");}
 }
}
}
