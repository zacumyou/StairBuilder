using UnityEngine;
namespace StairBuilder {
internal static class PointSnap {
 // Projection onto the perpendicular through the preceding point preserves
 // the cursor's signed distance, allowing either left or right turns.
 internal static Vector3 RightAngle(Stair stair,int index,Vector3 cursor){
  if(index<2||index>=stair.Points.Count)return cursor;
  var previous=stair.Point(index-1);var before=previous-stair.Point(index-2);before.y=0;
  if(before.sqrMagnitude<.0001f)return cursor;
  var perpendicular=Vector3.Cross(Vector3.up,before.normalized);
  var result=previous+perpendicular*Vector3.Dot(cursor-previous,perpendicular);
  result.y=cursor.y;return result;
 }
}
}
