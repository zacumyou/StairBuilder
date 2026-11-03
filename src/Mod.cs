using Game;
using Game.Modding;
using Game.SceneFlow;
using Unity.Entities;
namespace StairBuilder {
public sealed class Mod:IMod {
 internal static readonly Colossal.Logging.ILog Log=Colossal.Logging.LogManager.GetLogger("StairBuilder").SetShowsErrorsInUI(false);
 internal static Settings Options;Locale en,ko;
 public void OnLoad(UpdateSystem update){
  Options=new Settings(this);Colossal.IO.AssetDatabase.AssetDatabase.global.LoadSettings("StairBuilder",Options,new Settings(this));Options.RegisterKeyBindings();Options.RegisterInOptionsUI();
  en=new Locale(false);ko=new Locale(true);GameManager.instance.localizationManager.AddSource("en-US",en);GameManager.instance.localizationManager.AddSource("ko-KR",ko);
  update.UpdateAt<StairSystem>(SystemUpdatePhase.ToolUpdate);update.UpdateAt<StairTool>(SystemUpdatePhase.ToolUpdate);update.UpdateAt<StairUI>(SystemUpdatePhase.UIUpdate);
  World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<Game.Serialization.SerializerSystem>().SetDirty();
  BulldozerBridge.Install(this);SurfaceBridge.Install(this);
  Log.Info("Stair Builder "+typeof(Mod).Assembly.GetName().Version+" loaded");
 }
 public void OnDispose(){
  MoveItBridge.Dispose();SurfaceBridge.Dispose();BulldozerBridge.Dispose();
  var w=World.DefaultGameObjectInjectionWorld;if(w!=null&&w.IsCreated)w.GetExistingSystemManaged<StairSystem>()?.Shutdown();
  Options?.UnregisterInOptionsUI();if(en!=null)GameManager.instance.localizationManager.RemoveSource("en-US",en);if(ko!=null)GameManager.instance.localizationManager.RemoveSource("ko-KR",ko);
 }
}
}
