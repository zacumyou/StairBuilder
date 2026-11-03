using UnityEngine;
namespace StairBuilder {
internal static class WidthPolicy {
 internal const float Min=1f,Max=8f;
 internal static bool Valid(float value)=>Layout.Finite(value)&&value>=Min&&value<=Max;
 internal static float Snap(float value)=>Mathf.Round(Mathf.Clamp(value,Min,Max)*10f)/10f;
 internal static float Drag(float initial,float outward)=>Snap(initial+outward);
}
}
