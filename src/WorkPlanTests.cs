using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using OfficeOpenXml;
namespace ShortPay {
// Historical fixtures describe agreed dates with their existing attendance map.
// New tests below construct an independent contract and call the engine directly.
public static class PlanTestData {
 public static Result Calculate(Record r,Job j){WorkPlan.FromLegacy(r);return Engine.Calculate(r,j);}
}
public static class WorkPlanTests {
 static int checks;
 static void Check(bool ok,string message){if(!ok)throw new Exception("WORK PLAN: "+message);checks++;}
 static Record Fixed(int weekdays=42,decimal hours=8){return new Record{Name="가상계약",OriginalWorker="가상원근로자",Institution="가상학교",Reason="연차",Start=new DateTime(2026,9,15),End=new DateTime(2026,10,8),ContractWeekdays=weekdays,Hours=hours};}
 static Record Irregular(){var r=HolidayTests.Example();WorkPlan.FromLegacy(r);return r;}
 static Record Education(params int[] offsets){var r=new Record{Name="교육자료 예시",OriginalWorker="원근로자",Institution="가상학교",Reason="연차",ScheduleMode=1,Start=new DateTime(2026,11,2),End=new DateTime(2026,11,11)};foreach(int offset in offsets){string key=Engine.Key(r.Start.AddDays(offset));r.ContractDays[key]=1;r.Days[key]=1;}WorkPlan.PrepareComparison(r);return r;}
 [STAThread] public static void Main(string[] args){ExcelReports.RegisterDependencies();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Run(args.Last());}
 public static void Run(string folder){Directory.CreateDirectory(folder);checks=0;try{
  List<Job> jobs;using(var reader=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("jobs.json")))jobs=Engine.Parse<List<Job>>(reader.ReadToEnd());var job=jobs[2];
  var r=Education(0,1,2,7,8);var z=Engine.Calculate(r,job);
  Check(z.Ready&&z.ReferenceHours==40&&z.ReferenceDays==8&&z.HolidayUnit==5,"education 40h / 8d gives 5h without memo or confirmation");
  Check(z.Weeks[0].Holiday==5&&z.HolidayHours==5&&z.Weeks.All(w=>w.HolidayUnit==5),"first entitlement and end summary use the same 5h daily unit");
  Check(z.Weeks.All(w=>w.ReferenceStart==r.Start&&w.ReferenceEnd==r.End),"all weekly explanations refer to the entire 10-day contract");
  Check(z.Weeks[0].Average==28&&z.Weeks[1].Average==28,"under-four-week eligibility uses whole agreed period, 40h / (10/7 weeks)");
  Check(z.Weeks[1].Holiday==0&&!z.Weeks[1].Complete,"partial final week contributes agreed hours but no additional holiday");
  var redistributed=Education(0,7,8,9,3);Check(Engine.Calculate(redistributed,job).Weeks[0].Holiday==5,"distribution across weeks does not reprice a fixed whole-contract unit");
  r=Education(0,7,8,9,10);r.End=r.Start.AddDays(10);WorkPlan.PrepareComparison(r);z=Engine.Calculate(r,job);Check(z.Weeks[0].Actual==8&&z.Weeks[0].Average==40m*7/11&&z.Weeks[0].Holiday==40m/9,"whole-period 15h test is distinct from first-week actual hours");
  r=Education(0,1,2,7,8);r.Days["2026-11-03"]=2;z=Engine.Calculate(r,job);Check(z.Ready&&z.ReferenceHours==40&&z.HolidayUnit==5&&z.HolidayHours==0,"absence removes entitlement, never prescribed hours");
  r=Education(0,1,2,7,8);r.Days.Remove("2026-11-03");z=Engine.Calculate(r,job);Check(!z.Ready&&z.HolidayUnit==5,"missing attendance blocks finalization while preserving agreed unit");
  r.Days["2026-11-03"]=1;r.ContractDays.Remove("2026-11-04");r.Days.Remove("2026-11-04");z=Engine.Calculate(r,job);Check(z.Ready&&z.HolidayHours==4,"contract edit recalculates immediately without a confirmation step");
  r.ContractDays["2026-11-05"]=1;Check(!Engine.Calculate(r,job).Ready,"new contract day has unknown attendance");r.Days["2026-11-05"]=1;Check(Engine.Calculate(r,job).Ready&&Engine.Calculate(r,job).HolidayHours==5,"recorded attendance completes an edited plan without memo");
  r.ComparisonDays.Remove("2026-11-06");z=Engine.Calculate(r,job);Check(z.Ready&&z.HolidayHours==40m/7&&z.Weeks.All(w=>w.HolidayUnit==40m/7),"explicit comparator edit updates every period unit immediately");
  r.ComparisonDays.Clear();Check(!Engine.Calculate(r,job).Ready,"zero comparator cannot silently finalize ordinary work at zero holiday pay");
  r=Education(0,1,2,7,8);r.End=r.Start.AddDays(6);WorkPlan.PrepareComparison(r);z=Engine.Calculate(r,job);Check(z.Ready&&z.HolidayHours==4.8m,"changing the agreed contract end recomputes the actual reference period");
  r=Education(0,1,2,7,8);r.End=r.Start.AddDays(13);WorkPlan.PrepareComparison(r);z=Engine.Calculate(r,job);Check(z.Ready&&z.Weeks.All(w=>w.Holiday==4)&&z.HolidayHours==8,"two complete weeks use 40h/10d=4h twice");
  r=Education(0,2);r.End=r.Start.AddDays(6);r.Hours=7.5m;WorkPlan.PrepareComparison(r);Check(Engine.Calculate(r,job).HolidayHours==3,"exact 15h qualifies");r.Hours=7.49m;Check(Engine.Calculate(r,job).HolidayHours==0,"14.98h excluded");
  r=Education(0,1,2);r.End=r.Start.AddDays(4);WorkPlan.PrepareComparison(r);Check(Engine.Calculate(r,job).HolidayHours==0,"five-day contract has no completed weekly entitlement");
  r=Fixed();Engine.Fill(r,job);z=Engine.Calculate(r,job);Check(z.Ready&&z.HolidayHours==14.4m&&z.Weeks.Take(3).All(w=>w.Holiday==4.8m),"fixed 24h/week pattern preserves its 4.8h weekly proportion");
  Check(z.PaidHolidayDays==2&&z.PaidHolidayHours==9.6m,"fixed contract pays only public holidays on agreed weekdays");
  r.Days["2026-09-16"]=2;Check(Engine.Calculate(r,job).HolidayHours==9.6m,"fixed agreement absence removes only one entitlement");
  r=Fixed(62);Engine.Fill(r,job);z=Engine.Calculate(r,job);Check(z.Ready&&z.HolidayHours==24&&z.PaidHolidayHours==24,"fixed 40h/week remains 8h per holiday");
  r=Fixed();r.NormalWeekdays=126;Engine.Fill(r,job);Check(Engine.Calculate(r,job).Weeks[0].Holiday==4,"six-day full-time comparator supported");
  r=Irregular();z=Engine.Calculate(r,job);Check(z.Ready&&z.Weeks.Take(3).All(w=>w.Holiday==6.4m)&&z.HolidayHours==19.2m,"prior date-specific example now uses 96h/15d=6.4h three times");
  Check(z.PaidHolidayHours==19.2m&&z.PaidHolidayByDay.Values.All(v=>v==6.4m),"paid public holidays use the same daily unit");
  Check(z.BasePay==1270230&&z.HolidayPay==211700&&z.Gross==1481930&&z.Net==1468600,"independent worked/public/weekly amounts and won rounding");
  r.Days["2026-09-17"]=2;z=Engine.Calculate(r,job);Check(z.ReferenceHours==96&&z.WorkHours==88&&z.HolidayHours==12.8m,"date-specific absence retains contract and removes only one 6.4h unit");
  r=Irregular();r.Days["2026-09-16"]=1;Check(!Engine.Calculate(r,job).Ready,"outside-contract attendance still blocked");
  r=Irregular();r.ComparisonDays["2026-09-24"]=1;Check(!Engine.Calculate(r,job).Ready,"invalid holiday comparator still blocked");
  r=Irregular();r.WeeklyRestDay=1;Check(!Engine.Calculate(r,job).Ready,"rest day overlapping contract remains invalid");
  r=Education(0,1,2,7,8);r.Days["2026-11-08"]=1;Check(!Engine.Calculate(r,job).Ready,"actual work on designated rest day requires separate handling");
  r=new Record{ScheduleMode=1,Start=new DateTime(2026,11,2),End=new DateTime(2026,11,30)};for(int n=0;n<29;n++)if((int)r.Start.AddDays(n).DayOfWeek>=1&&(int)r.Start.AddDays(n).DayOfWeek<=5){string key=Engine.Key(r.Start.AddDays(n));r.ContractDays[key]=1;r.Days[key]=1;}WorkPlan.PrepareComparison(r);z=Engine.Calculate(r,job);
  Check(z.Ready&&z.Weeks.Take(4).All(w=>w.Holiday==8)&&z.HolidayHours==32,"29-day contract has four entitlements, no fifth for tail");
  Check(z.Weeks.Take(4).All(w=>w.ReferenceStart==r.Start&&w.ReferenceEnd==r.Start.AddDays(27))&&z.ReferenceStart==r.Start.AddDays(1),"four-week cap has explicit complete block and tail window");
  foreach(var example in new[]{Fixed(),Irregular(),Education(0,1,2,7,8)}){
   r=example;if(r.ScheduleMode==0)Engine.Fill(r,job);z=Engine.Calculate(r,job);string label=r.Start.Month==11?"education":r.ScheduleMode==0?"fixed":"irregular";
   string native=Path.Combine(folder,label+".stpay");Engine.Save(native,r);var loaded=Engine.Load(native);Check(Engine.Json(loaded)==Engine.Json(r),"native roundtrip "+label);
   Check(!Engine.Json(r).Contains("BasisNote")&&!Engine.Json(r).Contains("PlanSignature"),"removed confirmation data is not serialized "+label);
   foreach(string kind in new[]{"임금산정표","급여명세서","인건비 신청내역"}){string path=Path.Combine(folder,label+"-"+kind+".xlsx");ExcelReports.Save(path,kind,r,job,z,new ExportOptions());using(var workbook=new ExcelPackage(new FileInfo(path))){var sheet=workbook.Workbook.Worksheets.First();string net=kind=="임금산정표"?"G26":kind=="급여명세서"?"F23":"AR14";Check(Convert.ToDecimal(sheet.Cells[net].Value)==z.Net,"export reconciles "+label+kind);if(label=="education"&&kind=="임금산정표")Check(Convert.ToDecimal(sheet.Cells["P11"].Value)==5&&sheet.Cells["B33"].Text.Contains("5 = 5"),"education example worksheet pays 5h, not 4.8h");}}
   var reopened=WageWorkbook.Load(Path.Combine(folder,label+"-임금산정표.xlsx"),jobs);Check(reopened.Exact&&reopened.Record.Version==7&&Engine.Calculate(reopened.Record,job).HolidayHours==z.HolidayHours,"XLSX working-input roundtrip "+label);
   Check(reopened.Record.ContractDays.OrderBy(p=>p.Key).SequenceEqual(r.ContractDays.OrderBy(p=>p.Key))&&reopened.Record.ComparisonDays.OrderBy(p=>p.Key).SequenceEqual(r.ComparisonDays.OrderBy(p=>p.Key)),"independent contract and comparator maps persist "+label);
  }
  r=Irregular();r.Version=6;string old6=Path.Combine(folder,"legacy-v6.stpay");File.WriteAllText(old6,Engine.Json(r).Replace("\"ComparisonPeriod\"","\"BasisNote\":\"old note\",\"PlanSignature\":\"old signature\",\"ComparisonPeriod\""));var migrated=Engine.Load(old6);Check(migrated.Version==7&&migrated.ContractDays.Count==15&&Engine.Calculate(migrated,job).Ready,"v6 input and obsolete note/signature migrate without a confirmation gate");
  r=HolidayTests.Example();r.Version=5;string old5=Path.Combine(folder,"legacy-v5.stpay");Engine.Save(old5,r);migrated=Engine.Load(old5);Check(migrated.Version==7&&migrated.Days.Count==15&&Engine.Calculate(migrated,job).HolidayHours==19.2m,"v5 input preserves dates and recomputes whole-period pay");
  var oldWorkbook=WageWorkbook.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","test-fixtures","legacy-v14-wage.xlsx"),jobs);Check(oldWorkbook.Exact&&oldWorkbook.Record.Version==7&&Engine.Calculate(oldWorkbook.Record,job).Ready&&Engine.Calculate(oldWorkbook.Record,job).HolidayHours==19.2m,"public v1.4 workbook reopens without note/check gate and recalculates");
  foreach(int index in new[]{21,22}){var guard=new Record{JobIndex=index,Start=new DateTime(2026,11,2),End=new DateTime(2026,11,15),Hours=8.5m};Engine.Fill(guard,jobs[index]);var value=Engine.Calculate(guard,jobs[index]);Check(value.Ready&&value.Guard&&value.HolidayHours==0,"guard calculation unchanged "+index);}
  FlatUiTests.PrepareContext(Path.Combine(folder,"ui"));using(var form=new MainForm())form.CheckWorkPlanUi(folder,Check);
  File.WriteAllText(Path.Combine(folder,"work-plan-tests.txt"),"PASS "+checks+" whole-contract education example, eligibility/attendance, monetary, fixed/irregular, migration, native/XLSX and simplified-dialog checks.");
 }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"work-plan-tests.txt"),ex.ToString());Console.WriteLine(ex.ToString());Environment.ExitCode=1;}}
}
public partial class MainForm {
 internal void CheckWorkPlanUi(string folder,Action<bool,string> check){
  LoadFlatExample();OffscreenTest=true;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Location=new Point(SystemInformation.VirtualScreen.Right+200,SystemInformation.VirtualScreen.Top);Show();Application.DoEvents();
  check(!Bounds.IntersectsWith(SystemInformation.VirtualScreen)&&!ShowInTaskbar,"UI tests do not cover or activate desktop");
  check(result.Ready&&result.HolidayHours==19.2m&&!holidayFormula.Text.Contains("기준 확인 전")&&!holidayFormula.Text.Contains("미리보기"),"calculation needs no memo or confirmation");
  check(calendar.Controls.OfType<DayButton>().All(b=>b.Width==b.Height),"single calendar keeps square dates");
  check(calendar.Controls.OfType<DayButton>().Where(b=>b.WeeklyHoliday).All(b=>b.AccessibleName.StartsWith("2026-09-20")||b.AccessibleName.StartsWith("2026-09-27")||b.AccessibleName.StartsWith("2026-10-04")),"rest markers stay on designated Sunday");
  calendarEditMode.SelectedIndex=1;EditCalendarDay(new DateTime(2026,9,16));check(!result.Ready&&!r.Days.ContainsKey("2026-09-16"),"new calendar contract day requires recorded attendance");
  calendarEditMode.SelectedIndex=0;EditCalendarDay(new DateTime(2026,9,16));check(result.Ready&&Math.Abs(result.HolidayHours-20.8m)<.0000001m,"calendar contract and attendance edit recalculates without confirmation");
  LoadFlatExample();r.Days["2026-09-17"]=2;end.Value=r.End.AddDays(1);check(r.Days["2026-09-17"]==2&&r.Days.Count==15,"date edit preserves absence and does not invent attendance");
  LoadFlatExample();DateTime expectedStart=r.Start,expectedEnd=r.End;DateTime? expectedPayment=r.PaymentDate;string attendanceBefore=Engine.Json(r.Days);
  InspectPlanDialog=dialog=>{
   check(dialog.Controls.OfType<ComboBox>().Count()==2&&dialog.Controls.OfType<CheckBox>().Count()==14,"dialog retains weekday choices, removes bottom confirmation checkbox");
   check(!dialog.Controls.OfType<TextBox>().Any()&&!dialog.Controls.Cast<Control>().Any(c=>c.Text.Contains("메모")||c.Text.Contains("확인했습니다")),"memo field and confirmation label removed");
   check(dialog.Controls.Cast<Control>().All(c=>c.Left>=0&&c.Top>=0&&c.Right<=dialog.ClientSize.Width&&c.Bottom<=dialog.ClientSize.Height),"compact dialog controls fit bounds");
   using(var bitmap=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(folder,"계약주휴_설정창.png"));}
   dialog.Controls.OfType<ComboBox>().First(c=>c.Left==20).SelectedIndex=1;
   var apply=dialog.Controls.OfType<SoftButton>().Single(b=>b.Text=="적용");typeof(Button).GetMethod("OnClick",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(apply,new object[]{EventArgs.Empty});
  };EditWorkPlan();InspectPlanDialog=null;
  check(r.ScheduleMode==1&&result.Ready,"actual dialog apply works without any confirmation checkbox");
  check(r.Start==expectedStart&&r.End==expectedEnd&&r.PaymentDate==expectedPayment&&r.Start.Kind==expectedStart.Kind,"dialog preserves dates and timezone kind");
  check(Engine.Json(r.Days)==attendanceBefore,"dialog changes preserve recorded attendance");
  LoadFlatExample();calendarEditMode.SelectedIndex=0;tabs.SelectedIndex=1;PerformLayout();Application.DoEvents();using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(Point.Empty,Size));bitmap.Save(Path.Combine(folder,"계약주휴_테스트화면.png"));}
  dirty=false;Hide();
 }
}
}
