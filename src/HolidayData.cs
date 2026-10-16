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

namespace ShortPay {
public class HolidayDate { public string date,name; }
public class HolidayYear { public int year; public List<HolidayDate> holidays; }
public class HolidayDocument { public int schemaVersion; public string sourceUrl,fetchedAtUtc,contentVersion; public List<HolidayYear> years; }
public class HolidayChange {
 public string Date,Before,After;
 public override string ToString(){DateTime day=DateTime.ParseExact(Date,"yyyy-MM-dd",CultureInfo.InvariantCulture);return day.ToString("yyyy년 M월 d일")+"  "+(Before==null?After+" (추가)":After==null?Before+" (삭제)":Before+" → "+After+" (이름 변경)");}
}
class HolidayCheckState { public string checkedAtUtc;public List<string> lastChanges=new List<string>(); }
static class Holidays {
 internal const string SourceUrl="https://www.data.go.kr/data/15012690/openapi.do";
 internal const string DownloadUrl="https://raw.githubusercontent.com/isilria/temporary-worker-pay-calculator/main/data/holidays.json";
 internal static string StorageOverride;
 static HolidayDocument current;
 static HolidayCheckState state=new HolidayCheckState();
 static Dictionary<string,string> names;
 internal static string Origin="내장 자료",LoadWarning="";
 internal static string Folder{get{return StorageOverride??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ShortPay","Holidays");}}
 internal static string CacheFile{get{return Path.Combine(Folder,"holidays.json");}}
 internal static HolidayDocument Current{get{Ensure();return current;}}
 internal static string LastCheck{get{Ensure();return state.checkedAtUtc;}}
 internal static List<string> LastChanges{get{Ensure();return state.lastChanges??new List<string>();}}
 internal static DateTime SourceTime(HolidayDocument data){return DateTime.Parse(data.fetchedAtUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToUniversalTime();}
 internal static string Fingerprint(IEnumerable<HolidayYear> years){var text=new StringBuilder();foreach(var year in years.OrderBy(y=>y.year)){text.Append(year.year.ToString(CultureInfo.InvariantCulture)).Append('\n');foreach(var day in year.holidays.OrderBy(d=>d.date,StringComparer.Ordinal))text.Append(day.date).Append('|').Append(day.name).Append('\n');}using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","").ToLowerInvariant();}
 internal static HolidayDocument Parse(string text){
  if(String.IsNullOrWhiteSpace(text)||text.Length>524288)throw new InvalidDataException("공휴일 자료 크기가 올바르지 않습니다.");
  var data=Engine.Parse<HolidayDocument>(text);DateTime fetched;
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
 internal static void Ensure(){if(current!=null)return;
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("holidays.json"))using(var reader=new StreamReader(stream,Encoding.UTF8))Activate(Parse(reader.ReadToEnd()));
  Origin="내장 자료";LoadWarning="";state=new HolidayCheckState();
  if(File.Exists(CacheFile)){try{var stored=Parse(File.ReadAllText(CacheFile,Encoding.UTF8));if(SourceTime(stored)>=SourceTime(current)){Activate(stored);Origin="마지막 저장 자료";}}catch{LoadWarning="저장 자료 오류로 내장 자료를 사용합니다.";}}
  try{string path=Path.Combine(Folder,"check-state.json");if(File.Exists(path)){var stored=Engine.Parse<HolidayCheckState>(File.ReadAllText(path));DateTime checkedAt;if(stored!=null&&DateTime.TryParse(stored.checkedAtUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out checkedAt)&&stored.lastChanges!=null&&stored.lastChanges.Count<=1550&&stored.lastChanges.All(c=>c!=null&&c.Length<=400))state=stored;}}catch{}
 }
 internal static void ResetForTest(string folder){StorageOverride=folder;current=null;names=null;state=new HolidayCheckState();Ensure();}
 public static string Name(DateTime date){Ensure();string value;return names.TryGetValue(Engine.Key(date),out value)?value:"";}
 internal static bool Covers(int year){return Current.years.Any(y=>y.year==year);}
 internal static bool Covers(Record record){return Enumerable.Range(record.Start.Year,Math.Max(1,record.End.Year-record.Start.Year+1)).All(Covers);}
 internal static Dictionary<string,string> Snapshot(Record record){Ensure();string start=Engine.Key(record.Start),end=Engine.Key(record.End);return names.Where(p=>String.CompareOrdinal(p.Key,start)>=0&&String.CompareOrdinal(p.Key,end)<=0).ToDictionary(p=>p.Key,p=>p.Value);}
 internal static void Stamp(Record record){record.HolidayDataVersion=Current.contentVersion;record.HolidayDataFetchedAt=Current.fetchedAtUtc;record.HolidaySnapshot=Snapshot(record);}
 internal static List<HolidayChange> Differences(Dictionary<string,string> before,Dictionary<string,string> after){return before.Keys.Union(after.Keys).OrderBy(d=>d,StringComparer.Ordinal).Select(date=>new HolidayChange{Date=date,Before=before.ContainsKey(date)?before[date]:null,After=after.ContainsKey(date)?after[date]:null}).Where(c=>c.Before!=c.After).ToList();}
 internal static List<HolidayChange> Differences(HolidayDocument before,HolidayDocument after){return Differences(Map(before),Map(after));}
 internal static string ReconcileRecord(Record record,Dictionary<string,string> before){
  var after=Snapshot(record);var changes=Differences(before??new Dictionary<string,string>(),after);
  if(record.ScheduleMode==1&&record.ComparisonPeriod==WorkPlan.Period(record))foreach(var change in changes.Where(c=>(c.Before==null)!=(c.After==null))){DateTime date=DateTime.ParseExact(change.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture);if(change.After!=null)record.ComparisonDays.Remove(change.Date);else if(WorkPlan.Includes(record.NormalWeekdays,date))record.ComparisonDays[change.Date]=1;}
  Stamp(record);
  return changes.Count==0?"":String.Join("\n",changes.Select(c=>c.ToString()))+"\n\n계약일과 출근·결근 입력은 유지했습니다. 통상 비교일은 변경된 휴일을 반영했습니다. 변경된 날짜의 실제 근무·유급휴일 상태를 확인하세요.";
 }
 internal static string ReconcileLoaded(Record record){
  if(record.HolidaySnapshot==null||String.IsNullOrEmpty(record.HolidayDataVersion)){
   var legacyDates="01-01 02-16 02-17 02-18 03-01 03-02 05-01 05-05 05-24 05-25 06-03 06-06 07-17 08-15 08-17 09-24 09-25 09-26 10-03 10-05 10-09 12-25".Split(' ').Select(d=>"2026-"+d);
   string start=Engine.Key(record.Start),end=Engine.Key(record.End);var legacy=legacyDates.Where(d=>String.CompareOrdinal(d,start)>=0&&String.CompareOrdinal(d,end)<=0).ToDictionary(d=>d,d=>Name(DateTime.ParseExact(d,"yyyy-MM-dd",CultureInfo.InvariantCulture))==""?"이전 공휴일":Name(DateTime.ParseExact(d,"yyyy-MM-dd",CultureInfo.InvariantCulture)));
   string changed=ReconcileRecord(record,legacy);return "이전 파일에 공휴일 자료 버전이 없어 현재 공휴일 자료로 재계산했습니다. 출근상태와 통상 비교일을 확인하세요."+(changed==""?"":"\n\n"+changed);
  }
  if(record.HolidaySnapshot.Count>367||record.HolidaySnapshot.Any(p=>!Regex.IsMatch(p.Key,"\\A[0-9]{4}-[0-9]{2}-[0-9]{2}\\z")||String.IsNullOrWhiteSpace(p.Value)||p.Value.Length>100||p.Value.Any(Char.IsControl)))throw new InvalidDataException("저장된 공휴일 기준 자료가 올바르지 않습니다.");
  string old=record.HolidayDataVersion;string message=ReconcileRecord(record,record.HolidaySnapshot);
  return message!=""?"저장 당시와 공휴일 일정이 달라졌습니다.\n\n"+message:old==Current.contentVersion?"":"저장 이후 공휴일 자료가 갱신됐습니다. 이 계약기간의 공휴일 일정은 동일합니다.";
 }
 internal static void AtomicWrite(string path,string text){Directory.CreateDirectory(Path.GetDirectoryName(path));string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temporary,text,new UTF8Encoding(false));if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}}
 internal static List<HolidayChange> Accept(string text){
  var candidate=Parse(text);Ensure();
  if(SourceTime(candidate)<SourceTime(current))throw new InvalidDataException("현재보다 오래된 공휴일 자료입니다.");
  if(current.years.Any(y=>!candidate.years.Any(c=>c.year==y.year)))throw new InvalidDataException("기존 연도의 공휴일 자료가 누락됐습니다.");
  var changes=Differences(current,candidate);
  AtomicWrite(CacheFile,text);Activate(candidate);Origin="마지막 저장 자료";LoadWarning="";
  state.checkedAtUtc=DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");if(changes.Count>0)state.lastChanges=changes.Select(c=>c.ToString()).ToList();
  try{AtomicWrite(Path.Combine(Folder,"check-state.json"),Engine.Json(state));}catch{}
  return changes;
 }
 internal static string Fetch(){return FetchFrom(DownloadUrl);}
 internal static string FetchFrom(string source){
  Uri uri;if(!Uri.TryCreate(source,UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.Host!="raw.githubusercontent.com"||!uri.AbsolutePath.StartsWith("/isilria/temporary-worker-pay-calculator/",StringComparison.Ordinal)||!String.IsNullOrEmpty(uri.Query)||!String.IsNullOrEmpty(uri.Fragment)||!String.IsNullOrEmpty(uri.UserInfo))throw new InvalidDataException("공휴일 자료 주소가 올바르지 않습니다.");
  ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var request=(HttpWebRequest)WebRequest.Create(uri);request.AllowAutoRedirect=false;request.Timeout=12000;request.ReadWriteTimeout=12000;request.UserAgent="ShortPay-Holiday/1.5";request.CachePolicy=new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
  using(var response=(HttpWebResponse)request.GetResponse()){if(response.StatusCode!=HttpStatusCode.OK)throw new IOException("공휴일 자료를 받을 수 없습니다.");using(var reader=new StreamReader(response.GetResponseStream(),Encoding.UTF8)){var buffer=new char[524289];int count=0,n;while(count<buffer.Length&&(n=reader.Read(buffer,count,buffer.Length-count))>0)count+=n;if(count>524288)throw new InvalidDataException("공휴일 자료 크기가 제한을 초과했습니다.");string text=new string(buffer,0,count);Parse(text);return text;}}
 }
 internal static string Status(){return String.Join("·",Current.years.OrderBy(y=>y.year).Select(y=>y.year.ToString()))+"년 · "+Origin+" · 원천 확인 "+SourceTime(Current).ToLocalTime().ToString("MM/dd HH:mm")+(String.IsNullOrEmpty(LastCheck)?"":" · PC 확인 "+DateTime.Parse(LastCheck,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToLocalTime().ToString("MM/dd HH:mm"));}
 internal static string ListText(){return "출처: 한국천문연구원 특일 정보\r\n"+SourceUrl+"\r\n원천 자료 확인: "+SourceTime(Current).ToLocalTime().ToString("yyyy-MM-dd HH:mm")+"\r\n자료 버전: "+Current.contentVersion.Substring(0,12)+"\r\n\r\n"+String.Join("\r\n",Current.years.OrderBy(y=>y.year).SelectMany(y=>new[]{"["+y.year+"년]"}.Concat(y.holidays.OrderBy(h=>h.date).Select(h=>h.date+"  "+h.name)).Concat(new[]{""})))+"\r\n[마지막 변경 내역]\r\n"+(LastChanges.Count==0?"저장된 변경 내역이 없습니다.":String.Join("\r\n",LastChanges))+"\r\n\r\n공휴일 날짜와 임금 지급요건은 별도 기준입니다. 단가와 보수 기준은 기존 2026년 자료를 확인하세요.";}
}
public partial class MainForm {
 Label holidayStatus,holidayVersion;SoftButton holidayUpdateButton;bool holidayUpdateBusy;
 internal Action<string> HolidayAlert;
 internal Func<string> HolidayDownload=Holidays.Fetch;
 void HolidayMessage(string title,string message){if(HolidayAlert!=null){HolidayAlert(message);return;}if(OffscreenTest||!Visible)return;using(var dialog=new Form{Text=title,Font=Font,StartPosition=FormStartPosition.CenterParent,Size=new System.Drawing.Size(740,520),MinimumSize=new System.Drawing.Size(560,360),ShowInTaskbar=false}){
  var close=new SoftButton{Text="확인",Dock=DockStyle.Bottom,Height=42,DialogResult=DialogResult.OK};dialog.Controls.Add(close);var text=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,BackColor=System.Drawing.Color.White,Text=message.Replace("\r\n","\n").Replace("\n","\r\n")};dialog.Controls.Add(text);dialog.AcceptButton=close;dialog.CancelButton=close;dialog.ShowDialog(this);
 }}
 void BuildHolidaySettings(Control card){
  holidayVersion=L("공휴일 자료",9,true);holidayVersion.SetBounds(20,89,550,27);card.Controls.Add(holidayVersion);
  var list=new LinkLabel{Text="일정·변경 내역",AutoSize=false,BackColor=System.Drawing.Color.Transparent};list.SetBounds(592,95,140,25);list.Anchor=AnchorStyles.Top|AnchorStyles.Right;list.LinkClicked+=(s,e)=>HolidayMessage("공휴일 일정과 변경 내역",Holidays.ListText());card.Controls.Add(list);
  holidayUpdateButton=B("공휴일 확인",CheckHolidayManually,150);holidayUpdateButton.SetBounds(744,89,150,34);holidayUpdateButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;card.Controls.Add(holidayUpdateButton);
  holidayStatus=L("",8);holidayStatus.SetBounds(20,123,880,23);holidayStatus.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;card.Controls.Add(holidayStatus);UpdateHolidayStatus();
 }
 void UpdateHolidayStatus(){if(holidayStatus==null)return;holidayVersion.Text="공휴일 자료  "+Holidays.Current.contentVersion.Substring(0,12);holidayStatus.Text=Holidays.Status()+(Holidays.Covers(r)?"":" · 계약연도 자료 미확보")+(Holidays.LoadWarning==""?"":" · 내장 자료 복구");}
 async void CheckStartupUpdates(){await CheckHolidayUpdates(false);if(!IsDisposed&&automaticUpdates.Checked)CheckUpdates(false);}
 async void CheckHolidayManually(){await CheckHolidayUpdates(true);}
 internal async Task CheckHolidayUpdates(bool interactive){
  if(holidayUpdateBusy)return;holidayUpdateBusy=true;holidayUpdateButton.Enabled=false;holidayStatus.Text="공식 공휴일 자료의 변경 여부를 확인하고 있습니다…";
  try{string text=await Task.Run(HolidayDownload);if(IsDisposed)return;var oldSnapshot=Holidays.Snapshot(r);decimal before=result==null?0:result.Gross;var changes=Holidays.Accept(text);string recordNote=Holidays.ReconcileRecord(r,oldSnapshot);if(changes.Count>0){RefreshAll();dirty=true;}UpdateHolidayStatus();
   if(changes.Count>0){string message="연간 공휴일 일정이 변경되었습니다.\n\n"+String.Join("\n",changes.Select(c=>c.ToString()))+"\n\n최신 자료를 이 PC에 저장했습니다. 인터넷 연결이 없어도 이 자료를 사용합니다.";if(recordNote!="")message+="\n\n[현재 계약에 해당하는 변경]\n"+recordNote+"\n임금총액: "+before.ToString("N0")+"원 → "+result.Gross.ToString("N0")+"원";HolidayMessage("공휴일 일정 변경 안내",message);}
   else if(interactive)HolidayMessage("공휴일 확인","현재 공휴일 일정이 최신 자료와 같습니다.\n\n"+Holidays.Status());
  }catch(Exception){if(!IsDisposed){UpdateHolidayStatus();holidayStatus.Text="온라인 확인 실패 · "+Holidays.Status()+" 사용";if(interactive)HolidayMessage("공휴일 확인","온라인 확인을 완료하지 못했습니다.\n마지막 정상 저장 자료를 사용하며, 저장 자료가 없으면 프로그램 내장 자료를 사용합니다.\n\n"+Holidays.Status());}}
  finally{holidayUpdateBusy=false;if(!IsDisposed)holidayUpdateButton.Enabled=true;}
 }
}
}
