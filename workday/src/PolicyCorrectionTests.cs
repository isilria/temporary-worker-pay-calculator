using System;using System.Collections.Generic;using System.IO;using System.Linq;using OfficeOpenXml;
namespace SchoolWorkdayCalculator {
internal sealed partial class MainForm {
 void CheckPolicyCorrections(string folder){int count=0;Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception("POLICY: "+message);count++;};
  foreach(bool nonConsent in new[]{false,true}){
   year.Value=2026;SelectWorkerMode(nonConsent);foreach(var picker in discretionary)picker.Checked=false;
   var clean=BuildSettings(false);var initial=WorkbookBuilder.Calculate(clean);check(initial.RegularWorkDays==241&&initial.SeasonalWorkDays==(nonConsent?186:202),"valid baseline retained "+nonConsent);
   foreach(var invalid in new[]{new DateTime(2026,5,5),new DateTime(2026,5,9)}){discretionary[0].Value=invalid;discretionary[0].Checked=true;bool rejected=false;try{BuildSettings(false);}catch(Exception ex){rejected=ex.Message.Contains("공휴일·주말");}check(rejected,"both UI modes reject invalid discretionary date "+invalid+" "+nonConsent);discretionary[0].Checked=false;
    clean.Discretionary.Add(invalid);rejected=false;try{WorkbookBuilder.Calculate(clean);}catch(InvalidOperationException){rejected=true;}check(rejected,"calculation entry rejects invalid date "+nonConsent);clean.Discretionary.Clear();}
   discretionary[0].Value=new DateTime(2026,10,16);discretionary[0].Checked=true;var valid=BuildSettings(false);check(CalendarPolicy.DiscretionaryCount(valid)==1,"same valid count in both modes");var result=WorkbookBuilder.Calculate(valid);check(result.RegularWorkDays==initial.RegularWorkDays&&result.SeasonalWorkDays==initial.SeasonalWorkDays,"valid discretionary/learning compensation retained");discretionary[0].Checked=false;
  }
  SelectWorkerMode(false);var settings=BuildSettings(false);WorkbookBuilder.Build(Path.Combine(folder,"policy-consent.xlsx"),settings);
  using(var book=new ExcelPackage(new FileInfo(Path.Combine(folder,"policy-consent.xlsx")))){
   check(book.Workbook.Worksheets.Count==3,"consent exports two calendar tabs and copy sheet");
   var seasonal=book.Workbook.Worksheets["방중비 근로자"];
   check(seasonal.Cells[Helper(new DateTime(2026,12,26))].Text=="S"&&seasonal.Cells[Helper(new DateTime(2026,12,25))].Text=="H","vacation weekend with paid work stays paid and weekday holiday stays paid");
   check(seasonal.Cells[Helper(new DateTime(2027,2,6))].Text=="N"&&seasonal.Cells[Helper(new DateTime(2027,2,7))].Text=="N","holiday-only vacation weekend is unpaid");
   check(book.Workbook.VbaProject==null,"workbook contains no macros");
  }
  settings.Summer=new DateRange(new DateTime(2026,9,21),new DateTime(2026,10,4),"휴일 중복 검증용 방학");WorkbookBuilder.Build(Path.Combine(folder,"policy-holiday-weekend.xlsx"),settings);
  year.Value=2026;SelectWorkerMode(false);File.WriteAllText(Path.Combine(folder,"policy-correction-tests.txt"),"PASS "+count+" shared discretionary validation, learning-leave parity, four-tab metadata and holiday protection checks");
 }
}
}
