using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using OfficeOpenXml;
namespace ShortPay {
public static class HolidayTests {
 static int count;
 static void Check(bool ok,string label){if(!ok)throw new Exception("HOLIDAY TEST: "+label);count++;}
 internal static Record Example(){var r=new Record{Name="주휴검산",OriginalWorker="원근로자",Institution="테스트학교",Reason="휴가",Start=new DateTime(2026,9,15),End=new DateTime(2026,10,8),Hours=8};
 foreach(var d in new[]{"09-15","09-17","09-21","09-22","09-23","09-24","09-25","09-28","09-29","09-30","10-02","10-05","10-06","10-07","10-08"}){var date=DateTime.Parse("2026-"+d);r.Days[Engine.Key(date)]=Holidays.Name(date)==""?1:4;}return r;}
 public static void Run(string folder){Directory.CreateDirectory(folder);MainForm.SettingsTestFile=Path.Combine(folder,"isolated-settings.json");try{
 List<Job> jobs;using(var sr=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("jobs.json")))jobs=Engine.Parse<List<Job>>(sr.ReadToEnd());
 var r=Example();var z=PlanTestData.Calculate(r,jobs[2]);
 Check(z.Ready&&z.Days==12&&z.WorkHours==96,"actual twelve days, 96 hours");
 Check(z.ReferenceDays==15&&z.ReferenceHours==96&&z.HolidayUnit==6.4m,"96 prescribed hours / 15 nonholiday weekdays");
 Check(z.PaidHolidayDays==3&&z.PaidHolidayHours==19.2m&&z.PaidLeave==19.2m,"three paid holidays at proportional daily hours");
 Check(z.Weeks[0].Holiday==6.4m&&z.Weeks[1].Holiday==6.4m&&z.Weeks[2].Holiday==6.4m&&z.HolidayHours==19.2m,"three qualifying cycles");
 Check(!z.Weeks[3].Complete&&z.Weeks[3].Holiday==0,"three-day tail earns no extra holiday");
 Check(z.Weeks[1].Actual==24&&z.Weeks[2].Actual==24,"paid public holidays excluded from actual work");
 Check(z.WorkHours+z.PaidLeave+z.HolidayHours==134.4m,"total paid hours reconcile independently");
 Check(z.BasePay==Engine.Floor10(z.Hourly*115.2m)&&z.WorkAndHoliday==Engine.Floor10(z.Hourly*134.4m),"monetary and rounding reconciliation");
 Check(z.Hourly==11026.32m&&z.BasePay==1270230&&z.HolidayPay==211700&&z.Gross==1481930&&z.Net==1468600,"independently computed example amounts");
 r.Days["2026-09-17"]=2;z=PlanTestData.Calculate(r,jobs[2]);Check(z.ReferenceHours==96&&z.WorkHours==88&&z.Weeks[0].Holiday==0&&z.HolidayHours==12.8m,"absence preserves prescribed hours but removes that cycle");
 r=Example();r.Days["2026-09-25"]=1;z=PlanTestData.Calculate(r,jobs[2]);Check(!z.Ready&&z.Warnings.Any(w=>w.Contains("휴일근로")),"actual holiday work explicitly requires separate premium review");
 bool blocked=false;try{ExcelReports.Save(Path.Combine(folder,"must-not-write.xlsx"),"임금산정표",r,jobs[2],z,new ExportOptions());}catch(InvalidOperationException){blocked=true;}Check(blocked&&!File.Exists(Path.Combine(folder,"must-not-write.xlsx")),"all unreviewed holiday exports blocked");
 r=Example();r.Days["2026-09-17"]=4;Check(!PlanTestData.Calculate(r,jobs[2]).Ready,"invalid paid holiday rejected");
 r=Example();r.Days["2026-09-17"]=3;Check(!PlanTestData.Calculate(r,jobs[2]).Ready,"legacy paid leave keeps review requirement");
 r=Example();var file=Path.Combine(folder,"example.stpay");Engine.Save(file,r);var loaded=Engine.Load(file);Check(loaded.Version==7&&loaded.Days["2026-09-25"]==4&&PlanTestData.Calculate(loaded,jobs[2]).HolidayHours==19.2m,"save and reload paid holiday states");
 r.Version=4;r.Days["2026-09-25"]=1;Engine.Save(file,r);loaded=Engine.Load(file);Check(loaded.Version==7&&loaded.Days["2026-09-25"]==1&&!PlanTestData.Calculate(loaded,jobs[2]).Ready,"old data preserves original selections and flags ambiguous holiday work");
 r=new Record{Start=new DateTime(2026,6,1),End=new DateTime(2026,6,28),Hours=7};foreach(var d in new[]{1,2,4,8,9,10,15,16,17,22,23})r.Days["2026-06-"+d.ToString("00")]=1;z=PlanTestData.Calculate(r,jobs[2]);Check(z.Ready&&z.Weeks[3].Planned==14&&z.Weeks[3].Average==19.25m&&z.Weeks[3].Holiday>0,"four-week average qualifies last 14-hour week");
 r=new Record{Start=new DateTime(2026,9,7),End=new DateTime(2026,9,20),Hours=2};Engine.Fill(r,jobs[2]);Check(PlanTestData.Calculate(r,jobs[2]).HolidayHours==0,"true under15 remains excluded");
 r=new Record();Engine.Fill(r,jobs[2]);z=PlanTestData.Calculate(r,jobs[2]);Check(z.HolidayHours==16&&z.Net==1049000&&z.Ready,"ordinary holiday-free baseline unchanged");
 r=new Record{Start=new DateTime(2026,9,21),End=new DateTime(2026,9,27)};Engine.Fill(r,jobs[2]);z=PlanTestData.Calculate(r,jobs[2]);Check(r.Days["2026-09-24"]==4&&z.Days==3&&z.HolidayHours==8&&z.PaidHolidayHours==16,"normal five-day schedule with two Chuseok paid holidays");
 Check(Holidays.Name(new DateTime(2026,5,1))!=""&&Holidays.Name(new DateTime(2026,7,17))!=""&&Holidays.Name(new DateTime(2026,9,28))=="","updated 2026 holidays and no fictitious Chuseok substitute");
 r=Example();z=PlanTestData.Calculate(r,jobs[2]);foreach(var kind in new[]{"임금산정표","급여명세서","인건비 신청내역"}){
 var path=Path.Combine(folder,kind+".xlsx");ExcelReports.Save(path,kind,r,jobs[2],z,new ExportOptions{Year=2026,Month=9});using(var package=new ExcelPackage(new FileInfo(path))){var ws=package.Workbook.Worksheets.First();Check(Convert.ToDecimal(ws.Cells[kind=="임금산정표"?"G26":kind=="급여명세서"?"F23":"AR14"].Value)==z.Net,"reopened monetary net "+kind);
 if(kind=="임금산정표"){Check(ws.Cells["B33"].Text.Contains("6.4 + 6.4 + 6.4 = 19.2"),"exported correct numerator and denominator");Check(ws.Cells["J35"].Text.Contains("실근로 12일"),"actual days in wage output");Check(ws.Cells["L24"].Text=="유급휴일"&&Convert.ToDecimal(ws.Cells["M24"].Value)==6.4m,"holiday row proportional hours");}
 if(kind=="급여명세서")Check(Convert.ToDecimal(ws.Cells["D8"].Value)==12&&ws.Cells["D17"].Text.Contains("유급휴일"),"payslip separates actual days and paid holiday explanation");}}
 File.WriteAllText(Path.Combine(folder,"example-result.json"),Engine.Json(z));File.WriteAllText(Path.Combine(folder,"example-report.html"),Reports.Html(r,jobs[2],z,"임금산정표"));
 foreach(var kind in new[]{"임금산정표","급여명세서"}){var pdf=Path.Combine(folder,kind+".pdf");PdfReports.Save(pdf,kind,r,jobs[2],z,new ExportOptions{Year=2026,Month=9});Check(File.ReadAllBytes(pdf).Take(5).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("%PDF-")),"example PDF exported "+kind);}
 GuardTests.Run(jobs,Path.Combine(folder,"guards"));ScheduleTests.Run(jobs,Path.Combine(folder,"schedule"));
 using(var form=new MainForm()){form.ExerciseHolidayUI(folder);}
 File.WriteAllText(Path.Combine(folder,"holiday-tests.txt"),"PASS "+count+" holiday engine, saved-data and Excel checks; UI, guard and schedule results saved separately.");
 }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"holiday-tests.txt"),ex.ToString());Environment.ExitCode=1;}}
}
public partial class MainForm {
 internal void ExerciseHolidayFixUI(string folder){
 r=HolidayTests.Example();LoadUI();automaticUpdates.Checked=false;RefreshAll();tabs.SelectedIndex=1;Show();Application.DoEvents();
 if(result.HolidayHours!=19.2m||!holidayFormula.Text.Contains("6.4 + 6.4 + 6.4 = 19.2"))throw new Exception("holiday screen calculation");
 CaptureScreen(Path.Combine(folder,"holiday-screen.png"));Show();
 Func<DayButton> button=()=>calendar.Controls.OfType<DayButton>().Single(b=>b.AccessibleName.StartsWith("2026-09-24"));
 if(button().State!=4)throw new Exception("paid holiday green state");
 button().PerformClick();if(r.Days.ContainsKey("2026-09-24"))throw new Exception("paid holiday to off");
 button().PerformClick();if(result.Ready)throw new Exception("holiday work requires review");
 button().PerformClick();button().PerformClick();if(button().State!=4||result.HolidayHours!=19.2m||!result.Ready)throw new Exception("restore paid holiday");
 if(holidayFormula.Bottom>calendarHost.Height)throw new Exception("holiday formula exceeds card");
 CaptureScreen(Path.Combine(folder,"holiday-screen-after-clicks.png"));dirty=false;
 File.WriteAllText(Path.Combine(folder,"holiday-ui-tests.txt"),"PASS paid holiday visibility, click cycle, actual-work review, restored totals and formula layout");
 }
}
}
