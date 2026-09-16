using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Generic;
namespace ShortPay {
public static class Reports {
 static string E(string s){return WebUtility.HtmlEncode(s??"");} static string Won(decimal d){return d.ToString("N0")+"원";}
 public static string Html(Record r,Job j,Result z,string kind){
  if(Engine.ContractWarning(r)!="")throw new InvalidOperationException(Engine.ContractWarning(r));
  var b=new StringBuilder("<!doctype html><html lang='ko'><meta charset='utf-8'><title>"+E(kind)+"</title><style>body{font:14px 'Malgun Gothic',sans-serif;color:#293148;margin:36px auto;max-width:960px;line-height:1.6}h1{color:#4949a5;font-size:28px}h2{font-size:17px;margin-top:26px}table{border-collapse:collapse;width:100%;margin:12px 0}th,td{padding:9px 13px;border:1px solid #d5d8e5;text-align:left}th{background:#f0f0fc}td:last-child{text-align:right}.total{font-weight:bold;background:#f0f0fc}.note{white-space:pre-wrap;font-size:12px;color:#646b7f}.warn{background:#fff2de;padding:12px}button{padding:10px 20px;border:0;border-radius:8px;background:#4949a5;color:white;cursor:pointer}@page{size:A4;margin:16mm}@media print{body{margin:0;font-size:11px}button{display:none}thead{display:table-header-group}tr{break-inside:avoid}h2{break-after:avoid}}</style><button onclick='window.print()'>인쇄 / PDF 저장</button><h1>"+E(kind)+"</h1>");
  b.Append("<p>"+E(r.Institution)+" · "+r.Start.ToString("yyyy.MM.dd")+" ~ "+r.End.ToString("yyyy.MM.dd")+"</p><p class='warn'>"+(z.Ready?"테스트 계산자료 · 실제 지급 전 적용 기준 확인":"검토 필요 · 아래 금액은 미확정 예상액")+"</p>");
  b.Append("<table><tr><th>직종</th><td>"+E(j.name)+"</td><th>성명</th><td>"+E(r.Name)+"</td></tr><tr><th>생년월일 / 주민번호</th><td>"+E(new string(r.Identity.Where(Char.IsDigit).ToArray()).Length>8?(r.Identity.Length>6?r.Identity.Substring(0,6)+"-*******":"******-*******"):r.Identity)+"</td><th>담당자</th><td>"+E(r.Contact)+"</td></tr><tr><th>지급 계좌</th><td colspan='3'>"+E(r.Bank+" / "+r.Account+" / "+r.Holder)+"</td></tr><tr><th>대체 사유</th><td colspan='3'>"+E(r.OriginalWorker+" / "+r.Reason)+"</td></tr></table>");
  b.Append("<h2>지급 내역</h2><table><thead><tr><th>항목</th><th>산정 내용</th><th>금액</th></tr></thead><tbody>");
  b.Append("<tr><td>근무·유급휴가 임금</td><td>시급 "+z.Hourly.ToString("N2")+" × "+(z.WorkHours+z.PaidLeave).ToString("0.##")+"시간 · 10원 미만 절사</td><td>"+Won(z.BasePay)+"</td></tr>");
  b.Append("<tr><td>주휴수당</td><td>"+z.HolidayHours.ToString("0.##")+"시간 · 합산 절사 차액 포함"+(r.Override?" (수동 확인)":"")+"</td><td>"+Won(z.HolidayPay)+"</td></tr><tr><td>초과근로수당</td><td>연장(1배) "+r.Within+"시간 / 연장(1.5배) "+r.Overtime+"시간</td><td>"+Won(z.OvertimePay)+"</td></tr><tr><td>기타 지급</td><td>직접 입력</td><td>"+Won(r.ExtraPay)+"</td></tr><tr class='total'><td colspan='2'>지급 합계</td><td>"+Won(z.Gross)+"</td></tr></tbody></table>");
  b.Append("<h2>공제 내역</h2><table>");var deductions=new Dictionary<string,decimal>{{"식대",r.Food},{"소득세",r.Tax},{"지방소득세",r.LocalTax},{"건강보험",r.Health},{"장기요양보험",r.Care},{"국민연금",r.Pension},{"고용보험",z.Employment}};foreach(var d in deductions)b.Append("<tr><th>"+d.Key+"</th><td>"+Won(d.Value)+"</td></tr>");b.Append("<tr class='total'><th>공제 합계</th><td>"+Won(z.Deductions)+"</td></tr><tr class='total'><th>실수령 예상액</th><td>"+Won(z.Net)+"</td></tr></table>");
  if(kind!="급여명세서"){
   b.Append("<h2>근무일 및 주휴 검토</h2><p>일 소정근로 "+r.Hours+"시간 / 실제 근무 "+z.Days+"일 / 평균 주 소정근로 "+z.Average.ToString("0.##")+"시간 (기준기간 환산)</p><table><thead><tr><th>날짜</th><th>구분</th><th>시간</th></tr></thead><tbody>");foreach(var kv in r.Days.OrderBy(x=>x.Key))b.Append("<tr><td>"+E(kv.Key)+"</td><td>"+(kv.Value==1?"근무":kv.Value==2?"무급 결근":"유급휴가·휴일 (검토)")+"</td><td>"+(kv.Value==2?"0":r.Hours.ToString("0.##"))+"</td></tr>");b.Append("</tbody></table><table><thead><tr><th>계약 시작일 기준 주간</th><th>소정일수</th><th>소정시간</th><th>실근무시간</th><th>검토 결과</th><th>자동 주휴시간</th></tr></thead><tbody>");foreach(var w in z.Weeks)b.Append("<tr><td>"+w.Period+"</td><td>"+w.PlannedDays+"</td><td>"+w.Planned+"</td><td>"+w.Actual+"</td><td>"+E(w.Status)+"</td><td>"+w.Holiday.ToString("0.##")+"</td></tr>");b.Append("</tbody></table>");
  }
  b.Append("<p class='note'>주휴 기준: 시작일부터 7일 유지 · 해당 주 소정시간 15시간 이상 · 개근<br>비례 기준기간: "+r.Start.ToString("yyyy.MM.dd")+" ~ "+z.ReferenceEnd.ToString("yyyy.MM.dd")+"<br>주휴 1회 지급시간: "+z.ReferenceHours.ToString("0.##")+"시간 ÷ 통상근로 "+z.ReferenceDays+"일 = "+z.HolidayUnit.ToString("0.####")+"시간 (월~금 기준)</p>");
  b.Append("<h2>산정 기준</h2><p class='note'>월 기본급(8시간 기준) "+Won(r.MonthlyBase)+" / 급식비 "+Won(r.Meal)+" / 적용 수당 "+Won(r.Allowance)+"\n월 환산시간 "+z.MonthlyHours+" / 비교 최저시급 "+r.Minimum+" / 고용보험 "+r.Rate+"%"+(r.HourlyOverride>0?"\n시급 직접 지정: "+r.HourlyOverride:"")+"\n"+E(r.Override?"수동 확인 근거: "+r.ReviewNote:"")+"\n"+E(String.Join("\n",z.Warnings.Distinct()))+"</p><p class='note'>출처: 2026 임금 계산.xlsm · 직종참조표.xlsx / 고용노동부 1350 주휴 안내\n2025년 업무 담당자 보수교육 인쇄 60·67·68·106·110쪽 / 테스트3 · 신청내역은 기관 제출용 원본 서식과 다릅니다. 야간·휴일 가산, 세금 및 보험 적용 여부는 별도 확인합니다.</p></html>");return b.ToString();
 }
}
}
