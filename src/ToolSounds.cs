using Game.Prefabs;
using Unity.Entities;
using UnityEngine;
namespace StairBuilder {
public partial class StairSystem {
 EntityQuery soundQuery;bool soundQueryCreated,soundUnavailable;int soundFrame=-1;float soundTime=-1;
 // Native UI audio respects the player's UI volume/mute. Only accepted gestures call this.
 void PlayStairSound(int kind,bool continuous=false){
  if(soundUnavailable||!Application.isFocused||soundFrame==UnityEngine.Time.frameCount||UnityEngine.Time.realtimeSinceStartup-soundTime<(continuous?.12f:.06f))return;
  try{
   if(!soundQueryCreated){soundQuery=GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());soundQueryCreated=true;}
   if(soundQuery.CalculateEntityCount()!=1)return;
   var audio=World.GetExistingSystemManaged<Game.Audio.AudioManager>();if(audio==null)return;
   var d=soundQuery.GetSingleton<ToolUXSoundSettingsData>();
   var clip=kind==0?d.m_NetStartSound:kind==1?d.m_NetNodeSound:kind==2?d.m_NetBuildSound:kind==3?d.m_PropPlantBulldozeSound:kind==4?d.m_NetCancelSound:d.m_SelectEntitySound;
   if(clip==Entity.Null||!EntityManager.Exists(clip))return;
   audio.PlayUISound(clip);soundFrame=UnityEngine.Time.frameCount;soundTime=UnityEngine.Time.realtimeSinceStartup;
  }catch(System.Exception e){soundUnavailable=true;Mod.Log.Warn("Stair sound unavailable: "+e.Message);}
 }
}
}
