using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using Game.Prefabs;
using Unity.Entities;
using Unity.Collections;
namespace StairBuilder {
internal sealed class ImportedAssetException:Exception {
 internal ImportedAssetException(Exception cause):base("Imported stair assets could not be loaded: "+cause.Message,cause){}
}
internal sealed class Visual:IDisposable {
 internal SurfaceColor Colors;
 internal int RendererCount;
 internal bool Coarse;internal float DetailUntil;
 internal SharedGeometry Cache;
 internal readonly HashSet<SharedGeometry.Entry> Leases=new HashSet<SharedGeometry.Entry>();
 internal readonly List<MeshBatch.Part>[] Instances={new List<MeshBatch.Part>(),new List<MeshBatch.Part>(),new List<MeshBatch.Part>()};
 internal Bounds InstanceBounds;
 internal GameObject Root;internal readonly List<Mesh> Meshes=new List<Mesh>();
 internal Vector3 Origin;internal Quaternion Rotation;
 internal List<SupportPlan.Pier> Supports;
 internal int PickVertexCount;
 internal sealed class PickGeometry {internal Vector3[] Vertices;internal int[] Indices;internal Bounds Bounds;}
 internal struct PickChunk {internal Bounds Bounds;internal Matrix4x4 Inverse;internal PickGeometry Geometry;internal int Support;internal bool SurfaceOnly;}
 internal readonly List<PickChunk> PickChunks=new List<PickChunk>();
 internal bool Hit(Ray ray,out float distance)=>Hit(ray,out distance,out _,out _);
 internal bool Hit(Ray ray,out float distance,out int support)=>Hit(ray,out distance,out support,out _);
 internal bool Hit(Ray ray,out float distance,out int support,out Vector3 normal,bool surface=false){
  support=-1;distance=float.PositiveInfinity;normal=default;
  var inv=Quaternion.Inverse(Rotation);var origin=inv*(ray.origin-Origin);var direction=inv*ray.direction;
  var r=new Ray(origin,direction);if(!InstanceBounds.IntersectRay(r))return false;
  foreach(var chunk in PickChunks){
   if(chunk.SurfaceOnly&&!surface)continue;
   if(!chunk.Bounds.IntersectRay(r))continue;
   var o=chunk.Inverse.MultiplyPoint3x4(origin);var d=chunk.Inverse.MultiplyVector(direction);
   var v=chunk.Geometry.Vertices;var t=chunk.Geometry.Indices;
   for(int i=0;i<t.Length;i+=3){
    var e=v[t[i+1]]-v[t[i]];var f=v[t[i+2]]-v[t[i]];var cross=Vector3.Cross(d,f);
    float det=Vector3.Dot(e,cross);if(Mathf.Abs(det)<1e-9f)continue;
    var delta=o-v[t[i]];float u=Vector3.Dot(delta,cross)/det;if(u<-.00001f||u>1.00001f)continue;
    var q=Vector3.Cross(delta,e);float w=Vector3.Dot(d,q)/det;if(w<-.00001f||u+w>1.00001f)continue;
    float at=Vector3.Dot(f,q)/det;if(at>0&&at<distance){distance=at;support=chunk.Support;normal=Rotation*chunk.Inverse.transpose.MultiplyVector(Vector3.Cross(e,f)).normalized;}
   }
  }
  if(Vector3.Dot(normal,ray.direction)>0)normal=-normal;
  return !float.IsInfinity(distance);
 }
 public void Dispose(){
 using(var perf=PerformanceLog.Begin("visual.dispose",0,false)){try{InstanceRenderer.Remove(this);foreach(var lease in Leases)lease.Users--;Leases.Clear();if(Root!=null){Root.SetActive(false);UnityEngine.Object.Destroy(Root);}foreach(var m in Meshes)UnityEngine.Object.Destroy(m);Colors?.Dispose();PickChunks.Clear();
 }catch{perf?.Fail();throw;}}
 }
}
internal sealed class StairRenderer:IDisposable {
 // Native AreaDecalShader substitutes Terrain(1) for a zero receiver mask.
 // Receive object decals (Buildings/Other), excluding Terrain-only area surfaces.
 internal const uint SurfaceReceiverMask=4u|32u;
 readonly SharedGeometry shared=new SharedGeometry();
 readonly SurfaceColor.Pool colors=new SurfaceColor.Pool();
 internal object CacheStats=>new{shared.Hits,shared.Misses,shared.Inventory,colors=colors.State,loadedModules=modules.Count,repeatTextures=tiledTextures.Count};
 readonly Dictionary<string,Mesh[]> modules=new Dictionary<string,Mesh[]>();
 readonly Dictionary<string,Material[]> surfaceSets=new Dictionary<string,Material[]>();
 readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
 readonly List<RenderPrefab> acquired=new List<RenderPrefab>();
 readonly Dictionary<string,StaticObjectPrefab> imported=new Dictionary<string,StaticObjectPrefab>();
 readonly Game.Simulation.TerrainSystem terrainSystem;
 internal StairRenderer(PrefabSystem prefabs,EntityManager em){
 using(var perf=PerformanceLog.Begin("assets.load",0,false)){try{
  terrainSystem=em.World.GetOrCreateSystemManaged<Game.Simulation.TerrainSystem>();
  try{
   using(var query=em.CreateEntityQuery(ComponentType.ReadOnly<PrefabData>()))
   using(var entities=query.ToEntityArray(Allocator.Temp)){
    foreach(var entity in entities){
     if(!prefabs.TryGetPrefab<StaticObjectPrefab>(entity,out var candidate)||candidate==null||!candidate.name.StartsWith("CSKRStair",StringComparison.Ordinal))continue;
     if(imported.ContainsKey(candidate.name))throw new InvalidOperationException("Duplicate stair asset: "+candidate.name);
     imported.Add(candidate.name,candidate);
    }
   }
   foreach(var name in new[]{"CSKRStairStep2m","CSKRStairSlab2m","CSKRStairPost","CSKRStairRail","CSKRStairPier","CSKRStairPierShaft"})
    if(!imported.ContainsKey(name))throw new InvalidOperationException("Required imported stair asset missing: "+name);
  }catch(Exception e){Dispose();throw new ImportedAssetException(e);}
 }catch{perf?.Fail();throw;}}
 }
 void LoadModule(string name,int lod){
  if(modules.ContainsKey(name+":"+lod))return;
  int acquiredBefore=acquired.Count;
  try{using(PerformanceLog.Begin("assets.module")){
    if(!imported.TryGetValue(name,out var obj))
     throw new InvalidOperationException("Required imported stair asset missing: "+name);
    if(obj.m_Meshes==null||obj.m_Meshes.Length!=1||!(obj.m_Meshes[0].m_Mesh is RenderPrefab main))
     throw new InvalidOperationException("Expected one RenderPrefab: "+name);
    var info=obj.m_Meshes[0];
    if(!info.m_Position.Equals(Unity.Mathematics.float3.zero)||!info.m_Rotation.Equals(Unity.Mathematics.quaternion.identity))
     throw new InvalidOperationException("Expected origin/identity module: "+name);
    var lods=main.GetComponent<LodProperties>();
    bool baseOnly=false;
    if(!baseOnly&&(lods?.m_LodMeshes==null||lods.m_LodMeshes.Length!=2))
     throw new InvalidOperationException("Required LOD1/LOD2 missing: "+name);

     var render=lod==0?main:lods.m_LodMeshes[lod-1];
     if(render==null||render.geometryAsset==null||render.materialCount<1)
      throw new InvalidOperationException("Invalid imported module surface: "+name+" LOD"+lod);
     var meshes=render.geometryAsset.ObtainMeshes(true);acquired.Add(render);
     if(meshes==null||meshes.Length==0||Array.Exists(meshes,m=>m==null||!m.isReadable))
      throw new InvalidOperationException("Expected one readable imported mesh: "+name+" LOD"+lod);
     var surfaces=new Material[render.materialCount];
     for(int surface=0;surface<surfaces.Length;surface++){
     var sourceMaterial=render.ObtainMaterial(surface,false);
     if(sourceMaterial==null)throw new InvalidOperationException("Imported material unavailable: "+name);
     // Match the game debug renderer's static, non-VT path without mutating shared assets.
     var material=new Material(sourceMaterial){name=name+" Stair LOD"+lod,hideFlags=HideFlags.HideAndDontSave};
     material.enableInstancing=false;materials.Add(name+":"+lod+":"+surface,material);surfaces[surface]=material;
     if(name.Contains("Step")||name.Contains("Slab")||name.EndsWith("Rail")||name.EndsWith("Beam")||name.EndsWith("Column")||name.EndsWith("Brace")||name.StartsWith("CSKRStairGlassRoof"))foreach(var property in material.GetTexturePropertyNames()){
      var texture=material.GetTexture(property);if(!(texture is Texture2D)||texture.wrapMode==TextureWrapMode.Repeat)continue;
      if(!tiledTextures.TryGetValue(texture,out var tiled)){
       var source=(Texture2D)texture;
       var flags=UnityEngine.Experimental.Rendering.TextureCreationFlags.DontInitializePixels;
       if(source.mipmapCount>1)flags|=UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain;
       var copy=new Texture2D(source.width,source.height,source.graphicsFormat,source.mipmapCount,flags){name=source.name+" Stair repeat",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Repeat,filterMode=source.filterMode,anisoLevel=source.anisoLevel,mipMapBias=source.mipMapBias};
       try{
        // Native assets discard CPU pixels. Copy the actual GPU mip chain;
        // do not Apply afterwards (that would overwrite the GPU copy).
        Graphics.CopyTexture(source,copy);
       }catch{UnityEngine.Object.Destroy(copy);throw;}
       tiled=copy;tiledTextures.Add(texture,tiled);
       Mod.Log.Info("Stair GPU texture copy "+property+" "+source.width+"x"+source.height+" format="+source.graphicsFormat+" mips="+source.mipmapCount+" readable="+source.isReadable);
      }
      material.SetTexture(property,tiled);
     }
     material.DisableKeyword("ENABLE_VT");material.DisableKeyword("_GPU_ANIMATION_PROCEDURAL");material.DisableKeyword("_GPU_ANIMATION_SHAPE");
     // Opt only this material copy out of projected terrain/surface decals.
     material.SetFloat("colossal_DecalLayerMask",Unity.Mathematics.math.asfloat(SurfaceReceiverMask));
     if(name=="CSKRStairStep2m")Mod.Log.Info("Stair native receiver LOD"+lod+" mask="+Unity.Mathematics.math.asuint(material.GetFloat("colossal_DecalLayerMask")));
     if(material.HasProperty("_SupportDecals"))material.SetFloat("_SupportDecals",1);
     material.DisableKeyword("_DISABLE_DECALS");
     // Keep the imported queue: live0.2.4 logs confirmed it already uses2225.
     material.SetShaderPassEnabled("MOTIONVECTORS",false);
     if(material.HasProperty("colossal_LodFade"))material.SetFloat("colossal_LodFade",1f);
     if(meshes[0].vertexCount==0||meshes[0].GetIndexCount(0)==0)throw new InvalidOperationException("Empty imported mesh: "+name);
     }
     int slots=0;foreach(var mesh in meshes)slots+=mesh.subMeshCount;if(slots!=surfaces.Length)throw new InvalidOperationException("Surface slot mismatch: "+name);
     modules.Add(name+":"+lod,meshes);surfaceSets.Add(name+":"+lod,surfaces);
  }}catch(Exception e){
   modules.Remove(name+":"+lod);surfaceSets.Remove(name+":"+lod);
   var failed=new List<string>();foreach(var key in materials.Keys)if(key.StartsWith(name+":"+lod+":",StringComparison.Ordinal))failed.Add(key);
   foreach(var key in failed){UnityEngine.Object.Destroy(materials[key]);materials.Remove(key);}
   while(acquired.Count>acquiredBefore){int last=acquired.Count-1;acquired[last].ReleaseMeshes();acquired.RemoveAt(last);}
   throw new ImportedAssetException(e);
  }
 }

