using UnityEngine;
namespace StairBuilder {
public partial class StairSystem {
 internal bool RightAngle=>addRightAngle??Mod.Options.RightAngle;
 internal int HoverStairId,HoverSupport=-1,HoverPoint=-1,SelectedSupport=-1;
 internal bool DraggingPoint;int dragCandidate=-1;Vector2 dragOrigin;float surfaceOffset;
 internal void ArmPointDrag(int index){if(Draft==null||!HaveB||MovingPoint>=0||UnityEngine.InputSystem.Mouse.current==null)return;SelectPoint(index);dragCandidate=index;dragOrigin=UnityEngine.InputSystem.Mouse.current.position.ReadValue();}
 internal void PollPointDrag(Vector2 position,bool held){
  if(dragCandidate<0)return;
  if(!held){dragCandidate=-1;return;}
  if((position-dragOrigin).sqrMagnitude<36)return;
  int index=dragCandidate;dragCandidate=-1;BeginPointMove(index);DraggingPoint=MovingPoint>=0;
 }
 internal void AbortDrag(){EndWidthDrag(true);ClearWidthHover();dragCandidate=-1;if(DraggingPoint)CancelPointMove();DraggingPoint=false;}
 StairPoint beforePointMove;bool anchorPointMove;Vector3 pointMoveOffset;
 internal Stair GuideStair=>Draft??Stairs.Find(s=>s.Id==HoverStairId);
 internal int GuideId=>GuideStair?.Id??0;
 internal int GuideCount=>GuideStair?.Points.Count??0;
 internal void Hover(Ray ray,Vector2 screen){
 using(var perf=PerformanceLog.Begin("selection.hover",0,false)){try{
  if(GizmoHeld){HoverSupport=HoverPoint=-1;HoverStairId=0;return;}
  HoverSupport=-1;
  if(Draft!=null){preview?.Hit(ray,out _,out HoverSupport);HoverStairId=Draft.Id;}
  else{
   int old=HoverStairId;HoverStairId=0;float best=float.PositiveInfinity;
   foreach(var pair in visuals)if(pair.Value.Hit(ray,out float d,out int support)&&d<best){
    best=d;HoverStairId=pair.Key;HoverSupport=support;
   }
   // Keep a visible handle target when the cursor crosses its mesh boundary.
   if(HoverStairId==0&&PointUnderCursor(Stairs.Find(s=>s.Id==old),screen)>=0)HoverStairId=old;
  }
  HoverPoint=MovingPoint<0?PointUnderCursor(GuideStair,screen):-1;
  if(HoverPoint>=0)HoverSupport=-1;
  HoverWidth(screen);
 
 }catch{perf?.Fail();throw;}}
 }
 static int PointUnderCursor(Stair stair,Vector2 screen){
  if(stair==null||Camera.main==null)return -1;
  int found=-1;float best=22f*22f;
  for(int i=0;i<stair.Points.Count;i++){
   var p=Camera.main.WorldToScreenPoint(stair.Point(i));if(p.z<=0)continue;
   float d=(new Vector2(p.x,p.y)-screen).sqrMagnitude;if(d<best){best=d;found=i;}
  }return found;
 }
 void SelectStair(int id){
  if(Draft!=null)return;
  var source=Stairs.Find(s=>s.Id==id);if(source==null)return;
  Draft=source.Copy();Width=Draft.Width;HaveA=HaveB=true;ActivePoint=0;MovingPoint=-1;
  SelectedSupport=-1;Rebuild();visuals[id].Root.SetActive(false);
 }
 bool branchPlacement;Stair branchParent;Vector3 branchOrigin;
 internal bool CanBranch=>Draft!=null&&Draft.Id>0&&HaveA&&HaveB&&MovingPoint<0&&!GizmoHeld&&Stairs.Find(s=>s.Id==Draft.Id).DeckColor==Draft.DeckColor&&Stairs.Find(s=>s.Id==Draft.Id).RailColor==Draft.RailColor&&SameRoute(Stairs.Find(s=>s.Id==Draft.Id),Draft)&&Stairs.Find(s=>s.Id==Draft.Id).SuppressedSupports.SetEquals(Draft.SuppressedSupports);
 void SelectPoint(int index){
  if(Draft==null||MovingPoint>=0||index<0||index>=Draft.Points.Count)return;
  heightReference=0;ActivePoint=index;SelectedSupport=HoverSupport=-1;Status="pointSelected";PlayStairSound(5);
 }
 void BeginBranch(){
  if(!CanBranch)return;
  var parent=Draft.Copy();var origin=Draft.Point(ActivePoint);var branch=BranchPlan.Create(Draft,ActivePoint);
  Cancel();Draft=branch;Width=branch.Width;HaveA=true;HaveB=false;ActivePoint=MovingPoint=1;
  surfaceOffset=0;heightReference=0;placementHeight=origin.y;branchPlacement=true;branchParent=parent;branchOrigin=origin;Status="branchEnd";Activate();
 }
 void BeginPointMove(int index){
  if(Draft==null||!HaveA||!HaveB||GizmoHeld||index<0||index>=Draft.Points.Count)return;
  ActivePoint=MovingPoint=index;SelectedSupport=HoverSupport=HoverPoint=-1;beforePointMove=Draft.Points[index].Copy();
  surfaceOffset=0;heightReference=0;placementHeight=Draft.Point(index).y;anchorPointMove=true;Status="movingPoint";PlayStairSound(5);
 }
 bool CancelPointMove(){
  if(beforePointMove==null||Draft==null||MovingPoint<0)return false;
  Draft.Points[MovingPoint]=beforePointMove;beforePointMove=null;anchorPointMove=false;
  DraggingPoint=false;dragCandidate=-1;MovingPoint=-1;placementHeight=null;heightReference=0;Rebuild();return true;
 }
 void DeleteSupport(){
  if(Draft==null||SelectedSupport<0||MovingPoint>=0)return;
  Draft.SuppressedSupports.Add(SelectedSupport);SelectedSupport=HoverSupport=-1;
  Rebuild();Status="supportRemoved";PlayStairSound(3);
 }
 static bool SameRoute(Stair a,Stair b){
  if(a==null||a.Style!=b.Style||a.Solid!=b.Solid||a.Railing!=b.Railing||a.Width!=b.Width||a.Ramp!=b.Ramp||a.AutoLandings!=b.AutoLandings||a.Points.Count!=b.Points.Count)return false;
  for(int i=0;i<a.Points.Count;i++)if(a.Points[i].Ground!=b.Points[i].Ground||a.Points[i].Offset!=b.Points[i].Offset)return false;
  return true;
 }
 void RefreshJunctions(Stair before,Stair after){
 geometryRevision++;
 using(var perf=PerformanceLog.Begin("junction.refresh",0,false)){try{
  foreach(var neighbor in Stairs){
   if(neighbor==after||(before!=null&&neighbor.Id==before.Id))continue;
   if((before==null||BranchPlan.Shared(neighbor,before).Count==0)&&(after==null||BranchPlan.Shared(neighbor,after).Count==0))continue;
   var replacement=Renderer.Build(neighbor,Stairs,visuals.TryGetValue(neighbor.Id,out var resident)&&resident.Coarse);
   if(visuals.TryGetValue(neighbor.Id,out var old))old.Dispose();visuals[neighbor.Id]=replacement;
   if(Draft?.Id==neighbor.Id)replacement.Root.SetActive(false);
  }
 
 }catch{perf?.Fail();throw;}}
 }
 void DrawInteraction(){
  var overlay=World.GetOrCreateSystemManaged<Game.Rendering.OverlayRenderSystem>();
  if(Draft==null){
   var hovered=GuideStair;
   if(hovered!=null)StairGuide.Draw(overlay,hovered,true,new Color(.8f,.95f,1f,.65f));
  }
  var visual=Draft!=null?preview:visuals.TryGetValue(HoverStairId,out var v)?v:null;
  if(visual==null)return;
  foreach(var pier in visual.Supports){
   if(pier.Key==SelectedSupport&&Draft!=null)StairGuide.Support(overlay,pier,new Color(.25f,.9f,1f,.95f));
   else if(pier.Key==HoverSupport)StairGuide.Support(overlay,pier,new Color(1f,.9f,.5f,.9f));
  }
 }
}
}


