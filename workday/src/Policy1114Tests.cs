using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using OfficeOpenXml;
namespace SchoolWorkdayCalculator {
internal static class Policy1114Tests {
 static int checks;static List<string> log=new List<string>();
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;log.Add("PASS "+text);}
 static Settings Example(bool nonConsent,int year=2026){return new Settings{Year=year,NonConsent=nonConsent,Summer=new DateRange(new DateTime(year,7,21),new DateTime(year,8,2),"여름방학"),Winter=new DateRange(new DateTime(year,12,21),new DateTime(year+1,2,DateTime.DaysInMonth(year+1,2)),"겨울방학"),Discretionary=new List<DateTime>(),DiscretionaryNames=new Dictionary<DateTime,string>(),Extras=new List<ExtraHoliday>()};}
 internal static void Run(string folder){Directory.CreateDirectory(folder);checks=0;log.Clear();try{Holidays.ResetForTest(Path.Combine(folder,"holiday-cache"));
  foreach(bool nonConsent in new[]{false,true}){
   var s=Example(nonConsent);var result=WorkbookBuilder.Calculate(s);Check(result.RegularPaidDays==365&&result.RegularWorkDays==241,"full-year paid and prescribed days "+nonConsent);
   Check(result.SeasonalPaidDays==(nonConsent?284:312)&&result.SeasonalWorkDays==(nonConsent?186:202),"approved baseline for consent/nonconsent "+nonConsent);
   var official=HolidayCalculator.GetFiscalHolidays(s.Year);var prescribed=new HashSet<DateTime>();if(!nonConsent){foreach(int i in new[]{21,22,23})prescribed.Add(new DateTime(2026,12,i));foreach(int i in new[]{22,23,24,25,26})prescribed.Add(new DateTime(2027,2,i));}
   Check(WorkbookBuilder.DayCode(s,new DateTime(2026,12,25),official,prescribed,true)==(nonConsent?"N":"H"),"vacation public holiday differs by consent "+nonConsent);
   Check(WorkbookBuilder.DayCode(s,new DateTime(2027,1,2),official,prescribed,true)=="N"&&WorkbookBuilder.DayCode(s,new DateTime(2027,1,3),official,prescribed,true)=="N","public holiday alone creates no weekend "+nonConsent);
   var original=result;s.Discretionary.Add(new DateTime(2026,10,16));var changed=WorkbookBuilder.Calculate(s);Check(CalendarPolicy.DiscretionaryCount(s)==1&&changed.RegularWorkDays==original.RegularWorkDays&&changed.SeasonalWorkDays==original.SeasonalWorkDays,"one discretionary day offsets one learning day "+nonConsent);
   s.Discretionary.Add(new DateTime(2026,10,16));Check(CalendarPolicy.DiscretionaryCount(s)==1,"duplicate discretionary date counted once "+nonConsent);s.Discretionary.Clear();
   foreach(var d in new[]{new DateTime(2026,5,5),new DateTime(2026,5,9)}){s.Discretionary.Add(d);bool rejected=false;try{WorkbookBuilder.Calculate(s);}catch(InvalidOperationException){rejected=true;}Check(rejected,"holiday/weekend cannot reduce learning leave "+d.ToString("yyyy-MM-dd")+" "+nonConsent);s.Discretionary.Clear();}
   foreach(int day in new[]{6,7,8,9,10})s.Discretionary.Add(new DateTime(2026,4,day));bool tooMany=false;try{WorkbookBuilder.Calculate(s);}catch(InvalidOperationException e){tooMany=e.Message.Contains("최대 4일");}Check(tooMany,"calculation entry rejects more than four discretionary holidays "+nonConsent);s.Discretionary.RemoveAt(4);Check(CalendarPolicy.DiscretionaryCount(s)==4,"exactly four discretionary holidays accepted "+nonConsent);s.Discretionary.Clear();
   string path=Path.Combine(folder,nonConsent?"review-nonconsent.xlsx":"review-consent.xlsx");WorkbookBuilder.Build(path,s);
   using (var book = new ExcelPackage(new FileInfo(path)))
   {
    var sheet = book.Workbook.Worksheets[WorkbookBuilder.PrescribedWorkdaySheet];
    Check(sheet.Dimension.End.Row == 366 && sheet.Cells["A1"].Text == "날짜" &&
          sheet.Cells["B1"].Text == "상시근로자" &&
          sheet.Cells["C1"].Text == (nonConsent ? "320일 비동의자" : "방중비 근로자"), "dated copy table headers and year length " + nonConsent);
    DateTime start = new DateTime(2026, 3, 1);
    int regular = 0, seasonal = 0;
    for (int i = 0; i < 365; i++)
    {
     var dateCell = sheet.Cells[i + 2, 1];
     int a = sheet.Cells[i + 2, 2].GetValue<int>(), b = sheet.Cells[i + 2, 3].GetValue<int>();
     if (dateCell.GetValue<DateTime>() != start.AddDays(i) || (a != 0 && a != 1) || (b != 0 && b != 1))
      throw new Exception("Invalid date or flag at " + dateCell.Address);
     regular += a;
     seasonal += b;
    }
    Check(regular == result.RegularWorkDays + 4 && seasonal == result.SeasonalWorkDays + 4, "cached day flags match both calendars before learning dates " + nonConsent);
   }
   using(var book=new ExcelPackage(new FileInfo(path))){Check(book.Workbook.Worksheets.Count==3,"two calendars and copy sheet "+nonConsent);foreach(var sheet in book.Workbook.Worksheets.Where(t=>t.Name!=WorkbookBuilder.PrescribedWorkdaySheet)){Check(sheet.Cells[5,8].Formula.Contains("\"L\""),"annual leave included in prescribed-day formula "+sheet.Name);Check(sheet.Cells[56,6].GetValue<int>()==4,"learning leave remains separate from annual leave "+sheet.Name);}Check(book.Workbook.VbaProject==null,"workbook contains no macros "+nonConsent);}
  }
  var labour=Example(true);labour.Summer=new DateRange(new DateTime(2026,4,27),new DateTime(2026,5,10),"노동절 방학");var holidays=HolidayCalculator.GetFiscalHolidays(2026);Check(NonConsentPolicy.Code(labour,new DateTime(2026,5,1),holidays)=="H","labour day is separately paid for nonconsent");Check(NonConsentPolicy.Code(labour,new DateTime(2026,5,2),holidays)=="N","labour day alone does not qualify Saturday");labour.Discretionary.Add(new DateTime(2026,5,4));holidays[new DateTime(2026,5,4)]="재량휴업일";Check(NonConsentPolicy.Code(labour,new DateTime(2026,5,4),holidays)=="H","discretionary holiday remains separately paid in nonconsent vacation");
  foreach(bool nc in new[]{false,true}){
   var baseSettings=Example(nc);var agreedSettings=Example(nc);agreedSettings.AgreedWork.Add(new DateTime(2027,1,12));
   var before=WorkbookBuilder.Calculate(baseSettings);var after=WorkbookBuilder.Calculate(agreedSettings);
   Check(after.SeasonalWorkDays==before.SeasonalWorkDays+1&&after.SeasonalPaidDays==before.SeasonalPaidDays+3,"agreed vacation day adds one work day and its paid weekend "+nc);
   var agreedDetail=WorkbookBuilder.Analyze(agreedSettings);
   Check(agreedDetail.CodeOn(new DateTime(2027,1,12))=="W"&&agreedDetail.CodeOn(new DateTime(2027,1,16))=="S"&&agreedDetail.CodeOn(new DateTime(2027,1,17))=="U"&&agreedDetail.Agreed.Contains(new DateTime(2027,1,12)),"agreed day detail "+nc);
   var inVacation=Example(nc);inVacation.Discretionary.Add(new DateTime(2026,12,24));bool blocked=false;try{WorkbookBuilder.Calculate(inVacation);}catch(InvalidOperationException ex){blocked=ex.Message.Contains("방학");}
   Check(blocked,"discretionary day inside vacation is rejected "+nc);
  }
  var holidayOnly=WorkbookBuilder.Analyze(Example(false));
  Check(holidayOnly.CodeOn(new DateTime(2027,2,6))=="S"&&holidayOnly.CodeOn(new DateTime(2027,2,7))=="U"&&holidayOnly.CodeOn(new DateTime(2027,2,13))=="N"&&holidayOnly.CodeOn(new DateTime(2027,2,14))=="N"&&holidayOnly.Result.SeasonalPaidDays==312,"weekend holidays in vacation are paid for consenting workers; plain weekends of holiday-only weeks stay unpaid");
  Check(Assembly.GetExecutingAssembly().GetName().Version.ToString()==AppRelease.FileVersion,"new assembly version");
  File.WriteAllText(Path.Combine(folder,"policy1114-tests.txt"),"PASS "+checks+" policy and workbook checks\n"+String.Join("\n",log));
 }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"policy1114-tests.txt"),"FAIL after "+checks+"\n"+String.Join("\n",log)+"\n"+ex);Environment.ExitCode=1;}}
}
}