 readonly Dictionary<Mesh,Visual.PickGeometry> pickSources=new Dictionary<Mesh,Visual.PickGeometry>();
 readonly Dictionary<Texture,Texture> tiledTextures=new Dictionary<Texture,Texture>();
 internal Visual Build(Stair stair,IEnumerable<Stair> neighbors=null,bool coarse=false,Visual picking=null){
  using(var perf=PerformanceLog.Begin("visual.build",stair.Id,false)){try{Visual result=null;foreach(var step in BuildSteps(stair,neighbors,coarse,picking,v=>result=v)){}return result;}catch{perf?.Fail();throw;}}
 }
 internal IEnumerable<int> BuildSteps(Stair stair,IEnumerable<Stair> neighbors,bool coarse,Visual picking,Action<Visual> completed){
  if(picking!=null&&(picking.Rotation!=Quaternion.identity||picking.Origin!=stair.A))picking=null;
  RoutePlan plan;using(PerformanceLog.Begin("plan.route",stair.Id))if(!RoutePlan.Build(stair,out plan,out _))throw new ArgumentException("Invalid stair");
  var result=new Visual{Coarse=coarse,DetailUntil=Time.unscaledTime+8,Colors=new SurfaceColor(colors),Cache=shared,Origin=stair.A,Rotation=Quaternion.identity,Root=new GameObject("Stair Builder "+stair.Id)};
  result.Colors.Set(stair.DeckColor,stair.RailColor);
  result.Root.SetActive(false);result.Root.transform.SetPositionAndRotation(result.Origin,result.Rotation);
  bool transferred=false;try{
   int railKind=stair.EffectiveRailing;bool tunnel=stair.Style==3;bool glassStyle=railKind==2||railKind==6;
   RailingPlan railing;using(PerformanceLog.Begin("plan.railing",stair.Id))railing=RailingPlan.Build(plan,stair.Width,glassStyle||tunnel||railKind==4||railKind==5).OpenJunctions(BranchPlan.JunctionPads(stair,neighbors));
   var heightData=terrainSystem.GetHeightData();
   List<SupportPlan.Pier> supports;using(PerformanceLog.Begin("plan.supports",stair.Id))supports=SupportPlan.Build(plan,stair.Width,p=>Game.Simulation.TerrainUtils.SampleHeight(ref heightData,(Unity.Mathematics.float3)p),stair.SuppressedSupports,stair.Style==4||stair.Style==5,stair.Style,stair.Ramp);
   if(stair.Solid)supports.Clear();
   result.Supports=supports;
   if(picking!=null){result.PickChunks.AddRange(picking.PickChunks);result.PickVertexCount=picking.PickVertexCount;result.InstanceBounds=picking.InstanceBounds;}
   var scaledMeshes=new Dictionary<string,Mesh>();
   var splitMeshes=new Dictionary<Mesh,Mesh>();
   
   int lodCount=3;
   int moduleWidth=Mathf.Clamp(Mathf.CeilToInt(stair.Width),2,4);float widthScale=(float)stair.Width/moduleWidth;
   string prefix=stair.Style==5?"CSKRStairWood":stair.Style==4?"CSKRStairSteel":stair.Style==2?"CSKRStairOld":stair.Style==1||stair.Style==3?"CSKRStairModern":"CSKRStair";
   var lods=new LOD[coarse?1:lodCount];
   var axis=Quaternion.Euler(0,180,0);
   for(int lod=0;lod<lodCount;lod++){
   if(coarse&&(lod==1||(lod==0&&picking!=null)))continue;
   bool far=lod==2,pickOnly=coarse&&lod==0;
   var levelRailing=far?RailingPlan.Build(plan,stair.Width,true,4f).OpenJunctions(BranchPlan.JunctionPads(stair,neighbors)):railing;
 yield return 0;{
    var parts=new List<MeshBatch.Part>();bool railSurface=false;
    void Add(string name,Vector3 p,Quaternion rotation,Vector3 scale,bool step=false,int support=-1,float shear=0,float endCut=0,float curbStart=0,float curbEnd=0){
     if(far&&(name.EndsWith("Grip")||name.EndsWith("Clamp")||name.EndsWith("Gusset")||name.EndsWith("Brace")||name=="CSKRStairSteelEnd"||name=="CSKRStairSteelFoot"||name=="CSKRStairSteelHead"))return;
     string moduleKey=name+":"+lod;
     LoadModule(name,lod);
     var pieces=modules[moduleKey];
     int materialOffset=0;p-=result.Origin;
     foreach(var source in pieces){
     // GeometryAsset normals/tangents use game-specific packed formats. Keep them intact.
     Mesh display=source;int variation=name.StartsWith("CSKRStairOldStep")?Mathf.Abs(Mathf.RoundToInt(p.y/.17f))%7:0;var phase=new Vector2(.137f,0)*variation;
     bool oldConcrete=name.StartsWith("CSKRStairOld")&&(name.Contains("Step")||name.Contains("Slab"));
     var uvScale=oldConcrete?scale*(5f/1.2f):scale;
     if(!far&&!pickOnly&&(name.Contains("Step")||name.Contains("Slab")||name.EndsWith("Rail")||name.EndsWith("Beam")||name.EndsWith("Column")||name.EndsWith("Brace")||name.StartsWith("CSKRStairGlassRoof"))&&((scale-Vector3.one).sqrMagnitude>.000001f||variation>0||oldConcrete)){
      string key=name+":"+lod+":"+variation+":"+Array.IndexOf(pieces,source)+":"+scale.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+scale.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+scale.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
      if(!scaledMeshes.TryGetValue(key,out display)){using(PerformanceLog.Begin("mesh.uv",stair.Id))display=shared.Get(result,"uv:"+key,()=>ScaledModuleUV.Create(source,uvScale,phase));scaledMeshes.Add(key,display);}
     }
     if(Mathf.Abs(shear)>.00001f){var input=display;display=shared.Get(result,"shear:"+input.GetInstanceID()+":"+shear.ToString("R",CultureInfo.InvariantCulture),()=>PanelShear.Create(input,shear));}
     if(Mathf.Abs(endCut)>.00001f){var input=display;display=shared.Get(result,"cut:"+input.GetInstanceID()+":"+endCut.ToString("R",CultureInfo.InvariantCulture),()=>BeamEndCut.Create(input,endCut));}
     bool curbCut=Mathf.Abs(curbStart)+Mathf.Abs(curbEnd)>.00001f;
     if(curbCut){var input=display;display=shared.Get(result,"curb:"+input.GetInstanceID()+":"+curbStart.ToString("R",CultureInfo.InvariantCulture)+":"+curbEnd.ToString("R",CultureInfo.InvariantCulture),()=>CurbMiter.Create(input,curbStart,curbEnd));}
     var subset=new Material[source.subMeshCount];Array.Copy(surfaceSets[moduleKey],materialOffset,subset,0,subset.Length);materialOffset+=subset.Length;if(!pickOnly&&name.StartsWith("CSKRStairOld")&&(name.Contains("Step")||name.Contains("Slab"))&&name!="CSKRStairOldCleanStep"){
      LoadModule("CSKRStairOldCleanStep",lod);
      if(!surfaceSets.TryGetValue("CSKRStairOldCleanStep:"+lod,out var clean))throw new InvalidOperationException("Missing clean concrete style asset");
      if(!splitMeshes.TryGetValue(display,out var split)){
      var splitSource=display;split=shared.Get(result,"oldsplit:"+splitSource.GetInstanceID(),()=>{var built=UnityEngine.Object.Instantiate(splitSource);var vv=splitSource.vertices;var ii=splitSource.triangles;var sides=new List<int>();var tops=new List<int>();
      for(int t=0;t<ii.Length;t+=3){var normal=Vector3.Cross(vv[ii[t+1]]-vv[ii[t]],vv[ii[t+2]]-vv[ii[t]]).normalized;var list=normal.y>.7f?tops:sides;list.Add(ii[t]);list.Add(ii[t+1]);list.Add(ii[t+2]);}
      built.subMeshCount=2;built.SetTriangles(sides,0);built.SetTriangles(tops,1);return built;});splitMeshes.Add(display,split);}
      display=split;subset=new[]{subset[0],clean[0]};
     }
     if(!pickOnly)parts.Add(new MeshBatch.Part{Mesh=display,Materials=subset,Position=p,Rotation=rotation*axis,Scale=scale,Rail=railSurface});
     if(lod==0&&picking==null){
      // Immutable source triangles are shared; only per-module transforms/bounds vary.
      bool deformed=curbCut||Mathf.Abs(endCut)>.00001f||Mathf.Abs(shear)>.00001f;var pickSource=deformed?display:source;
      Visual.PickGeometry geometry;
      if(deformed)geometry=shared.Pick(pickSource);
      else if(!pickSources.TryGetValue(pickSource,out geometry)){
       geometry=new Visual.PickGeometry{Vertices=pickSource.vertices,Indices=pickSource.triangles,Bounds=pickSource.bounds};
       pickSources.Add(pickSource,geometry);
      }
      var matrix=Matrix4x4.TRS(p,rotation*axis,scale);var box=geometry.Bounds;
      var bounds=new Bounds(matrix.MultiplyPoint3x4(box.center),Vector3.zero);
      for(int corner=0;corner<8;corner++)bounds.Encapsulate(matrix.MultiplyPoint3x4(box.center+Vector3.Scale(box.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));
      // Conservative broad phase: transformed float bounds must not reject an edge hit.
      bounds.Expand(.002f);
      if(result.PickChunks.Count==0)result.InstanceBounds=bounds;else result.InstanceBounds.Encapsulate(bounds);
      result.PickChunks.Add(new Visual.PickChunk{Bounds=bounds,Inverse=matrix.inverse,Geometry=geometry,Support=support,SurfaceOnly=!step});
      if(step)result.PickVertexCount+=geometry.Vertices.Length;
     }
    }
    }
    string railPrefix=railKind==7?"CSKRStairWood":railKind==2?"CSKRStairModern":railKind==3?"CSKRStairOld":railKind==4?"CSKRStairSteel":"CSKRStair";
    void Rail(Vector3 a,Vector3 b){if((b-a).sqrMagnitude<.0001f)return;Add(railPrefix+"Rail",a,Quaternion.LookRotation(b-a),new Vector3(1,1,(b-a).magnitude));}
    void Box(string boxPrefix,Vector3 top,Vector3 forward,float width,float length,float depth,int support=-1){
     if(depth<=.005f)return;
     float sourceDepth=boxPrefix=="CSKRStairModern"?.50f:.32f,scaleY=depth/sourceDepth;
     Add(boxPrefix+"Slab"+moduleWidth+"m",top-Vector3.up*(.17f*scaleY),Quaternion.LookRotation(forward),new Vector3(width/moduleWidth,scaleY,length/.3f),true,support);
    }
    void Fill(Vector3 a,Vector3 b,float width){
     var delta=b-a;var forward=delta;forward.y=0;float length=forward.magnitude;if(length<.005f)return;forward/=length;var right=Vector3.Cross(Vector3.up,forward);
     int cells=Math.Max(1,Mathf.CeilToInt(length/.5f));
     for(int i=0;i<cells;i++){
      var start=Vector3.Lerp(a,b,(float)i/cells);var end=Vector3.Lerp(a,b,(float)(i+1)/cells);float bottom=float.PositiveInfinity;
      foreach(float t in new[]{0f,.5f,1f})foreach(float x in new[]{-.5f,0f,.5f}){
       var p=Vector3.Lerp(start,end,t)+right*(width*x);bottom=Mathf.Min(bottom,Game.Simulation.TerrainUtils.SampleHeight(ref heightData,(Unity.Mathematics.float3)p));
      }
      float top=Mathf.Min(start.y,end.y)-.015f;bottom-=.20f;
      Box(stair.Style==4||stair.Style==5?"CSKRStair":prefix,new Vector3(start.x,top,start.z)-forward*.003f,forward,width-.004f,length/cells+.006f,top-bottom);
     }
    }
    if(stair.Solid){
     foreach(var run in plan.Runs){
      if(run.Ramp)Fill(run.A,run.B,stair.Width);
      else {var forward=run.B-run.A;forward.y=0;forward.Normalize();for(int i=0;i<run.Steps;i++){var a=run.A+forward*(i*run.Tread)+Vector3.up*((i+1)*run.Rise);Fill(a,a+forward*run.Tread,stair.Width);}}
     }
     foreach(var pad in plan.Pads)Fill(pad.Center-pad.Forward*pad.Length*.5f,pad.Center+pad.Forward*pad.Length*.5f,pad.Width);
    }
    foreach(var run in plan.Runs){
     yield return 0;
     if(run.Ramp||far){var direction=run.B-run.A;var tilt=Quaternion.LookRotation(direction);var normal=tilt*Vector3.up;
      if(stair.Style==5&&!far){int boards=Math.Max(1,Mathf.CeilToInt(direction.magnitude/.3f));for(int k=0;k<boards;k++)Add(prefix+"Slab"+moduleWidth+"m",run.A+direction*((float)k/boards)-normal*.17f-Vector3.up*.003f,tilt,new Vector3(widthScale,1,direction.magnitude/boards/.3f),true);}else Add(prefix+"Slab"+moduleWidth+"m",run.A-normal*.17f-Vector3.up*.003f,tilt,new Vector3(widthScale,1,direction.magnitude/.3f),true);continue;
     }
     int n=run.Steps;float h=run.Rise,d=run.Tread;var forward=run.B-run.A;forward.y=0;forward.Normalize();var rotation=Quaternion.LookRotation(forward);var right=Vector3.Cross(Vector3.up,forward);
     Vector3 At(float x,float y,float z)=>run.A+right*x+Vector3.up*y+forward*z;
     for(int i=0;i<n;i++){
      if(i%8==0)yield return 0;
      Add(prefix+"Step"+moduleWidth+"m",At(0,i*h,i*d),rotation,new Vector3(widthScale,h/.17f,d/.3f),true);
      if(stair.Style==1||stair.Style==3)Add("CSKRStairModernGrip",At(0,(i+1)*h+.002f,i*d),rotation,new Vector3(stair.Width-.2f,1,1));
     }
    }
    foreach(var pad in plan.Pads){
     yield return 0;
     var right=Vector3.Cross(Vector3.up,pad.Forward);var rotation=Quaternion.LookRotation(pad.Forward);
     float sourceDepth=stair.Style==1||stair.Style==3?.50f:.32f;
     float verticalScale=stair.Ramp||stair.Style==4||stair.Style==5?1:SupportPlan.PadThickness(plan,pad,stair.Style)/sourceDepth;
     int tiles=stair.Style==5&&!far?Math.Max(1,Mathf.CeilToInt(pad.Length/.3f)):1;for(int k=0;k<tiles;k++)Add(prefix+"Slab"+moduleWidth+"m",pad.Center-pad.Forward*(pad.Length*.5f-k*pad.Length/tiles)-Vector3.up*(.17f*verticalScale+.003f),rotation,new Vector3(pad.Width/moduleWidth,verticalScale,pad.Length/tiles/.3f),true);
    }
    if(stair.Style==4||stair.Style==5){
     void Stringers(Vector3 a,Vector3 b,float width,bool level){
      var delta=b-a;var rotation=Quaternion.LookRotation(delta);var right=rotation*Vector3.right;var normal=rotation*Vector3.up;
      var flat=delta;flat.y=0;float slope=delta.y/flat.magnitude,cos=flat.magnitude/delta.magnitude;
      foreach(float side in new[]{-1f,1f}){
       var offset=right*(side*(width*.5f-.07f))-Vector3.up*.015f;
       Add(prefix+"Beam",a+offset,rotation,new Vector3(1,1,delta.magnitude),true,-1,0,slope/delta.magnitude);
       if(stair.Style==4)foreach(var end in new[]{a,b})Add("CSKRStairSteelEnd",end+offset,Quaternion.LookRotation(right),new Vector3(1,1/cos,1),true);
      }
     }
     foreach(var run in plan.Runs)Stringers(run.A,run.B,stair.Width,run.Ramp);
     foreach(var pad in plan.Pads)Stringers(pad.Center-pad.Forward*pad.Length*.5f,pad.Center+pad.Forward*pad.Length*.5f,pad.Width,true);
    }
    railSurface=true;
    if(tunnel)foreach(var p in levelRailing.Posts)Add("CSKRStairGlassTower",p,Quaternion.identity,Vector3.one);
    else if(railKind!=0&&railKind!=5)foreach(var p in levelRailing.Posts)Add(railPrefix+"Post",p,Quaternion.identity,Vector3.one);
    foreach(var bar in levelRailing.Bars){
     yield return 0;
     if(railKind==0)continue;
     var delta=bar.B-bar.A;var horizontal=delta;horizontal.y=0;float length=horizontal.magnitude;if(length<.03f)continue;
     if(railKind==5){
      string curbPrefix=stair.Style==4||stair.Style==5?"CSKRStair":prefix;
      float depth=stair.Style==1||stair.Style==3?.50f:.32f,k=.20f/depth;
      float zScale=length/.3f;
      // Shear the curb vertically: ends stay upright and share the landing elevation.
      Add(curbPrefix+"Slab"+moduleWidth+"m",bar.A+Vector3.up*(.20f-.17f*k),Quaternion.LookRotation(horizontal),new Vector3(.15f/moduleWidth,k,zScale),true,-1,-delta.y/(.3f*k),0,CurbJoin.Slope(levelRailing,bar,true)*(.15f/moduleWidth)/zScale,CurbJoin.Slope(levelRailing,bar,false)*(.15f/moduleWidth)/zScale);continue;
     }
     foreach(float y in glassStyle?new[]{1.05f}:railKind==3?new[]{.14f,.5f,1.05f}:new[]{.5f,1.05f})Rail(bar.A+Vector3.up*y,bar.B+Vector3.up*y);
     if(railKind==3&&!far){int picketBays=Math.Max(1,Mathf.CeilToInt(length/.16f));for(int k=1;k<picketBays;k++)Add("CSKRStairOldRail",Vector3.Lerp(bar.A,bar.B,(float)k/picketBays)+Vector3.up*.12f,Quaternion.LookRotation(Vector3.up),new Vector3(.38f,.38f,.90f));}
     if(!glassStyle)continue;
     var rotation=Quaternion.LookRotation(horizontal);int bays=far?1:Math.Max(1,Mathf.CeilToInt(length/1.2f));
     for(int bay=0;bay<bays;bay++){
      var a=Vector3.Lerp(bar.A,bar.B,(float)bay/bays);float span=length/bays,rise=delta.y/bays;
      Add(railKind==6?"CSKRStairGlassWall":"CSKRStairGlassPanel",a,rotation,new Vector3(1,1,span),false,-1,-rise);
      foreach(float t in new[]{.035f,.965f})foreach(float z in railKind==6?new[]{1.3f,2.4f}:new[]{.25f,.78f})Add("CSKRStairGlassClamp",a+horizontal.normalized*(span*t)+Vector3.up*(rise*t+z),rotation,Vector3.one);
     }
     if(tunnel)Rail(bar.A+Vector3.up*2.6f,bar.B+Vector3.up*2.6f);
    }
    if(tunnel){
     void Roof(Vector3 a,Vector3 b,float width){var d=b-a;int bays=far?1:Math.Max(1,Mathf.CeilToInt(d.magnitude/1.2f));var rotation=Quaternion.LookRotation(d);
      var horizontal=d;horizontal.y=0;float length=d.magnitude/bays,cos=horizontal.magnitude/d.magnitude,sin=d.y/d.magnitude;
      // Vertical end planes, with unchanged world-height cross section. This also
      // matches the flat landing roof without a height step at slope transitions.
      for(int i=0;i<bays;i++)Add("CSKRStairGlassRoof"+moduleWidth+"m",Vector3.Lerp(a,b,(float)i/bays)+Vector3.up*2.6f,rotation,new Vector3(width/moduleWidth,cos,length),true,-1,0,sin/length);
     }
     foreach(var run in plan.Runs)Roof(run.A,run.B,stair.Width);
     foreach(var pad in plan.Pads)Roof(pad.Center-pad.Forward*pad.Length*.5f,pad.Center+pad.Forward*pad.Length*.5f,pad.Width);
    }
    railSurface=false;
    foreach(var pier in supports){
     yield return 0;
     if(stair.Style==5){var forward=pier.Forward;forward.y=0;forward.Normalize();var right=Vector3.Cross(Vector3.up,forward);float half=stair.Width*.5f-.16f;Add("CSKRStairWoodBeam",pier.Top-right*(half+.07f),Quaternion.LookRotation(right),new Vector3(1,1,2*(half+.07f)),true,pier.Key);foreach(float side in new[]{-1f,1f}){var top=pier.Top+right*(side*half)-Vector3.up*.32f;float ground=Game.Simulation.TerrainUtils.SampleHeight(ref heightData,(Unity.Mathematics.float3)top);if(top.y>ground)Add("CSKRStairWoodColumn",top,Quaternion.LookRotation(forward),new Vector3(1,top.y-ground+.08f,1),true,pier.Key);}continue;}
     if(stair.Style==4){
      var forward=pier.Forward;forward.y=0;forward.Normalize();var right=Vector3.Cross(Vector3.up,forward);float half=stair.Width*.5f-.20f;
      var top=pier.Top-Vector3.up*.344f;float ground=pier.Bottom+.20f;float steelHeight=top.y-ground;if(steelHeight<.1f)continue;
      Add("CSKRStairSteelBeam",pier.Top-right*(half+.08f),Quaternion.LookRotation(right),new Vector3(1,1,2*(half+.08f)),true,pier.Key);
      foreach(float side in new[]{-1f,1f}){
       var p=top+right*(side*half);
       float footGround=Game.Simulation.TerrainUtils.SampleHeight(ref heightData,(Unity.Mathematics.float3)p);
       float columnHeight=p.y-footGround-.022f;if(columnHeight<.05f)continue;
       Add("CSKRStairSteelColumn",p,Quaternion.LookRotation(forward),new Vector3(1,columnHeight,1),true,pier.Key);
       Add("CSKRStairSteelHead",p,Quaternion.LookRotation(forward),Vector3.one,true,pier.Key);
       Add("CSKRStairSteelFoot",new Vector3(p.x,footGround,p.z),Quaternion.LookRotation(forward),Vector3.one,true,pier.Key);
       Add("CSKRStairSteelEnd",pier.Top+right*(side*(half+.08f)),Quaternion.LookRotation(forward),Vector3.one,true,pier.Key);
      }
      void Brace(Vector3 a,Vector3 b){var d=b-a;if(d.magnitude<.3f)return;var dir=d.normalized;
       // Offset opposite braces into parallel planes; no solid-tube intersection at X.
       var rotation=Quaternion.LookRotation(d,Vector3.Cross(d,forward));
       Add("CSKRStairSteelBrace",a+dir*.10f,rotation,new Vector3(1,1,d.magnitude-.20f),true,pier.Key);
       Add("CSKRStairSteelGusset",a,rotation,Vector3.one,true,pier.Key);
       Add("CSKRStairSteelGusset",b,Quaternion.LookRotation(-d,Vector3.Cross(-d,forward)),Vector3.one,true,pier.Key);
      }
      if(!far&&steelHeight>=2f){
       int tiers=Math.Max(1,Mathf.CeilToInt(steelHeight/3f));
       for(int tier=0;tier<tiers;tier++){
        var low=new Vector3(top.x,ground+.16f+(steelHeight-.32f)*tier/tiers,top.z);var high=new Vector3(top.x,ground+.16f+(steelHeight-.32f)*(tier+1)/tiers,top.z);
        Brace(low-right*half+forward*.035f,high+right*half+forward*.035f);Brace(low+right*half-forward*.035f,high-right*half-forward*.035f);
       }
      }
      continue;
     }
     if(stair.Style==2){
      var forward=pier.Forward;forward.y=0;forward.Normalize();
      var capForward=pier.Forward.normalized;var cap=pier.Top-capForward*.32f;
      Box("CSKRStairOld",cap,capForward,pier.Width,.64f,.40f,pier.Key);
      var shaft=pier.Top-Vector3.up*.38f-forward*.25f;
      Box("CSKRStairOld",shaft,forward,.50f,.50f,shaft.y-pier.Bottom,pier.Key);continue;
     }
     Add("CSKRStairPier",pier.Top,Quaternion.LookRotation(pier.Forward),new Vector3(pier.Width/1.6f,1,1),true,pier.Key);
     // Cap remains rigid. Repeat the imported shaft at near-unit length instead
     // of stretching its aggregate texture over the entire support height.
     var shaftTop=pier.ShaftTop;
     float height=shaftTop.y-pier.Bottom;
     int pieces=far?1:Math.Max(1,Mathf.CeilToInt(height));
     float pieceHeight=height/pieces;
     for(int i=0;i<pieces;i++)
      Add("CSKRStairPierShaft",shaftTop+Vector3.down*(i*pieceHeight),Quaternion.identity,new Vector3(1,pieceHeight,1),true,pier.Key);
    }
    if(pickOnly)continue;
    var renderers=new List<Renderer>();
    if(InstanceRenderer.Mode==2){foreach(var step in MeshBatch.BuildSteps(result,parts,renderers))yield return step;}
    else renderers=InstanceRenderer.Prepare(result,parts,lod);
    lods[coarse?0:lod]=new LOD(coarse?.002f:lod==0?.16f:lod==1?.045f:.002f,renderers.ToArray());
   
 }
 }
   // Temporary UV/shear clones are no longer needed once all LOD batches exist.
   var retained=new HashSet<Mesh>();foreach(var level in result.Instances)foreach(var part in level)retained.Add(part.Mesh);foreach(var level in lods)foreach(var r in level.renderers)retained.Add(r.GetComponent<MeshFilter>().sharedMesh);
   for(int i=result.Meshes.Count-1;i>=0;i--)if(!retained.Contains(result.Meshes[i])){UnityEngine.Object.Destroy(result.Meshes[i]);result.Meshes.RemoveAt(i);}
   shared.Retain(result,retained);
   if(result.PickChunks.Count==0)throw new InvalidOperationException("Stair selection geometry is empty");
   using(PerformanceLog.Begin("visual.pickMesh",stair.Id)){
   var group=result.Root.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();result.Root.SetActive(true);InstanceRenderer.Add(result);shared.Trim();PerformanceLog.Event("visual.renderers",stair.Id,result.RendererCount);PerformanceLog.Event("visual.ownedMeshes",stair.Id,result.Meshes.Count);PerformanceLog.Event("visual.pickVertices",stair.Id,result.PickVertexCount);completed(result);transferred=true;}
  }finally{if(!transferred)result.Dispose();}
 }
 internal void TrimCache()=>shared.Trim();
 public void Dispose(){
  // Shared generated geometry/material copies are owned; imported meshes stay borrowed.
  shared.Dispose();colors.Dispose();InstanceRenderer.Shutdown();foreach(var material in materials.Values)UnityEngine.Object.Destroy(material);
  foreach(var texture in tiledTextures.Values)UnityEngine.Object.Destroy(texture);tiledTextures.Clear();
  foreach(var render in acquired)render.ReleaseMeshes();
  acquired.Clear();modules.Clear();materials.Clear();pickSources.Clear();
 }
}
}





