using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
namespace StairBuilder {
// Main-thread, opt-in diagnostics. Fixed stage set; bounded samples and rotating files.
internal static class PerformanceLog {
 const long Limit=8*1024*1024;const int MaxLines=2048;
 static readonly Dictionary<string,Stats> stats=new Dictionary<string,Stats>();
 static readonly List<string> pending=new List<string>();
 static readonly string session=DateTime.UtcNow.ToString("yyyyMMddTHHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
 static long serial,trace;static double next,nextDrain;static bool active,failed;static int dropped;
 internal static bool Enabled=>!failed&&Mod.Options!=null&&Mod.Options.PerformanceLogging;
 static double Now=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
 static string N(double n)=>n.ToString("0.###",CultureInfo.InvariantCulture);
 static string Header(string type)=>"{\"utc\":\""+DateTime.UtcNow.ToString("o")+"\",\"session\":\""+session+"\",\"type\":\""+type+"\"";
 sealed class Stats {internal long Count;internal double Sum,Max;internal readonly long[] Buckets=new long[24];internal void Add(double ms){Count++;Sum+=ms;Max=Math.Max(Max,ms);int b=0;double edge=.01;while(ms>edge&&b<23){edge*=2;b++;}Buckets[b]++;}internal double P95(){long n=0;for(int i=0;i<24;i++){n+=Buckets[i];if(n>=Math.Ceiling(Count*.95))return .01*Math.Pow(2,i);}return Max;}}
 internal sealed class Scope:IDisposable {
  readonly string stage;readonly long start,parent,op;readonly bool detailed,root;readonly long heap;readonly int gc0;readonly int id;bool failedScope;
  internal Scope(string name,int stair,bool detail){stage=name;id=stair;parent=trace;root=detail;heap=detail?GC.GetTotalMemory(false):0;gc0=detail?GC.CollectionCount(0):0;if(detail)trace=++serial;op=trace;detailed=op!=0;start=Stopwatch.GetTimestamp();}
  internal void Fail(){failedScope=true;}
  public void Dispose(){double ms=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
   if(!stats.TryGetValue(stage,out var value)){value=new Stats();stats.Add(stage,value);}value.Add(ms);
   if(detailed)Queue(Header("stage")+",\"op\":"+op+",\"stair\":"+id+",\"stage\":\""+stage+"\",\"ms\":"+N(ms)+",\"failed\":"+(failedScope?"true":"false")+"}");
   if(root)Queue(Header("operationMemory")+",\"op\":"+op+",\"managedHeapDeltaBytes\":"+(GC.GetTotalMemory(false)-heap)+",\"gc0Delta\":"+(GC.CollectionCount(0)-gc0)+"}");
   trace=parent;
  }
 }
 internal static Scope Begin(string stage,int id=0,bool detail=false)=>Enabled?new Scope(stage,id,detail):null;
 static void Queue(string line){if(pending.Count>=MaxLines-128&&(line.Contains("\"type\":\"stage\"")||line.Contains("\"type\":\"event\""))){dropped++;return;}if(pending.Count<MaxLines)pending.Add(line);else dropped++;}
 internal static void Event(string name,int id,int value){if(Enabled&&trace!=0)Queue(Header("event")+",\"op\":"+trace+",\"name\":\""+name+"\",\"stair\":"+id+",\"value\":"+value+"}");}
 internal static void Tick(int stairs,IEnumerable<Visual> visuals,IEnumerable<Stair> designs,bool preview){
  bool enabled=Enabled;
  if(!enabled){if(active){Summarize();Flush();active=false;}return;}
  if(!active){active=true;next=Now+10;Queue(Header("start")+",\"version\":\""+typeof(Mod).Assembly.GetName().Version+"\"}");Mod.Log.Info("Performance diagnostics enabled: "+Folder);}
  if(pending.Count>=1536&&Now>=nextDrain){Flush();nextDrain=Now+1;}
  if(Now<next)return;next=Now+10;
  int nativeReferences=0;foreach(var stair in designs)nativeReferences+=3+stair.ExtraPaths.Count;
  int renderers=0,meshes=0,pickVertices=0;foreach(var v in visuals){renderers+=v.RendererCount;meshes+=v.Meshes.Count;pickVertices+=v.PickVertexCount;}
  Queue(Header("inventory")+",\"stairs\":"+stairs+",\"renderersAllLods\":"+renderers+",\"ownedMeshes\":"+meshes+",\"pickVertices\":"+pickVertices+",\"nativeReferences\":"+nativeReferences+",\"preview\":"+(preview?"true":"false")+",\"managedHeapBytes\":"+GC.GetTotalMemory(false)+",\"gc0\":"+GC.CollectionCount(0)+",\"gc1\":"+GC.CollectionCount(1)+",\"gc2\":"+GC.CollectionCount(2)+",\"dropped\":"+dropped+"}");
  Summarize();Flush();
 }
 static void Summarize(){
  foreach(var pair in stats){var s=pair.Value;Queue(Header("summary")+",\"stage\":\""+pair.Key+"\",\"calls\":"+s.Count+",\"totalMs\":"+N(s.Sum)+",\"meanMs\":"+N(s.Sum/s.Count)+",\"maxMs\":"+N(s.Max)+",\"p95BucketUpperMs\":"+N(s.P95())+"}");}
  stats.Clear();
 }

 static string Folder=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"Low","Colossal Order","Cities Skylines II","ModsData","StairBuilder","Diagnostics");
 internal static void Flush(){Summarize();if(pending.Count==0)return;try{Directory.CreateDirectory(Folder);string file=Path.Combine(Folder,"performance.jsonl");if(File.Exists(file)&&new FileInfo(file).Length>=Limit){string oldest=file+".2";if(File.Exists(oldest))File.Delete(oldest);if(File.Exists(file+".1"))File.Move(file+".1",oldest);File.Move(file,file+".1");}File.AppendAllText(file,string.Join("\n",pending)+"\n",new UTF8Encoding(false));pending.Clear();}catch(Exception e){pending.Clear();failed=true;Mod.Log.Warn("Performance logging disabled after I/O failure: "+e.GetType().Name);}}
 }
}
