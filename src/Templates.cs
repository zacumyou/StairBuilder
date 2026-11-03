using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace StairBuilder {
public partial class StairSystem {
 TemplateLibrary library;TemplateLibrary Library=>library??(library=new TemplateLibrary(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"Low","Colossal Order","Cities Skylines II","ModsData","StairBuilder","templates.json")));
 internal string TemplatesJson=>Library.Json();
 internal float TemplateDraftLength{get{float length=0;if(Draft!=null)for(int i=1;i<Draft.Points.Count;i++){var d=Draft.Point(i)-Draft.Point(i-1);d.y=0;length+=d.magnitude;}return length;}}
 internal bool GroupPlacement=>groupSource!=null;Stair groupSource;Vector3 groupAnchor;float groupAngle,groupLift;bool groupMove;float groupMoveHeight;
 internal bool CanStoreTemplate=>Draft!=null&&HaveA&&HaveB&&MovingPoint<0&&!DraggingWidth&&!GizmoHeld&&Draft.Solve(out _,out _,out _,out _);
 void LibraryCommand(string json){
  try{var cmd=JObject.Parse(json);string action=(string)cmd["action"],id=(string)cmd["id"],name=(string)cmd["name"];
   if(action=="load"){
    // Resolve the template before committing; invalid entries must leave the draft intact.
    var source=Library.Find(id).Create();
    if(Draft!=null&&(HaveA||Draft.Id>0)){
     if(!CanCommit||MovingPoint>=0||GroupPlacement){Status="templateFinishDraft";return;}
     Commit();
     // Commit can decline (limit/path availability). Never discard that draft.
     if(Draft!=null)return;
    }
    StartGroup(source);return;
   }
   if(action=="save"){if(!CanStoreTemplate)return;Library.Save(Draft,name);Status="templateSaved";}
   else if(action=="overwrite"){if(!CanStoreTemplate)return;Library.Save(Draft,Library.Find(id).Name,id);Status="templateSaved";}
   else if(action=="rename"){Library.Rename(id,name);Status="templateRenamed";}
   else if(action=="delete"){Library.Delete(id);Status="templateDeleted";}
  }catch(ArgumentException e){Status=e.Message.StartsWith("template")?e.Message:"templateError";}catch(Exception e){Status="templateError";Mod.Log.Error(e,"Template operation failed; library preserved");}
 }
 void CopyDraft(){if(CanStoreTemplate)StartGroup(Draft.Copy());}
 void StartGroup(Stair source,bool move=false){
  var relative=StairTemplate.From(source,"copy").Create();Cancel();Open=true;Activate();groupSource=relative;groupAngle=groupLift=0;groupAnchor=source.A;
  groupMove=move;groupMoveHeight=source.A.y;
  Draft=relative.Copy();if(move)Draft.Id=source.Id;Width=Draft.Width;HaveA=true;HaveB=false;ActivePoint=0;MovingPoint=0;addObjects=addNetworks=!move;Status=move?"placeMove":"placeCopy";PositionGroup();
 }
 void PositionGroup(){
  if(!GroupPlacement||Draft==null)return;var rotation=Quaternion.Euler(0,groupAngle,0);var terrain=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
  for(int i=0;i<Draft.Points.Count;i++){var position=groupAnchor+rotation*groupSource.Point(i)+Vector3.up*groupLift;float ground=Game.Simulation.TerrainUtils.SampleHeight(ref terrain,(Unity.Mathematics.float3)position);Draft.Points[i].AtHeight(new Vector3(position.x,ground,position.z),position.y);}
 }
 void CursorGroup(Vector3 point,bool click){
  if(groupMove)point.y=groupMoveHeight;groupAnchor=point;PositionGroup();if(click){groupSource=null;groupMove=false;MovingPoint=-1;HaveB=true;ResetAddedPointSnaps();Status="adjust";Rebuild();if(Draft.Id>0&&visuals.TryGetValue(Draft.Id,out var original))original.Root.SetActive(false);PlayStairSound(1);}
 }
 void RotateGroup(float angle){if(GroupPlacement){groupAngle=(groupAngle+angle)%360;PositionGroup();}}
}
}
