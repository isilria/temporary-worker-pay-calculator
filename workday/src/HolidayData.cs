using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SchoolWorkdayCalculator {
public class HolidayDate { public string date,name; }
public class HolidayYear { public int year; public List<HolidayDate> holidays; }
public class HolidayDocument { public int schemaVersion; public string sourceUrl,fetchedAtUtc,contentVersion; public List<HolidayYear> years; }
public class HolidayChange {
 public string Date,Before,After;
 public override string ToString(){DateTime day=DateTime.ParseExact(Date,"yyyy-MM-dd",CultureInfo.InvariantCulture);return day.ToString("yyyy년 M월 d일")+"  "+(Before==null?After+" (추가)":After==null?Before+" (삭제)":Before+" → "+After+" (이름 변경)");}
}
class HolidayCheckState { public string checkedAtUtc;public List<string> lastChanges=new List<string>(); }
static class Holidays {
 internal const int EarliestSchoolYear=2021;
 const int LastArchivedYear=2025;
 internal const string SourceUrl="https://www.data.go.kr/data/15012690/openapi.do";
 internal const string DownloadUrl="https://raw.githubusercontent.com/isilria/temporary-worker-pay-calculator/refs/heads/main/data/holidays.json";
 internal static string StorageOverride;
 static HolidayDocument current;
 static HolidayCheckState state=new HolidayCheckState();
 static Dictionary<string,string> names;
 internal static string Origin="내장 자료",LoadWarning="";
 internal static string Folder{get{return StorageOverride??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SchoolWorkday320","Holidays");}}
 internal static string CacheFile{get{return Path.Combine(Folder,"holidays.json");}}
 internal static HolidayDocument Current{get{Ensure();return current;}}
 internal static string LastCheck{get{Ensure();return state.checkedAtUtc;}}
 internal static List<string> LastChanges{get{Ensure();return state.lastChanges??new List<string>();}}
 internal static DateTime SourceTime(HolidayDocument data){return DateTime.Parse(data.fetchedAtUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToUniversalTime();}
 internal static string Fingerprint(IEnumerable<HolidayYear> years){var text=new StringBuilder();foreach(var year in years.OrderBy(y=>y.year)){text.Append(year.year.ToString(CultureInfo.InvariantCulture)).Append('\n');foreach(var day in year.holidays.OrderBy(d=>d.date,StringComparer.Ordinal))text.Append(day.date).Append('|').Append(day.name).Append('\n');}using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","").ToLowerInvariant();}
 internal static HolidayDocument Parse(string text){
  if(String.IsNullOrWhiteSpace(text)||text.Length>524288)throw new InvalidDataException("공휴일 자료 크기가 올바르지 않습니다.");
  var data=HolidayJson.Parse<HolidayDocument>(text);DateTime fetched;
  if(data==null||data.schemaVersion!=1||data.sourceUrl!=SourceUrl||data.years==null||data.years.Count<1||data.years.Count>31||!Regex.IsMatch(data.contentVersion??"","\\A[0-9a-f]{64}\\z")||!DateTime.TryParse(data.fetchedAtUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out fetched)||!data.fetchedAtUtc.EndsWith("Z")||fetched.ToUniversalTime()>DateTime.UtcNow.AddMinutes(10)||fetched.Year<2025)throw new InvalidDataException("공휴일 자료의 출처·버전·확인 시각이 올바르지 않습니다.");
  var years=new HashSet<int>();foreach(var year in data.years){
   if(year==null||!years.Add(year.year)||year.year<2020||year.year>2050||year.holidays==null||year.holidays.Count<10||year.holidays.Count>50)throw new InvalidDataException("연간 공휴일 자료가 비어 있거나 불완전합니다.");
   var dates=new HashSet<string>();foreach(var day in year.holidays){DateTime value;if(day==null||!DateTime.TryParseExact(day.date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out value)||value.Year!=year.year||!dates.Add(day.date)||String.IsNullOrWhiteSpace(day.name)||day.name.Length>100||day.name.Any(c=>Char.IsControl(c)||c=='|'))throw new InvalidDataException("공휴일 날짜·이름이 올바르지 않습니다.");}
   if(!dates.Contains(year.year+"-01-01")||!dates.Contains(year.year+"-12-25"))throw new InvalidDataException("연간 공휴일 자료의 필수 날짜가 누락됐습니다.");
  }
  if(data.contentVersion!=Fingerprint(data.years))throw new InvalidDataException("공휴일 자료의 검증값이 일치하지 않습니다.");return data;
 }
 static Dictionary<string,string> Map(HolidayDocument data){return data.years.SelectMany(y=>y.holidays).ToDictionary(d=>d.date,d=>d.name,StringComparer.Ordinal);}
 static void Activate(HolidayDocument data){current=data;names=Map(data);}
 static bool RestoreArchivedYears(HolidayDocument data,HolidayDocument fallback){
  var missing=fallback.years.Where(y=>y.year<=LastArchivedYear&&!data.years.Any(existing=>existing.year==y.year)).ToList();
  if(missing.Count==0)return false;
  data.years.AddRange(missing);
  data.years.Sort((a,b)=>a.year.CompareTo(b.year));
  data.contentVersion=Fingerprint(data.years);
  return true;
 }
 internal static void Ensure(){if(current!=null)return;
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("holidays.json"))using(var reader=new StreamReader(stream,Encoding.UTF8))Activate(Parse(reader.ReadToEnd()));
  Origin="내장 자료";LoadWarning="";state=new HolidayCheckState();
  if(File.Exists(CacheFile)){try{var stored=Parse(File.ReadAllText(CacheFile,Encoding.UTF8));if(SourceTime(stored)>=SourceTime(current)){RestoreArchivedYears(stored,current);Activate(stored);Origin="마지막 저장 자료";}}catch{LoadWarning="저장 자료 오류로 내장 자료를 사용합니다.";}}
  try{string path=Path.Combine(Folder,"check-state.json");if(File.Exists(path)){var stored=HolidayJson.Parse<HolidayCheckState>(File.ReadAllText(path));DateTime checkedAt;if(stored!=null&&DateTime.TryParse(stored.checkedAtUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out checkedAt)&&stored.lastChanges!=null&&stored.lastChanges.Count<=1550&&stored.lastChanges.All(c=>c!=null&&c.Length<=400))state=stored;}}catch{}
 }
 internal static void ResetForTest(string folder){StorageOverride=folder;current=null;names=null;state=new HolidayCheckState();Ensure();}
 public static string Name(DateTime date){Ensure();string value;return names.TryGetValue(date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),out value)?value:"";}
 internal static bool Covers(int year){return Current.years.Any(y=>y.year==year);}
 internal static List<HolidayChange> Differences(Dictionary<string,string> before,Dictionary<string,string> after){return before.Keys.Union(after.Keys).OrderBy(d=>d,StringComparer.Ordinal).Select(date=>new HolidayChange{Date=date,Before=before.ContainsKey(date)?before[date]:null,After=after.ContainsKey(date)?after[date]:null}).Where(c=>c.Before!=c.After).ToList();}
 internal static List<HolidayChange> Differences(HolidayDocument before,HolidayDocument after){return Differences(Map(before),Map(after));}
 internal static void AtomicWrite(string path,string text){Directory.CreateDirectory(Path.GetDirectoryName(path));string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temporary,text,new UTF8Encoding(false));if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}}
 internal static List<HolidayChange> Accept(string text){
  var candidate=Parse(text);Ensure();
  if(RestoreArchivedYears(candidate,current))text=HolidayJson.Json(candidate);
  if(SourceTime(candidate)<SourceTime(current)){
   if(candidate.contentVersion!=current.contentVersion)throw new InvalidDataException("현재보다 오래된 공휴일 자료입니다.");
   candidate=current;text=HolidayJson.Json(current);
  }
  if(current.years.Any(y=>!candidate.years.Any(c=>c.year==y.year)))throw new InvalidDataException("기존 연도의 공휴일 자료가 누락됐습니다.");
  var changes=Differences(current,candidate);
  AtomicWrite(CacheFile,text);Activate(candidate);Origin="마지막 저장 자료";LoadWarning="";
  state.checkedAtUtc=DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");if(changes.Count>0)state.lastChanges=changes.Select(c=>c.ToString()).ToList();
  try{AtomicWrite(Path.Combine(Folder,"check-state.json"),HolidayJson.Json(state));}catch{}
  return changes;
 }
 internal static string Fetch(){return FetchFrom(DownloadUrl);}
 internal static string FetchFrom(string source){
  Uri uri;if(!Uri.TryCreate(source,UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.Host!="raw.githubusercontent.com"||!uri.AbsolutePath.StartsWith("/isilria/temporary-worker-pay-calculator/",StringComparison.Ordinal)||!String.IsNullOrEmpty(uri.Query)||!String.IsNullOrEmpty(uri.Fragment)||!String.IsNullOrEmpty(uri.UserInfo))throw new InvalidDataException("공휴일 자료 주소가 올바르지 않습니다.");
  var fresh=new UriBuilder(uri);fresh.Query="shortpay_check="+DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
  ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var request=(HttpWebRequest)WebRequest.Create(fresh.Uri);request.AllowAutoRedirect=false;request.Timeout=12000;request.ReadWriteTimeout=12000;request.UserAgent="Workday320-Holiday/1.9.2";request.CachePolicy=new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
  using(var response=(HttpWebResponse)request.GetResponse()){if(response.StatusCode!=HttpStatusCode.OK)throw new IOException("공휴일 자료를 받을 수 없습니다.");using(var reader=new StreamReader(response.GetResponseStream(),Encoding.UTF8)){var buffer=new char[524289];int count=0,n;while(count<buffer.Length&&(n=reader.Read(buffer,count,buffer.Length-count))>0)count+=n;if(count>524288)throw new InvalidDataException("공휴일 자료 크기가 제한을 초과했습니다.");string text=new string(buffer,0,count);Parse(text);return text;}}
 }
 internal static string LocalCheckDate(){return String.IsNullOrEmpty(LastCheck)?"확인 전 (내장 자료)":DateTime.Parse(LastCheck,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToLocalTime().ToString("yyyy-MM-dd");}
 internal static string Status(){
  var available=Current.years.Select(y=>y.year).OrderBy(y=>y).ToArray();
  return "공공데이터포털 공휴일 자료 활용 (현재 반영 기간: "+available.First()+"년~"+available.Last()+"년)\r\n최종 데이터 확인일 (공공데이터포털) "+SourceTime(Current).ToLocalTime().ToString("yyyy-MM-dd")+"  (로컬파일) "+LocalCheckDate();
 }
 internal static string RequestStatus(){int currentYear=DateTime.Today.Year;var missing=Enumerable.Range(currentYear,5).Where(y=>!Covers(y)).ToArray();return "조회 대상: "+currentYear+"년~"+(currentYear+4)+"년"+(missing.Length==0?"":" · 미제공: "+String.Join("·",missing.Select(y=>y+"년")));}

 internal static string ListText(){return "출처: 한국천문연구원 특일 정보\r\n"+SourceUrl+"\r\n원천 자료 확인: "+SourceTime(Current).ToLocalTime().ToString("yyyy-MM-dd")+"\r\n자료 버전: "+Current.contentVersion.Substring(0,12)+"\r\n\r\n"+String.Join("\r\n",Current.years.OrderBy(y=>y.year).SelectMany(y=>new[]{"["+y.year+"년]"}.Concat(y.holidays.OrderBy(h=>h.date).Select(h=>h.date+"  "+h.name)).Concat(new[]{""})))+"\r\n[마지막 변경 내역]\r\n"+(LastChanges.Count==0?"저장된 변경 내역이 없습니다.":String.Join("\r\n",LastChanges))+"\r\n\r\n학교회계연도는 해당 연도와 다음 연도 공휴일 자료를 사용합니다. 학교 지정 휴일은 입력값으로 별도 반영합니다.";}
}
static class HolidayJson {
 internal static T Parse<T>(string value){return new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=1048576}.Deserialize<T>(value);}
 internal static string Json(object value){return new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=1048576}.Serialize(value);}
}
internal sealed partial class MainForm {
 Label holidayDataStatus,holidaySourceLabel; Button holidayCheckButton,holidayHistoryButton; bool holidayBusy;
 internal string HolidayCheckWarning="";
 internal Func<string> HolidayDownload=Holidays.Fetch;
 void BuildHolidayPanel(Control parent){
  holidaySourceLabel=FlatDesign.Text("",518,6,250,35,8.5F,false);holidaySourceLabel.Name="HolidaySource";holidaySourceLabel.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;holidaySourceLabel.ForeColor=FlatDesign.Muted;parent.Controls.Add(holidaySourceLabel);
  holidayDataStatus=FlatDesign.Text("",518,42,250,22,8.5F,true);holidayDataStatus.Name="HolidayAvailability";holidayDataStatus.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;parent.Controls.Add(holidayDataStatus);
  holidayCheckButton=ButtonAt("공휴일 확인",782,6,104,26);holidayCheckButton.Name="HolidayCheck";holidayCheckButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;holidayCheckButton.Font=new System.Drawing.Font("맑은 고딕",8F);holidayCheckButton.Click+=async(s,e)=>await CheckHolidayUpdates(true);parent.Controls.Add(holidayCheckButton);
  holidayHistoryButton=ButtonAt("일정 변경 내역",782,37,104,26);holidayHistoryButton.Name="HolidayHistory";holidayHistoryButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;holidayHistoryButton.Font=new System.Drawing.Font("맑은 고딕",8F);holidayHistoryButton.Click+=(s,e)=>ShowHolidayInfo("공휴일 일정 변경 내역",Holidays.Status()+"\r\n"+Holidays.RequestStatus()+"\r\n\r\n"+Holidays.ListText());parent.Controls.Add(holidayHistoryButton);
 }

 void InitializeHolidayUpdates(){RefreshHolidayStatus();Shown+=async(s,e)=>{if(!Offscreen)await CheckHolidayUpdates(false);};}
 void RefreshHolidayStatus(){
  if(holidayDataStatus==null)return;int y=(int)year.Value;var available=Holidays.Current.years.Select(h=>h.year).OrderBy(h=>h).ToArray();
  holidaySourceLabel.Text="공휴일 자료 · "+available.First()+"~"+available.Last()+"년";
  var missing=new[]{y,y+1}.Where(n=>!Holidays.Covers(n)).ToArray();
  holidayDataStatus.Text=missing.Length==0?"작성 가능":"작성불가 · "+String.Join("·",missing.Select(n=>n+"년"))+" 데이터 없음";
  holidayDataStatus.ForeColor=missing.Length==0?FlatDesign.Blue:System.Drawing.Color.FromArgb(192,45,45);
  holidayDataStatus.AccessibleDescription=HolidayCheckWarning;QueuePreview();
 }

 void ShowHolidayInfo(string title,string message){if(Offscreen)return;using(var dialog=new Form{Text=title,Size=new System.Drawing.Size(760,550),StartPosition=FormStartPosition.CenterParent,Font=Font,ShowInTaskbar=false}){var close=new Button{Text="확인",Dock=DockStyle.Bottom,Height=38,DialogResult=DialogResult.OK};dialog.Controls.Add(close);dialog.Controls.Add(new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,Text=message.Replace("\r\n","\n").Replace("\n","\r\n")});dialog.AcceptButton=close;dialog.ShowDialog(this);}}
 internal async System.Threading.Tasks.Task CheckHolidayUpdates(bool interactive){
  if(holidayBusy)return;holidayBusy=true;holidayCheckButton.Enabled=false;createButton.Enabled=false;holidayCheckButton.Text="확인 중…";
  try{string text=await System.Threading.Tasks.Task.Run(HolidayDownload);if(IsDisposed)return;var changes=Holidays.Accept(text);HolidayCheckWarning="";RefreshHolidayStatus();if(changes.Count>0)ShowHolidayInfo("공휴일 일정 변경 안내","연간 공휴일 일정이 변경되었습니다.\r\n\r\n"+String.Join("\r\n",changes.Select(c=>c.ToString()))+"\r\n\r\n최신 자료를 저장했습니다. 새로 생성하는 엑셀부터 반영합니다. 이미 저장한 엑셀은 그대로 유지합니다.");else if(interactive)ShowHolidayInfo("공휴일 확인","현재 공휴일 일정이 최신 자료와 같습니다.\r\n"+Holidays.Status());}
  catch(Exception ex){if(!IsDisposed){HolidayCheckWarning="온라인 확인 실패 · 저장/내장 자료 사용";RefreshHolidayStatus();if(interactive)ShowHolidayInfo("공휴일 확인","온라인 확인을 완료하지 못했습니다. 마지막 정상 저장 자료 또는 내장 자료를 사용합니다.\r\n\r\n"+ex.Message);}}
  finally{holidayBusy=false;if(!IsDisposed){holidayCheckButton.Enabled=true;holidayCheckButton.Text="공휴일 확인";createButton.Enabled=true;}}
 }
}
}
