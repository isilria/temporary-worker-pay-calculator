using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
namespace ShortPay {
public static class Tests {
 static int count;static void Check(bool condition,string title){if(!condition)throw new Exception("TEST FAILED: "+title);count++;}
 public static void Run(string folder){Directory.CreateDirectory(folder);MainForm.SettingsTestFile=Path.Combine(folder,"isolated-settings.json");try{
  List<Job> jobs;using(var sr=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("jobs.json")))jobs=Engine.Parse<List<Job>>(sr.ReadToEnd());Check(jobs.Count==23,"23 jobs");
  var r=new Record();Engine.Fill(r);var z=Engine.Calculate(r,jobs[2]);Check(z.Ready&&z.Days==10&&z.WorkHours==80&&z.HolidayHours==16,"2 full weeks");Check(z.Hourly==11026.32m&&z.Gross==1058520&&z.Employment==9520&&z.Net==1049000,"original formula reconciliation");
  r.Days.Remove("2026-09-09");z=Engine.Calculate(r,jobs[2]);Check(z.Days==9&&z.Average==36&&z.HolidayHours==14.4m,"nonconsecutive prescribed days");
  Engine.Fill(r);r.Days["2026-09-09"]=2;z=Engine.Calculate(r,jobs[2]);Check(z.Days==9&&z.Average==40&&z.HolidayHours==8,"absence retains prescribed hours");
  r.Hours=2;z=Engine.Calculate(r,jobs[2]);Check(z.HolidayHours==0&&z.Average==10,"under 15 hours");
  r=new Record{Start=new DateTime(2026,9,9),End=new DateTime(2026,9,18)};Engine.Fill(r);z=Engine.Calculate(r,jobs[2]);Check(z.Ready&&z.HolidayHours==8&&z.Weeks.Count==2&&!z.Weeks[1].Complete,"Wednesday start full week plus remainder");r.Override=true;r.HolidayHours=8;r.ReviewNote="검토 예시";z=Engine.Calculate(r,jobs[2]);Check(z.Ready&&z.HolidayHours==8,"manual reviewed override");r.ReviewNote="";Check(!Engine.Calculate(r,jobs[2]).Ready,"override evidence required");
  r=new Record{Start=new DateTime(2026,9,1),End=new DateTime(2026,10,1)};Check(!Engine.Calculate(r,jobs[2]).Ready,"month boundary rejected");
  r=new Record{Start=new DateTime(2026,9,28),End=new DateTime(2026,10,11)};Engine.Fill(r);z=Engine.Calculate(r,jobs[2]);Check(z.Days==10&&z.HolidayHours==16&&z.Ready,"crossmonth full weeks");
  r=new Record();Engine.Fill(r);r.Separate=true;Check(!Engine.Calculate(r,jobs[2]).Ready,"separate employment review");r.Separate=false;r.Days["2026-09-09"]=3;Check(!Engine.Calculate(r,jobs[2]).Ready,"paid leave review");
  r=new Record();Engine.Fill(r);Check(!Engine.Calculate(r,jobs[20]).Ready,"special job review");
  r.Days["2026-09-12"]=1;Check(!Engine.Calculate(r,jobs[2]).Ready,"over 40 hours review");
  r=new Record{Name="홍길동",Account="001-012345",Identity="900101-1234567",IdentityMode="주민등록번호",Institution="테스트학교"};Engine.Fill(r);r.Tax=1500;r.LocalTax=150;r.Employment=false;z=Engine.Calculate(r,jobs[2]);Check(z.Deductions==1650&&z.Net==z.Gross-1650,"manual deductions");
  string path=Path.Combine(folder,"roundtrip.stpay");Engine.Save(path,r);var loaded=Engine.Load(path);Check(loaded.Account==r.Account&&loaded.Days.Count==10&&loaded.Identity==r.Identity,"save roundtrip");r.Name="수정";Engine.Save(path,r);Check(Engine.Load(path).Name=="수정","atomic overwrite");
  r.Name="홍길동<script>";string html=Reports.Html(r,jobs[2],z,"임금산정표");Check(html.Contains("&lt;script&gt;")&&!html.Contains("900101-1234567")&&html.Contains("001-012345"),"report encoding and identifiers");File.WriteAllText(Path.Combine(folder,"sample-report.html"),html);
  HolidayRegression(jobs,folder);GuardTests.Run(jobs,folder);ScheduleTests.Run(jobs,Path.Combine(folder,"schedule"));
  var shortRecord=new Record{Start=new DateTime(2026,9,7),End=new DateTime(2026,9,7),Allowance=70000};Engine.Fill(shortRecord);var shortResult=Engine.Calculate(shortRecord,jobs[11]);Check(shortResult.Average==8&&shortResult.OrdinaryHourly==10260.77m&&shortResult.Hourly==10320,"one-day under15 excludes meal and job allowance");
  shortRecord.End=new DateTime(2026,9,13);shortRecord.Days.Clear();shortRecord.Days["2026-09-07"]=1;shortRecord.Days["2026-09-09"]=1;shortRecord.Hours=7.5m;shortResult=Engine.Calculate(shortRecord,jobs[11]);Check(shortResult.Average==15&&shortResult.OrdinaryHourly>shortRecord.Minimum,"exactly fifteen includes eligible allowances");
  using(var form=new MainForm()){form.CaptureScreen(Path.Combine(folder,"main-screen.png"));form.ExerciseUI(folder);form.ExerciseHolidayUI(folder);}
  File.WriteAllText(Path.Combine(folder,"test-result.txt"),"PASS "+count+" engine/report checks; see ui-test-result.txt for UI checks");
 }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"test-result.txt"),ex.ToString());Environment.ExitCode=1;}}
 static void HolidayRegression(List<Job> jobs,string folder){
  var j=jobs[2];
  for(int offset=0;offset<7;offset++){var a=new Record{Start=new DateTime(2026,9,7).AddDays(offset),Hours=7.5m};a.End=a.Start.AddDays(6);Engine.Fill(a);var v=Engine.Calculate(a,j);Check(v.Ready&&v.HolidayHours==7.5m&&v.Weeks[0].Start==a.Start,"all seven contract start weekdays "+offset);}
  var r=new Record{Start=new DateTime(2026,9,8),End=new DateTime(2026,9,13)};Engine.Fill(r);Check(Engine.Calculate(r,j).HolidayHours==0,"Tuesday through Sunday only six days");r.End=new DateTime(2026,9,14);Engine.Fill(r);Check(Engine.Calculate(r,j).HolidayHours==8,"Tuesday through Monday seven days");r.End=new DateTime(2026,9,20);Engine.Fill(r);Check(Engine.Calculate(r,j).HolidayHours==8,"13 days only one full cycle");r.End=new DateTime(2026,9,21);Engine.Fill(r);Check(Engine.Calculate(r,j).HolidayHours==16,"14 days two cycles");
  r=new Record{Start=new DateTime(2026,5,4),End=new DateTime(2026,5,10)};r.Days.Clear();foreach(int d in new[]{4,6,8})r.Days["2026-05-"+d.ToString("00")]=1;var z=Engine.Calculate(r,j);Check(z.ReferenceDays==5&&z.ReferenceHours==24&&z.HolidayHours==4.8m,"three days x eight hours divided by five");
  r.End=new DateTime(2026,5,13);r.Days.Clear();foreach(int d in new[]{4,5,7,12,13})r.Days["2026-05-"+d.ToString("00")]=1;z=Engine.Calculate(r,j);Check(z.ReferenceDays==8&&z.ReferenceHours==40&&z.HolidayHours==5&&z.Ready,"education printed p68 40 hours divided by eight weekdays");r.Name="교육자료 비례 예시";File.WriteAllText(Path.Combine(folder,"education-example.html"),Reports.Html(r,j,z,"임금산정표"));
  r.Hours=7.5m;z=Engine.Calculate(r,j);Check(z.HolidayHours==4.6875m,"education printed p67 prior agreed scattered work");r.Separate=true;Check(!Engine.Calculate(r,j).Ready,"separate later agreements stay review");
  r=new Record{Start=new DateTime(2026,5,5),End=new DateTime(2026,5,14),Hours=7.5m};foreach(int d in new[]{5,12,14})r.Days["2026-05-"+d.ToString("00")]=1;z=Engine.Calculate(r,j);Check(z.HolidayHours==0&&z.Weeks[0].Status.Contains("15시간")&&!z.Weeks[1].Complete,"education p67 first week under fifteen and remainder under seven days");
  r=new Record{Start=new DateTime(2026,9,8),End=new DateTime(2026,9,21)};Engine.Fill(r);r.Days["2026-09-14"]=2;z=Engine.Calculate(r,j);Check(z.HolidayHours==8&&z.ReferenceHours==80&&z.Weeks[0].Holiday==0,"Monday absence belongs to Tuesday-origin first week");
  r=new Record{Start=new DateTime(2026,9,8),End=new DateTime(2026,9,14),Hours=7.5m};r.Days["2026-09-08"]=1;r.Days["2026-09-10"]=1;Check(Engine.Calculate(r,j).HolidayHours==3,"exactly fifteen qualifies");r.Hours=7.49m;Check(Engine.Calculate(r,j).HolidayHours==0,"just below fifteen excluded");
  r=new Record{Start=new DateTime(2026,9,8),End=new DateTime(2026,9,14),Hours=4};Engine.Fill(r);z=Engine.Calculate(r,j);Check(z.ReferenceHours==20&&z.ReferenceDays==5&&z.HolidayHours==4,"four-hour worker five days earns four holiday hours");r.Days.Remove("2026-09-09");z=Engine.Calculate(r,j);Check(z.HolidayHours==3.2m,"four-hour worker four days earns 3.2 holiday hours");r.Days.Remove("2026-09-10");Check(Engine.Calculate(r,j).HolidayHours==0,"four-hour worker three days under fifteen");
  r=new Record{Start=new DateTime(2026,9,2),End=new DateTime(2026,9,30)};Engine.Fill(r);Check(Engine.ContractWarning(r)==""&&Engine.Calculate(r,j).ReferenceDays==20,"29-day shorter-than-month first four-week reference");r.End=new DateTime(2026,10,1);Check(Engine.ContractWarning(r).StartsWith("계약기간이 1개월입니다")&&!Engine.Calculate(r,j).Ready,"education p106 exact month September 2 to October 1");r.End=new DateTime(2026,10,2);Check(Engine.ContractWarning(r)=="계약기간이 1개월을 초과하였습니다. 월급제 근로자로 인건비를 계산하시기 바랍니다.","requested over-month warning");
  bool rejected=false;try{Reports.Html(r,j,Engine.Calculate(r,j),"임금산정표");}catch(InvalidOperationException){rejected=true;}Check(rejected,"monthly contract report blocked");
  Check(Engine.OneMonthEnd(new DateTime(2026,1,31))==new DateTime(2026,2,28),"shorter destination month end");Check(Engine.OneMonthEnd(new DateTime(2024,2,1))==new DateTime(2024,2,29),"leap February month boundary");
  r=new Record{Version=1,Override=true,HolidayHours=77,ReviewNote="old manual result"};Engine.Fill(r);string old=Path.Combine(folder,"legacy.stpay");Engine.Save(old,r);var migrated=Engine.Load(old);Check(migrated.Version==4&&!migrated.Override&&Engine.Calculate(migrated,j).HolidayHours==16,"test1 saved file clears old manual confirmation");
 }
}
}
