using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
namespace StairBuilder {
public partial class StairSystem {
 Stair gizmoSource;Vector3 gizmoOrigin,gizmoPosition;Vector3? pendingGizmo;
 internal bool GizmoHeld=>gizmoSource!=null;internal int GizmoCommits;
 internal Vector3 GizmoAnchor=>gizmoPosition;
 internal bool GizmoReady=>GizmoHeld&&!pendingGizmo.HasValue&&CanCommit;
 bool SelectedStairToGizmo(bool copy){
  if(Draft!=null||DetailId<=0||DetailStair==null)return false;
  var type=System.Array.Find(System.AppDomain.CurrentDomain.GetAssemblies(),a=>a.GetName().Name=="GizmoXYZ")?.GetType("GizmoXYZ.GizmoSession");
  var session=type==null?null:World.GetExistingSystemManaged(type);
  if(session==null)return false;
  // Do not take over another object's active Gizmo operation.
  var held=type.GetProperty("Held",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
  if(held==null||!(held.GetValue(session) is bool busy)||busy)return false;
  int id=DetailId;DetailId=0;detailPressed=0;Open=true;Activate();SelectStair(id);BeginGizmo(!copy);return GizmoHeld;
 }
 internal void BeginGizmo(bool move){
  if(Draft==null||Draft.Id<=0||MovingPoint>=0||GizmoHeld)return;
  var selected=Draft.Copy();gizmoSource=selected;gizmoOrigin=gizmoPosition=selected.A;pendingGizmo=null;GizmoCommits=0;
  Draft=selected.Copy();Draft.Id=move?selected.Id:0;
  if(!move&&visuals.TryGetValue(selected.Id,out var original))original.Root.SetActive(true);
  Rebuild();
 }
 internal bool QueueGizmo(Vector3 position){if(!GizmoHeld)return false;if((position-gizmoPosition).sqrMagnitude>.000001f)pendingGizmo=position;return true;}
 void ApplyGizmo(){
  if(!pendingGizmo.HasValue||!GizmoHeld)return;var position=pendingGizmo.Value;pendingGizmo=null;var delta=position-gizmoOrigin;
  var terrain=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();
  for(int i=0;i<Draft.Points.Count;i++){var desired=gizmoSource.Point(i)+delta;float ground=Game.Simulation.TerrainUtils.SampleHeight(ref terrain,(float3)desired);Draft.Points[i].Ground=new Vector3(desired.x,ground,desired.z);Draft.Points[i].Offset=desired.y-ground;}
  gizmoPosition=position;Rebuild();
 }
 void ResetGizmo(){gizmoSource=null;pendingGizmo=null;}
}
public partial class StairTool {
 StairSystem Bridge=>World.GetOrCreateSystemManaged<StairSystem>();
 public int GizmoBridgeVersion=>1;
 public bool GizmoTranslationOnly=>true;
 public bool GizmoHeld=>Bridge.GizmoHeld;
 public float3 GizmoAnchor=>(float3)Bridge.GizmoAnchor;
 public quaternion GizmoRotation=>quaternion.identity;
 public quaternion GizmoLocalRotation=>Bridge.Draft!=null?(quaternion)Quaternion.LookRotation(new Vector3(Bridge.Draft.Point(1).x-Bridge.Draft.A.x,0,Bridge.Draft.Point(1).z-Bridge.Draft.A.z)):quaternion.identity;
 public float3 GizmoBoundsMin=>Bounds(false);
 public float3 GizmoBoundsMax=>Bounds(true);
 float3 Bounds(bool max){if(Bridge.Draft==null)return float3.zero;var s=Bridge.Draft;var b=new UnityEngine.Bounds(s.A,Vector3.zero);for(int i=1;i<s.Points.Count;i++)b.Encapsulate(s.Point(i));b.Expand(new Vector3(s.Width,2.2f,s.Width));return (float3)((max?b.max:b.min)-Bridge.GizmoAnchor);}
 public bool GizmoReady=>Bridge.GizmoReady;
 public int GizmoCount=>GizmoHeld?1:0;
 public int GizmoCommits=>Bridge.GizmoCommits;
 public Entity[] GizmoLastCreated=>System.Array.Empty<Entity>();
 public string GizmoStatus=>"Stair Builder";
 public bool GizmoEditAvailable=>Bridge.Draft?.Id>0&&Bridge.MovingPoint<0&&!GizmoHeld;
 public bool RequestGizmoEdit(){if(!GizmoEditAvailable)return false;Bridge.Enqueue("gizmoMove");return true;}
 public bool RequestGizmo(){if(!GizmoEditAvailable)return false;Bridge.Enqueue("gizmoCopy");return true;}
 public bool SetGizmoTransform(float3 position,quaternion rotation)=>math.all(math.isfinite(position))&&Bridge.QueueGizmo(position);
 public void ConfirmGizmo(){if(GizmoReady)Bridge.Enqueue("commit");}
 public void CancelGizmo(){if(GizmoHeld)Bridge.Enqueue("cancel");}
}
}
