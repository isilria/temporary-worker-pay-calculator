using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShortPay {
class UpdateInfo { public Version Version; public string Url, Notes, DownloadUrl, Sha256; }
static class PayrollUpdates {
 internal const string ManifestUrl="https://raw.githubusercontent.com/isilria/temporary-worker-pay-calculator/main/latest.json";
 internal static string SourceFile {get{return Path.Combine(Application.StartupPath,"update-source.json");}}
 internal static string Source(){if(!File.Exists(SourceFile))return ManifestUrl;var data=Engine.Parse<Dictionary<string,string>>(File.ReadAllText(SourceFile));string url;if(data==null||!data.TryGetValue("manifestUrl",out url)||String.IsNullOrWhiteSpace(url))throw new InvalidDataException("업데이트 주소 설정이 비어 있습니다.");return url;}
 internal static Version Normalize(Version v){return new Version(v.Major,v.Minor,Math.Max(0,v.Build),Math.Max(0,v.Revision));}
 internal static bool IsNewer(Version candidate,Version current){return Normalize(candidate)>Normalize(current);}
 internal static UpdateInfo Parse(string json){
  var data=Engine.Parse<Dictionary<string,string>>(json);string app,version,url,notes,download,hash;Version parsed;Uri uri;
  if(data==null||!data.TryGetValue("appId",out app)||app!="shortpay"||!data.TryGetValue("version",out version)||!Version.TryParse(version,out parsed)||!data.TryGetValue("url",out url)||!Uri.TryCreate(url,UriKind.Absolute,out uri)||uri.Scheme!="https")throw new InvalidDataException("이 임금계산기의 업데이트 정보가 올바르지 않습니다.");
  data.TryGetValue("notes",out notes);data.TryGetValue("downloadUrl",out download);data.TryGetValue("sha256",out hash);
  if(!String.IsNullOrEmpty(download)||!String.IsNullOrEmpty(hash)){
   if(!Uri.TryCreate(download,UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.Host!="github.com"||!uri.AbsolutePath.StartsWith("/isilria/temporary-worker-pay-calculator/releases/download/",StringComparison.Ordinal)||!uri.AbsolutePath.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||!String.IsNullOrEmpty(uri.Query)||!String.IsNullOrEmpty(uri.Fragment)||!String.IsNullOrEmpty(uri.UserInfo)||!System.Text.RegularExpressions.Regex.IsMatch(hash??"","\\A[0-9a-fA-F]{64}\\z"))throw new InvalidDataException("배포 파일 주소 또는 검증값이 올바르지 않습니다.");
  }
  return new UpdateInfo{Version=Normalize(parsed),Url=url,Notes=notes??"",DownloadUrl=download,Sha256=hash};
 }
 static HttpWebRequest Request(string url){Uri uri;if(!Uri.TryCreate(url,UriKind.Absolute,out uri)||uri.Scheme!="https")throw new InvalidDataException("업데이트 주소는 HTTPS여야 합니다.");ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var request=(HttpWebRequest)WebRequest.Create(uri);request.Timeout=15000;request.ReadWriteTimeout=15000;request.UserAgent="ShortPay/"+Assembly.GetExecutingAssembly().GetName().Version;request.CachePolicy=new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);return request;}
 internal static UpdateInfo Fetch(string url){using(var response=Request(url).GetResponse()){if(response.ResponseUri.Scheme!="https")throw new InvalidDataException("안전하지 않은 업데이트 응답입니다.");using(var reader=new StreamReader(response.GetResponseStream())){var buffer=new char[65537];int total=0,n;while(total<buffer.Length&&(n=reader.Read(buffer,total,buffer.Length-total))>0)total+=n;if(total>65536)throw new InvalidDataException("업데이트 정보가 너무 큽니다.");return Parse(new string(buffer,0,total));}}}
 internal static void Verify(string file,string expected){using(var sha=SHA256.Create())using(var stream=File.OpenRead(file)){string actual=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");if(!String.Equals(actual,expected,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("파일 검증에 실패했습니다. 다시 다운로드하세요.");}}
 internal static string Download(UpdateInfo info){
  if(String.IsNullOrEmpty(info.DownloadUrl)||String.IsNullOrEmpty(info.Sha256))throw new InvalidDataException("직접 다운로드 정보가 없습니다.");
  string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ShortPay","Updates",info.Version+"_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  string file=Path.Combine(folder,"대체근로자_임금계산기_"+info.Version.ToString(2)+".exe"),temp=file+".part";
  try{using(var response=Request(info.DownloadUrl).GetResponse()){if(response.ResponseUri.Scheme!="https")throw new InvalidDataException("안전하지 않은 다운로드 응답입니다.");using(var input=response.GetResponseStream())using(var output=File.Create(temp)){byte[] buffer=new byte[81920];long total=0;int n;while((n=input.Read(buffer,0,buffer.Length))>0){total+=n;if(total>100*1024*1024)throw new InvalidDataException("배포 파일 크기가 제한을 초과했습니다.");output.Write(buffer,0,n);}}}Verify(temp,info.Sha256);using(var reader=new BinaryReader(File.OpenRead(temp))){if(reader.ReadUInt16()!=0x5A4D)throw new InvalidDataException("Windows 실행 파일이 아닙니다.");}File.Move(temp,file);return file;}
  finally{if(File.Exists(temp))File.Delete(temp);}
 }
}
public partial class MainForm {
 CheckBox automaticUpdates;Label updateStatus;SoftButton updateButton;bool updateBusy;
 void BuildUpdateSettings(Control page){var c=Section(page,"업데이트 확인",94);var version=L("현재 버전  "+Assembly.GetExecutingAssembly().GetName().Version.ToString(2),10,true);version.SetBounds(20,32,260,26);c.Controls.Add(version);automaticUpdates=new CheckBox{Text="시작 시 업데이트 확인",Checked=true,AutoSize=true,BackColor=System.Drawing.Color.Transparent};automaticUpdates.SetBounds(318,32,260,28);c.Controls.Add(automaticUpdates);updateButton=B("지금 확인",()=>CheckUpdates(true),150);updateButton.SetBounds(744,28,150,36);updateButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;c.Controls.Add(updateButton);updateStatus=L("업데이트 확인 전",9);updateStatus.SetBounds(20,63,870,25);updateStatus.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;c.Controls.Add(updateStatus);}
 async void CheckUpdates(bool interactive){
  if(updateBusy)return;updateBusy=true;updateButton.Enabled=false;updateStatus.Text="새 버전을 확인하고 있습니다…";
  try{string source=PayrollUpdates.Source();var info=await Task.Run(()=>PayrollUpdates.Fetch(source));if(IsDisposed)return;
   if(!PayrollUpdates.IsNewer(info.Version,Assembly.GetExecutingAssembly().GetName().Version)){updateStatus.Text="최신 버전입니다. · "+DateTime.Now.ToString("yyyy-MM-dd HH:mm");return;}
   updateStatus.Text="새 버전 "+info.Version.ToString(2)+"이 있습니다.";
   string action=String.IsNullOrEmpty(info.DownloadUrl)?"다운로드 페이지를 열까요?":"새 실행 파일을 다운로드할까요? 현재 작업은 유지됩니다.";
   if(MessageBox.Show(this,"새 버전 "+info.Version.ToString(2)+"이 있습니다.\n"+info.Notes+"\n\n"+action,"업데이트 확인",MessageBoxButtons.YesNo,MessageBoxIcon.Information)!=DialogResult.Yes)return;
   if(String.IsNullOrEmpty(info.DownloadUrl)){System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(info.Url){UseShellExecute=true});return;}
   updateStatus.Text="업데이트 다운로드 및 파일 확인 중…";string file=await Task.Run(()=>PayrollUpdates.Download(info));if(IsDisposed)return;
   updateStatus.Text="새 버전 다운로드 완료 · 파일 검증 성공";
   MessageBox.Show(this,"새 실행 파일을 다운로드하고 검증했습니다.\n\n현재 작업을 저장하고 프로그램을 종료한 뒤, 열리는 폴더의 새 파일을 바탕화면에 옮겨 실행하세요.\n기존 설정과 저장자료는 계속 사용할 수 있습니다.\n\n"+file,"업데이트 준비 완료",MessageBoxButtons.OK,MessageBoxIcon.Information);
   System.Diagnostics.Process.Start("explorer.exe","/select,\""+file+"\"");
  }catch(Exception){if(!IsDisposed){updateStatus.Text="업데이트 확인 또는 다운로드 실패 · 잠시 후 다시 확인하세요.";if(interactive)MessageBox.Show(this,"업데이트를 완료하지 못했습니다. 인터넷 연결을 확인하고 다시 시도하세요. 현재 프로그램은 계속 사용할 수 있습니다.","업데이트 확인",MessageBoxButtons.OK,MessageBoxIcon.Information);}}
  finally{updateBusy=false;if(!IsDisposed)updateButton.Enabled=true;}
 }
}
}
