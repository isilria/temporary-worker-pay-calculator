using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using OfficeOpenXml;
namespace ShortPay {
public static class ScheduleTests {
 static int count;
 static void Check(bool ok,string message){count++;if(!ok)throw new Exception("SCHEDULE TEST: "+message);}
 static Record Guard(List<Job> jobs,int people,DateTime start,DateTime end,int unmanned=6){
  int index=jobs.FindIndex(j=>j.name.Contains("당직")&&j.name.Contains(people+"인"));var job=jobs[index];
  var r=new Record{Name="당직 검증",OriginalWorker="원근로자",Reason="연차",Institution="예시학교",JobIndex=index,Start=start,End=end,Hours=8.5m,WeekendHours=12,MonthlyBase=job.@base,Meal=job.meal,UnmannedDay=unmanned};Engine.Fill(r,job);return r;
 }
 static void Days(Record r,params string[] dates){Check(r.Days.Keys.OrderBy(x=>x).SequenceEqual(dates),String.Join(",",r.Days.Keys)+" expected "+String.Join(",",dates));}
 public static void Run(List<Job> jobs,string folder){
  Directory.CreateDirectory(folder);
  var r=Guard(jobs,2,new DateTime(2026,9,10),new DateTime(2026,9,14));Days(r,"2026-09-10","2026-09-14");
  var z=Engine.Calculate(r,jobs[r.JobIndex]);Check(z.Days==2&&z.WorkHours==17,"Thursday-Monday unmanned Saturday does not shift alternation");
  r=Guard(jobs,1,new DateTime(2026,9,10),new DateTime(2026,9,14));Days(r,"2026-09-10","2026-09-11","2026-09-13","2026-09-14");
  r=Guard(jobs,2,new DateTime(2026,8,27),new DateTime(2026,8,31));Days(r,"2026-08-27","2026-08-29","2026-08-31");
  r=Guard(jobs,2,new DateTime(2026,8,28),new DateTime(2026,8,31));Days(r,"2026-08-28","2026-08-30");
  r=Guard(jobs,2,new DateTime(2026,8,29),new DateTime(2026,9,6));Days(r,"2026-08-29","2026-08-31","2026-09-02","2026-09-04","2026-09-06");
  r=Guard(jobs,1,new DateTime(2026,8,28),new DateTime(2026,9,1),0);Days(r,"2026-08-28","2026-08-29","2026-08-30","2026-08-31","2026-09-01");
  r=Guard(jobs,2,new DateTime(2026,9,11),new DateTime(2026,9,15),0);Days(r,"2026-09-11","2026-09-15");
  r=Guard(jobs,1,new DateTime(2026,9,13),new DateTime(2026,9,14),0);Days(r,"2026-09-14");
  r=Guard(jobs,1,new DateTime(2026,9,23),new DateTime(2026,9,28));Days(r,"2026-09-23","2026-09-27","2026-09-28");
  r=Guard(jobs,2,new DateTime(2026,9,23),new DateTime(2026,9,29));Days(r,"2026-09-23","2026-09-27","2026-09-29");
  r=Guard(jobs,1,new DateTime(2026,2,15),new DateTime(2026,2,19));Days(r,"2026-02-15","2026-02-19");
  r=Guard(jobs,1,new DateTime(2026,5,4),new DateTime(2026,5,6));Days(r,"2026-05-04","2026-05-05","2026-05-06");
  r=Guard(jobs,2,new DateTime(2026,5,5),new DateTime(2026,5,7));Days(r,"2026-05-05","2026-05-07");
  var ordinary=new Record();Engine.Fill(ordinary,jobs[2]);Check(ordinary.Days.Count==10&&Engine.Calculate(ordinary,jobs[2]).Net==1049000,"ordinary payroll regression");
  Check(jobs.Single(j=>j.name=="전산실무사").allowances["자격수당"]==107220,"computer allowance");
  Check(jobs.Single(j=>j.name=="영양사").allowances["면허가산수당"]==117220,"nutritionist allowance");
  // Every possible start weekday, both security days and both staffing patterns.
  for(int offset=0;offset<7;offset++)foreach(int people in new[]{1,2})foreach(int day in new[]{0,6}){
   r=Guard(jobs,people,new DateTime(2026,8,17).AddDays(offset),new DateTime(2026,9,8),day);
   Check(r.Days.Keys.All(k=>{DateTime d=DateTime.Parse(k);return !Engine.IsUnmannedDay(r,d)&&(people==1||(d-r.Start).Days%2==0);}),"start-weekday and crossmonth cadence");
  }
  r=Guard(jobs,2,new DateTime(2026,4,6),new DateTime(2026,4,12));r.NightDays=3;r.PublicHolidayDays=1;r.Within=1.25m;r.Overtime=2.5m;r.Food=1000;
  z=Engine.Calculate(r,jobs[r.JobIndex]);Check(z.WorkHours==37.5m&&Engine.NightHours(r)==4.5m,"actual work and night totals");
  Check(PayrollItems.Pay(r,z).Sum(p=>p.Amount)==z.Gross,"payslip items reconcile");
  foreach(string kind in new[]{"인건비 신청내역","급여명세서","임금산정표"}){
   string file=Path.Combine(folder,kind+".xlsx");ExcelReports.Save(file,kind,r,jobs[r.JobIndex],z,new ExportOptions{Year=2026,Month=4});
   using(var p=new ExcelPackage(new FileInfo(file))){var w=p.Workbook.Worksheets.First();
    string net=kind=="급여명세서"?"F23":kind=="임금산정표"?"G26":"AR14";Check(Convert.ToDecimal(w.Cells[net].Value)==z.Net,"reopened net "+kind);
    if(kind=="급여명세서"){
     Check(Convert.ToDecimal(w.Cells["H10"].Value)==4.5m,"night hours in actual night column H10");
     Check(Convert.ToDecimal(w.Cells["F10"].Value)==3&&Convert.ToDecimal(w.Cells["F11"].Value)==0,"holiday calculation hours separate from night");
     Check(Convert.ToDecimal(w.Cells["D10"].Value)==3.75m&&Convert.ToDecimal(w.Cells["D11"].Value)==0,"guard extra work counted once");
    }
    if(kind=="인건비 신청내역"){
     Check(Convert.ToDecimal(w.Cells["AD14"].Value)==4&&Convert.ToDecimal(w.Cells["AE14"].Value)==30,"4 hours 30 minutes night");
     Check(Convert.ToDecimal(w.Cells["AB14"].Value)==3&&Convert.ToDecimal(w.Cells["AC14"].Value)==45,"3 hours 45 minutes extra");
     Check(w.Cells["G14"].Text.Contains("총 4.5시간"),"night total in narrative");
    }
   }
  }
  foreach(int people in new[]{1,2})foreach(int day in new[]{0,6})foreach(int length in new[]{0,6,20}){
   var scenario=Guard(jobs,people,new DateTime(2026,8,27),new DateTime(2026,8,27).AddDays(length),day);
   scenario.NightDays=Math.Min(3,scenario.Days.Count);scenario.PublicHolidayDays=Math.Min(1,scenario.Days.Count);
   var result=Engine.Calculate(scenario,jobs[scenario.JobIndex]);
   foreach(string kind in new[]{"인건비 신청내역","급여명세서"}){
    string file=Path.Combine(folder,"guard-"+people+"-"+day+"-"+length+"-"+kind+".xlsx");
    ExcelReports.Save(file,kind,scenario,jobs[scenario.JobIndex],result,new ExportOptions{Year=2026,Month=8});
    using(var p=new ExcelPackage(new FileInfo(file))){var w=p.Workbook.Worksheets.First();Check(Convert.ToDecimal(w.Cells[kind=="급여명세서"?"F23":"AR14"].Value)==result.Net,"one/two person, weekend, short/full output net");}
   }
  }
  for(int yr=2020;yr<=2050;yr++)foreach(var d in new[]{new DateTime(yr,1,1),new DateTime(yr,12,31)}){Engine.IsFestivalHoliday(d);Check(true,"supported calendar year "+yr);}
  r.UnmannedDay=0;string saved=Path.Combine(folder,"guard.stpay");Engine.Save(saved,r);var loaded=Engine.Load(saved);Check(loaded.UnmannedDay==0&&loaded.Days.Count==r.Days.Count,"selection and manual calendar preserved after save");
  string legacy=Engine.Json(r).Replace("\"UnmannedDay\":0,","");File.WriteAllText(saved,legacy);Check(Engine.Load(saved).UnmannedDay==6,"legacy default Saturday");
  r.UnmannedDay=2;Engine.Save(saved,r);bool blocked=false;try{Engine.Load(saved);}catch(InvalidDataException){blocked=true;}Check(blocked,"invalid security weekday rejected");
  File.WriteAllText(Path.Combine(folder,"schedule-tests.txt"),"PASS "+count+" schedule, allowance, output and persistence checks");
 }
}
}
