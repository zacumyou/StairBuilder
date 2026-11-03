using System;
using System.Collections.Generic;
using UnityEngine;
namespace StairBuilder {
// MaterialPropertyBlock disables SRP batching in this shader path. Share tint variants
// across stairs instead; geometry and textures remain shared with the authored material.
internal sealed class SurfaceColor:IDisposable {
 internal sealed class Pool:IDisposable {
  sealed class Entry {internal long Key;internal Material Material;internal int Users;internal LinkedListNode<Entry> Unused;}
  readonly Dictionary<long,Entry> entries=new Dictionary<long,Entry>();
  readonly Dictionary<Material,Entry> owned=new Dictionary<Material,Entry>();
  readonly LinkedList<Entry> unused=new LinkedList<Entry>();
  internal object State=>new{variants=entries.Count,unused=unused.Count};
  internal Material Acquire(Material original,int rgb){
   long key=((long)original.GetInstanceID()<<32)|(uint)rgb;
   if(!entries.TryGetValue(key,out var entry)){
    var material=new Material(original){name=original.name+" tint "+rgb.ToString("X6"),hideFlags=HideFlags.HideAndDontSave};
    if(original.HasProperty(baseColor))material.SetColor(baseColor,Tint(original,rgb));
    entry=new Entry{Key=key,Material=material};entries.Add(key,entry);owned.Add(material,entry);
   }
   if(entry.Unused!=null){unused.Remove(entry.Unused);entry.Unused=null;}entry.Users++;return entry.Material;
  }
  internal void Release(Material material){
   if(!owned.TryGetValue(material,out var entry))return;
   if(--entry.Users==0)entry.Unused=unused.AddLast(entry);
   while(unused.Count>128){var oldest=unused.First.Value;unused.RemoveFirst();entries.Remove(oldest.Key);owned.Remove(oldest.Material);UnityEngine.Object.Destroy(oldest.Material);}
  }
  public void Dispose(){foreach(var entry in entries.Values)UnityEngine.Object.Destroy(entry.Material);entries.Clear();owned.Clear();unused.Clear();}
 }
 sealed class Target {internal Renderer Renderer;internal Material[] Materials,Current;internal bool Rail;}
 readonly List<Target> targets=new List<Target>();readonly Pool pool;
 static readonly int baseColor=Shader.PropertyToID("_BaseColor");
 int deck=-1,rail=-1;
 internal SurfaceColor(Pool shared){pool=shared;}
 static Color Tint(Material original,int rgb){var c=new Color(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f).linear;var a=original.GetColor(baseColor);return new Color(c.r*a.r,c.g*a.g,c.b*a.b,a.a);}
 internal object State {get{int decks=0,rails=0,unsupported=0;foreach(var t in targets){if(t.Rail)rails++;else decks++;foreach(var m in t.Materials)if(!m.HasProperty(baseColor))unsupported++;}return new{deck,rail,decks,rails,unsupported};}}
 internal void Verify(){
  foreach(var t in targets){var actual=t.Renderer.sharedMaterials;int rgb=t.Rail?rail:deck;
   if(t.Renderer.HasPropertyBlock())throw new InvalidOperationException("Tint must retain SRP batching");
   for(int i=0;i<t.Materials.Length;i++){
    var material=t.Materials[i];if(!material.HasProperty(baseColor))throw new InvalidOperationException("Missing color property: "+material.name);
    if(rgb<0){if(actual[i]!=material)throw new InvalidOperationException("Authored material not restored");continue;}
    var expected=Tint(material,rgb);var c=actual[i].GetColor(baseColor);
    if(Mathf.Abs(c.r-expected.r)>1e-5f||Mathf.Abs(c.g-expected.g)>1e-5f||Mathf.Abs(c.b-expected.b)>1e-5f||c.a!=expected.a)throw new InvalidOperationException("Color/alpha mismatch");
   }
  }
 }
 internal void Add(Renderer renderer,Material[] materials,bool isRail){var target=new Target{Renderer=renderer,Materials=materials,Current=materials,Rail=isRail};targets.Add(target);int rgb=isRail?rail:deck;if(rgb>=0)Apply(target,rgb);}
 internal void Set(int deckColor,int railColor){foreach(var target in targets)if(target.Rail?rail!=railColor:deck!=deckColor)Apply(target,target.Rail?railColor:deckColor);deck=deckColor;rail=railColor;}
 void Apply(Target target,int rgb){
  var next=target.Materials;
  if(rgb>=0){next=new Material[target.Materials.Length];for(int i=0;i<next.Length;i++)next[i]=pool.Acquire(target.Materials[i],rgb);}
  target.Renderer.sharedMaterials=next;Release(target);target.Current=next;
 }
 void Release(Target target){if(target.Current!=target.Materials)foreach(var material in target.Current)pool.Release(material);}
 public void Dispose(){foreach(var target in targets)Release(target);targets.Clear();}
}
}
