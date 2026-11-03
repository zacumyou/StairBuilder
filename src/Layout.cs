using System;
namespace StairBuilder {
internal static class Layout {
 internal static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
 // Prototype bounds limit malformed saves and mesh allocation, not building regulations.
 internal static bool Solve(float length,float height,float width,out int count,out float rise,out float tread,out string reason){
  count=0;rise=tread=0;reason="";
  if(!Finite(length)||!Finite(height)||length<.6f||length>100||height<.12f||height>30||!WidthPolicy.Valid(width)){reason="range";return false;}
  count=Math.Max(1,(int)Math.Round(height/.17f));rise=height/count;tread=length/count;
  if(count>180||rise<.10f||rise>.24f||tread<.20f||tread>.60f){reason="proportion";return false;}
  return true;
 }
 internal static int[] Posts(int count){
  int bays=Math.Max(1,(int)Math.Ceiling((count-1)/4.0));var a=new int[bays+1];
  for(int i=0;i<=bays;i++)a[i]=(int)Math.Round((count-1)*(double)i/bays);
  return count==1?new[]{0}:a;
 }
}
}
