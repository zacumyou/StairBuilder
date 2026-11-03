using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace StairBuilder {
public partial class StairSystem {
 internal string TemplateTestRequest="",TemplateTestReport="";string templateTestPrefix;int templateTestSerial;
 readonly System.Collections.Generic.HashSet<string> templateTestOriginal=new System.Collections.Generic.HashSet<string>();
 internal void ReceiveTemplateTestReport(string json){if(templateTestPrefix==null||json==null||json.Length>12000)return;try{var r=JObject.Parse(json);if((int?)r["serial"]==templateTestSerial)TemplateTestReport=json;}catch{}}
 void TemplateTestTick(string action,int target,float value){
  if(action=="width-template-begin"){
   if(templateTestPrefix!=null||Library.Failed)throw new InvalidOperationException("Template test already active or library unreadable");
   templateTestPrefix="CB"+Guid.NewGuid().ToString("N").Substring(0,6);foreach(var item in Library.Items)templateTestOriginal.Add(item.Id);return;
  }
  if(templateTestPrefix==null)throw new InvalidOperationException("Template test not initialized");
  var owned=Library.Items.Where(x=>!templateTestOriginal.Contains(x.Id)).ToArray();
  if(action=="width-template-cleanup"){foreach(var item in owned)Library.Delete(item.Id);Cancel();TemplateTestRequest="";TemplateTestReport="";return;}
  if(action=="width-template-reload"){var fresh=new TemplateLibrary(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"Low","Colossal Order","Cities Skylines II","ModsData","StairBuilder","templates.json"));if(fresh.Failed||fresh.Json()!=Library.Json())throw new InvalidOperationException("Disk template reload differs");library=fresh;return;}
  if(action=="width-template-place"){if(!GroupPlacement)throw new InvalidOperationException("No group placement");RotateGroup(90);CursorGroup(new UnityEngine.Vector3(benchmarkX+60,450,benchmarkZ+30),true);if(!CanCommit)throw new InvalidOperationException("Loaded template cannot commit");return;}
  if(action=="width-template-limit"){if(!CanStoreTemplate||Library.Items.Count!=50)throw new InvalidOperationException("Limit probe requires full library and valid draft");LibraryCommand(JsonConvert.SerializeObject(new{action="save",name=templateTestPrefix+" overflow"}));if(Status!="templateFull"||Library.Items.Count!=50)throw new InvalidOperationException("Library limit not enforced");return;}
  string[] actions={"open","name","save","queryMatch","queryEmpty","queryClear","rename","renameSubmit","delete","confirm","cancel","load","overwrite","duplicateName","blankName","longName","close","renameDuplicate","nameLong","queryHangul","renameCancel","renameName"};
  if(action!="width-template-ui"||target<0||target>=actions.Length)throw new ArgumentException("Unknown template test action");
  int slot=(int)value;if(slot<0||slot>60)throw new ArgumentOutOfRangeException("slot");
  string id=slot<owned.Length?owned[slot].Id:null;
  TemplateTestReport="";TemplateTestRequest=JsonConvert.SerializeObject(new{serial=++templateTestSerial,action=actions[target],id,name=templateTestPrefix+" 한글 데크 "+slot,duplicate=owned.FirstOrDefault()?.Name,prefix=templateTestPrefix});
 }
}
}
