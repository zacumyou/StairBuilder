using System;
using System.Collections.Generic;
using System.Globalization;
using Colossal.Serialization.Entities;
using Game;
using Game.Tools;
using Unity.Entities;
using UnityEngine;
namespace StairBuilder {
public partial class StairSystem:GameSystemBase,IDefaultSerializable {
 internal readonly List<Stair> Stairs=new List<Stair>();
 readonly Dictionary<int,Visual> visuals=new Dictionary<int,Visual>();readonly List<string> commands=new List<string>();
 readonly HashSet<int> missingPaths=new HashSet<int>();
 Stair previewDesign;int geometryRevision,previewRevision;internal int PreviewReuses;
 StairRenderer renderer;NativePaths paths;Visual preview;bool previewReady;int nextId=1;bool restore;float nextPreview,nextCheck;
 internal Stair Draft;internal bool Open,Ready,HaveA,HaveB;internal int ActivePoint,MovingPoint=-1;internal float Width=2;internal string Status="select",Connection="";internal float PendingOffset;float? placementHeight;int heightReference;float requestedHeight;
 internal float WorldHeight=>Draft==null?0:Draft.Point(ActivePoint).y;
 internal float GroundHeight=>Draft==null?0:Draft.Points[ActivePoint].Offset;
 internal bool AbsolutePlacement=>MovingPoint>=0&&placementHeight.HasValue;
 internal bool AbsoluteHeight=>AbsolutePlacement||(Draft!=null&&ActivePoint>0);
 internal float DisplayHeight=>Draft==null?0:AbsolutePlacement?placementHeight.Value:AbsoluteHeight?Draft.Point(ActivePoint).y:!HaveA?PendingOffset:Draft.Points[ActivePoint].Offset;
 internal bool CanCommit=>!DraggingWidth&&HaveA&&HaveB&&Draft!=null&&Draft.Solve(out _,out _,out _,out _)&&preview!=null&&previewReady;
 // Placement modes use temporary snap choices without overwriting saved settings.
 bool newStairPlacement;
 bool? addObjects,addNetworks,addDirections,addAlignment,addRightAngle;
 internal bool SnapObjects=>addObjects??Mod.Options.SnapObjects;
 internal bool SnapNetworks=>addNetworks??Mod.Options.SnapNetworks;
 internal bool SnapDirections=>addDirections??Mod.Options.SnapDirections;
 internal bool SnapAlignment=>addAlignment??Mod.Options.SnapAlignment;
 void ResetAddedPointSnaps(){addObjects=addNetworks=addDirections=addAlignment=addRightAngle=null;}
 void ToggleSnap(ref bool? local,bool current,Action<bool> save){if(local.HasValue)local=!current;else{save(!current);Mod.Options.ApplyAndSave();}snapGuide=null;}
 PlacementSnap.Result snapGuide;
 bool appearanceView;
 internal void DrawGuide(){
  if(Ready&&Open&&DraggingWidth&&Draft!=null)StairGuide.DrawWidth(World.GetOrCreateSystemManaged<Game.Rendering.OverlayRenderSystem>(),widthDragPlan,Draft.Width);
  else if(Ready&&Open&&HaveA&&Draft!=null&&(!appearanceView||MovingPoint>=0))
   StairGuide.Draw(World.GetOrCreateSystemManaged<Game.Rendering.OverlayRenderSystem>(),Draft,Draft.Solve(out _,out _,out _,out _),MovingPoint>=0&&preview!=null?(Color?)new Color(1f,.68f,.22f,.8f):null);
  if(Ready&&Open)DrawInteraction();
  if(Ready&&Open&&MovingPoint>=2&&Draft!=null&&Mod.Options.ShowSnapGuides&&snapGuide!=null)StairGuide.Snap(World.GetOrCreateSystemManaged<Game.Rendering.OverlayRenderSystem>(),snapGuide,Draft.Point(MovingPoint).y);
 }
 internal string Guide {
  get{
   var shown=GuideStair;if(!Open||shown==null||Camera.main==null)return "";
   var c=Camera.main;var values=new List<float>();
   for(int i=0;i<shown.Points.Count;i++){var p=c.WorldToViewportPoint(shown.Point(i));if(p.z<=0)return "";values.Add(p.x*100);values.Add((1-p.y)*100);}
   return string.Join(",",values.ToArray().ConvertInvariant());
  }
 }
 internal void Enqueue(string command){if(command!=null&&command.Length<4096&&commands.Count<32)commands.Add(command);}
 protected override void OnCreate(){base.OnCreate();paths=new NativePaths(EntityManager,World.GetOrCreateSystemManaged<Game.Prefabs.PrefabSystem>());}
 StairRenderer Renderer=>renderer??(renderer=new StairRenderer(World.GetOrCreateSystemManaged<Game.Prefabs.PrefabSystem>(),EntityManager));
 protected override void OnUpdate(){
 using(var perf=PerformanceLog.Begin("system.update",0,false)){try{
  PerformanceLog.Tick(Stairs.Count,visuals.Values,Stairs,preview!=null);
  if(!Ready){commands.Clear();return;}
  try{
   if(restore){
    restore=false;BeginVisualRestore();
   }
   ObserveMoveIt();
   BenchmarkTick();
   StreamVisuals();
   HandleDefaultSelection();
   ApplyGizmo();
   while(commands.Count>0){var command=commands[0];commands.RemoveAt(0);Command(command);}
   if(UnityEngine.Time.unscaledTime>=nextCheck){nextCheck=UnityEngine.Time.unscaledTime+1;
    using(PerformanceLog.Begin("maintenance.alive"))foreach(var s in Stairs){
     if(paths.Alive(s)){missingPaths.Remove(s.Id);continue;}
     if(missingPaths.Add(s.Id))Mod.Log.Warn("Stair path incomplete; preserving visual/design id="+s.Id+" "+paths.Describe(s));
    }
    Connection=Draft!=null&&Draft.Id>0?paths.Connection(Stairs.Find(x=>x.Id==Draft.Id)):Stairs.Count>0?paths.Connection(Stairs[Stairs.Count-1]):"";
   }
  }catch(Exception e){Status=e is ImportedAssetException?"assets":"error";Mod.Log.Error(e,"Stair Builder command failed");}
 
 }catch{perf?.Fail();throw;}}
 }
 void Activate(){World.GetOrCreateSystemManaged<ToolSystem>().activeTool=World.GetOrCreateSystemManaged<StairTool>();}
 void Command(string value){
  if(value=="toggle"){Open=!Open;if(Open)Activate();else{Cancel();var t=World.GetOrCreateSystemManaged<ToolSystem>();if(t.activeTool is StairTool)t.activeTool=World.GetOrCreateSystemManaged<DefaultToolSystem>();}return;}
  if(value=="detailClose"){DetailId=0;return;}
  if(value=="detailCopy"){var source=Stairs.Find(x=>x.Id==DetailId);if(source!=null)StartGroup(source);return;}
  if(value=="detailMove"){var source=Stairs.Find(x=>x.Id==DetailId);if(source!=null)StartGroup(source,true);return;}
  if(value=="detailEdit"){int id=DetailId;DetailId=0;if(Stairs.Exists(s=>s.Id==id)){Open=true;Activate();SelectStair(id);}return;}
  if(value=="detailDelete"){int id=DetailId;DetailId=0;if(id>0&&Stairs.Exists(x=>x.Id==id)){RemoveStair(id);PlayStairSound(3);}return;}
  if(!Open)return;var parts=value.Split(':');
  if(DraggingWidth){if(parts[0]=="cancel")EndWidthDrag(true);return;}
  if(value.StartsWith("library:",StringComparison.Ordinal)){if(!GizmoHeld&&!DraggingWidth)LibraryCommand(value.Substring(8));return;}
  if(GizmoHeld&&parts[0]!="commit"&&parts[0]!="cancel")return;
  switch(parts[0]){
   case "copy":CopyDraft();break;
   case "moveWhole":MoveWhole();break;
   case "rotateLeft":RotateGroup(-90);break;
   case "rotateRight":RotateGroup(90);break;
   case "view":appearanceView=parts.Length==2&&parts[1]=="appearance";break;
   case "gizmoMove":BeginGizmo(true);break;
   case "gizmoCopy":BeginGizmo(false);break;
   case "new":Cancel();Activate();newStairPlacement=true;addObjects=addNetworks=addDirections=addAlignment=true;Draft=new Stair{Width=Width};HaveA=HaveB=false;ActivePoint=0;MovingPoint=0;PendingOffset=0;Status="start";break;
   case "cancel":if(Draft!=null)PlayStairSound(4);if(!CancelPointMove())Cancel();break;
   case "snapObjects":ToggleSnap(ref addObjects,SnapObjects,v=>Mod.Options.SnapObjects=v);break;
   case "snapNetworks":ToggleSnap(ref addNetworks,SnapNetworks,v=>Mod.Options.SnapNetworks=v);break;
   case "rightAngle":ToggleSnap(ref addRightAngle,RightAngle,v=>Mod.Options.RightAngle=v);snapGuide=null;break;
   case "snapDirections":ToggleSnap(ref addDirections,SnapDirections,v=>Mod.Options.SnapDirections=v);snapGuide=null;break;
   case "snapAlignment":ToggleSnap(ref addAlignment,SnapAlignment,v=>Mod.Options.SnapAlignment=v);snapGuide=null;break;
   case "showSnapGuides":Mod.Options.ShowSnapGuides=!Mod.Options.ShowSnapGuides;Mod.Options.ApplyAndSave();break;
   case "snapRadius":if(parts.Length==2&&float.TryParse(parts[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float radius)&&!float.IsNaN(radius)){Mod.Options.SnapRadius=Mathf.Clamp(radius,.1f,4f);Mod.Options.ApplyAndSave();snapGuide=null;}break;
   case "deleteSupport":DeleteSupport();break;
   case "restoreSupports":if(Draft!=null&&MovingPoint<0){Draft.SuppressedSupports.Clear();SelectedSupport=-1;Rebuild();}break;
   case "dragPoint":if(parts.Length==3&&int.TryParse(parts[1],out int dragId)&&int.TryParse(parts[2],out int dragIndex)){if(Draft==null)SelectStair(dragId);if(Draft?.Id==dragId)ArmPointDrag(dragIndex);}break;
   case "absoluteValue":case "relativeValue":if(parts.Length==2&&float.TryParse(parts[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float coordinate)&&Layout.Finite(coordinate)&&Mathf.Abs(coordinate)<=10000)SetCoordinateHeight(coordinate,parts[0]=="relativeValue");break;
   case "setHeight":if(parts.Length==2&&float.TryParse(parts[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float valueHeight)&&Layout.Finite(valueHeight)&&Mathf.Abs(valueHeight)<=10000)SetHeight(valueHeight);break;
   case "selectPoint":if(parts.Length==3&&MovingPoint<0&&int.TryParse(parts[1],out int sid)&&int.TryParse(parts[2],out int index)){if(Draft==null)SelectStair(sid);if(Draft!=null&&Draft.Id==sid)SelectPoint(index);}break;
   case "hoverPoint":if(parts.Length==2&&int.TryParse(parts[1],out int hp))HoverPoint=hp;break;
   case "addPoint":if(Draft!=null&&HaveA&&HaveB&&MovingPoint<0&&Draft.Points.Count<32){newStairPlacement=false;SelectedSupport=-1;Draft.AutoLandings=true;Draft.Points.Add(Draft.Points[Draft.Points.Count-1].Copy());ActivePoint=MovingPoint=Draft.Points.Count-1;HaveB=false;surfaceOffset=0;heightReference=1;requestedHeight=Draft.Point(ActivePoint).y;placementHeight=requestedHeight;addObjects=addNetworks=addDirections=addAlignment=addRightAngle=false;snapGuide=null;PlayStairSound(5);Rebuild();}break;
   case "undoPoint":if(Draft!=null&&Draft.Points.Count>2&&MovingPoint<0){Draft.SuppressedSupports.RemoveWhere(key=>SupportPlan.RemovedWithLastPoint(key,Draft.Points.Count-1));SelectedSupport=-1;Draft.Points.RemoveAt(Draft.Points.Count-1);ActivePoint=Draft.Points.Count-1;MovingPoint=-1;HaveB=true;Rebuild();}break;
   case "commit":Commit();break;
   case "point":if(Draft!=null&&int.TryParse(parts[1],out var p)&&p>=0&&p<Draft.Points.Count)SelectPoint(p);break;
   case "move":BeginPointMove(ActivePoint);break;
   case "branch":BeginBranch();break;
   case "level":if(Draft!=null&&HaveA&&HaveB&&MovingPoint<0&&ActivePoint>0){Draft.LevelPoint(ActivePoint);Rebuild();}break;
   case "heightStep":if(parts.Length==2&&float.TryParse(parts[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float step)&&(step==1f||step==.5f||step==.25f)){Mod.Options.HeightStep=step;Mod.Options.ApplyAndSave();}break;
   case "solid":if(Draft!=null){Draft.Solid=!Draft.Solid;SelectedSupport=-1;PlayStairSound(5);Rebuild();}break;
   case "railing":if(Draft!=null&&parts.Length==2&&int.TryParse(parts[1],out int rail)&&rail>=-1&&(rail<=5||rail==7)){if(Draft.Railing==rail)break;Draft.Railing=rail;PlayStairSound(5);Rebuild();}break;
   case "deckColor":case "railColor":
    if(Draft!=null&&parts.Length==2&&int.TryParse(parts[1],out int rgb)&&rgb>=-1&&rgb<=0xffffff){
     using(PerformanceLog.Begin("preview.color",Draft.Id,false)){if(parts[0]=="deckColor")Draft.DeckColor=rgb;else Draft.RailColor=rgb;preview?.Colors.Set(Draft.DeckColor,Draft.RailColor);}
    }break;
   case "style":if(Draft!=null&&parts.Length==2&&int.TryParse(parts[1],out int style)&&style>=0&&style<=5){if(Draft.Style==style)break;Draft.Style=style;PlayStairSound(5);Rebuild();}break;
   case "ramp":if(Draft!=null){Draft.Ramp=!Draft.Ramp;SelectedSupport=-1;Draft.SuppressedSupports.Clear();PlayStairSound(5);Rebuild();}break;
   case "raise":Adjust(1);break;
   case "lower":Adjust(-1);break;
   case "width":if(parts.Length==2&&float.TryParse(parts[1],NumberStyles.Float,CultureInfo.InvariantCulture,out var w)&&Layout.Finite(w)&&!DraggingWidth)SetWidth(w);break;
   case "delete":if(Draft!=null&&Draft.Id>0){int id=Draft.Id;var s=Stairs.Find(x=>x.Id==id);Cancel();if(s!=null){paths.Delete(s);Stairs.Remove(s);if(visuals.TryGetValue(id,out var v)){v.Dispose();visuals.Remove(id);}RefreshJunctions(s,null);PlayStairSound(3);Status="deleted";Mod.Log.Info("Deleted stair "+id);}}break;
  }
 }
 void SetCoordinateHeight(float value,bool relative){
  if(Draft==null)return;
  if(GroupPlacement){float targetHeight=relative?Draft.Points[0].Ground.y+value:value;groupLift+=targetHeight-Draft.A.y;PositionGroup();return;}
  var point=Draft.Points[ActivePoint];float world=relative?point.Ground.y+value:value;
  point.Offset=world-point.Ground.y;
  heightReference=relative?2:1;requestedHeight=value;
  if(MovingPoint>=0)placementHeight=world;
  if(!HaveA)PendingOffset=point.Offset;
  Rebuild();
 }
 void SetHeight(float value){if(Draft==null)return;ApplyHeightDelta(value-DisplayHeight);}
 internal void Adjust(float delta)=>ApplyHeightDelta(delta*Mod.Options.EffectiveHeightStep);
 void ApplyHeightDelta(float delta){
  if(Draft==null)return;
  if(GroupPlacement){groupLift+=delta;PositionGroup();return;}
  if(heightReference!=0){SetCoordinateHeight(requestedHeight+delta,heightReference==2);return;}
  if(MovingPoint>=0&&HaveA)surfaceOffset+=delta;
  if(AbsolutePlacement){placementHeight+=delta;Draft.Points[ActivePoint].Offset=placementHeight.Value-Draft.Points[ActivePoint].Ground.y;}
  else if(!HaveA){float previous=PendingOffset;PendingOffset=Mathf.Clamp(PendingOffset+delta,-100,100);Draft.Points[0].Offset+=PendingOffset-previous;}
  else if(ActivePoint>0)Draft.Points[ActivePoint].Offset+=delta;
  else Draft.Points[ActivePoint].Offset=Mathf.Clamp(Draft.Points[ActivePoint].Offset+delta,-100,100);
  Rebuild();
 }
 internal void Cursor(Vector3 ground,bool click,bool surfaceHit=false){
  if(Draft==null||MovingPoint<0)return;
  if(GroupPlacement){CursorGroup(ground,click);return;}
  if(anchorPointMove){pointMoveOffset=Draft.Points[MovingPoint].Ground-ground;pointMoveOffset.y=0;anchorPointMove=false;}
  if(beforePointMove!=null&&!surfaceHit)ground+=pointMoveOffset;
  snapGuide=PlacementSnap.Solve(Draft,branchPlacement?1:MovingPoint,ground,SnapDirections,SnapAlignment,RightAngle,Mod.Options.SnapRadius);
  if(surfaceHit)snapGuide=null;else ground=snapGuide.Position;
  float surfaceY=ground.y;
  var terrainData=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
  ground.y=Game.Simulation.TerrainUtils.SampleHeight(ref terrainData,(Unity.Mathematics.float3)ground);
  var prior=Draft.Point(MovingPoint);
  if(heightReference!=0)placementHeight=heightReference==2?ground.y+requestedHeight:requestedHeight;
  else if((newStairPlacement||surfaceHit)&&HaveA)placementHeight=surfaceY+surfaceOffset;
  Draft.Points[MovingPoint].AtHeight(ground,placementHeight??(surfaceY+PendingOffset));
  bool changed=(Draft.Point(MovingPoint)-prior).sqrMagnitude>.0001f;
  if(branchPlacement&&branchParent!=null){
   var edge=BranchPlan.EdgeStart(branchParent,branchOrigin,Draft.B);
   var terrain=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();float y=Game.Simulation.TerrainUtils.SampleHeight(ref terrain,(Unity.Mathematics.float3)edge);
   Draft.Points[0].Ground=new Vector3(edge.x,y,edge.z);Draft.Points[0].Offset=edge.y-y;
   var toTarget=Draft.B-branchOrigin;toTarget.y=0;var toEdge=edge-branchOrigin;toEdge.y=0;
   if(toTarget.magnitude<=toEdge.magnitude+.6f){preview?.Dispose();preview=null;Status="branchSpace";return;}
  }
  if(!HaveA){Draft.GroundB=ground;}
  if(click){
   PlayStairSound(HaveA?1:0);
   if(!HaveA){surfaceOffset=0;heightReference=0;placementHeight=Draft.A.y;Draft.Points[1].AtHeight(ground,placementHeight.Value);HaveA=true;ActivePoint=MovingPoint=1;Status="end";}
   else {newStairPlacement=false;ResetAddedPointSnaps();DraggingPoint=false;HaveB=true;MovingPoint=-1;placementHeight=null;beforePointMove=null;branchPlacement=false;Status="adjust";}
   Rebuild();
  }else if(changed&&HaveA&&UnityEngine.Time.unscaledTime>=nextPreview){nextPreview=UnityEngine.Time.unscaledTime+.08f;Rebuild();}
 }
 void Rebuild(){
 using(var perf=PerformanceLog.Begin("preview.rebuild",0,false)){try{
  previewReady=false;
  if(Draft==null||!HaveA)return;
  if(MovingPoint>=0){preview?.Dispose();preview=null;if(Draft.Id>0&&visuals.TryGetValue(Draft.Id,out var original))original.Root.SetActive(true);Status=Draft.Solve(out _,out _,out _,out string movingReason)?"movingPoint":movingReason;return;}
  if(Draft.Solve(out _,out _,out _,out string reason)){
   if(preview!=null&&previewDesign!=null&&previewRevision==geometryRevision&&SameRoute(previewDesign,Draft)&&previewDesign.SuppressedSupports.SetEquals(Draft.SuppressedSupports)){
    preview.Colors.Set(Draft.DeckColor,Draft.RailColor);preview.Root.SetActive(true);PreviewReuses++;
   }else{var replacement=Renderer.Build(Draft,Stairs);preview?.Dispose();preview=replacement;previewDesign=Draft.Copy();previewRevision=geometryRevision;}
   previewReady=true;if(Draft.Id>0&&visuals.TryGetValue(Draft.Id,out var original))original.Root.SetActive(false);Status=beforePointMove!=null?"movingPoint":HaveB?"adjust":branchPlacement?"branchEnd":"end";}
  else {preview?.Dispose();preview=null;Status=reason;}
 
 }catch{perf?.Fail();throw;}}
 }
 internal void Pick(Ray ray){
  if(HoverPoint>=0){if(Draft==null)SelectStair(HoverStairId);SelectPoint(HoverPoint);return;}
  if(Draft==null)SelectStair(HoverStairId);
  if(Draft!=null){SelectedSupport=HoverSupport;Status=SelectedSupport>=0?"supportSelected":"adjust";}
 }
 void Commit(){
 using(var perf=PerformanceLog.Begin("commit",Draft?.Id??0,true)){try{
  if(!CanCommit||MovingPoint>=0)return;
  if(Draft.Id==0&&Stairs.Count>=10000){Status="limit";return;}
  if(!paths.Available){Status="prefabs";return;}
  var s=Draft.Copy();var old=s.Id>0?Stairs.Find(x=>x.Id==s.Id):null;
  if(s.Id==0)s.Id=nextId;
  // The validated preview already has final geometry. Keep ownership here until
  // native creation succeeds, so a failed commit remains editable.
  var visual=preview;
  bool colorOnly=old!=null&&(old.DeckColor!=s.DeckColor||old.RailColor!=s.RailColor)&&SameRoute(old,s)&&old.SuppressedSupports.SetEquals(s.SuppressedSupports);
  bool routeChanged=!colorOnly; // Pure tint edits retain the existing walking entities.
  if(routeChanged)paths.Create(s,Stairs);
  if(old!=null){if(routeChanged)paths.Delete(old);Stairs.Remove(old);visuals[old.Id].Dispose();visuals.Remove(old.Id);}else nextId++;
  if(GizmoHeld){GizmoCommits++;ResetGizmo();}
  Stairs.Add(s);visuals[s.Id]=visual;preview=null;visual.Root.name="Stair Builder "+s.Id;PerformanceLog.Event("commit.previewReused",s.Id,1);Draft=null;HaveA=HaveB=false;MovingPoint=-1;
  SelectedSupport=HoverSupport=HoverPoint=-1;HoverStairId=0;
  if(!colorOnly)RefreshJunctions(old,s);PerformanceLog.Event("commit.success",s.Id,Stairs.Count);
  PlayStairSound(2);Status=old==null?"created":"applied";Connection=colorOnly?paths.Connection(s):"linksPending";Mod.Log.Info("Committed stair "+s.Id+" renderers="+visual.RendererCount+" pickVertices="+visual.PickVertexCount+" width="+s.Width+" A="+s.A+" B="+s.B);
 
 }catch{perf?.Fail();throw;}}
 }
 internal void Cancel(){
  EndWidthDrag(true);ClearWidthHover();groupSource=null;
  newStairPlacement=false;ResetAddedPointSnaps();
  appearanceView=false;
  DraggingPoint=false;dragCandidate=-1;surfaceOffset=0;heightReference=0;ResetGizmo();placementHeight=null;DetailId=0;detailPressed=0;branchParent=null;branchPlacement=false;snapGuide=null;
  if(Draft!=null&&Draft.Id>0&&visuals.TryGetValue(Draft.Id,out var original))original.Root.SetActive(true);
  preview?.Dispose();preview=null;Draft=null;HaveA=HaveB=false;MovingPoint=-1;Status="select";
  beforePointMove=null;anchorPointMove=false;SelectedSupport=HoverSupport=HoverPoint=-1;HoverStairId=0;
 }
 internal void ToolStopped(){Cancel();Open=false;}
 internal void Shutdown(){ResetMoveIt();ResetBenchmark();Ready=false;Open=false;commands.Clear();ClearVisuals();renderer?.Dispose();renderer=null;PerformanceLog.Flush();}
 void ClearVisuals(){previewDesign=null;geometryRevision=previewRevision=PreviewReuses=0;ResetStreaming();ResetBulldozer();missingPaths.Clear();Cancel();foreach(var v in visuals.Values)v.Dispose();visuals.Clear();}
 protected override void OnDestroy(){Shutdown();base.OnDestroy();}
 protected override void OnGamePreload(Purpose purpose,GameMode mode){base.OnGamePreload(purpose,mode);Shutdown();Stairs.Clear();nextId=1;restore=false;}
 protected override void OnGameLoadingComplete(Purpose purpose,GameMode mode){base.OnGameLoadingComplete(purpose,mode);Ready=mode==GameMode.Game;restore=Ready;Mod.Log.Info("City loaded; saved stairs="+Stairs.Count);}
 public void SetDefaults(Context context){Stairs.Clear();nextId=1;restore=true;}
 public void Serialize<TWriter>(TWriter writer) where TWriter:IWriter{using(PerformanceLog.Begin("save.write",0,true))SaveCodec.Write(writer,nextId,Stairs);}
 public void Deserialize<TReader>(TReader reader) where TReader:IReader {using(PerformanceLog.Begin("save.read",0,true)){var loaded=SaveCodec.Read(reader,out int next);Stairs.Clear();Stairs.AddRange(loaded);nextId=next;restore=true;}}

 static bool Finite(Vector3 p)=>Layout.Finite(p.x)&&Layout.Finite(p.y)&&Layout.Finite(p.z);
}
internal static class TextNumbers {internal static string[] ConvertInvariant(this float[] a)=>Array.ConvertAll(a,x=>x.ToString("F3",CultureInfo.InvariantCulture));}
}


