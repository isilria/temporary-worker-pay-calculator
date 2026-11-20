using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
namespace SchoolWorkdayCalculator {
internal static class NonConsentPolicy {
 internal static bool InVacation(Settings s,DateTime d){return s.Summer.Contains(d)||s.Winter.Contains(d);}
 internal static bool Weekday(DateTime d){return d.DayOfWeek!=DayOfWeek.Saturday&&d.DayOfWeek!=DayOfWeek.Sunday;}
 internal static bool InYear(Settings s,DateTime d){return d>=new DateTime(s.Year,3,1)&&d<new DateTime(s.Year+1,3,1);}
 internal static bool PaidHoliday(Settings s,DateTime d,Dictionary<DateTime,string> holidays){return holidays.ContainsKey(d)&&(!InVacation(s,d)||(d.Month==5&&d.Day==1)||s.Discretionary.Contains(d));}
 internal static bool WeekHasTermWork(Settings s,DateTime d,Dictionary<DateTime,string> holidays){DateTime monday=d.AddDays(-(((int)d.DayOfWeek+6)%7));return Enumerable.Range(0,5).Select(i=>monday.AddDays(i)).Any(x=>InYear(s,x)&&((!InVacation(s,x)&&!holidays.ContainsKey(x))||Agreed(s,x,holidays)));}
 internal static bool Agreed(Settings s,DateTime d,Dictionary<DateTime,string> holidays){return s.AgreedWork!=null&&s.AgreedWork.Contains(d.Date)&&InVacation(s,d)&&Weekday(d)&&!holidays.ContainsKey(d);}
 internal static string Code(Settings s,DateTime d,Dictionary<DateTime,string> holidays){if(PaidHoliday(s,d,holidays))return "H";if(InVacation(s,d)){if(Agreed(s,d,holidays))return "W";if(!Weekday(d)&&WeekHasTermWork(s,d,holidays))return d.DayOfWeek==DayOfWeek.Saturday?"S":"U";return "N";}return Weekday(d)?"W":d.DayOfWeek==DayOfWeek.Saturday?"S":"U";}
 internal static int DiscretionaryCount(Settings s){return CalendarPolicy.DiscretionaryCount(s);}
}
internal static class CalendarPolicy {
 internal static bool ValidDiscretionary(int year,DateTime date,Dictionary<DateTime,string> official){return date>=new DateTime(year,3,1)&&date<new DateTime(year+1,3,1)&&NonConsentPolicy.Weekday(date)&&!official.ContainsKey(date);}
 internal static void ValidateDiscretionary(int year,IEnumerable<DateTime> dates){if(dates.Select(d=>d.Date).Distinct().Count()>4)throw new InvalidOperationException("재량휴업일은 개교기념일을 포함하여 학년도별 최대 4일입니다.");var official=HolidayCalculator.GetFiscalHolidays(year);foreach(var date in dates)if(!ValidDiscretionary(year,date,official))throw new InvalidOperationException("재량휴업일은 선택한 학년도 안에서 공휴일·주말과 겹치지 않는 날짜를 지정하세요. 학습휴가 차감 기준을 확인하세요.");}
 internal static int DiscretionaryCount(Settings s){var official=HolidayCalculator.GetFiscalHolidays(s.Year);return s.Discretionary.Distinct().Count(d=>ValidDiscretionary(s.Year,d,official));}
 internal static void ValidateDiscretionaryOutsideVacation(Settings s){
  foreach(var date in s.Discretionary.Select(d=>d.Date).Distinct()){
   DateRange range=s.Summer.Contains(date)?s.Summer:s.Winter.Contains(date)?s.Winter:null;
   if(range!=null)throw new InvalidOperationException(date.ToString("yyyy-MM-dd")+"은(는) "+range.Name+"("+range.Start.ToString("yyyy-MM-dd")+" ~ "+range.End.ToString("yyyy-MM-dd")+") 기간입니다. 방학 중에는 재량휴업일을 지정할 수 없습니다.");
  }
 }
}
}
