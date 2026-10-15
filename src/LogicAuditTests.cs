using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using OfficeOpenXml;
namespace ShortPay {
public static class LogicAuditTests {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception("LOGIC AUDIT: "+name);checks++;}
 [STAThread] public static void Main(string[] args){ExcelReports.RegisterDependencies();if(args.Contains("--excel-only"))ExcelTests.Run(args.Last());else Run(args.Last());}
 static Record Case(int[] offsets,decimal hours,int days=14){var r=new Record{Start=new DateTime(2026,11,2),Hours=hours,Name="검산",OriginalWorker="원근로자",Reason="휴가",Institution="검산학교"};r.End=r.Start.AddDays(days-1);foreach(var n in offsets)r.Days[Engine.Key(r.Start.AddDays(n))]=1;return r;}
 public static void Run(string folder){Directory.CreateDirectory(folder);checks=0;try{
  List<Job> jobs;using(var reader=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("jobs.json")))jobs=Engine.Parse<List<Job>>(reader.ReadToEnd());var j=jobs[2];
  var r=Case(new[]{0,2,7,8,9,10},8);var z=PlanTestData.Calculate(r,j);
  Check(z.Ready&&z.Weeks[0].Holiday==4.8m&&z.Weeks[1].Holiday==4.8m&&z.HolidayHours==9.6m,"16h then 32h: whole-period 48/10=4.8h for each entitlement");
  var first=z.Weeks[0].Holiday;r.End=r.Start.AddDays(6);Check(PlanTestData.Calculate(r,j).Weeks[0].Holiday==3.2m,"shortening the agreed contract changes the reference period");
  r.End=r.Start.AddDays(13);r.Days[Engine.Key(r.Start.AddDays(11))]=1;Check(PlanTestData.Calculate(r,j).Weeks[0].Holiday==5.6m,"editing agreed hours recomputes the whole-period daily unit");
  r=Case(new[]{0,1,2,3,4,7},3,8);z=PlanTestData.Calculate(r,j);
  Check(z.Average==15.75m&&z.Weeks[0].Average==15.75m&&z.HolidayHours==3m,"18 prescribed hours / (8/7 weeks), with one completed qualifying week");
  Check(z.Hourly==12361.38m&&z.Ready,"three-hour daily base plus eligible meal, divided by 78 monthly hours");
  r=Case(new[]{0,2},7.5m,7);z=PlanTestData.Calculate(r,j);Check(z.HolidayHours==3&&z.Weeks[0].Average==15,"exact 15-hour threshold");
  r.Hours=7.49m;Check(PlanTestData.Calculate(r,j).HolidayHours==0,"14.98 hours below threshold");
  r=Case(new[]{0,1,2,7,8,9,14,15,16,21,22},7,28);z=PlanTestData.Calculate(r,j);
  Check(z.Weeks[3].Planned==14&&z.Weeks[3].Average==19.25m&&z.Weeks[3].Holiday==3.85m,"21/21/21/14: 77/4 qualifies final week and 77/20 daily hours");
  Check(z.HolidayHours==15.4m,"whole-period four-cycle total 3.85h x 4");
  r=Case(new[]{0,1,2,3,4,7,8,9,10,11},8);z=PlanTestData.Calculate(r,j);Check(z.HolidayHours==16&&z.Gross==1058520&&z.Net==1049000,"normal 40-hour schedule and original money unchanged");
  r.Days[Engine.Key(r.Start.AddDays(2))]=2;z=PlanTestData.Calculate(r,j);Check(z.HolidayHours==8&&z.Weeks[0].ReferenceHours==80&&z.Weeks[1].ReferenceHours==80,"absence removes only its own entitlement, preserving agreed hours");
  r.Days.Remove(Engine.Key(r.Start.AddDays(2)));z=PlanTestData.Calculate(r,j);Check(z.HolidayHours==14.4m&&z.Ready,"agreed non-work day keeps attendance qualification and proportionate hours");
  r=Case(new[]{0,1,2,3,4},8,5);Check(PlanTestData.Calculate(r,j).HolidayHours==0,"Monday-Friday end: no completed seven-day employment period");
  r.End=r.Start.AddDays(6);Check(PlanTestData.Calculate(r,j).HolidayHours==8,"Monday-Sunday end: no next-week work required");
  for(int offset=0;offset<7;offset++){r=new Record{Start=new DateTime(2026,11,2).AddDays(offset)};r.End=r.Start.AddDays(6);Engine.Fill(r,j);z=PlanTestData.Calculate(r,j);Check(z.Ready&&z.HolidayHours==8,"all contract start weekdays: "+offset);}
  r=new Record{Start=new DateTime(2026,5,1),End=new DateTime(2026,5,1)};r.Days[Engine.Key(r.Start)]=4;Check(!PlanTestData.Calculate(r,j).Ready,"holiday-only comparison period cannot silently finalize zero pay");
  r=new Record{Start=new DateTime(2026,9,21),End=new DateTime(2026,10,4),Hours=4};r.Days["2026-09-21"]=1;r.Days["2026-09-24"]=4;r.Days["2026-09-25"]=4;foreach(var day in new[]{"2026-09-28","2026-09-29","2026-09-30","2026-10-01","2026-10-02"})r.Days[day]=1;z=PlanTestData.Calculate(r,j);Check(z.Average==16&&z.Ready&&z.Weeks[0].Average==16,"under-four-week eligibility uses whole agreed period consistently");
  r=Case(new[]{0,1,2,3,4,7,8,9,10,11,14,15,16,17,18,21,22,23,24,25,28},8,29);z=PlanTestData.Calculate(r,j);Check(z.ReferenceStart==r.Start.AddDays(1)&&z.ReferenceEnd==r.End&&z.ReferenceDays==20,"29-day contract-end summary rolls past first day");Check(z.HolidayHours==32,"one-day tail creates no fifth entitlement");
  r=HolidayTests.Example();z=PlanTestData.Calculate(r,j);
  Check(z.Days==12&&z.WorkHours==96&&z.Weeks.Count(w=>w.Holiday>0)==3,"prior example: twelve worked days and three entitlements");
  Check(z.Weeks[0].ReferenceHours==96&&z.Weeks[0].ReferenceDays==15&&z.Weeks[0].Holiday==6.4m,"prior example cycle one: whole-period 96/15");
  Check(z.Weeks[1].ReferenceHours==96&&z.Weeks[1].ReferenceDays==15&&z.Weeks[1].Holiday==6.4m,"prior example cycle two: same whole-period 96/15");
  Check(z.Weeks[2].ReferenceHours==96&&z.Weeks[2].ReferenceDays==15&&z.Weeks[2].Holiday==6.4m&&z.HolidayHours==19.2m,"prior example cycle three and total: 96/15 x 3 = 19.2");
  Check(z.PaidHolidayHours==19.2m&&z.PaidHolidayByDay.Values.All(x=>x==6.4m),"public holidays use the same whole-period daily basis");
  Check(z.Average==35&&z.BasePay==1270230&&z.HolidayPay==211700&&z.Gross==1481930&&z.Net==1468600,"independent exact monetary reconciliation");
  var saved=Path.Combine(folder,"example.stpay");Engine.Save(saved,r);Check(PlanTestData.Calculate(Engine.Load(saved),j).HolidayHours==19.2m,"saved input recomputes with corrected logic");
  File.WriteAllText(Path.Combine(folder,"example-result.json"),Engine.Json(z));File.WriteAllText(Path.Combine(folder,"example-report.html"),Reports.Html(r,j,z,"임금산정표"));
  foreach(var kind in new[]{"임금산정표","급여명세서","인건비 신청내역"}){
   var path=Path.Combine(folder,kind+".xlsx");ExcelReports.Save(path,kind,r,j,z,new ExportOptions());using(var book=new ExcelPackage(new FileInfo(path))){var ws=book.Workbook.Worksheets.First();var addr=kind=="임금산정표"?"G26":kind=="급여명세서"?"F23":"AR14";Check(Convert.ToDecimal(ws.Cells[addr].Value)==z.Net,"export reopens with same net: "+kind);
    if(kind=="임금산정표"){Check(ws.Cells["B33"].Text.Contains("6.4 + 6.4 + 6.4 = 19.2"),"worksheet exposes one consistent reference-period amount");Check(Convert.ToDecimal(ws.Cells["P11"].Value)==6.4m&&Convert.ToDecimal(ws.Cells["P12"].Value)==6.4m&&Convert.ToDecimal(ws.Cells["P13"].Value)==6.4m,"worksheet weekly cells");Check(Convert.ToDecimal(ws.Cells["M24"].Value)==6.4m,"worksheet public holiday row uses its own day amount");}
    if(kind=="인건비 신청내역")Check(Convert.ToDecimal(ws.Cells["V14"].Value)==6.4m&&Convert.ToDecimal(ws.Cells["U14"].Value)==3,"single-row form represents exact total as three times the common 6.4-hour unit");}}
  r.Days["2026-09-25"]=1;z=PlanTestData.Calculate(r,j);Check(!z.Ready,"actual public holiday work still requires premium review");
  ScheduleTests.Run(jobs,Path.Combine(folder,"schedule"));
  File.WriteAllText(Path.Combine(folder,"logic-audit-tests.txt"),"PASS "+checks+" engine, saved-input, monetary and export checks. Separate schedule and guard-workbook checks saved in schedule-tests.txt. PDF and interactive UI checks are outside this logic audit.");
 }catch(Exception e){File.WriteAllText(Path.Combine(folder,"logic-audit-tests.txt"),e.ToString());Environment.ExitCode=1;Console.WriteLine(e.Message);}}
}
}
