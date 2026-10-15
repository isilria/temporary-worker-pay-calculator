using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace ShortPay {
public static class WorkPlan {
 public static int DayCount(int mask){return Enumerable.Range(0,7).Count(d=>(mask&(1<<d))!=0);}
 public static bool Includes(int mask,DateTime date){return (mask&(1<<(int)date.DayOfWeek))!=0;}
 public static string Period(Record r){return Engine.Key(r.Start)+"/"+Engine.Key(r.End);}
 static IEnumerable<DateTime> Dates(Record r){return r.End>=r.Start&&(r.End-r.Start).Days<=366?Enumerable.Range(0,(r.End.Date-r.Start.Date).Days+1).Select(i=>r.Start.Date.AddDays(i)):Enumerable.Empty<DateTime>();}
 static Dictionary<DateTime,int> InRange(Record r,Dictionary<string,int> source){var result=new Dictionary<DateTime,int>();if(source==null)return result;foreach(var item in source){DateTime day;if(item.Value==1&&DateTime.TryParseExact(item.Key,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out day)&&day>=r.Start.Date&&day<=r.End.Date)result[day]=1;}return result;}
 public static Dictionary<DateTime,int> Contract(Record r){return r.ScheduleMode==0?Dates(r).Where(d=>Includes(r.ContractWeekdays,d)).ToDictionary(d=>d,d=>1):InRange(r,r.ContractDays);}
 public static Dictionary<DateTime,int> Comparison(Record r){if(r.ScheduleMode==0)return Dates(r).Where(d=>Includes(r.NormalWeekdays,d)).ToDictionary(d=>d,d=>1);return r.ComparisonPeriod==Period(r)?InRange(r,r.ComparisonDays):Dates(r).Where(d=>Includes(r.NormalWeekdays,d)&&Holidays.Name(d)=="").ToDictionary(d=>d,d=>1);}
 public static void PrepareComparison(Record r,bool reset=false){if(reset||r.ComparisonPeriod!=Period(r)){r.ComparisonDays=Dates(r).Where(d=>Includes(r.NormalWeekdays,d)&&Holidays.Name(d)=="").ToDictionary(d=>Engine.Key(d),d=>1);r.ComparisonPeriod=Period(r);}}
 public static string[] Problems(Record r){var problems=new List<string>();if(r.ScheduleMode<0||r.ScheduleMode>1||r.ContractWeekdays<1||r.ContractWeekdays>127||r.NormalWeekdays<1||r.NormalWeekdays>127||r.WeeklyRestDay<0||r.WeeklyRestDay>6||r.NormalDayHours<=0||r.NormalDayHours>8)problems.Add("계약·통상근로자·주휴일의 설정 범위를 확인하세요.");if(Contract(r).Keys.Any(d=>(int)d.DayOfWeek==r.WeeklyRestDay))problems.Add("약정 근무일과 주휴일이 겹칩니다. 계약서의 근무요일·주휴일을 확인하세요.");if(r.ScheduleMode==0&&r.Hours*DayCount(r.ContractWeekdays)>r.NormalDayHours*DayCount(r.NormalWeekdays))problems.Add("약정 주시간이 비교대상 통상근로자보다 많습니다. 근로시간 기준을 확인하세요.");if(r.ScheduleMode==1&&Comparison(r).Keys.Any(d=>Holidays.Name(d)!=""))problems.Add("비교근로일에 법정 공휴일이 포함되어 있습니다. 통상근로자의 소정근로일을 확인하세요.");return problems.ToArray();}
 public static void FromLegacy(Record r){r.Version=7;r.ScheduleMode=1;r.ContractDays=r.Days.ToDictionary(x=>x.Key,x=>1);r.NormalWeekdays=62;r.NormalDayHours=8;r.WeeklyRestDay=0;PrepareComparison(r,true);}
 public static void Upgrade(Record r){if(r==null||r.Days==null)return;if(r.Version>=1&&r.Version<=5)FromLegacy(r);else if(r.Version==6)r.Version=7;}
 public static void Validate(Record r){if(r.ContractDays==null||r.ComparisonDays==null||r.ScheduleMode<0||r.ScheduleMode>1||r.ContractWeekdays<1||r.ContractWeekdays>127||r.NormalWeekdays<1||r.NormalWeekdays>127||r.WeeklyRestDay<0||r.WeeklyRestDay>6||r.NormalDayHours<=0||r.NormalDayHours>8)throw new System.IO.InvalidDataException("계약·주휴 기준이 올바르지 않습니다.");foreach(var map in new[]{r.ContractDays,r.ComparisonDays}){if(map.Count>366)throw new System.IO.InvalidDataException("계약·비교 날짜가 너무 많습니다.");foreach(var pair in map){DateTime day;if(pair.Value!=1||!DateTime.TryParseExact(pair.Key,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out day)||day.Year<2020||day.Year>2050)throw new System.IO.InvalidDataException("계약·비교 날짜를 확인하세요.");}}}
 public static string BasisText(Record r,Week w){return (r.ScheduleMode==0?"고정 약정 주 ":w.ReferenceStart.ToString("MM.dd")+"~"+w.ReferenceEnd.ToString("MM.dd")+" 전체 약정 ")+w.ReferenceHours.ToString("0.####")+"시간 ÷ "+(r.ScheduleMode==0?"통상 주 ":"같은 기간 통상 ")+w.ReferenceDays+"일 = "+w.HolidayUnit.ToString("0.####")+"시간";}
 public static string Summary(Record r){string[] days={"일","월","화","수","목","금","토"};return (r.ScheduleMode==0?"매주 같은 요일 · 약정 주 "+(r.Hours*DayCount(r.ContractWeekdays)).ToString("0.##")+"시간":"날짜별 약정 · 4주 이하 계약 전체 기준")+" / 통상 "+String.Join("·",Enumerable.Range(0,7).Where(i=>(r.NormalWeekdays&(1<<i))!=0).Select(i=>days[i]))+" / 주휴 "+days[Math.Max(0,Math.Min(6,r.WeeklyRestDay))]+"요일";}
}
}
