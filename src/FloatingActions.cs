using System.Globalization;
using Game.Tools;
using UnityEngine;
namespace StairBuilder {
public partial class StairSystem {
 internal string FloatingActions {
  get{
   if(!Ready||GizmoHeld||DraggingWidth||MoveItCaptured||GroupPlacement||Camera.main==null)return "";
   var tool=World.GetOrCreateSystemManaged<ToolSystem>().activeTool;
   var stair=Open&&tool is StairTool&&CanStoreTemplate?Draft:!Open&&tool is DefaultToolSystem?DetailStair:null;
   if(stair==null)return "";
   var bounds=new Bounds(stair.A,Vector3.zero);for(int i=1;i<stair.Points.Count;i++)bounds.Encapsulate(stair.Point(i));
   var anchor=bounds.center;anchor.y=bounds.max.y+1.3f;
   var p=Camera.main.WorldToViewportPoint(anchor);if(p.z<=0||p.x<0||p.x>1||p.y<0||p.y>1)return "";
   return string.Format(CultureInfo.InvariantCulture,"{0:F3},{1:F3}",p.x*100,(1-p.y)*100);
  }
 }
 void MoveWhole(){if(CanStoreTemplate)StartGroup(Draft.Copy(),true);}
}
}
