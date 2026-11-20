using System;using System.Drawing;using System.IO;using System.Reflection;using System.Security.Cryptography;using System.Threading.Tasks;using System.Windows.Forms;
namespace SchoolWorkdayCalculator {
internal static class AppUpdateTests {
 internal static void Run(string folder,bool live){Directory.CreateDirectory(folder);int checks=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);checks++;};Action<Action,string> rejects=(action,name)=>{bool rejected=false;try{action();}catch{rejected=true;}check(rejected,name);};try{
  string file=Assembly.GetExecutingAssembly().Location,hash;using(var sha=SHA256.Create())using(var input=File.OpenRead(file))hash=BitConverter.ToString(sha.ComputeHash(input)).Replace("-","").ToLowerInvariant();
  string json=HolidayJson.Json(new AppUpdateInfo{appId="workday320",version=AppRelease.FileVersion,url=AppUpdates.ReleaseRoot+"tag/workday-v"+AppRelease.DisplayVersion,downloadUrl=AppUpdates.ReleaseRoot+"download/workday-v"+AppRelease.DisplayVersion+"/Workday-"+AppRelease.DisplayVersion+".exe",sha256=hash,notes="업데이트 테스트"});
  var info=AppUpdates.Parse(json);check(info.Version==new Version(AppRelease.FileVersion),"own product and release parsed");
  check(!AppUpdates.IsNewer(new Version(AppRelease.DisplayVersion),new Version(AppRelease.FileVersion)),"three/four-part equal versions normalized");
  check(AppUpdates.IsNewer(new Version(1,12,4),new Version(1,12,3,0)),"newer patch recognized");check(!AppUpdates.IsNewer(new Version(1,12,2),new Version(1,12,3,0)),"older release never downgrades");
  rejects(()=>AppUpdates.Parse(json.Replace("workday320","shortpay")),"other calculator manifest rejected");rejects(()=>AppUpdates.Parse(json.Replace("https://github.com/","https://invalid.example/")),"foreign release host rejected");
  rejects(()=>AppUpdates.Parse(json.Replace("workday-v","v")),"other product release prefix rejected");rejects(()=>AppUpdates.Parse(json.Replace("Workday-","ShortTermPayroll-")),"other product EXE rejected");
  rejects(()=>AppUpdates.Parse(json.Replace(hash,"broken")),"malformed digest rejected");rejects(()=>AppUpdates.Parse("{}"),"missing fields rejected");rejects(()=>AppUpdates.Parse(new string('x',65537)),"oversized metadata rejected");
  AppUpdates.Verify(file,hash);check(true,"real EXE digest verified");rejects(()=>AppUpdates.Verify(file,new string('0',64)),"wrong digest rejected");AppUpdates.ValidateFile(file,info);check(true,"real EXE product and version verified");
  using(var form=(MainForm)Activator.CreateInstance(typeof(MainForm),true)){
   form.Offscreen=true;form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(SystemInformation.VirtualScreen.Right+200,SystemInformation.VirtualScreen.Top);string message="",title="";
   form.TestMessage=(text,caption,icon)=>{message=text;title=caption;};form.Show();Application.DoEvents();
   var button=(Button)typeof(MainForm).GetField("appUpdateButton",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);check(button.Text=="업데이트 확인"&&button.Parent==form.Controls.Find("HolidayHistory",true)[0].Parent,"new button beside history in the same header");
   form.AppUpdateFetch=()=>json;Await(form.CheckAppUpdates());check(message.Contains("최신 버전")&&button.Enabled&&button.Text=="업데이트 확인","latest result and button recovery");
   string newer=json.Replace(AppRelease.FileVersion,"1.12.4.0").Replace(AppRelease.DisplayVersion,"1.12.4");form.AppUpdateFetch=()=>newer;Await(form.CheckAppUpdates());check(title=="새 버전 안내"&&message.Contains("1.12.4"),"new version offered without disrupting inputs");
   form.AppUpdateFetch=()=>{throw new IOException("offline");};Await(form.CheckAppUpdates());check(message.Contains("계속 사용할 수 있습니다")&&button.Enabled,"offline restores button and keeps current program usable");form.Hide();
  }
  if(live){var published=AppUpdates.Parse(AppUpdates.Fetch());check(published.Version==new Version(AppRelease.FileVersion),"published manifest matches this release");string downloaded=AppUpdates.Download(published,Path.Combine(folder,"download"));AppUpdates.ValidateFile(downloaded,published);check(true,"published EXE downloaded and verified");}
  File.WriteAllText(Path.Combine(folder,live?"update-live-tests.txt":"update-tests.txt"),"PASS "+checks+" product isolation, version, URL, digest, real EXE, button and offline checks");
 }catch(Exception ex){File.WriteAllText(Path.Combine(folder,live?"update-live-tests.txt":"update-tests.txt"),"FAIL after "+checks+"\r\n"+ex);Environment.ExitCode=1;}}
 static void Await(Task task){while(!task.IsCompleted){Application.DoEvents();System.Threading.Thread.Sleep(10);}task.GetAwaiter().GetResult();}
}
}
