using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
namespace StairBuilder {
[FileLocation("StairBuilder")]
[SettingsUIKeyboardAction("Confirm",ActionType.Button,usages:new[]{"StairBuilder.Tool"})]
[SettingsUIKeyboardAction("Cancel",ActionType.Button,usages:new[]{"StairBuilder.Tool"})]
[SettingsUIKeyboardAction("Raise",ActionType.Button,usages:new[]{"StairBuilder.Tool"})]
[SettingsUIKeyboardAction("Lower",ActionType.Button,usages:new[]{"StairBuilder.Tool"})]
public sealed class Settings:ModSetting {
 public Settings(IMod mod):base(mod){}
 [SettingsUIKeyboardBinding(BindingKeyboard.Enter,"Confirm")][SettingsUISection("General")]public ProxyBinding ConfirmKey{get;set;}
 [SettingsUIKeyboardBinding(BindingKeyboard.Escape,"Cancel")][SettingsUISection("General")]public ProxyBinding CancelKey{get;set;}
 [SettingsUIKeyboardBinding(BindingKeyboard.PageUp,"Raise")][SettingsUISection("General")]public ProxyBinding RaiseKey{get;set;}
 [SettingsUIKeyboardBinding(BindingKeyboard.PageDown,"Lower")][SettingsUISection("General")]public ProxyBinding LowerKey{get;set;}
 [SettingsUIHidden] public bool SnapObjects{get;set;}
 [SettingsUIHidden] public bool SnapNetworks{get;set;}
 [SettingsUIHidden] public bool SnapDirections{get;set;}=true;
 [SettingsUIHidden] public bool SnapAlignment{get;set;}=true;
 [SettingsUIHidden] public bool ShowSnapGuides{get;set;}=true;
 [SettingsUIHidden] public bool RightAngle{get;set;}
 [SettingsUIHidden] public float HeightStep{get;set;}=1f;
 internal float EffectiveHeightStep=>HeightStep==.5f||HeightStep==.25f?HeightStep:1f;
 [SettingsUIHidden] public float SnapRadius{get;set;}=.75f;
 [SettingsUISection("General")] public bool PerformanceLogging{get;set;}
 public override void SetDefaults(){SnapObjects=SnapNetworks=false;SnapDirections=SnapAlignment=ShowSnapGuides=true;RightAngle=false;SnapRadius=.75f;HeightStep=1f;PerformanceLogging=false;}
}
}

