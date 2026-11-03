using Colossal.UI.Binding;
using Game;
namespace StairBuilder {
public partial class StairUI:Game.UI.UISystemBase {
 StairSystem s;ValueBinding<bool> open,ready,draft,canCommit,editing,haveA;ValueBinding<float> width,detailWidth;ValueBinding<int> point,moving,count,pointCount;ValueBinding<float> offsetA,offsetB,selectedOffset,worldHeight,groundHeight;ValueBinding<string> status,connection,guide,dimensions;
 ValueBinding<int> detailId,style,railing,deckColor,railColor;
 ValueBinding<string> floatingActions;ValueBinding<float> templateLength;ValueBinding<string> templateDate;ValueBinding<string> templateTest;ValueBinding<string> templates;ValueBinding<bool> canStoreTemplate,groupPlacement;ValueBinding<string> widthHandle;ValueBinding<bool> snapObjects,snapNetworks,absoluteHeight;
 ValueBinding<bool> canBranch,ramp,gentle,solid;
 ValueBinding<bool> snapDirections,snapAlignment,showSnapGuides;ValueBinding<float> snapRadius,heightStep;
 ValueBinding<bool> rightAngle;ValueBinding<int> guideId,guideCount,hoverPoint,selectedSupport,hiddenSupports;
 public override GameMode gameMode=>GameMode.Game;
 protected override void OnCreate(){
  base.OnCreate();AddBinding(floatingActions=new ValueBinding<string>("StairBuilder","floatingActions",""));AddBinding(templateLength=new ValueBinding<float>("StairBuilder","templateLength",0));AddBinding(templateDate=new ValueBinding<string>("StairBuilder","templateDate",""));AddBinding(templateTest=new ValueBinding<string>("StairBuilder","templateTest",""));AddBinding(new TriggerBinding<string>("StairBuilder","templateTestReport",v=>s.ReceiveTemplateTestReport(v)));AddBinding(templates=new ValueBinding<string>("StairBuilder","templates",""));AddBinding(canStoreTemplate=new ValueBinding<bool>("StairBuilder","canStoreTemplate",false));AddBinding(groupPlacement=new ValueBinding<bool>("StairBuilder","groupPlacement",false));AddBinding(widthHandle=new ValueBinding<string>("StairBuilder","widthHandle",""));AddBinding(snapObjects=new ValueBinding<bool>("StairBuilder","snapObjects",false));AddBinding(snapNetworks=new ValueBinding<bool>("StairBuilder","snapNetworks",false));AddBinding(absoluteHeight=new ValueBinding<bool>("StairBuilder","absoluteHeight",false));AddBinding(detailId=new ValueBinding<int>("StairBuilder","detailId",0));AddBinding(detailWidth=new ValueBinding<float>("StairBuilder","detailWidth",2));AddBinding(canBranch=new ValueBinding<bool>("StairBuilder","canBranch",false));s=World.GetOrCreateSystemManaged<StairSystem>();
  AddBinding(snapDirections=new ValueBinding<bool>("StairBuilder","snapDirections",true));AddBinding(snapAlignment=new ValueBinding<bool>("StairBuilder","snapAlignment",true));AddBinding(showSnapGuides=new ValueBinding<bool>("StairBuilder","showSnapGuides",true));AddBinding(snapRadius=new ValueBinding<float>("StairBuilder","snapRadius",.75f));
  AddBinding(solid=new ValueBinding<bool>("StairBuilder","solid",false));AddBinding(railing=new ValueBinding<int>("StairBuilder","railing",-1));
  AddBinding(style=new ValueBinding<int>("StairBuilder","style",0));
  AddBinding(deckColor=new ValueBinding<int>("StairBuilder","deckColor",-1));AddBinding(railColor=new ValueBinding<int>("StairBuilder","railColor",-1));
  AddBinding(worldHeight=new ValueBinding<float>("StairBuilder","worldHeight",0));AddBinding(groundHeight=new ValueBinding<float>("StairBuilder","groundHeight",0));AddBinding(gentle=new ValueBinding<bool>("StairBuilder","gentle",false));
  AddBinding(ramp=new ValueBinding<bool>("StairBuilder","ramp",false));
  AddBinding(heightStep=new ValueBinding<float>("StairBuilder","heightStep",1f));
  AddBinding(rightAngle=new ValueBinding<bool>("StairBuilder","rightAngle",false));
  AddBinding(guideId=new ValueBinding<int>("StairBuilder","guideId",0));AddBinding(guideCount=new ValueBinding<int>("StairBuilder","guideCount",0));
  AddBinding(hoverPoint=new ValueBinding<int>("StairBuilder","hoverPoint",-1));AddBinding(selectedSupport=new ValueBinding<int>("StairBuilder","selectedSupport",-1));AddBinding(hiddenSupports=new ValueBinding<int>("StairBuilder","hiddenSupports",0));
  AddBinding(pointCount=new ValueBinding<int>("StairBuilder","pointCount",2));AddBinding(selectedOffset=new ValueBinding<float>("StairBuilder","selectedOffset",0));
  AddBinding(open=new ValueBinding<bool>("StairBuilder","open",false));AddBinding(ready=new ValueBinding<bool>("StairBuilder","ready",false));
  AddBinding(haveA=new ValueBinding<bool>("StairBuilder","haveA",false));
  AddBinding(draft=new ValueBinding<bool>("StairBuilder","draft",false));AddBinding(canCommit=new ValueBinding<bool>("StairBuilder","canCommit",false));AddBinding(editing=new ValueBinding<bool>("StairBuilder","editing",false));
  AddBinding(width=new ValueBinding<float>("StairBuilder","width",2));AddBinding(point=new ValueBinding<int>("StairBuilder","point",0));AddBinding(moving=new ValueBinding<int>("StairBuilder","moving",-1));AddBinding(count=new ValueBinding<int>("StairBuilder","count",0));
  AddBinding(offsetA=new ValueBinding<float>("StairBuilder","offsetA",0));AddBinding(offsetB=new ValueBinding<float>("StairBuilder","offsetB",0));
  AddBinding(status=new ValueBinding<string>("StairBuilder","status","select"));AddBinding(connection=new ValueBinding<string>("StairBuilder","connection",""));AddBinding(guide=new ValueBinding<string>("StairBuilder","guide",""));AddBinding(dimensions=new ValueBinding<string>("StairBuilder","dimensions",""));
  AddBinding(new TriggerBinding<string>("StairBuilder","command",s.Enqueue));
 }
 protected override void OnUpdate(){
  base.OnUpdate();floatingActions.Update(s.FloatingActions);templateLength.Update(s.TemplateDraftLength);templateDate.Update(System.DateTime.Now.ToString("yyyyMMdd",System.Globalization.CultureInfo.InvariantCulture));templateTest.Update(s.TemplateTestRequest);templates.Update(s.TemplatesJson);canStoreTemplate.Update(s.CanStoreTemplate);groupPlacement.Update(s.GroupPlacement);widthHandle.Update(s.WidthHandle);snapObjects.Update(s.SnapObjects);snapNetworks.Update(s.SnapNetworks);absoluteHeight.Update(s.AbsoluteHeight);detailId.Update(s.DetailStair?.Id??0);detailWidth.Update(s.DetailStair?.Width??2);canBranch.Update(s.CanBranch);pointCount.Update(s.Draft?.Points.Count??2);selectedOffset.Update(s.DisplayHeight);haveA.Update(s.HaveA);open.Update(s.Open);ready.Update(s.Ready);draft.Update(s.Draft!=null);canCommit.Update(s.CanCommit&&s.MovingPoint<0);editing.Update(s.Draft?.Id>0);width.Update(s.Width);point.Update(s.ActivePoint);moving.Update(s.MovingPoint);count.Update(s.Stairs.Count);
  snapDirections.Update(s.SnapDirections);snapAlignment.Update(s.SnapAlignment);showSnapGuides.Update(Mod.Options.ShowSnapGuides);snapRadius.Update(Mod.Options.SnapRadius);
  style.Update(s.Draft?.Style??0);solid.Update(s.Draft?.Solid??false);railing.Update(s.Draft?.Railing??-1);
  deckColor.Update(s.Draft?.DeckColor??-1);railColor.Update(s.Draft?.RailColor??-1);
  ramp.Update(s.Draft?.Ramp??false);worldHeight.Update(s.WorldHeight);groundHeight.Update(s.GroundHeight);
  heightStep.Update(Mod.Options.EffectiveHeightStep);
  rightAngle.Update(s.RightAngle);guideId.Update(s.GuideId);guideCount.Update(s.GuideCount);hoverPoint.Update(s.HoverPoint);selectedSupport.Update(s.SelectedSupport);hiddenSupports.Update(s.Draft?.SuppressedSupports.Count??0);
  offsetA.Update(s.Draft?.OffsetA??0);offsetB.Update(s.Draft?.OffsetB??0);status.Update(s.Status);connection.Update(s.Connection);guide.Update(s.Guide);
  RoutePlan currentPlan=null;bool valid=s.Draft!=null&&RoutePlan.Build(s.Draft,out currentPlan,out _);
  gentle.Update(valid&&currentPlan.Gentle);
  dimensions.Update(valid?string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0} 쨌 {1:F3} m 횞 {2:F3} m",currentPlan.Steps,currentPlan.Rise,currentPlan.Tread):"");
 }
}
}
