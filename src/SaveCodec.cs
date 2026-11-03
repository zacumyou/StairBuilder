using System.Collections.Generic;
using Colossal.Serialization.Entities;
using UnityEngine;
namespace StairBuilder {
internal static class SaveCodec {
 internal static void Write<TWriter>(TWriter writer,int nextId,List<Stair> Stairs) where TWriter:IWriter {
  writer.Write(8);writer.Write(nextId);writer.Write(Stairs.Count);
  foreach(var s in Stairs){
   writer.Write(s.Id);writer.Write(s.Width);writer.Write(s.AutoLandings);writer.Write(s.Ramp);writer.Write(s.Style);writer.Write(s.Solid);writer.Write(s.Railing);writer.Write(s.Points.Count);
   foreach(var p in s.Points){writer.Write((Unity.Mathematics.float3)p.Ground);writer.Write(p.Offset);}
   writer.Write(s.Area);writer.Write(s.AccessA);writer.Write(s.AccessB);writer.Write(s.ExtraPaths.Count);foreach(var e in s.ExtraPaths)writer.Write(e);
   writer.Write(s.SuppressedSupports.Count);foreach(int key in s.SuppressedSupports)writer.Write(key);
   writer.Write(s.DeckColor);writer.Write(s.RailColor);
  }
 }
 internal static List<Stair> Read<TReader>(TReader reader,out int nextId) where TReader:IReader {
  reader.Read(out int version);if(version<1||version>8)throw new System.IO.InvalidDataException("Unsupported Stair Builder save version");
  reader.Read(out int next);reader.Read(out int count);if(count<0||count>10000||next<1)throw new System.IO.InvalidDataException("Invalid stair count");
  var loaded=new List<Stair>();var ids=new HashSet<int>();
  for(int i=0;i<count;i++){
   var s=new Stair();reader.Read(out s.Id);if(version>=8)reader.Read(out s.Width);else{reader.Read(out int legacyWidth);s.Width=legacyWidth;}
   if(version==1){
    s.AutoLandings=false;reader.Read(out Unity.Mathematics.float3 a);reader.Read(out Unity.Mathematics.float3 b);s.GroundA=a;s.GroundB=b;
    reader.Read(out float oa);reader.Read(out float ob);s.OffsetA=oa;s.OffsetB=ob;
   }else{
    reader.Read(out s.AutoLandings);if(version>=4)reader.Read(out s.Ramp);if(version>=5){reader.Read(out s.Style);if(s.Style<0||s.Style>5)throw new System.IO.InvalidDataException("Unknown stair style");}if(version>=6){reader.Read(out s.Solid);reader.Read(out s.Railing);if(s.Railing< -1||(s.Railing>5&&s.Railing!=7))throw new System.IO.InvalidDataException("Unknown railing style");}reader.Read(out int size);if(size<2||size>32)throw new System.IO.InvalidDataException("Invalid point count");s.Points.Clear();
    for(int j=0;j<size;j++){reader.Read(out Unity.Mathematics.float3 g);reader.Read(out float offset);if(!Finite(g)||!Layout.Finite(offset))throw new System.IO.InvalidDataException("Invalid point");s.Points.Add(new StairPoint{Ground=g,Offset=offset});}
   }
   reader.Read(out s.Area);reader.Read(out s.AccessA);reader.Read(out s.AccessB);
   if(version>=2){reader.Read(out int pathsCount);if(pathsCount<0||pathsCount>4096)throw new System.IO.InvalidDataException("Invalid path count");for(int j=0;j<pathsCount;j++){reader.Read(out Unity.Entities.Entity e);s.ExtraPaths.Add(e);}}
   if(version>=3){reader.Read(out int hidden);if(hidden<0||hidden>4096)throw new System.IO.InvalidDataException("Invalid support count");for(int j=0;j<hidden;j++){reader.Read(out int key);if(key<0||key>2000000||!s.SuppressedSupports.Add(key))throw new System.IO.InvalidDataException("Invalid support key");}}
   if(s.Id<1||s.Id>=next||!ids.Add(s.Id)||!Finite(s.GroundA)||!Finite(s.GroundB)||!Layout.Finite(s.OffsetA)||!Layout.Finite(s.OffsetB)||!s.Solve(out _,out _,out _,out _))throw new System.IO.InvalidDataException("Invalid saved stair");
   if(version>=7){reader.Read(out s.DeckColor);reader.Read(out s.RailColor);if(s.DeckColor< -1||s.DeckColor>0xffffff||s.RailColor< -1||s.RailColor>0xffffff)throw new System.IO.InvalidDataException("Invalid stair color");}
   loaded.Add(s);
  }
  nextId=next;return loaded;
 }
 static bool Finite(Vector3 p)=>Layout.Finite(p.x)&&Layout.Finite(p.y)&&Layout.Finite(p.z);
}
}
