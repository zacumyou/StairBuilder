using System;
using UnityEngine;
namespace StairBuilder {
public partial class StairSystem {
 // Opt-in, isolated benchmark only. Same commands and drag methods as the tool, no input injection.
 void StressDraft(int i){
  var a=new Vector3(benchmarkX+(i%25)*40,0,benchmarkZ+(i/25)*75);
  var terrain=World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>().GetHeightData();a.y=Game.Simulation.TerrainUtils.SampleHeight(ref terrain,(Unity.Mathematics.float3)a);
  Draft=new Stair{Width=1,Style=i%6,Railing=new[]{-1,0,1,2,3,4,5,7}[(i/6)%8]};Width=1;
  Draft.Points[0].AtHeight(a,a.y+2);Draft.Points[1].AtHeight(a+Vector3.forward*30,a.y+5.4f);
  HaveA=HaveB=true;MovingPoint=-1;ActivePoint=1;Open=true;Activate();
  int shape=(i/336)%3;
  for(int j=0;j<shape;j++){
   Command("addPoint");if(MovingPoint<0)throw new InvalidOperationException("Add point did not start");
   var endpoint=Draft.Point(MovingPoint)+(j==0?Vector3.right*24:Vector3.forward*20);
   SetCoordinateHeight(endpoint.y+(j==0?0:3.4f),false);Cursor(endpoint,true);
   if(MovingPoint>=0||!HaveB)throw new InvalidOperationException("Add point did not finish");stressPoints++;
  }
  Rebuild();if(!CanCommit)throw new InvalidOperationException("Stress preview invalid: "+Status);
  float desired=new[]{1f,1.1f,2.3f,3.5f,5.1f,6.4f,8f}[(i/48)%7];
  widthOut=Vector3.right*(i%2==0?1:-1);widthAnchor=(Draft.A+Draft.Point(1))*.5f+widthOut*.5f;widthHover=true;
  var fixedEdge=Draft.A-widthOut*.5f;
  var ray=new Ray(widthAnchor+Vector3.up*100,Vector3.down);
  if(!BeginWidthDrag(ray))throw new InvalidOperationException("Width drag did not start");
  var moved=new Ray(ray.origin+widthOut*(desired-1),Vector3.down);UpdateWidthDrag(moved,true);
  if(preview.Root.activeSelf)throw new InvalidOperationException("Drag preview was not hidden");
  UpdateWidthDrag(moved,false);
  if(DraggingWidth||Mathf.Abs(Draft.Width-desired)>.001f||(Draft.A-widthOut*desired*.5f-fixedEdge).sqrMagnitude>.0001f||!CanCommit)throw new InvalidOperationException("One-sided width invariant failed");
  stressWidths++;
 }
}
}
