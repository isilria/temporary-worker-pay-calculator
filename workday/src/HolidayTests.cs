using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OfficeOpenXml;
namespace SchoolWorkdayCalculator {
internal sealed partial class MainForm {
 internal static void TestHolidays(string folder,bool live){
  Directory.CreateDirectory(folder);int count=0;
  Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception(name);count++;};
  Action<Action,string> reject=(action,name)=>{bool failed=false;try{action();}catch{failed=true;}check(failed,name);};
  try{
   Holidays.ResetForTest(Path.Combine(folder,"cache-"+Guid.NewGuid().ToString("N")));
   string original=HolidayJson.Json(Holidays.Current),version=Holidays.Current.contentVersion;
   check(Holidays.Covers(2026)&&Holidays.Covers(2027),"embedded data covers fiscal2026");
   check(Holidays.Covers(2028),"expanded source contains published2028");
   check(Holidays.Status().Contains("공공데이터포털 공휴일 자료 활용 (현재 반영 기간: 2021년~2028년)"),"actual available span displayed");
   check(Holidays.Status().Contains("(공공데이터포털)")&&Holidays.Status().Contains("(로컬파일) 확인 전"),"source and local dates distinguished before first check");
   check(!System.Text.RegularExpressions.Regex.IsMatch(Holidays.Status(),@"\d{2}:\d{2}"),"date-only source status");
   check(Holidays.RequestStatus().Contains("2026년~2030년")&&Holidays.RequestStatus().Contains("2029년·2030년"),"five-year target and missing years clearly displayed");
   var fiscal=HolidayCalculator.GetFiscalHolidays(2026);
   check(fiscal.Keys.All(d=>d>=new DateTime(2026,3,1)&&d<new DateTime(2027,3,1)),"fiscal boundary");
   check(fiscal.ContainsKey(new DateTime(2026,6,3)),"official election holiday");
   check(fiscal.ContainsKey(new DateTime(2027,2,8)),"next-year lunar holiday included");
   check(fiscal[new DateTime(2026,5,1)].Length>0,"labour day retained");
   var specialDays=new[]{new DateTime(2021,8,16),new DateTime(2022,6,1),new DateTime(2023,10,2),new DateTime(2024,10,1),new DateTime(2025,6,3)};
   var nextYearSeollal=new[]{new DateTime(2022,2,1),new DateTime(2023,1,24),new DateTime(2024,2,12),new DateTime(2025,1,27),new DateTime(2026,2,17)};
   for(int i=0;i<specialDays.Length;i++){
    var historical=HolidayCalculator.GetFiscalHolidays(specialDays[i].Year);
    check(historical.ContainsKey(specialDays[i])&&historical.ContainsKey(nextYearSeollal[i]),"historical special day and next-year holiday "+specialDays[i].Year);
   }
   reject(()=>HolidayCalculator.GetFiscalHolidays(2040),"unsupported year blocks generation");
   reject(()=>Holidays.Parse(original.Replace(version,new string('0',64))),"invalid fingerprint rejected");
   reject(()=>Holidays.Parse(original.Replace(Holidays.SourceUrl,"https://invalid.test")),"invalid source rejected");
   reject(()=>Holidays.Parse("{}"),"incomplete source rejected");
   var candidate=HolidayJson.Parse<HolidayDocument>(original);candidate.fetchedAtUtc=DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
   candidate.years.Single(y=>y.year==2026).holidays.Add(new HolidayDate{date="2026-10-16",name="테스트 임시공휴일"});candidate.contentVersion=Holidays.Fingerprint(candidate.years);
   var changed=Holidays.Accept(HolidayJson.Json(candidate));
   check(changed.Count==1&&changed[0].After=="테스트 임시공휴일","add holiday difference and actual name");
   check(HolidayCalculator.GetFiscalHolidays(2026).ContainsKey(new DateTime(2026,10,16)),"new calculation uses update");
   string cache=Holidays.Folder;Holidays.ResetForTest(cache);
   check(Holidays.Current.contentVersion==candidate.contentVersion&&Holidays.LastChanges.Count==1,"relaunch restores saved update and change history");
   candidate.years.Single(y=>y.year==2026).holidays.Single(d=>d.date=="2026-10-16").name="이름 수정";candidate.contentVersion=Holidays.Fingerprint(candidate.years);
   changed=Holidays.Accept(HolidayJson.Json(candidate));check(changed.Count==1&&changed[0].Before=="테스트 임시공휴일"&&changed[0].After=="이름 수정","rename diff");
   candidate.years.Single(y=>y.year==2026).holidays.RemoveAll(d=>d.date=="2026-10-16");candidate.contentVersion=Holidays.Fingerprint(candidate.years);
   changed=Holidays.Accept(HolidayJson.Json(candidate));check(changed.Count==1&&changed[0].After==null,"deletion diff");
   candidate.fetchedAtUtc="2025-01-01T00:00:00Z";check(Holidays.Accept(HolidayJson.Json(candidate)).Count==0,"older identical content is successful check");
   candidate.years.RemoveAll(y=>y.year<=2025);candidate.contentVersion=Holidays.Fingerprint(candidate.years);candidate.fetchedAtUtc=DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
   string currentOnly=HolidayJson.Json(candidate);
   File.WriteAllText(Holidays.CacheFile,currentOnly);
   Holidays.ResetForTest(cache);
   check(Holidays.Current.contentVersion==version,"older cache format restores bundled archive");
   check(Holidays.Accept(currentOnly).Count==0,"online update without historical years retains the archive");
   check(Holidays.Parse(File.ReadAllText(Holidays.CacheFile)).years.Count(y=>y.year<=2025)==5,"all historical years persisted in cache");
   Holidays.ResetForTest(cache);
   check(Holidays.Current.contentVersion==version,"relaunch reloads historical cache");
   candidate.years.RemoveAll(y=>y.year==2026);candidate.contentVersion=Holidays.Fingerprint(candidate.years);
   reject(()=>Holidays.Accept(HolidayJson.Json(candidate)),"missing known current year rejected");
   File.WriteAllText(Holidays.CacheFile,"broken");Holidays.ResetForTest(cache);check(Holidays.Current.contentVersion==version&&Holidays.LoadWarning!="","broken cache falls back to embedded");
   Holidays.ResetForTest(Path.Combine(folder,"clean-cache"));
   using(var f=new MainForm{Offscreen=true,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new Point(SystemInformation.VirtualScreen.Right+200,SystemInformation.VirtualScreen.Top)}){
    f.Show();Application.DoEvents();f.outputFolder.Text=Path.Combine(folder,"historical-workbooks");
    f.TestMessage=(message,title,icon)=>{if(title=="오류")throw new Exception(message);};
    f.HolidayDownload=()=>{throw new IOException("simulated offline");};
    var task=f.CheckHolidayUpdates(false);while(!task.IsCompleted){Application.DoEvents();System.Threading.Thread.Sleep(10);}task.GetAwaiter().GetResult();
    check(f.HolidayCheckWarning.Contains("온라인 확인 실패")&&f.createButton.Enabled,"offline preserves ability to generate");
    for(int i=0;i<specialDays.Length;i++){
     int schoolYear=specialDays[i].Year;
     f.year.Text=schoolYear.ToString(CultureInfo.InvariantCulture);
     check(f.BuildSettings(false).Year==schoolYear&&f.holidayDataStatus.Text=="작성 가능","typed historical school year "+schoolYear);
     check(f.winterEnd.Value.Date==new DateTime(schoolYear+1,2,DateTime.DaysInMonth(schoolYear+1,2)),"historical fiscal end "+schoolYear);
     foreach(bool nonConsent in new[]{false,true}){
      f.SelectWorkerMode(nonConsent);f.RefreshLivePreview();
      check(f.liveResult!=null&&f.previewError=="","historical live preview "+schoolYear+" "+nonConsent);
      f.createButton.PerformClick();
      string file=Path.Combine(f.outputFolder.Text,schoolYear+(nonConsent?"학년도_320일비동의자_근로일수_산정.xlsx":"학년도_교육공무직_근로일수_산정.xlsx"));
      using(var book=new ExcelPackage(new FileInfo(file))){
       var sheet=book.Workbook.Worksheets[1];
       check(book.Workbook.Worksheets.Count==3&&sheet.Cells[Helper(specialDays[i])].Text=="H"&&sheet.Cells[Helper(nextYearSeollal[i])].Text=="H","both calendar years exported "+schoolYear+" "+nonConsent);
      }
     }
    }
    f.HolidayDownload=()=>original;task=f.CheckHolidayUpdates(false);while(!task.IsCompleted){Application.DoEvents();System.Threading.Thread.Sleep(10);}task.GetAwaiter().GetResult();
    check(!f.HolidayCheckWarning.Contains("온라인 확인 실패")&&File.Exists(Holidays.CacheFile),"manual success clears failure and saves");
    check(Holidays.Status().Contains("(로컬파일) "+DateTime.Today.ToString("yyyy-MM-dd")),"successful local check shows current date");
    string lastCheck=Holidays.LastCheck;f.HolidayDownload=()=>{throw new IOException("simulated offline");};task=f.CheckHolidayUpdates(false);while(!task.IsCompleted){Application.DoEvents();System.Threading.Thread.Sleep(10);}task.GetAwaiter().GetResult();check(Holidays.LastCheck==lastCheck,"failed recheck preserves last successful date");
    f.year.Value=2040;check(f.holidayDataStatus.Text.Contains("작성불가"),"year change displays missing coverage");
   }
   if(live){var text=Holidays.Fetch();Holidays.Accept(text);check(Holidays.Covers(2026)&&Holidays.Covers(2027)&&File.Exists(Holidays.CacheFile),"actual public feed download validated and saved");}
   File.WriteAllText(Path.Combine(folder,live?"holiday-live-tests.txt":"holiday-tests.txt"),"PASS "+count+" official source, fiscal range, cache, offline, add/delete/rename, malformed-data and UI recovery checks\r\n"+Holidays.Status());
  }catch(Exception ex){File.WriteAllText(Path.Combine(folder,live?"holiday-live-tests.txt":"holiday-tests.txt"),ex.ToString());Environment.ExitCode=1;}
 }
}
}
