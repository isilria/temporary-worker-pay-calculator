using System;
using System.Collections.Generic;
using System.Linq;
namespace ShortPay {
class PayItem {
 public string Name,Formula;public decimal Amount;
 public PayItem(string name,decimal amount,string formula=""){Name=name;Amount=amount;Formula=formula;}
}
static class PayrollItems {
 static string N(decimal n){return n.ToString("#,##0.####");}
 public static List<PayItem> Pay(Record r,Result z){return new[]{
  new PayItem("기본급",z.BasePay,N(z.Hourly)+"원 × "+N(z.WorkHours+z.PaidLeave)+"시간"),
  new PayItem("주휴수당",z.HolidayPay,N(z.Hourly)+"원 × "+N(z.HolidayHours)+"시간"),
  new PayItem("연장근로",z.OvertimePay,N(z.Hourly)+"원 × ("+N(r.Within)+"시간 + "+N(r.Overtime)+"시간 × "+(z.Guard?"1":"1.5")+")"),
  new PayItem("야간근로가산수당",z.NightPay,N(r.NightDays)+"일 × "+N(r.NightHoursPerDay)+"시간 × "+N(z.Hourly)+"원 × 50%"),
  new PayItem("추가 공휴일 근무수당",z.PublicHolidayPay,N(r.PublicHolidayDays)+"일 × "+N(r.MonthlyBase)+"원 ÷ "+N(z.MonthlyHours)+"시간 × 3"),
  new PayItem("기타 지급",r.ExtraPay,"직접 입력")}.Where(x=>x.Amount!=0).ToList();}
 public static List<PayItem> Deduct(Record r,Result z){return new[]{new PayItem("고용보험",z.Employment),new PayItem("식대",r.Food),new PayItem("건강보험",r.Health),new PayItem("장기요양",r.Care),new PayItem("국민연금",r.Pension),new PayItem("소득세",r.Tax),new PayItem("지방소득세",r.LocalTax),new PayItem("기타 공제",r.Other)}.Where(x=>x.Amount!=0).ToList();}
}
}
