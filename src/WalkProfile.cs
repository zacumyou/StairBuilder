using System.Collections.Generic;
using UnityEngine;
namespace StairBuilder {
internal static class WalkProfile {
 // Small sole clearance; shared by all areas and access markers.
 internal const float Clearance=.04f;
 internal static List<Vector3> Run(RoutePlan.Run run){
  // One connected flight. Sub-tread areas are below native node tolerances
  // and introduce hundreds of ambiguous access-marker connections.
  return new List<Vector3>{run.A+Vector3.up*Clearance,run.B+Vector3.up*Clearance};
 }
 internal static List<Vector3> Pad(RoutePlan.Pad pad){
  var center=pad.Center+Vector3.up*Clearance;
  return new List<Vector3>{center-pad.Forward*(pad.Length*.5f),center,center+pad.Forward*(pad.Length*.5f)};
 }
}
}
