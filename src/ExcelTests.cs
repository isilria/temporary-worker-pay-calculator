using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using OfficeOpenXml;
namespace ShortPay {
public static class ExcelTests {
 static int count;static void Check(bool b,string label){count++;if(!b)throw new Exception("EXCEL TEST: "+label);}
 public static void Run(string folder){Directory.CreateDirectory(folder);try{
  List<Job> jobs;using(var reader=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("jobs.json")))jobs=Engine.Parse<List<Job>>(reader.ReadToEnd());
  var opt=new ExportOptions{Year=2026,Month=9,Round=3,Recipient="가123",Manager="이담당",PaymentDate=new DateTime(2026,9,25),ExtraIsAdjustment=false};
  var scenarios=new List<Record>{
   new Record{Start=new DateTime(2026,9,9),End=new DateTime(2026,9,10)},
   new Record{Start=new DateTime(2026,9,7),End=new DateTime(2026,9,20),Within=1.25m,Overtime=2.5m,Food=15000,Tax=1540,LocalTax=150,Health=25340,Care=3280,Pension=32500,ExtraPay=100},
   new Record{Start=new DateTime(2026,9,7),End=new DateTime(2026,9,13)},
   new Record{Start=new DateTime(2026,9,28),End=new DateTime(2026,10,4)},
   new Record{Start=new DateTime(2026,9,7),End=new DateTime(2026,9,7)},
   new Record{Start=new DateTime(2026,6,29),End=new DateTime(2026,7,15)}
  };
  for(int k=0;k<scenarios.Count;k++){
   var r=scenarios[k];r.Name="김대체";r.Identity="900101-1234567";r.OriginalWorker="홍길동";r.Reason="특별휴가";r.Bank="농협은행";r.Account="001-0023-004567";r.Holder="김대체";r.Institution="인천예시초등학교";r.Contact="이담당";r.JobIndex=11;r.Allowance=70000;Engine.Fill(r);
   if(k==2){r.Days.Remove("2026-09-08");r.Days.Remove("2026-09-10");}
   var z=Engine.Calculate(r,jobs[11]);var dir=Path.Combine(folder,"case-"+k);Directory.CreateDirectory(dir);
   foreach(string kind in new[]{"임금산정표","급여명세서","인건비 신청내역"}){
    var path=Path.Combine(dir,kind+".xlsx");ExcelReports.Save(path,kind,r,jobs[11],z,opt);
    using(var pkg=new ExcelPackage(new FileInfo(path))){var ws=pkg.Workbook.Worksheets.First();string total=kind=="급여명세서"?"F23":kind=="임금산정표"?"G26":"AR14";Check(Convert.ToDecimal(ws.Cells[total].Value)==z.Net,"reopen net "+k+kind);Check(ws.Cells[total].Formula!="","formula preserved "+k+kind);
     if(kind=="급여명세서"){Check(ws.Cells["D6"].Text==r.Name&&Convert.ToDecimal(ws.Cells["D8"].Value)==z.Days,"name and worked days");Check(ws.Cells["F6"].Text=="1990.01.01","birthday not full ID");Check(ws.Cells["F8"].Formula=="ROUND((H7*365/12/7),0)","original monthly hours formula");for(int rr=17;rr<=21;rr++){bool payBlank=String.IsNullOrEmpty(ws.Cells["F"+rr].Text);Check(payBlank?(ws.Cells["C"+rr].Text==""&&ws.Cells["D"+rr].Text==""):Convert.ToDecimal(ws.Cells["F"+rr].Value)!=0,"zero pay rows blank");bool deductBlank=String.IsNullOrEmpty(ws.Cells["H"+rr].Text);Check(deductBlank?ws.Cells["G"+rr].Text=="":Convert.ToDecimal(ws.Cells["H"+rr].Value)!=0,"zero deduction rows blank");}}
     if(kind=="인건비 신청내역"){Check(ws.Cells["G14"].Text.Contains("홍길동의 특별휴가 대체")&&ws.Cells["G14"].Text.Replace("\r", "").Contains("\n주15시간"),"narrative single line break and calculation");Check(ws.Cells["I14"].Text==r.Account,"account leading zeros");Check(!ws.Cells["G14"].Text.Contains("\n\n"),"no blank line");if(k==5)Check(ws.Cells["G14"].Text.Contains("8시간×15일"),"holiday included in 15 paid days");Check(ws.Cells["B6"].Text=="(예시)","examples retained");Check(pkg.Workbook.Worksheets.Count==2,"bank sheet retained");Check(ws.Protection.IsProtected,"sheet protection retained");Check(Convert.ToDecimal(ws.Cells["AR2"].Value)==z.Net,"summary recalculated");}
    }
   }
  }
  var mismatch=scenarios[0];mismatch.HourlyOverride=20000;bool blocked=false;string conflict=Path.Combine(folder,"must-not-write.xlsx");try{ExcelReports.Save(conflict,"인건비 신청내역",mismatch,jobs[11],Engine.Calculate(mismatch,jobs[11]),opt);}catch(InvalidOperationException e){blocked=e.Message.Contains("다릅니다");}Check(blocked&&!File.Exists(conflict),"mismatched formula not overwritten or saved");
  var careJob=jobs.FindIndex(x=>x.name=="초등돌봄전담사");var care=jobs[careJob];
  var request=new Record{Name="가상대체",OriginalWorker="홍길동",Reason="연차",Institution="가상학교",JobIndex=careJob,Start=new DateTime(2026,10,15),End=new DateTime(2026,10,20),MonthlyBase=care.@base,Meal=care.meal,Allowance=care.allowances.Where(x=>x.Key!="기관근무수당").Sum(x=>x.Value)};Engine.Fill(request,care);var requestResult=Engine.Calculate(request,care);
  string requestPath=Path.Combine(folder,"신청서_표기수정_예시.xlsx");ExcelReports.Save(requestPath,"인건비 신청내역",request,care,requestResult,new ExportOptions{Year=2026,Month=10});
  using(var p=new ExcelPackage(new FileInfo(requestPath))){var ws=p.Workbook.Worksheets.First();Check(ws.Cells["G14"].Text.Replace("\r","").Split('\n')[0]=="초등돌봄전담사 원근로자 홍길동의 연차 대체(2026. 10. 15.~10. 20.)","actual request job / original-worker label / name order and same-year end date");Check(Convert.ToDecimal(ws.Cells["AR14"].Value)==requestResult.Net&&ws.Cells["AR14"].Formula!="","request narrative change preserves formula and net amount");Check(requestResult.WorkAndHoliday==352840&&requestResult.HolidayHours==0,"six-day example money remains unchanged");}
  request.Start=new DateTime(2026,9,28);request.End=new DateTime(2026,10,4);Check(ExcelReports.Narrative(request,care,requestResult).Split('\n')[0]=="초등돌봄전담사 원근로자 홍길동의 연차 대체(2026. 9. 28.~10. 4.)","same year across months omits only end year");
  request.Start=new DateTime(2026,12,28);request.End=new DateTime(2027,1,3);Check(ExcelReports.Narrative(request,care,requestResult).Split('\n')[0]=="초등돌봄전담사 원근로자 홍길동의 연차 대체(2026. 12. 28.~2027. 1. 3.)","different end year remains explicit");
  request.Start=request.End=new DateTime(2026,10,15);Check(ExcelReports.Narrative(request,care,requestResult).Split('\n')[0].EndsWith("(2026. 10. 15.~10. 15.)"),"single-day range keeps same-year formatting");
  File.WriteAllText(Path.Combine(folder,"excel-tests.txt"),"PASS "+count+" export and reopen checks");
 }catch(Exception e){File.WriteAllText(Path.Combine(folder,"excel-tests.txt"),e.ToString());Environment.ExitCode=1;}}
}
}
