using UnityEngine;
namespace StairBuilder {
internal static class CurbJoin {
 internal static float Slope(RailingPlan plan,RailingPlan.Bar bar,bool start){
  var point=start?bar.A:bar.B;var forward=bar.B-bar.A;forward.y=0;forward.Normalize();var away=start?forward:-forward;Vector3 other=Vector3.zero;int matches=0;
  foreach(var b in plan.Bars){
   if((b.A-bar.A).sqrMagnitude<.000001f&&(b.B-bar.B).sqrMagnitude<.000001f)continue;
   Vector3 d;if((b.A-point).sqrMagnitude<.000001f)d=b.B-b.A;else if((b.B-point).sqrMagnitude<.000001f)d=b.A-b.B;else continue;
   d.y=0;if(d.sqrMagnitude<.000001f)continue;other=d.normalized;matches++;
  }
  if(matches!=1)return 0;var normal=away-other;float denominator=Vector3.Dot(forward,normal);
  if(Mathf.Abs(denominator)<.001f)return 0;
  return Mathf.Clamp(-Vector3.Dot(Vector3.Cross(Vector3.up,forward),normal)/denominator,-4,4);
 }
}
}
