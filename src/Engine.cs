using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace ShortPay {
public class Job { public string name; public decimal @base; public decimal meal; public Dictionary<string,decimal> allowances; public bool special; }
public class Record {
 public string Recipient="",Manager="";public int RequestRound=1,OutputYear=0,OutputMonth=0;public DateTime? PaymentDate;public bool ExtraIsAdjustment=false;
 public decimal WeekendHours=12,NightDays=0,PublicHolidayDays=0,NightHoursPerDay=1.5m;public string EmploymentExclusionReason="";
 public int UnmannedDay=6; // Saturday=6, Sunday=0; monthly occurrences 1-4
 public int Version=4; public string Id=Guid.NewGuid().ToString(); public string Name="",Identity="",IdentityMode="생년월일",Bank="",Account="",Holder="",Reason="",Institution="",Contact="",OriginalWorker="";
 public int JobIndex=2,Theme=0; public DateTime Start=new DateTime(2026,9,7),End=new DateTime(2026,9,20); public decimal Hours=8;
 public Dictionary<string,int> Days=new Dictionary<string,int>(); // 1 planned and attended, 2 absence, 3 paid leave (requires review)
 public bool Separate=false,Override=false,RateConfirmed=false,Employment=true; public decimal HolidayHours=0,HourlyOverride=0,Minimum=10320,Rate=.9m,MonthlyBase=2144500,Meal=160000,Allowance=0,Overtime=0,Within=0,ExtraPay=0;
 public decimal Food=0,Tax=0,LocalTax=0,Health=0,Care=0,Pension=0,Other=0; public string ReviewNote="";
}
public class Week { public string Period,Status; public DateTime Start,End; public bool Complete; public int PlannedDays; public decimal Planned,Actual,Holiday; }
public class Result {
 public bool Guard;
 public List<Week> Weeks=new List<Week>(); public List<string> Warnings=new List<string>(); public int Days,ReferenceDays; public DateTime ReferenceEnd; public decimal ReferenceHours,HolidayUnit; public decimal NightPay,PublicHolidayPay; public decimal WorkHours,PaidLeave,Average,HolidayHours,OrdinaryHourly,Hourly,BasePay,HolidayPay,WorkAndHoliday,OvertimePay,Gross,Employment,Deductions,Net,MonthlyHours; public bool Ready;
}
public static class Engine {
 public static void ValidateExport(Record r,Job j,Result z){if(!r.Employment&&String.IsNullOrWhiteSpace(r.EmploymentExclusionReason))throw new InvalidOperationException("고용보험 적용 제외 사유를 입력하세요.");if(IsGuard(j)&&(r.NightDays<0||r.NightDays>z.Days||r.PublicHolidayDays<0||r.PublicHolidayDays>z.Days))throw new InvalidOperationException("야간·공휴일 지급일은 실제 근무일수 이내로 입력하세요.");}
 public static bool IsGuard(Job j){return j.name.Contains("당직전담실무원");}
 public static decimal DayHours(Record r,Job j,DateTime d){return IsGuard(j)&&(d.DayOfWeek==DayOfWeek.Saturday||d.DayOfWeek==DayOfWeek.Sunday)?r.WeekendHours:r.Hours;}
 public static string Key(DateTime d){return d.ToString("yyyy-MM-dd");}
 public static decimal Floor10(decimal x){return Math.Floor(x/10)*10;}
 public static DateTime OneMonthEnd(DateTime start){var next=start.Date.AddMonths(1);return next.Day<start.Day?next:next.AddDays(-1);}
 public static string ContractWarning(Record r){
  if(r.End.Date<r.Start.Date)return "계약 종료일은 시작일보다 빠를 수 없습니다.";
  var boundary=OneMonthEnd(r.Start);
  if(r.End.Date>boundary)return "계약기간이 1개월을 초과하였습니다. 월급제 근로자로 인건비를 계산하시기 바랍니다.";
  if(r.End.Date==boundary)return "계약기간이 1개월입니다. 월급제 근로자로 인건비를 계산하시기 바랍니다.";
  return "";
 }
 public static bool IsFestivalHoliday(DateTime date){
  var lunar=new System.Globalization.KoreanLunisolarCalendar();
  // Compare the day before, day of and day after lunar New Year / Chuseok.
  for(int offset=-1;offset<=1;offset++){
   var d=date.Date.AddDays(offset);int year=lunar.GetYear(d),month=lunar.GetMonth(d),leap=lunar.GetLeapMonth(year);
   bool leapMonth=leap>0&&month==leap;if(leap>0&&month>=leap)month--;
   int day=lunar.GetDayOfMonth(d);
   if(!leapMonth&&((month==1&&day==1)||(month==8&&day==15)))return true;
  }
  return false;
 }
 public static bool IsUnmannedDay(Record r,DateTime d){return (int)d.DayOfWeek==r.UnmannedDay&&d.Day<=28;}
 public static void Fill(Record r){Fill(r,null);}
 public static void Fill(Record r,Job j){
  r.Days.Clear();if(r.End<r.Start||(r.End-r.Start).TotalDays>366)return;
  bool guard=j!=null?IsGuard(j):r.JobIndex==21||r.JobIndex==22;
  bool two=j!=null?j.name.Contains("2인"):r.JobIndex==22;
  for(var d=r.Start.Date;d<=r.End.Date;d=d.AddDays(1)){
   bool work=guard?(!two||(d-r.Start.Date).Days%2==0):d.DayOfWeek!=DayOfWeek.Saturday&&d.DayOfWeek!=DayOfWeek.Sunday;
   if(guard&&(IsUnmannedDay(r,d)||IsFestivalHoliday(d)))work=false;
   if(work)r.Days[Key(d)]=1;
  }
 }
 public static decimal NightHours(Record r){return r.NightDays*r.NightHoursPerDay;}
 public static Result Calculate(Record r,Job j){
  bool guard=IsGuard(j);var z=new Result{Guard=guard}; int span=(r.End.Date-r.Start.Date).Days+1;
  string contractWarning=ContractWarning(r);if(contractWarning!=""){z.Warnings.Add(contractWarning);return z;}
  if(r.Hours<=0||r.Hours>(guard?24:8)||guard&&(r.WeekendHours<=0||r.WeekendHours>24)){z.Warnings.Add(guard?"당직 소정근로시간은 0시간 초과~24시간입니다.":"일 소정근로시간은 0시간 초과~8시간입니다.");return z;}
  var valid=new Dictionary<DateTime,int>(); foreach(var kv in r.Days){DateTime d;if(DateTime.TryParseExact(kv.Key,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out d)&&d>=r.Start.Date&&d<=r.End.Date&&kv.Value>=1&&kv.Value<=3)valid[d]=kv.Value;}
  z.Days=valid.Count(x=>x.Value==1);z.WorkHours=valid.Where(x=>x.Value==1).Sum(x=>DayHours(r,j,x.Key));z.PaidLeave=valid.Where(x=>x.Value==3).Sum(x=>DayHours(r,j,x.Key));
  // Education p.68: prescribed hours / ordinary worker's weekdays in the SAME period.
  // First four weeks; use the entire contract when shorter than four weeks.
  z.ReferenceEnd=r.End.Date<r.Start.Date.AddDays(27)?r.End.Date:r.Start.Date.AddDays(27);
  z.ReferenceHours=valid.Where(x=>x.Key<=z.ReferenceEnd).Sum(x=>DayHours(r,j,x.Key));
  for(var d=r.Start.Date;d<=z.ReferenceEnd;d=d.AddDays(1))if(d.DayOfWeek!=DayOfWeek.Saturday&&d.DayOfWeek!=DayOfWeek.Sunday)z.ReferenceDays++;
  z.Average=z.ReferenceHours/Math.Ceiling(((z.ReferenceEnd-r.Start.Date).Days+1)/7m);
  z.HolidayUnit=!guard&&z.ReferenceDays>0?Math.Min(8,z.ReferenceHours/z.ReferenceDays):0;
  if(r.Separate)z.Warnings.Add("날짜별 별도 계약: 근로관계의 계속 여부를 확인하세요.");
  if(j.special&&!guard)z.Warnings.Add("특수운영직군: 시급·주휴·근로시간 기준을 별도로 확인하세요.");
  if(valid.Any(x=>x.Value==3))z.Warnings.Add("유급휴가/휴일 포함: 주휴 인정 및 유급시간을 확인하세요.");
  if(!guard&&valid.Keys.Any(x=>x.DayOfWeek==DayOfWeek.Sunday))z.Warnings.Add("일요일 근무 포함: 주휴일 변경 및 휴일근로수당을 확인하세요.");
  for(var w=r.Start.Date;w<=r.End.Date;w=w.AddDays(7)){
   var end=w.AddDays(6);bool complete=end<=r.End.Date;var shownEnd=complete?end:r.End.Date;var items=valid.Where(x=>x.Key>=w&&x.Key<=shownEnd).ToList();var a=new Week{Start=w,End=shownEnd,Complete=complete,PlannedDays=items.Count,Period=w.ToString("MM.dd")+" ~ "+shownEnd.ToString("MM.dd"),Planned=items.Sum(x=>DayHours(r,j,x.Key)),Actual=items.Where(x=>x.Value==1).Sum(x=>DayHours(r,j,x.Key))};
   if(guard)a.Status="당직 · 주휴 미산정";
   else if(a.Planned>40){z.Warnings.Add(a.Period+": 주 40시간 초과분을 연장근로로 분리하세요.");a.Status="시간 검토";}
   else if(!complete)a.Status="7일 미충족 ("+((shownEnd-w).Days+1)+"일)";
   else if(r.Separate||j.special||items.Any(x=>x.Value==3)||items.Any(x=>x.Key.DayOfWeek==DayOfWeek.Sunday))a.Status="기준 검토";
   else if(a.Planned<15)a.Status="해당 주 15시간 미만";
   else if(items.Any(x=>x.Value==2))a.Status="결근으로 미산정";
   else if(a.Actual==0)a.Status="근무 없음";
   else {a.Status="7일 유지 · 개근 · 비례 지급";a.Holiday=z.HolidayUnit;}
   z.Weeks.Add(a);
  }
  z.HolidayHours=z.Weeks.Sum(x=>x.Holiday);
  if(r.Override){z.HolidayHours=r.HolidayHours;if(String.IsNullOrWhiteSpace(r.ReviewNote))z.Warnings.Add("수동 확정 시 확인 근거를 입력하세요.");}
  if(r.Start.Year!=2026||r.End.Year!=2026)z.Warnings.Add("2026년 외 계약: 해당 연도의 단가와 적용 기준을 별도로 확인하세요.");
  z.MonthlyHours=guard?(j.name.Contains("1인")?289:144):Math.Round(r.Hours*6*4.345m,0,MidpointRounding.AwayFromZero);
  z.OrdinaryHourly=Math.Ceiling((((guard?r.MonthlyBase:r.MonthlyBase*r.Hours/8)+(z.Average>=15?r.Meal+(guard?0:r.Allowance):0))/z.MonthlyHours)*100)/100;
  z.Hourly=r.HourlyOverride>0?r.HourlyOverride:Math.Max(r.Minimum,z.OrdinaryHourly);
  z.BasePay=Floor10(z.Hourly*(z.WorkHours+z.PaidLeave));z.WorkAndHoliday=Floor10(z.Hourly*(z.WorkHours+z.PaidLeave+z.HolidayHours));z.HolidayPay=z.WorkAndHoliday-z.BasePay;
  z.OvertimePay=Floor10(z.Hourly*(r.Within+r.Overtime*(guard?1:1.5m)));
  if(guard){z.NightPay=Floor10(z.Hourly*(r.Within+r.Overtime+NightHours(r)*.5m))-z.OvertimePay;z.PublicHolidayPay=Floor10(r.MonthlyBase/z.MonthlyHours*3*r.PublicHolidayDays);}
  z.Gross=z.WorkAndHoliday+z.OvertimePay+z.NightPay+z.PublicHolidayPay+r.ExtraPay;
  z.Employment=r.Employment?Floor10(z.Gross*r.Rate/100):0;
  z.Deductions=z.Employment+r.Food+r.Tax+r.LocalTax+r.Health+r.Care+r.Pension+r.Other;z.Net=z.Gross-z.Deductions;
  bool reviewNeeded=r.Separate||(j.special&&!guard)||valid.Any(x=>x.Value==3)||(!guard&&valid.Keys.Any(x=>x.DayOfWeek==DayOfWeek.Sunday));
  z.Ready=(!reviewNeeded||(r.Override&&!String.IsNullOrWhiteSpace(r.ReviewNote)))&&(!j.special||guard||r.HourlyOverride>0)&&((r.Start.Year==2026&&r.End.Year==2026)||r.RateConfirmed)&&(guard||!z.Weeks.Any(x=>x.Planned>40))&&valid.Count>0;
  if(r.Override&&String.IsNullOrWhiteSpace(r.ReviewNote))z.Ready=false;
  if(!r.Employment&&String.IsNullOrWhiteSpace(r.EmploymentExclusionReason)){z.Ready=false;z.Warnings.Add("고용보험 적용 제외 사유를 입력하세요.");}
  if(guard&&(r.NightDays<0||r.NightDays>z.Days||r.PublicHolidayDays<0||r.PublicHolidayDays>z.Days||r.NightHoursPerDay<0||r.NightHoursPerDay>8)){z.Ready=false;z.Warnings.Add("야간·공휴일 지급일은 실제 근무일수 이내로 입력하세요.");}
  return z;
 }
 public static string Json(object o){return new JavaScriptSerializer().Serialize(o);}
 public static T Parse<T>(string s){return new JavaScriptSerializer().Deserialize<T>(s);}
 public static void Save(string path,Record r){string tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(tmp,Json(r),new UTF8Encoding(true));if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path);}finally{if(File.Exists(tmp))File.Delete(tmp);}}
 public static Record Load(string path){var r=Parse<Record>(File.ReadAllText(path,Encoding.UTF8));if(r!=null){r.Start=r.Start.Kind==DateTimeKind.Utc?r.Start.ToLocalTime().Date:r.Start.Date;r.End=r.End.Kind==DateTimeKind.Utc?r.End.ToLocalTime().Date:r.End.Date;}if(r!=null&&r.Version>=1&&r.Version<=3){if(r.JobIndex==12)r.JobIndex=11;else if(r.JobIndex>12)r.JobIndex--;if(r.Version==1)r.Override=false;r.Version=4;}if(r==null||r.Version!=4||(r.UnmannedDay!=0&&r.UnmannedDay!=6)||r.Days==null||r.JobIndex<0||r.JobIndex>=23||r.Theme<0||r.Theme>5||r.Hours<.25m||r.Hours>(r.JobIndex>=21?24:8)||r.WeekendHours<=0||r.WeekendHours>24||r.NightDays<0||r.NightDays>31||r.PublicHolidayDays<0||r.PublicHolidayDays>31||r.NightHoursPerDay<0||r.NightHoursPerDay>8||r.End<r.Start||r.Start.Year<2020||r.End.Year>2050)throw new InvalidDataException("지원하지 않거나 올바르지 않은 저장자료입니다.");
  if(r.RequestRound<1||r.RequestRound>4||r.OutputYear<0||r.OutputYear>2050||r.OutputMonth<0||r.OutputMonth>12)throw new InvalidDataException("출력 설정 범위가 올바르지 않습니다.");
  decimal[] amounts={r.MonthlyBase,r.Meal,r.Allowance,r.ExtraPay,r.Food,r.Tax,r.LocalTax,r.Health,r.Care,r.Pension,r.Other};
  if(amounts.Any(x=>x<0||x>100000000)||r.HolidayHours<0||r.HolidayHours>80||r.HourlyOverride<0||r.HourlyOverride>1000000||r.Minimum<0||r.Minimum>100000||r.Rate<0||r.Rate>100||r.Within<0||r.Within>300||r.Overtime<0||r.Overtime>300)throw new InvalidDataException("저장자료의 금액 또는 시간 범위가 올바르지 않습니다.");
  foreach(var kv in r.Days){DateTime d;if(!DateTime.TryParseExact(kv.Key,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out d)||d<r.Start.Date||d>r.End.Date||kv.Value<1||kv.Value>3)throw new InvalidDataException("저장자료의 근무일 정보가 올바르지 않습니다.");}
  if(r.Version==1){r.Override=false;r.Version=2;}return r;}
}
}
