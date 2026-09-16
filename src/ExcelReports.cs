using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;
using OfficeOpenXml;
namespace ShortPay {
public class ExportOptions {
 public string Recipient="",Manager="";public int Year=2026,Month=9,Round=1;public DateTime? PaymentDate;public bool ExtraIsAdjustment=false;
}
public static class ExcelReports {
 class FormulaValue{public string Text;public FormulaValue(string text){Text=text;}}
 const string Ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static string N(decimal x){return x.ToString("#,##0.####",Inv);}
 static string Date(DateTime x){return x.ToString("yyyy. M. d.",Inv);}
 internal static byte[] Resource(string name){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)) {if(s==null)throw new InvalidOperationException("출력 양식을 찾지 못했습니다: "+name);using(var m=new MemoryStream()){s.CopyTo(m);return m.ToArray();}}}
 public static void RegisterDependencies(){AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>e.Name.StartsWith("EPPlus,")?Assembly.Load(Resource("EPPlus.dll")):null;}
 public static string Narrative(Record r,Job j,Result z){
  string reason=r.Reason.Trim();if(!reason.EndsWith("대체"))reason+=" 대체";
  string first="원근로자 "+j.name+" "+r.OriginalWorker.Trim()+"의 "+reason+"("+Date(r.Start)+"~"+Date(r.End)+")";
  decimal paidDays=(z.WorkHours+z.PaidLeave+z.HolidayHours)/r.Hours;
  string calc="주15시간 "+(z.Average<15?"미만":"이상")+" "+N(z.Hourly)+"원×"+N(r.Hours)+"시간×"+N(paidDays)+"일="+N(z.WorkAndHoliday)+"원";
  if(Engine.IsGuard(j))calc="주15시간 "+(z.Average<15?"미만":"이상")+" · 평일 "+N(r.Hours)+"시간 / 주말 "+N(r.WeekendHours)+"시간 · "+N(z.Hourly)+"원 × 총 "+N(z.WorkHours+z.PaidLeave)+"시간 = "+N(z.BasePay)+"원\n야간 "+N(r.NightDays)+"일 × "+N(r.NightHoursPerDay)+"시간 (총 "+N(Engine.NightHours(r))+"시간) × "+N(z.Hourly)+"원 × 50% = "+N(z.NightPay)+"원\n공휴일 추가근로 "+N(r.PublicHolidayDays)+"일 × "+N(r.MonthlyBase)+"원 ÷ "+N(z.MonthlyHours)+" × 3 = "+N(z.PublicHolidayPay)+"원";
  return first+"\n"+calc;

 }
 public static string Birth(Record r){var digits=new string((r.Identity??"").Where(Char.IsDigit).ToArray());if(digits.Length==13){int century=digits[6]=='1'||digits[6]=='2'||digits[6]=='5'||digits[6]=='6'?1900:digits[6]=='3'||digits[6]=='4'||digits[6]=='7'||digits[6]=='8'?2000:1800;return (century+Int32.Parse(digits.Substring(0,2)))+"."+digits.Substring(2,2)+"."+digits.Substring(4,2);}if(digits.Length==8)return digits.Substring(0,4)+"."+digits.Substring(4,2)+"."+digits.Substring(6,2);return digits.Length==6?digits.Substring(0,2)+"."+digits.Substring(2,2)+"."+digits.Substring(4,2):r.Identity;}
 public static void Save(string path,string kind,Record r,Job j,Result z,ExportOptions o){
  Engine.ValidateExport(r,j,z);
  if(Engine.ContractWarning(r)!="")throw new InvalidOperationException(Engine.ContractWarning(r));
  if(String.IsNullOrWhiteSpace(r.Name))throw new InvalidOperationException("대상자 성명을 입력하세요.");
  string key=kind=="임금산정표"?"wage":kind=="급여명세서"?"payslip":"request";
  byte[] source=Resource(key+".xlsx");var map=new Dictionary<string,object>();
  if(key=="request")RequestMap(map,r,j,z,o);else if(key=="payslip")PayslipMap(map,r,j,z,o);else WageMap(map,r,j,z,o);
  Dictionary<int,double> heights=null;if(Engine.IsGuard(j)){if(key=="request")heights=new Dictionary<int,double>{{14,145}};else if(key=="wage")heights=new Dictionary<int,double>{{11,42},{12,42},{13,42},{14,42}};}
  byte[] filled=Patch(source,map,null,key=="wage"?HiddenWageRows(r,z):null,heights);
  Dictionary<string,decimal> caches;
  using(var memory=new MemoryStream(filled))using(var package=new ExcelPackage(memory)){
   var ws=package.Workbook.Worksheets.First();caches=new Dictionary<string,decimal>();
   string[] addresses=key=="request"?new[]{"M14","N14","Q14","W14","AF14","AG14","AO14","AQ14","AR14","W2","AF2","AG2","AO2","AQ2","AR2"}:key=="payslip"?new[]{"F8","F22","H22","F23"}:new[]{"G15","G25","G26"};
   var calculator=new TemplateFormula(ws);foreach(string addr in addresses){object value=calculator.NumberAt(addr);decimal amount;if(value==null||!Decimal.TryParse(Convert.ToString(value,Inv),NumberStyles.Any,Inv,out amount))throw new InvalidOperationException("양식 수식을 계산하지 못했습니다: "+addr+" = "+value+" / "+String.Join(";",addresses.Select(a=>a+":"+ws.Cells[a].Value+"("+ws.Cells[a].Formula+")")));caches[addr]=amount;}
   if(key=="request"){
    CheckEqual("신청서 통상시급(Q14)",caches["Q14"],z.Hourly);
    CheckEqual("신청서 기본급·주휴(W14)",caches["W14"],z.WorkAndHoliday);
    CheckEqual("신청서 연장수당(AF14)",caches["AF14"],z.OvertimePay+z.NightPay);
    CheckEqual("신청서 지급합계(AG14)",caches["AG14"],z.Gross);
    CheckEqual("신청서 고용보험(AO14)",caches["AO14"],z.Employment);
    CheckEqual("신청서 공제합계(AQ14)",caches["AQ14"],z.Deductions);
    CheckEqual("신청서 실지급액(AR14)",caches["AR14"],z.Net);
   }else {CheckEqual("지급합계",caches[key=="payslip"?"F22":"G15"],z.Gross);CheckEqual("공제합계",caches[key=="payslip"?"H22":"G25"],z.Deductions);CheckEqual("실수령액",caches[key=="payslip"?"F23":"G26"],z.Net);}
  }
  byte[] final=Patch(filled,new Dictionary<string,object>(),caches,null);
  string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{File.WriteAllBytes(temp,final);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
 }
 static void CheckEqual(string name,decimal actual,decimal expected){if(actual!=expected)throw new InvalidOperationException(name+"이 프로그램 계산과 다릅니다.\n양식: "+N(actual)+" / 프로그램: "+N(expected)+"\n원본 수식을 보존하므로 값을 덮어쓰지 않고 신청서 저장을 중지했습니다. 기본급·수당·근로시간·직접 지정시급을 확인하세요.");}
 static void RequestMap(Dictionary<string,object> m,Record r,Job j,Result z,ExportOptions o){
  if(!z.Ready)throw new InvalidOperationException("계산 검토가 남아 있습니다. 신청서 출력 전에 근무일·직종 기준을 확인하세요.");
  if(String.IsNullOrWhiteSpace(r.OriginalWorker)||String.IsNullOrWhiteSpace(r.Reason))throw new InvalidOperationException("신청서에 필요한 원근로자 성명과 사유를 입력하세요.");
  m["A1"]=o.Year+". "+o.Month+"월 "+o.Round+"차- 1개월 미만 기간제근로자(대체근로자)-인건비 신청";
  m["B14"]=o.Recipient;m["C14"]=r.Institution;m["D14"]=o.Manager;m["E14"]=j.name;m["F14"]=r.Name;m["G14"]=Narrative(r,j,z);m["H14"]=r.Bank;m["I14"]=r.Account;m["J14"]=r.Holder;
  m["K14"]=j.name.Contains("당직")?(j.name.Contains("1인")?"1인 당직":"2인 당직"):j.@base==2344500?"1유형":"2유형";
  m["L14"]=z.Average<15?"미만":"이상";m["O14"]=z.Average>=15?r.Allowance:0;m["P14"]=z.MonthlyHours;m["R14"]=r.Hours;m["S14"]=(z.WorkHours+z.PaidLeave)/r.Hours;
  int count=z.Weeks.Count(w=>w.Holiday>0);m["T14"]=z.HolidayHours>0&&z.HolidayUnit!=r.Hours?"O":"X";m["U14"]=count;m["V14"]=count>0?z.HolidayHours/count:0;
  m["X14"]=r.ExtraPay+z.PublicHolidayPay;if(z.PublicHolidayPay!=0)m["X5"]="기타 지급\n(공휴일 추가근로 포함)";m["Y14"]=j.name.Contains("당직")?"O":"X";
  m["Z14"]=Math.Floor(r.Within);m["AA14"]=(r.Within-Math.Floor(r.Within))*60;m["AB14"]=Math.Floor(r.Overtime);m["AC14"]=(r.Overtime-Math.Floor(r.Overtime))*60;m["AD14"]=0;m["AE14"]=0;
  if(Engine.IsGuard(j)){
   decimal nightHours=Engine.NightHours(r);decimal extraHours=r.Within+r.Overtime;
   m["AD14"]=Math.Floor(nightHours);m["AE14"]=(nightHours-Math.Floor(nightHours))*60;m["AB14"]=Math.Floor(extraHours);m["AC14"]=(extraHours-Math.Floor(extraHours))*60;
   m["R14"]=r.Hours;m["S14"]=r.Days.Count(x=>x.Value==1&&DateTime.ParseExact(x.Key,"yyyy-MM-dd",Inv).DayOfWeek!=DayOfWeek.Saturday&&DateTime.ParseExact(x.Key,"yyyy-MM-dd",Inv).DayOfWeek!=DayOfWeek.Sunday);
   m["W14"]=new FormulaValue("ROUNDDOWN(Q14*"+(z.WorkHours+z.PaidLeave).ToString(Inv)+",-1)");
  }
  m["AH14"]=r.Employment?"":"O";m["AI14"]=r.Food;m["AJ14"]=r.Tax;m["AK14"]=r.LocalTax;m["AL14"]=r.Health;m["AM14"]=r.Care;m["AN14"]=r.Pension;m["AP14"]=r.Employment?"":r.EmploymentExclusionReason;
 }
 static void PayslipMap(Dictionary<string,object> m,Record r,Job j,Result z,ExportOptions o){
  m["B3"]=o.Year+"년 "+o.Month+"월 임금 명세서";m["B5"]="소속: "+r.Institution;m["G5"]="지급(예정)일 : "+(o.PaymentDate.HasValue?Date(o.PaymentDate.Value):"");m["D6"]=r.Name;m["F6"]=Birth(r);m["H6"]=j.name;
  m["D7"]=Date(r.Start)+"~"+Date(r.End);m["F7"]="1개월 미만 대체근로";m["H7"]=z.Average;m["D8"]=z.Days;m["H8"]=r.Hours;m["D10"]=r.Overtime;m["D11"]=r.Within;m["F10"]=0;m["F11"]=0;m["H10"]=0;m["D12"]=r.Days.Count(x=>x.Value==2);m["D13"]=r.Days.Where(x=>x.Value==2).Sum(x=>Engine.DayHours(r,j,DateTime.ParseExact(x.Key,"yyyy-MM-dd",Inv)));m["F12"]=0;m["H12"]=z.Hourly;
  var payItems=PayrollItems.Pay(r,z);if(!Engine.IsGuard(j)){decimal within=Engine.Floor10(z.Hourly*r.Within);int at=payItems.FindIndex(x=>x.Name=="연장근로");if(at>=0){payItems.RemoveAt(at);if(z.OvertimePay-within!=0)payItems.Insert(at,new PayItem("연장근로(1.5배)",z.OvertimePay-within,N(z.Hourly)+"원 × "+N(r.Overtime)+"시간 × 1.5 (합산 절사)"));if(within!=0)payItems.Insert(at,new PayItem("연장근로(1배)",within,N(z.Hourly)+"원 × "+N(r.Within)+"시간"));}}
  for(int i=0;i<5;i++){m["C"+(17+i)]=i<payItems.Count?payItems[i].Name:"";m["D"+(17+i)]=i<payItems.Count?payItems[i].Formula:"";m["F"+(17+i)]=i<payItems.Count?(object)payItems[i].Amount:"";}
  if(Engine.IsGuard(j)){m["H8"]="평일 "+N(r.Hours)+" / 주말 "+N(r.WeekendHours);m["C10"]="추가근로시간 (1배)";m["D10"]=r.Within+r.Overtime;m["D11"]=0;m["E10"]="공휴일 추가근로\n(산정시간)";m["F10"]=r.PublicHolidayDays*3;m["F11"]=0;m["H10"]=Engine.NightHours(r);m["F8"]=new FormulaValue(z.MonthlyHours.ToString(Inv));}
  var deductions=new List<KeyValuePair<string,decimal>>();
  if(z.Employment!=0)deductions.Add(new KeyValuePair<string,decimal>("고용보험",z.Employment));
  foreach(var item in new[]{new KeyValuePair<string,decimal>("식대",r.Food),new KeyValuePair<string,decimal>("소득세",r.Tax),new KeyValuePair<string,decimal>("지방소득세",r.LocalTax),new KeyValuePair<string,decimal>("건강보험",r.Health),new KeyValuePair<string,decimal>("장기요양보험",r.Care),new KeyValuePair<string,decimal>("국민연금",r.Pension)})if(item.Value!=0)deductions.Add(item);
  if(deductions.Count>5){deductions=new List<KeyValuePair<string,decimal>>{new KeyValuePair<string,decimal>("고용보험",z.Employment),new KeyValuePair<string,decimal>("식대",r.Food),new KeyValuePair<string,decimal>("소득세 "+N(r.Tax)+"원\n지방세 "+N(r.LocalTax)+"원",r.Tax+r.LocalTax),new KeyValuePair<string,decimal>("건강 "+N(r.Health)+"원\n요양 "+N(r.Care)+"원",r.Health+r.Care),new KeyValuePair<string,decimal>("국민연금",r.Pension)};}
  for(int i=0;i<5;i++){m["G"+(17+i)]=i<deductions.Count?deductions[i].Key:"";m["H"+(17+i)]=i<deductions.Count?(object)deductions[i].Value:"";}
  m["B25"]="※ 위 근로자 "+r.Name+"은(는) 임금명세서 1부를 교부받았습니다. 성명                    (인)                    20  .   .   .";
 }
 static void WageMap(Dictionary<string,object> m,Record r,Job j,Result z,ExportOptions o){
  m["B3"]=r.Institution+"   |   계약기간 "+Date(r.Start)+" ~ "+Date(r.End);
  m["B5"]="직종 / 성명   "+j.name+" / "+r.Name;m["J5"]="생년월일   "+Birth(r);
  m["B6"]="지급계좌   "+r.Bank+" / "+r.Account+" / "+r.Holder;m["J6"]="원근로자   "+r.OriginalWorker+" / "+r.Reason;
  m["B7"]=(z.Ready?"계산자료":"검토 필요 · 미확정 예상액")+"   담당자 "+r.Contact;
  m["D11"]=N(z.Hourly)+"원 × "+N(z.WorkHours+z.PaidLeave)+"시간";m["G11"]=z.BasePay;
  m["D12"]=N(z.Hourly)+"원 × "+N(z.HolidayHours)+"시간";m["G12"]=z.HolidayPay;
  m["D13"]="1배 "+N(r.Within)+"시간 / 1.5배 "+N(r.Overtime)+"시간";m["G13"]=z.OvertimePay;m["D14"]="직접 입력";m["G14"]=r.ExtraPay;
  if(Engine.IsGuard(j)){var items=PayrollItems.Pay(r,z);if(items.Count>4){var last=items[4];items[3]=new PayItem(items[3].Name+" / "+last.Name,items[3].Amount+last.Amount,items[3].Formula+" / "+last.Formula);items.RemoveAt(4);}for(int i=0;i<4;i++){m["B"+(11+i)]=i<items.Count?items[i].Name:"";m["D"+(11+i)]=i<items.Count?items[i].Formula:"";m["G"+(11+i)]=i<items.Count?(object)items[i].Amount:"";}}
  decimal[] amounts={r.Food,r.Tax,r.LocalTax,r.Health,r.Care,r.Pension,z.Employment};for(int i=0;i<7;i++)m["G"+(18+i)]=amounts[i];
  for(int i=0;i<z.Weeks.Count;i++){var w=z.Weeks[i];int row=11+i;m["J"+row]=w.Period;m["K"+row]=w.PlannedDays;m["L"+row]=w.Planned;m["M"+row]=w.Actual;m["N"+row]=w.Holiday>0?"지급":w.Status;m["P"+row]=w.Holiday;}
  var days=r.Days.OrderBy(x=>x.Key).ToList();for(int i=0;i<days.Count;i++){int row=19+i%16;bool right=i>=16;var day=days[i];m[(right?"N":"J")+row]=System.DateTime.ParseExact(day.Key,"yyyy-MM-dd",Inv).ToString("MM월dd일");m[(right?"O":"L")+row]=day.Value==1?"근무":day.Value==2?"결근":"유급";m[(right?"P":"M")+row]=day.Value==2?0:Engine.DayHours(r,j,DateTime.ParseExact(day.Key,"yyyy-MM-dd",Inv));}
  string[] basis={
   "일 소정근로 "+N(r.Hours)+"시간 · 주 평균 "+N(z.Average)+"시간 · 월 임금산정 "+N(z.MonthlyHours)+"시간",
   "월 기본급 "+N(r.MonthlyBase)+"원 / 급식비 "+N(z.Average>=15?r.Meal:0)+"원 / 수당 "+N(z.Average>=15?r.Allowance:0)+"원",
   "통상시급 "+N(z.OrdinaryHourly)+"원 / 최저시급 "+N(r.Minimum)+"원 / 적용 "+N(z.Hourly)+"원",
   "주휴 기준기간 "+Date(r.Start)+" ~ "+Date(z.ReferenceEnd),
   "주휴 1회 = "+N(z.ReferenceHours)+"시간 ÷ "+z.ReferenceDays+"일 = "+N(z.HolidayUnit)+"시간",
   "고용보험 "+N(r.Rate)+"% · 임금·주휴 합산 / 연장 합산 후 각각 10원 미만 절사",
   String.Join(" / ",z.Warnings.Distinct())};
  for(int i=0;i<basis.Length;i++)m["B"+(29+i)]=basis[i];
  m["J35"]="실근로 "+z.Days+"일 · 주휴 "+N(z.HolidayHours)+"시간 · 유급휴가 "+N(z.PaidLeave)+"시간";
 }
 static HashSet<int> HiddenWageRows(Record r,Result z){return new HashSet<int>();}
 static XmlNamespaceManager Manager(XmlDocument doc){var n=new XmlNamespaceManager(doc.NameTable);n.AddNamespace("m",Ns);return n;}
 internal static byte[] Patch(byte[] source,Dictionary<string,object> values,Dictionary<string,decimal> caches,HashSet<int> hide,Dictionary<int,double> heights=null){
  using(var input=new MemoryStream(source))using(var original=new ZipArchive(input,ZipArchiveMode.Read))using(var output=new MemoryStream()){
   using(var dest=new ZipArchive(output,ZipArchiveMode.Create,true))foreach(var item in original.Entries){var entry=dest.CreateEntry(item.FullName,System.IO.Compression.CompressionLevel.Optimal);entry.LastWriteTime=item.LastWriteTime;using(var stream=entry.Open()){
    if(item.FullName!="xl/worksheets/sheet1.xml"){using(var src=item.Open())src.CopyTo(stream);continue;}
    var doc=new XmlDocument{PreserveWhitespace=true};using(var src=item.Open())doc.Load(src);var ns=Manager(doc);
    foreach(var kv in values){var cell=Cell(doc,ns,kv.Key);if(kv.Value is FormulaValue){foreach(XmlNode node in cell.SelectNodes("m:f|m:v|m:is",ns))cell.RemoveChild(node);cell.RemoveAttribute("t");var formula=doc.CreateElement("f",Ns);formula.InnerText=((FormulaValue)kv.Value).Text;cell.AppendChild(formula);continue;}if(cell.SelectSingleNode("m:f",ns)!=null)throw new InvalidOperationException("원본 수식 셀에 값을 쓸 수 없습니다: "+kv.Key);SetValue(doc,ns,cell,kv.Value);}
    if(caches!=null)foreach(var kv in caches){var cell=Cell(doc,ns,kv.Key);if(cell.SelectSingleNode("m:f",ns)==null)throw new InvalidOperationException("계산 셀 수식이 없습니다: "+kv.Key);foreach(XmlNode node in cell.SelectNodes("m:v|m:is",ns))cell.RemoveChild(node);cell.RemoveAttribute("t");var v=doc.CreateElement("v",Ns);v.InnerText=kv.Value.ToString(Inv);cell.AppendChild(v);}
    if(hide!=null)foreach(XmlElement row in doc.SelectNodes("/m:worksheet/m:sheetData/m:row",ns)){if(hide.Contains(Int32.Parse(row.GetAttribute("r"))))row.SetAttribute("hidden","1");}
    if(heights!=null)foreach(XmlElement row in doc.SelectNodes("/m:worksheet/m:sheetData/m:row",ns)){double height;if(heights.TryGetValue(Int32.Parse(row.GetAttribute("r")),out height)){row.SetAttribute("ht",height.ToString(Inv));row.SetAttribute("customHeight","1");}}
    using(var writer=XmlWriter.Create(stream,new XmlWriterSettings{Encoding=new UTF8Encoding(false),CloseOutput=false}))doc.Save(writer);
   }}return output.ToArray();
  }
 }
 static XmlElement Cell(XmlDocument doc,XmlNamespaceManager ns,string addr){var c=(XmlElement)doc.SelectSingleNode("/m:worksheet/m:sheetData/m:row/m:c[@r='"+addr+"']",ns);if(c!=null)return c;int rownum=Int32.Parse(new string(addr.Where(Char.IsDigit).ToArray()));var row=(XmlElement)doc.SelectSingleNode("/m:worksheet/m:sheetData/m:row[@r='"+rownum+"']",ns);if(row==null){row=doc.CreateElement("row",Ns);row.SetAttribute("r",rownum.ToString());var data=doc.SelectSingleNode("/m:worksheet/m:sheetData",ns);var after=data.ChildNodes.Cast<XmlElement>().FirstOrDefault(x=>Int32.Parse(x.GetAttribute("r"))>rownum);data.InsertBefore(row,after);}c=doc.CreateElement("c",Ns);c.SetAttribute("r",addr);var next=row.ChildNodes.Cast<XmlElement>().FirstOrDefault(x=>Column(x.GetAttribute("r"))>Column(addr));row.InsertBefore(c,next);return c;}
 static int Column(string address){int col=0;foreach(char c in address){if(!Char.IsLetter(c))break;col=col*26+c-'A'+1;}return col;}
 static void SetValue(XmlDocument doc,XmlNamespaceManager ns,XmlElement c,object value){foreach(XmlNode x in c.SelectNodes("m:v|m:is",ns))c.RemoveChild(x);c.RemoveAttribute("t");if(value is string){c.SetAttribute("t","inlineStr");var inline=doc.CreateElement("is",Ns);var text=doc.CreateElement("t",Ns);text.SetAttribute("space","http://www.w3.org/XML/1998/namespace","preserve");text.InnerText=(string)value;inline.AppendChild(text);c.AppendChild(inline);}else if(value!=null){var v=doc.CreateElement("v",Ns);v.InnerText=Convert.ToString(value,Inv);c.AppendChild(v);}}
}
}



