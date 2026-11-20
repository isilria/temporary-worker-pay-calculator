using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SchoolWorkdayCalculator {
internal static class AppRelease {
 internal const string DisplayVersion = "1.12.3";
 internal const string FileVersion = "1.12.3.0";
}
internal sealed class AppUpdateInfo {
 public string appId,version,url,notes,downloadUrl,sha256;
 internal Version Version {get{return AppUpdates.Normalize(new Version(version));}}
}
internal static class AppUpdates {
 internal const string ManifestUrl="https://raw.githubusercontent.com/isilria/temporary-worker-pay-calculator/main/workday/latest.json";
 internal const string ReleaseRoot="https://github.com/isilria/temporary-worker-pay-calculator/releases/";
 internal static Version Normalize(Version version){return new Version(version.Major,version.Minor,Math.Max(0,version.Build),Math.Max(0,version.Revision));}
 internal static bool IsNewer(Version candidate,Version current){return Normalize(candidate)>Normalize(current);}
 internal static AppUpdateInfo Parse(string json){
  if(String.IsNullOrWhiteSpace(json)||json.Length>65536)throw new InvalidDataException("업데이트 정보 크기가 올바르지 않습니다.");
  AppUpdateInfo info=HolidayJson.Parse<AppUpdateInfo>(json);Version version;
  if(info==null||info.appId!="workday320"||!Version.TryParse(info.version,out version)||version.Build<0||version.Revision>0)throw new InvalidDataException("320일 근무 편성기의 업데이트 정보가 아닙니다.");
  string number=Normalize(version).ToString(3),tag="workday-v"+number;
  if(info.url!=ReleaseRoot+"tag/"+tag||info.downloadUrl!=ReleaseRoot+"download/"+tag+"/Workday-"+number+".exe"||!Regex.IsMatch(info.sha256??"","\\A[0-9a-fA-F]{64}\\z"))throw new InvalidDataException("배포 파일 주소 또는 검증값이 올바르지 않습니다.");
  info.notes=info.notes??"";return info;
 }
 static HttpWebRequest Request(string url){
  var uri=new Uri(url);if(uri.Scheme!="https")throw new InvalidDataException("업데이트 주소는 HTTPS여야 합니다.");
  ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
  var request=(HttpWebRequest)WebRequest.Create(uri);request.Timeout=15000;request.ReadWriteTimeout=15000;request.UserAgent="Workday320/"+AppRelease.DisplayVersion;
  request.CachePolicy=new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);return request;
 }
 internal static string Fetch(){
  using(var response=Request(ManifestUrl+"?check="+DateTime.UtcNow.Ticks).GetResponse()){
   if(response.ResponseUri.Scheme!="https")throw new InvalidDataException("안전하지 않은 업데이트 응답입니다.");
   using(var reader=new StreamReader(response.GetResponseStream(),Encoding.UTF8)){
    var buffer=new char[65537];int count=0,read;while(count<buffer.Length&&(read=reader.Read(buffer,count,buffer.Length-count))>0)count+=read;
    if(count>65536)throw new InvalidDataException("업데이트 정보가 너무 큽니다.");return new string(buffer,0,count);
   }
  }
 }
 internal static void Verify(string file,string expected){
  using(var sha=SHA256.Create())using(var stream=File.OpenRead(file)){
   string actual=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");
   if(!String.Equals(actual,expected,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("다운로드한 파일의 검증값이 다릅니다. 다시 시도해 주세요.");
  }
 }
 internal static void ValidateFile(string file,AppUpdateInfo info){
  Verify(file,info.sha256);
  if(Normalize(AssemblyName.GetAssemblyName(file).Version)!=info.Version||FileVersionInfo.GetVersionInfo(file).ProductName!="교육공무직원 320일 근무 편성기")throw new InvalidDataException("배포 버전 또는 프로그램이 일치하지 않습니다.");
 }
 internal static string Download(AppUpdateInfo info,string folder){
  info=Parse(HolidayJson.Json(info));Directory.CreateDirectory(folder);
  string file=Path.Combine(folder,"Workday-"+info.Version.ToString(3)+".exe"),temporary=file+"."+Guid.NewGuid().ToString("N")+".part";
  if(File.Exists(file))throw new IOException("같은 이름의 파일이 있습니다. 다른 다운로드 폴더를 사용해 주세요.");
  try{
   using(var response=Request(info.downloadUrl).GetResponse()){
    if(response.ResponseUri.Scheme!="https")throw new InvalidDataException("안전하지 않은 다운로드 응답입니다.");
    using(var input=response.GetResponseStream())using(var output=File.Create(temporary)){
     var buffer=new byte[81920];int read;long total=0;while((read=input.Read(buffer,0,buffer.Length))>0){total+=read;if(total>100*1024*1024)throw new InvalidDataException("배포 파일이 너무 큽니다.");output.Write(buffer,0,read);}
    }
   }
   ValidateFile(temporary,info);File.Move(temporary,file);return file;
  }finally{if(File.Exists(temporary))File.Delete(temporary);}
 }
}
internal sealed partial class MainForm {
 Button appUpdateButton;bool appUpdateBusy;
 internal Func<string> AppUpdateFetch=AppUpdates.Fetch;
 void BuildAppUpdateButton(Control parent){
  appUpdateButton=ButtonAt("업데이트 확인",0,15,112,34);appUpdateButton.Name="AppUpdateCheck";appUpdateButton.Font=new Font("맑은 고딕",8F);appUpdateButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;
  appUpdateButton.Click+=async(s,e)=>await CheckAppUpdates();parent.Controls.Add(appUpdateButton);
 }
 internal async Task CheckAppUpdates(){
  if(appUpdateBusy)return;appUpdateBusy=true;appUpdateButton.Enabled=false;appUpdateButton.Text="확인 중…";
  try{
   AppUpdateInfo info=AppUpdates.Parse(await Task.Run(AppUpdateFetch));if(IsDisposed)return;
   if(!AppUpdates.IsNewer(info.Version,Assembly.GetExecutingAssembly().GetName().Version)){Notify("현재 최신 버전("+AppRelease.DisplayVersion+")을 사용하고 있습니다.","업데이트 확인",MessageBoxIcon.Information);return;}
   string message="새 버전 "+info.Version.ToString(3)+"이 있습니다.\r\n현재 버전 "+AppRelease.DisplayVersion+"\r\n\r\n"+info.notes+"\r\n\r\n새 실행파일을 다운로드할까요? 현재 입력 내용은 유지됩니다.";
   if(Offscreen){Notify(message,"새 버전 안내",MessageBoxIcon.Information);return;}
   if(MessageBox.Show(this,message,"업데이트 확인",MessageBoxButtons.YesNo,MessageBoxIcon.Information)!=DialogResult.Yes)return;
   appUpdateButton.Text="다운로드 중…";
   string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SchoolWorkday320","Updates",info.Version+"_"+Guid.NewGuid().ToString("N"));
   string file=await Task.Run(()=>AppUpdates.Download(info,folder));if(IsDisposed)return;
   Notify("새 버전을 다운로드하고 파일 검증을 완료했습니다.\r\n\r\n현재 프로그램을 닫은 뒤, 열리는 폴더의 새 실행파일을 원하는 위치로 옮겨 사용해 주세요.\r\n\r\n"+file,"업데이트 다운로드 완료",MessageBoxIcon.Information);
   Process.Start(new ProcessStartInfo("explorer.exe","/select,\""+file+"\""){UseShellExecute=true});
  }catch(Exception){if(!IsDisposed)Notify("업데이트를 확인하거나 다운로드하지 못했습니다. 인터넷 연결을 확인하고 잠시 후 다시 시도해 주세요. 현재 프로그램은 계속 사용할 수 있습니다.","업데이트 확인",MessageBoxIcon.Information);}
  finally{appUpdateBusy=false;if(!IsDisposed){appUpdateButton.Text="업데이트 확인";appUpdateButton.Enabled=true;}}
 }
}
}
