using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
namespace StairBuilder {
internal sealed class StairPoint {internal Vector3 Ground;internal float Offset;internal void AtHeight(Vector3 ground,float height){Ground=ground;Offset=height-ground.y;}internal StairPoint Copy()=>new StairPoint{Ground=Ground,Offset=Offset};}
internal sealed class Stair {
 internal int Id,Style;internal float Width=2;internal bool Solid;internal int Railing=-1;
 internal int DeckColor=-1,RailColor=-1; // -1 keeps the authored material; otherwise packed sRGB.
 internal int EffectiveRailing=>Railing>=0?Railing:Style==5?7:Style==1?2:Style==2?3:Style==3?6:Style==4?4:1;internal bool AutoLandings=true;internal bool Ramp;
 internal List<StairPoint> Points=new List<StairPoint>{new StairPoint(),new StairPoint()};
 internal List<Entity> ExtraPaths=new List<Entity>();
 internal HashSet<int> SuppressedSupports=new HashSet<int>();
 internal Vector3 GroundA {get=>Points[0].Ground;set=>Points[0].Ground=value;}
 internal Vector3 GroundB {get=>Points[Points.Count-1].Ground;set=>Points[Points.Count-1].Ground=value;}
 internal float OffsetA {get=>Points[0].Offset;set=>Points[0].Offset=value;}
 internal float OffsetB {get=>Points[Points.Count-1].Offset;set=>Points[Points.Count-1].Offset=value;}
 internal Entity Area,AccessA,AccessB;
 internal Vector3 Point(int i)=>Points[i].Ground+Vector3.up*Points[i].Offset;
 internal void LevelPoint(int i){if(i>0&&i<Points.Count)Points[i].Offset=Point(i-1).y-Points[i].Ground.y;}
 internal Vector3 A=>Point(0);internal Vector3 B=>Point(Points.Count-1);
 internal Stair Copy(){var s=(Stair)MemberwiseClone();s.Points=Points.ConvertAll(p=>p.Copy());s.ExtraPaths=new List<Entity>(ExtraPaths);s.SuppressedSupports=new HashSet<int>(SuppressedSupports);return s;}
 internal bool Solve(out int n,out float h,out float d,out string error){bool ok=RoutePlan.Build(this,out var p,out error);n=p.Steps;h=p.Rise;d=p.Tread;return ok;}
 internal Vector3 Low=>A.y<=B.y?A:B;
 internal Vector3 High=>A.y<=B.y?B:A;
 internal Quaternion Rotation=>Quaternion.LookRotation(new Vector3(High.x-Low.x,0,High.z-Low.z));
}
}
