using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace ShortPay {
static class HolidayDataTests {
 static int count;
 static void Check(bool ok,string label){if(!ok)throw new Exception("Holiday data: "+label);count++;}
 static void Reject(Action action,string label){bool failed=false;try{action();}catch{failed=true;}Check(failed,label);}
 internal static HolidayDocument Copy(HolidayDocument data){return Engine.Parse<HolidayDocument>(Engine.Json(data));}
 internal static string Encode(HolidayDocument data,int seconds=1){data.fetchedAtUtc=Holidays.SourceTime(data).AddSeconds(seconds).ToString("yyyy-MM-ddTHH:mm:ssZ");data.contentVersion=Holidays.Fingerprint(data.years);return Engine.Json(data);}
 internal static void Run(string folder){Directory.CreateDirectory(folder);try{count=0;Holidays.ResetForTest(Path.Combine(folder,"cache"));var initial=Copy(Holidays.Current);var baseline=Engine.Json(initial);
  Check(new[]{2025,2026,2027}.All(y=>initial.years.Any(value=>value.year==y)),"embedded supported years");
  Check(initial.years.All(y=>y.holidays.Count>=10),"complete official initial years");
  Check(Holidays.Name(new DateTime(2027,2,9))=="대체공휴일(설날)","official names and next year");
  Check(Holidays.Name(new DateTime(2027,1,2))=="","ordinary date");
  Check(Holidays.Origin=="내장 자료","first offline launch uses embedded data");
  int unsupported=initial.years.Max(y=>y.year)+1;Check(Holidays.Covers(2025)&&Holidays.Covers(2027)&&!Holidays.Covers(unsupported),"coverage explicit");
  Check(!Engine.Calculate(new Record{Start=new DateTime(unsupported,11,1),End=new DateTime(unsupported,11,7),RateConfirmed=true},new Job{name="일반직종"}).Ready,"missing year cannot finalize");
  Reject(()=>Holidays.Parse(""),"empty data rejected");Reject(()=>Holidays.Parse("{}"),"wrong schema rejected");
  Reject(()=>Holidays.Parse(baseline.Replace("삼일절","임의변경")),"modified content hash rejected");
  Reject(()=>Holidays.Parse(baseline.Replace(Holidays.SourceUrl,"https://example.com")),"wrong source rejected");
  var invalid=Copy(initial);invalid.years[0].holidays.Clear();Reject(()=>Holidays.Parse(Encode(invalid)),"empty year rejected");
  invalid=Copy(initial);invalid.years[0].holidays.RemoveAll(d=>d.date.EndsWith("-01-01"));Reject(()=>Holidays.Parse(Encode(invalid)),"partial year rejected");
  invalid=Copy(initial);invalid.years[0].holidays[1].date=invalid.years[0].holidays[0].date;Reject(()=>Holidays.Parse(Encode(invalid)),"duplicate dates rejected");
  invalid=Copy(initial);invalid.years[0].holidays[0].date="2025-02-30";Reject(()=>Holidays.Parse(Encode(invalid)),"impossible date rejected");
  invalid=Copy(initial);invalid.years[0].holidays[0].name="bad\nname";Reject(()=>Holidays.Parse(Encode(invalid)),"control characters rejected");
  invalid=Copy(initial);invalid.years.Add(invalid.years[0]);Reject(()=>Holidays.Parse(Encode(invalid)),"duplicate year rejected");
  invalid=Copy(initial);invalid.fetchedAtUtc="2099-01-01T00:00:00Z";Reject(()=>Holidays.Parse(Engine.Json(invalid)),"future source time rejected");
  var updated=Copy(initial);updated.years.Single(y=>y.year==2026).holidays.Add(new HolidayDate{date="2026-11-03",name="검증용 임시공휴일"});string added=Encode(updated);
  var differences=Holidays.Accept(added);Check(differences.Count==1&&differences[0].Before==null,"added holiday identified");
  Check(Holidays.Name(new DateTime(2026,11,3))=="검증용 임시공휴일","new date applied");
  Check(File.Exists(Holidays.CacheFile),"validated file persisted");
  string cached=File.ReadAllText(Holidays.CacheFile);
  Reject(()=>Holidays.Accept("not-json"),"malformed response rejected");Check(File.ReadAllText(Holidays.CacheFile)==cached,"bad response preserves last file");
  Reject(()=>Holidays.Accept(baseline),"older source cannot overwrite");Check(Holidays.Name(new DateTime(2026,11,3))!="","older source preserves active data");
  invalid=Copy(updated);invalid.years.RemoveAt(0);Reject(()=>Holidays.Accept(Encode(invalid)),"missing existing year rejected");
  Holidays.ResetForTest(Path.Combine(folder,"cache"));Check(Holidays.Origin=="마지막 저장 자료"&&Holidays.Name(new DateTime(2026,11,3))!="","offline restart restores latest data");
  Check(Holidays.LastChanges.Count==1,"change history survives restart");
  Check(Holidays.Accept(added).Count==0,"same content does not notify again");
  var newerSame=Copy(Holidays.Current);Holidays.Accept(Encode(newerSame,1));string newerTime=Holidays.Current.fetchedAtUtc;
  Check(Holidays.Accept(added).Count==0,"older timestamp with identical content remains a successful check");
  Check(Holidays.Current.fetchedAtUtc==newerTime,"identical older response cannot downgrade source check time");
  var renamed=Copy(updated);renamed.years.Single(y=>y.year==2026).holidays.Single(d=>d.date=="2026-11-03").name="검증용 명칭 변경";string renamedText=Encode(renamed,2);differences=Holidays.Accept(renamedText);Check(differences.Count==1&&differences[0].Before!=null&&differences[0].After!=null&&differences[0].ToString().Contains("이름 변경"),"name-only change identified");
  var removed=Copy(renamed);removed.years.Single(y=>y.year==2026).holidays.RemoveAll(d=>d.date=="2026-11-03");differences=Holidays.Accept(Encode(removed,3));Check(differences.Count==1&&differences[0].After==null,"removed holiday identified");
  var original=HolidayTests.Example();WorkPlan.FromLegacy(original);Holidays.Stamp(original);var days=Engine.Json(original.Days);string oldVersion=original.HolidayDataVersion;
  var change2026=Copy(Holidays.Current);change2026.years.Single(y=>y.year==2026).holidays.RemoveAll(d=>d.date=="2026-09-24");Holidays.Accept(Encode(change2026,1));string note=Holidays.ReconcileLoaded(original);
  Check(note.Contains("2026년 9월 24일")&&note.Contains("삭제"),"saved record describes changed date");
  Check(Engine.Json(original.Days)==days,"attendance and user-entered paid days preserved");
  Check(original.ComparisonDays.ContainsKey("2026-09-24"),"comparison updates removed holiday");
  Check(original.HolidayDataVersion!=oldVersion,"saved data version tracked");
  var jobs=Engine.Parse<List<Job>>(ReadResource("jobs.json"));Check(!Engine.Calculate(original,jobs[original.JobIndex]).Ready,"removed paid holiday requires attendance correction");
  string saved=Path.Combine(folder,"record.stpay");Engine.Save(saved,original);var restored=Engine.Load(saved);Check(restored.HolidayDataVersion==original.HolidayDataVersion&&restored.HolidaySnapshot.Count==original.HolidaySnapshot.Count,"holiday basis roundtrip in saved record");
  Holidays.ResetForTest(Path.Combine(folder,"fresh"));original=HolidayTests.Example();WorkPlan.FromLegacy(original);original.Name="검증용대상자";Holidays.Stamp(original);string workbook=Path.Combine(folder,"record.xlsx");ExcelReports.Save(workbook,"임금산정표",original,jobs[original.JobIndex],Engine.Calculate(original,jobs[original.JobIndex]),new ExportOptions());var archived=WageWorkbook.Load(workbook,jobs);Check(archived.Record.HolidayDataVersion==Holidays.Current.contentVersion,"Excel working archive includes holiday version");
  string badFolder=Path.Combine(folder,"bad-cache");Directory.CreateDirectory(badFolder);File.WriteAllText(Path.Combine(badFolder,"holidays.json"),"broken");Holidays.ResetForTest(badFolder);Check(Holidays.Covers(2026)&&Holidays.Origin=="내장 자료"&&Holidays.LoadWarning!="","corrupt cache recovers embedded data");
  File.WriteAllText(Path.Combine(badFolder,"check-state.json"),"{\"checkedAtUtc\":\"bad date\"}");Holidays.ResetForTest(badFolder);Check(Holidays.Status().Contains("내장 자료"),"bad check metadata cannot crash startup");
  Holidays.ResetForTest(Path.Combine(folder,"ui-cache"));FlatUiTests.PrepareContext(folder);using(var form=new MainForm()){form.CheckHolidayDataUI(folder,ref count);}
  File.WriteAllText(Path.Combine(folder,"holiday-data-tests.txt"),"PASS "+count+" holiday data checks: official initial data, validation, atomic persistence, offline fallback, diff notifications, saved files, calculation impact, UI and network failure.");
 }catch(Exception error){File.WriteAllText(Path.Combine(folder,"holiday-data-tests.txt"),error.ToString());Environment.ExitCode=1;}}
 static string ReadResource(string name){using(var reader=new StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name)))return reader.ReadToEnd();}
 internal static void Live(string folder,bool candidate=false){Directory.CreateDirectory(folder);try{Holidays.ResetForTest(Path.Combine(folder,"live-cache"));string source=candidate?"https://raw.githubusercontent.com/isilria/temporary-worker-pay-calculator/codex/holiday-data-update/data/holidays.json":Holidays.DownloadUrl;string text=Holidays.FetchFrom(source);var changes=Holidays.Accept(text);Holidays.ResetForTest(Path.Combine(folder,"live-cache"));if(!Holidays.Covers(2027)||Holidays.Origin!="마지막 저장 자료")throw new Exception("Live downloaded cache did not reopen");if(!candidate){FlatUiTests.PrepareContext(folder);using(var form=new MainForm())form.CheckLiveHolidayUI(folder);}File.WriteAllText(Path.Combine(folder,"holiday-live-tests.txt"),"PASS public HTTPS file download, schema/hash validation, local atomic save, offline restart and official holiday names"+(candidate?"":"; production manual-check UI succeeded")+"\n"+Holidays.Status()+"\nChanges "+changes.Count+"\nSource "+source+"\nProduction publication "+(candidate?"pending approval":"verified"));}catch(Exception error){File.WriteAllText(Path.Combine(folder,"holiday-live-tests.txt"),error.ToString());Environment.ExitCode=1;}}
}
public partial class MainForm {
 internal void CheckLiveHolidayUI(string folder){
  OffscreenTest=true;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Location=new Point(SystemInformation.VirtualScreen.Right+200,SystemInformation.VirtualScreen.Top);HolidayAlert=message=>{};Show();Application.DoEvents();
  var task=CheckHolidayUpdates(true);while(!task.IsCompleted){Application.DoEvents();System.Threading.Thread.Sleep(10);}task.GetAwaiter().GetResult();
  if(holidayStatus.Text.Contains("온라인 확인 실패")||!holidayUpdateButton.Enabled||String.IsNullOrEmpty(Holidays.LastCheck))throw new Exception("Production manual holiday check failed: "+holidayStatus.Text);
  tabs.SelectedIndex=5;PerformLayout();Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(Path.Combine(folder,"공휴일_정식연결_설정화면.png"));}dirty=false;Hide();
 }
 internal void CheckHolidayDataUI(string folder,ref int total){int checks=0;Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception("Holiday UI: "+label);checks++;};
  OffscreenTest=true;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Location=new Point(SystemInformation.VirtualScreen.Right+200,SystemInformation.VirtualScreen.Top);Show();Application.DoEvents();automaticUpdates.Checked=false;
  var notices=new List<string>();HolidayAlert=notices.Add;string baseline=Engine.Json(Holidays.Current);HolidayDownload=()=>baseline;
  Action<bool> run=interactive=>{var task=CheckHolidayUpdates(interactive);while(!task.IsCompleted){Application.DoEvents();System.Threading.Thread.Sleep(10);}task.GetAwaiter().GetResult();};
  run(false);check(notices.Count==0,"no startup notice without change");
  var fixture=HolidayDataTests.Copy(Holidays.Current);fixture.years.Single(y=>y.year==2026).holidays.Add(new HolidayDate{date="2026-11-03",name="검증용 임시공휴일"});string fresh=HolidayDataTests.Encode(fixture);HolidayDownload=()=>fresh;run(false);check(notices.Count==1&&notices[0].Contains("연간 공휴일 일정이 변경되었습니다")&&notices[0].Contains("11월 3일")&&notices[0].Contains("검증용 임시공휴일"),"startup change notice includes date and name");
  run(false);check(notices.Count==1,"no repeated notice for same revision");
  HolidayDownload=()=>{throw new IOException("offline");};run(false);check(notices.Count==1&&holidayStatus.Text.Contains("온라인 확인 실패"),"offline startup stays usable without popup");check(Holidays.Name(new DateTime(2026,11,3))!="","offline keeps last update");
  HolidayDownload=()=>"invalid";run(false);check(notices.Count==1&&Holidays.Name(new DateTime(2026,11,3))!="","bad response preserves active data");
  Holidays.ResetForTest(Path.Combine(folder,"screens-cache"));UpdateHolidayStatus();tabs.SelectedIndex=5;ClientSize=new Size(1280,800);PerformLayout();Application.DoEvents();
  check(holidayUpdateButton.Enabled&&holidayStatus.Text.Contains("2027"),"manual check and year coverage visible");
  using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height));bitmap.Save(Path.Combine(folder,"공휴일_설정화면.png"));}
  for(int i=0;i<6;i++){theme.SelectedIndex=i;PerformLayout();Application.DoEvents();check(holidayStatus.Bottom<=holidayStatus.Parent.Height,"holiday information fits theme "+i);}
  dirty=false;Hide();total+=checks;
 }
}
}
