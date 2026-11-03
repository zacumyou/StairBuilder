using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
namespace StairBuilder {
internal sealed class StairTemplate {
 public string Id,Name;public float Width;public int Style,Railing,DeckColor,RailColor;public bool Solid,Ramp,AutoLandings;public float[] Points;public int[] Hidden;
 internal static StairTemplate From(Stair s,string name){
  var points=new List<float>();var origin=s.A;
  foreach(var p in s.Points){var v=p.Ground+Vector3.up*p.Offset-origin;points.Add(v.x);points.Add(v.y);points.Add(v.z);}
  return new StairTemplate{Id=Guid.NewGuid().ToString("N"),Name=name,Width=s.Width,Style=s.Style,Railing=s.Railing,DeckColor=s.DeckColor,RailColor=s.RailColor,Solid=s.Solid,Ramp=s.Ramp,AutoLandings=s.AutoLandings,Points=points.ToArray(),Hidden=s.SuppressedSupports.ToArray()};
 }
 internal Stair Create(){
  if(Points==null||Points.Length<6||Points.Length>96||Points.Length%3!=0||!WidthPolicy.Valid(Width)||Style<0||Style>5||Railing< -1||(Railing>5&&Railing!=7)||DeckColor< -1||DeckColor>0xffffff||RailColor< -1||RailColor>0xffffff||Hidden==null||Hidden.Length>4096)throw new InvalidDataException("Invalid template");
  var s=new Stair{Width=Width,Style=Style,Railing=Railing,DeckColor=DeckColor,RailColor=RailColor,Solid=Solid,Ramp=Ramp,AutoLandings=AutoLandings};s.Points.Clear();
  for(int i=0;i<Points.Length;i+=3){var p=new Vector3(Points[i],Points[i+1],Points[i+2]);if(!Layout.Finite(p.x)||!Layout.Finite(p.y)||!Layout.Finite(p.z)||p.sqrMagnitude>100000000)throw new InvalidDataException("Invalid point");s.Points.Add(new StairPoint{Ground=p});}
  foreach(int key in Hidden){if(key<0||key>2000000||!s.SuppressedSupports.Add(key))throw new InvalidDataException("Invalid support");}
  if(!s.Solve(out _,out _,out _,out _))throw new InvalidDataException("Invalid route");return s;
 }
}
internal sealed class TemplateLibrary {
 internal const int Limit=50;readonly string path;string cachedJson;internal bool Failed;internal List<StairTemplate> Items=new List<StairTemplate>();
 sealed class Document {public int Version=1;public List<StairTemplate> Items=new List<StairTemplate>();}
 internal TemplateLibrary(string file){path=file;try{if(File.Exists(path)){if(new FileInfo(path).Length>1048576)throw new InvalidDataException("Library too large");var d=JsonConvert.DeserializeObject<Document>(File.ReadAllText(path));if(d==null||d.Version!=1||d.Items==null||d.Items.Count>Limit)throw new InvalidDataException("Invalid library");var ids=new HashSet<string>();foreach(var item in d.Items){if(item==null||!Guid.TryParseExact(item.Id,"N",out _)||!ids.Add(item.Id)||CleanName(item.Name)!=item.Name)throw new InvalidDataException("Invalid entry");item.Create();}Items=d.Items;}}catch{Failed=true;}}
 internal static string CleanName(string name){name=(name??"").Trim();if(name.Length<1||name.Length>40||name.Any(char.IsControl))throw new ArgumentException("templateNameInvalid");return name;}
 internal StairTemplate Find(string id)=>Items.Find(x=>x.Id==id)??throw new ArgumentException("templateMissing");
 internal void Save(Stair source,string name,string id=null){
  name=CleanName(name);if(Items.Any(x=>x.Id!=id&&string.Equals(x.Name,name,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("templateNameExists");
  if(id==null&&Items.Count>=Limit)throw new ArgumentException("templateFull");
  var item=StairTemplate.From(source,name);item.Create();var list=new List<StairTemplate>(Items);
  if(id==null)list.Add(item);else{int index=list.IndexOf(Find(id));item.Id=id;list[index]=item;}Persist(list);
 }
 internal void Rename(string id,string name){var old=Find(id);Save(old.Create(),name,id);}
 internal void Delete(string id){var list=new List<StairTemplate>(Items);list.Remove(Find(id));Persist(list);}
 void Persist(List<StairTemplate> next){
  if(Failed)throw new InvalidOperationException("templateReadError");Directory.CreateDirectory(Path.GetDirectoryName(path));var temp=path+".tmp";
  File.WriteAllText(temp,JsonConvert.SerializeObject(new Document{Items=next},Formatting.Indented),new UTF8Encoding(false));
  if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);Items=next;cachedJson=null;
 }
 internal string Json()=>cachedJson??(cachedJson=JsonConvert.SerializeObject(new{failed=Failed,limit=Limit,items=Items.Select(x=>new{id=x.Id,name=x.Name,width=x.Width,style=x.Style,points=x.Points.Length/3})}));
}
}
