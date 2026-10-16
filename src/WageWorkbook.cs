using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using OfficeOpenXml;
namespace ShortPay {
public class WageImport { public Record Record;public string Note="";public bool Exact,Legacy; }
public static class WageWorkbook {
 internal const string ArchiveSheet="_ShortPay작업복원1",Marker="SHORTPAY-WAGE-1";
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static void NormalizeDates(Record r){r.Start=(r.Start.Kind==DateTimeKind.Utc?r.Start.ToLocalTime():r.Start).Date;r.End=(r.End.Kind==DateTimeKind.Utc?r.End.ToLocalTime():r.End).Date;if(r.PaymentDate.HasValue){var d=r.PaymentDate.Value;r.PaymentDate=(d.Kind==DateTimeKind.Utc?d.ToLocalTime():d).Date;}}
 public class Archive { public string Format=Marker,JobName;public int Schema=1;public Record Record;public Dictionary<string,string> Cells; }
 static string Hash(string text){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","");}
 static string Value(ExcelWorksheet ws,string address){var cell=ws.Cells[address];return String.IsNullOrEmpty(cell.Formula)?Convert.ToString(cell.Value,Inv)??"":"="+cell.Formula;}
 static Dictionary<string,string> Snapshot(ExcelWorksheet ws){var data=new Dictionary<string,string>();for(int row=1;row<=35;row++)for(int col=2;col<=16;col++){var cell=ws.Cells[row,col];data[cell.Address]=Value(ws,cell.Address);}return data;}
 internal static byte[] Attach(byte[] workbook,Record record,Job job,ExportOptions options){
  var copy=Engine.Parse<Record>(Engine.Json(record));Holidays.Stamp(copy);
  // A wage report has always disclosed birth date only. Do not hide a full resident ID in it.
  copy.Identity=ExcelReports.Birth(copy);copy.IdentityMode="생년월일";
  copy.Recipient=options.Recipient;copy.Manager=options.Manager;copy.RequestRound=options.Round;copy.PaymentDate=options.PaymentDate;
  string json;using(var memory=new MemoryStream(workbook))using(var package=new ExcelPackage(memory))json=Engine.Json(new Archive{JobName=job.name,Record=copy,Cells=Snapshot(package.Workbook.Worksheets.First())});
  string encoded=Convert.ToBase64String(Encoding.UTF8.GetBytes(json));var rows=new Dictionary<int,string>{{1,Marker},{2,Hash(json)},{3,((encoded.Length+29999)/30000).ToString(Inv)}};
  for(int offset=0,index=5;offset<encoded.Length;offset+=30000,index++)rows[index]=encoded.Substring(offset,Math.Min(30000,encoded.Length-offset));
  // Add a VeryHidden worksheet without reserializing the original report, its formulas or styles.
  const string sheetNs="http://schemas.openxmlformats.org/spreadsheetml/2006/main",relNs="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
  var hidden=new XmlDocument();hidden.AppendChild(hidden.CreateElement("worksheet",sheetNs));var data=hidden.CreateElement("sheetData",sheetNs);hidden.DocumentElement.AppendChild(data);
  foreach(var item in rows){var row=hidden.CreateElement("row",sheetNs);row.SetAttribute("r",item.Key.ToString(Inv));data.AppendChild(row);var cell=hidden.CreateElement("c",sheetNs);cell.SetAttribute("r","A"+item.Key);cell.SetAttribute("t","inlineStr");row.AppendChild(cell);var inline=hidden.CreateElement("is",sheetNs);cell.AppendChild(inline);var text=hidden.CreateElement("t",sheetNs);text.InnerText=item.Value;inline.AppendChild(text);}
  using(var input=new MemoryStream(workbook))using(var original=new ZipArchive(input,ZipArchiveMode.Read))using(var output=new MemoryStream()){
   using(var dest=new ZipArchive(output,ZipArchiveMode.Create,true)){
    foreach(var item in original.Entries){var entry=dest.CreateEntry(item.FullName,System.IO.Compression.CompressionLevel.Optimal);entry.LastWriteTime=item.LastWriteTime;using(var stream=entry.Open()){
     if(item.FullName!="xl/workbook.xml"&&item.FullName!="xl/_rels/workbook.xml.rels"&&item.FullName!="[Content_Types].xml"){using(var src=item.Open())src.CopyTo(stream);continue;}
     var doc=new XmlDocument{PreserveWhitespace=true,XmlResolver=null};using(var src=item.Open())doc.Load(src);
     if(item.FullName=="xl/workbook.xml"){
      var sheets=doc.DocumentElement.ChildNodes.Cast<XmlNode>().First(n=>n.LocalName=="sheets");int next=sheets.ChildNodes.OfType<XmlElement>().Select(n=>Int32.Parse(n.GetAttribute("sheetId"))).Max()+1;var sheet=doc.CreateElement("sheet",sheetNs);sheet.SetAttribute("name",ArchiveSheet);sheet.SetAttribute("sheetId",next.ToString(Inv));sheet.SetAttribute("id",relNs,"rIdShortPayWorkspace");sheet.SetAttribute("state","veryHidden");sheets.AppendChild(sheet);
     }else if(item.FullName.EndsWith(".rels")){
      var rel=doc.CreateElement("Relationship",doc.DocumentElement.NamespaceURI);rel.SetAttribute("Id","rIdShortPayWorkspace");rel.SetAttribute("Type",relNs+"/worksheet");rel.SetAttribute("Target","worksheets/shortpayWorkspace.xml");doc.DocumentElement.AppendChild(rel);
     }else{
      var type=doc.CreateElement("Override",doc.DocumentElement.NamespaceURI);type.SetAttribute("PartName","/xl/worksheets/shortpayWorkspace.xml");type.SetAttribute("ContentType","application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");doc.DocumentElement.AppendChild(type);
     }
     using(var writer=XmlWriter.Create(stream,new XmlWriterSettings{Encoding=new UTF8Encoding(false)}))doc.Save(writer);
    }}
    var archiveEntry=dest.CreateEntry("xl/worksheets/shortpayWorkspace.xml",System.IO.Compression.CompressionLevel.Optimal);using(var stream=archiveEntry.Open())using(var writer=XmlWriter.Create(stream,new XmlWriterSettings{Encoding=new UTF8Encoding(false)}))hidden.Save(writer);
   }return output.ToArray();
  }
 }
 static void CheckPackage(string path){
  if(new FileInfo(path).Length>20*1024*1024)throw new InvalidDataException("임금산정표 파일이 너무 큽니다.");
  using(var zip=ZipFile.OpenRead(path)){long total=0;foreach(var entry in zip.Entries){total+=entry.Length;if(total>50*1024*1024||entry.Length>20*1024*1024)throw new InvalidDataException("임금산정표 파일의 크기를 확인하세요.");}}
 }
 public static WageImport Load(string path,List<Job> jobs){
  CheckPackage(path);
  using(var package=new ExcelPackage(new FileInfo(path))){
   var hidden=package.Workbook.Worksheets[ArchiveSheet];var ws=package.Workbook.Worksheets.FirstOrDefault(w=>w.Name!=ArchiveSheet&&w.Hidden==eWorkSheetHidden.Visible);
   if(ws==null||!ws.Cells["B3"].Text.Contains("계약기간")||!ws.Cells["B5"].Text.StartsWith("직종"))throw new InvalidDataException("이 프로그램에서 저장한 임금산정표 엑셀을 선택하세요. 급여명세서·신청서·일반 엑셀은 복원 대상이 아닙니다.");
   if(hidden==null){var legacy=ReadVisible(ws,jobs,null,null);Validate(legacy,jobs);return new WageImport{Record=legacy,Legacy=true,Note="기존 양식에서 복원했습니다. 기관기호·신청 차수·지급예정일·고용보험 제외 사유 등 표에 없는 정보는 확인하세요. 주휴와 지급액은 현재 로직으로 다시 계산합니다."};}
   if(hidden.Cells[1,1].Text!=Marker)throw new InvalidDataException("지원하지 않는 임금산정표 복원 형식입니다.");
   int count;if(!Int32.TryParse(hidden.Cells[3,1].Text,out count)||count<1||count>60)throw new InvalidDataException("임금산정표 복원자료가 올바르지 않습니다.");
   var encoded=new StringBuilder();for(int i=0;i<count;i++)encoded.Append(hidden.Cells[5+i,1].Text);
   string json=Encoding.UTF8.GetString(Convert.FromBase64String(encoded.ToString()));
   if(!String.Equals(Hash(json),hidden.Cells[2,1].Text,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("임금산정표의 복원자료가 손상되었습니다.");
   var archive=Engine.Parse<Archive>(json);if(archive==null||archive.Format!=Marker||archive.Schema!=1||archive.Record==null||archive.Cells==null)throw new InvalidDataException("지원하지 않는 임금산정표 복원 버전입니다.");
   int jobIndex=jobs.FindIndex(j=>j.name==archive.JobName);if(jobIndex<0)throw new InvalidDataException("현재 직종표에 없는 직종입니다: "+archive.JobName);archive.Record.JobIndex=jobIndex;Validate(archive.Record,jobs);
   var current=Snapshot(ws);var changed=new HashSet<string>(current.Keys.Where(key=>!archive.Cells.ContainsKey(key)||current[key]!=archive.Cells[key]));
   if(changed.Count==0)return new WageImport{Record=archive.Record,Exact=true,Note="임금산정표의 저장된 계산 입력을 복원했습니다. 수정 후 다시 저장할 수 있습니다."};
   if(Engine.IsGuard(jobs[jobIndex]))throw new InvalidDataException("당직 임금산정표의 표 내용이 저장 후 변경되었습니다. 변경 전 파일을 열고 프로그램에서 근무시간·지급일을 수정해 주세요.");
   var editable=new HashSet<string>(new[]{"B3","B5","J5","B6","J6","B7","D13","G14","B29","B30","B31","B34"});
   foreach(int row in Enumerable.Range(18,6))editable.Add("G"+row);
   for(int row=19;row<=34;row++)foreach(string col in new[]{"J","L","M","N","O","P"})editable.Add(col+row);
   var derived=new Dictionary<string,Func<Result,decimal>>{{"G11",z=>z.BasePay},{"G12",z=>z.HolidayPay},{"G13",z=>z.OvertimePay},{"G15",z=>z.Gross},{"G24",z=>z.Employment},{"G25",z=>z.Deductions},{"G26",z=>z.Net}};
   string unsupported=changed.FirstOrDefault(key=>!editable.Contains(key)&&!derived.ContainsKey(key));if(unsupported!=null)throw new InvalidDataException("복원에 필요한 표의 구조 또는 계산 설명이 변경되었습니다 ("+unsupported+"). 변경 전 파일을 열고 프로그램에서 수정해 주세요.");
   var restored=ReadVisible(ws,jobs,archive.Record,changed);Validate(restored,jobs);var result=Engine.Calculate(restored,jobs[restored.JobIndex]);
   foreach(var cell in derived)if(changed.Contains(cell.Key)&&Number(ws,cell.Key)!=cell.Value(result))throw new InvalidDataException("계산 결과 셀 "+cell.Key+"의 직접 수정값을 입력자료로 복원할 수 없습니다. 근무일·시급·공제 입력을 프로그램에서 수정해 주세요.");
   return new WageImport{Record=restored,Note="엑셀에서 수정한 입력 항목을 반영하고 주휴·지급액을 현재 로직으로 다시 계산했습니다."};
  }
 }
 static decimal Number(ExcelWorksheet ws,string addr){decimal number;var cell=ws.Cells[addr];if(!String.IsNullOrEmpty(cell.Formula))throw new InvalidDataException("직접 입력 셀에 수식이 있습니다: "+addr);string text=Convert.ToString(cell.Value,Inv)??"";if(String.IsNullOrWhiteSpace(text))return 0;if(!Decimal.TryParse(text.Replace("원","").Replace("시간",""),NumberStyles.Number,Inv,out number))throw new InvalidDataException("숫자를 확인하세요: "+addr);return number;}
 static Match Required(string text,string pattern,string label){var match=Regex.Match(text??"",pattern);if(!match.Success)throw new InvalidDataException("임금산정표의 "+label+"을 읽을 수 없습니다.");return match;}
 static decimal Amount(Match m,int group){return Decimal.Parse(m.Groups[group].Value,NumberStyles.Number,Inv);}
 static DateTime DateFrom(Match m,int group){return new DateTime(Int32.Parse(m.Groups[group].Value),Int32.Parse(m.Groups[group+1].Value),Int32.Parse(m.Groups[group+2].Value));}
 static Record ReadVisible(ExcelWorksheet ws,List<Job> jobs,Record seed,HashSet<string> changed){
  bool legacy=seed==null;var r=legacy?new Record():Engine.Parse<Record>(Engine.Json(seed));NormalizeDates(r);Func<string,bool> read=addr=>legacy||changed.Contains(addr);
  if(read("B3")){var m=Required(ws.Cells["B3"].Text,@"^(.*?)\s*\|\s*계약기간\s+(\d{4})\.\s*(\d{1,2})\.\s*(\d{1,2})\.\s*~\s*(\d{4})\.\s*(\d{1,2})\.\s*(\d{1,2})\.","기관·계약기간");r.Institution=m.Groups[1].Value.Trim();r.Start=DateFrom(m,2);r.End=DateFrom(m,5);}
  if(r.End<r.Start||(r.End-r.Start).TotalDays>31)throw new InvalidDataException("1개월 미만 임금산정표의 계약기간을 확인하세요.");
  if(read("B5")){var m=Required(ws.Cells["B5"].Text,@"^직종\s*/\s*성명\s+(.+?)\s*/\s*(.*)$","직종·성명");r.JobIndex=jobs.FindIndex(j=>j.name==m.Groups[1].Value.Trim());if(r.JobIndex<0)throw new InvalidDataException("직종을 확인하세요: "+m.Groups[1].Value);r.Name=m.Groups[2].Value.Trim();}
  if(legacy&&Engine.IsGuard(jobs[r.JobIndex]))throw new InvalidDataException("복원자료가 없는 이전 당직 산정표는 평일·주말·야간 설정을 정확히 복원할 수 없습니다. 대상자 자료(.stpay) 또는 새 버전에서 저장한 임금산정표를 열어 주세요.");
  if(read("J5")){r.Identity=Regex.Replace(ws.Cells["J5"].Text,@"^생년월일\s*","");r.IdentityMode="생년월일";}
  if(read("B6")){string value=Regex.Replace(ws.Cells["B6"].Text,@"^지급계좌\s*","");var parts=value.Split('/');if(parts.Length!=3)throw new InvalidDataException("지급계좌의 은행 / 계좌번호 / 예금주 구분을 확인하세요.");r.Bank=parts[0].Trim();r.Account=parts[1].Trim();r.Holder=parts[2].Trim();}
  if(read("J6")){string value=Regex.Replace(ws.Cells["J6"].Text,@"^원근로자\s*","");int slash=value.IndexOf('/');if(slash<0)throw new InvalidDataException("원근로자 / 사유 구분을 확인하세요.");r.OriginalWorker=value.Substring(0,slash).Trim();r.Reason=value.Substring(slash+1).Trim();}
  if(read("B7")){string value=ws.Cells["B7"].Text;int at=value.IndexOf("담당자 ",StringComparison.Ordinal);r.Contact=at<0?"":value.Substring(at+4).Trim();}
  if(read("B29")){var m=Required(ws.Cells["B29"].Text,@"일 소정근로\s+([\d,.]+)시간","일 소정근로시간");r.Hours=Amount(m,1);}
  if(read("B30")){var m=Required(ws.Cells["B30"].Text,@"월 기본급\s+([\d,.]+)원\s*/\s*급식비\s+([\d,.]+)원\s*/\s*수당\s+([\d,.]+)원","월 급여 기준");r.MonthlyBase=Amount(m,1);r.Meal=Amount(m,2);r.Allowance=Amount(m,3);}
  if(read("B31")){var m=Required(ws.Cells["B31"].Text,@"통상시급\s+([\d,.]+)원\s*/\s*최저시급\s+([\d,.]+)원\s*/\s*적용\s+([\d,.]+)원","적용 시급");r.Minimum=Amount(m,2);decimal applied=Amount(m,3);r.HourlyOverride=applied==Math.Max(Amount(m,1),r.Minimum)?0:applied;}
  if(read("B34")){var m=Required(ws.Cells["B34"].Text,@"고용보험\s+([\d,.]+)%","고용보험 요율");r.Rate=Amount(m,1);}
  if(read("D13")){var m=Required(ws.Cells["D13"].Text,@"1배\s+([\d,.]+)시간\s*/\s*1\.5배\s+([\d,.]+)시간","추가 근로시간");r.Within=Amount(m,1);r.Overtime=Amount(m,2);}
  if(read("G14"))r.ExtraPay=Number(ws,"G14");
  string[] fields={"Food","Tax","LocalTax","Health","Care","Pension"};for(int i=0;i<fields.Length;i++)if(read("G"+(18+i)))typeof(Record).GetField(fields[i]).SetValue(r,Number(ws,"G"+(18+i)));
  if(legacy||changed.Contains("B3")||changed.Contains("B29")||changed.Any(a=>Regex.IsMatch(a,@"^(J|L|M|N|O|P)(19|2\d|3[0-4])$"))){
   r.Days.Clear();for(int row=19;row<=34;row++)for(int side=0;side<2;side++){
    string date=ws.Cells[(side==0?"J":"N")+row].Text.Trim();string state=ws.Cells[(side==0?"L":"O")+row].Text.Trim();if(date==""&&state=="")continue;
    var m=Required(date,@"^(\d{1,2})월\s*(\d{1,2})일$","근무일자");int month=Int32.Parse(m.Groups[1].Value),day=Int32.Parse(m.Groups[2].Value);var matches=Enumerable.Range(0,(r.End-r.Start).Days+1).Select(i=>r.Start.AddDays(i)).Where(d=>d.Month==month&&d.Day==day).ToArray();if(matches.Length!=1)throw new InvalidDataException("계약기간 밖의 근무일자가 있습니다: "+date);
    int value=state=="근무"?1:state=="결근"?2:state=="유급휴가"?3:state=="유급휴일"?4:0;if(value==0)throw new InvalidDataException("근무상태를 확인하세요: "+state);string key=Engine.Key(matches[0]);if(r.Days.ContainsKey(key))throw new InvalidDataException("근무일자가 중복되었습니다: "+date);
    decimal hours=Number(ws,(side==0?"M":"P")+row);if(value==1&&hours!=r.Hours)throw new InvalidDataException("날짜별 근로시간이 일 소정근로시간과 다릅니다: "+date);r.Days.Add(key,value);
   }
  }
  if(legacy){decimal insurance=Number(ws,"G24");if(insurance<0)throw new InvalidDataException("고용보험 금액을 확인하세요.");r.Employment=insurance!=0||Engine.Calculate(r,jobs[r.JobIndex]).Employment==0;r.EmploymentExclusionReason="";r.RateConfirmed=false;WorkPlan.FromLegacy(r);}
  return r;
 }
 public static void Validate(Record r,List<Job> jobs){
  if(r!=null){NormalizeDates(r);if(r.Days!=null)WorkPlan.Upgrade(r);}
  if(r==null||r.Version!=7||r.Days==null||r.ContractDays==null||r.ComparisonDays==null||r.ScheduleMode<0||r.ScheduleMode>1||r.ContractWeekdays<1||r.ContractWeekdays>127||r.NormalWeekdays<1||r.NormalWeekdays>127||r.WeeklyRestDay<0||r.WeeklyRestDay>6||r.NormalDayHours<=0||r.NormalDayHours>8||r.JobIndex<0||r.JobIndex>=jobs.Count||r.Theme<0||r.Theme>5||r.Start.Year<2020||r.End.Year>2050||r.End<r.Start||(r.End-r.Start).TotalDays>31||r.Hours<.25m||r.Hours>(Engine.IsGuard(jobs[r.JobIndex])?24:8)||r.WeekendHours<=0||r.WeekendHours>24||r.NightDays<0||r.NightDays>31||r.PublicHolidayDays<0||r.PublicHolidayDays>31||r.NightHoursPerDay<0||r.NightHoursPerDay>8||(r.UnmannedDay!=0&&r.UnmannedDay!=6))throw new InvalidDataException("복원자료의 날짜·직종·계약·주휴 기준 범위를 확인하세요.");
  WorkPlan.Validate(r);foreach(var field in typeof(Record).GetFields())if(field.FieldType==typeof(string)&&field.GetValue(r)==null)field.SetValue(r,"");
  decimal[] money={r.MonthlyBase,r.Meal,r.Allowance,r.ExtraPay,r.Food,r.Tax,r.LocalTax,r.Health,r.Care,r.Pension,r.Other};
  if(money.Any(x=>x<0||x>100000000)||r.Minimum<0||r.Minimum>100000||r.HourlyOverride<0||r.HourlyOverride>1000000||r.Within<0||r.Within>300||r.Overtime<0||r.Overtime>300||r.Rate<0||r.Rate>100||r.HolidayHours<0||r.HolidayHours>80||r.RequestRound<1||r.RequestRound>4||r.OutputYear<0||r.OutputYear>2050||r.OutputMonth<0||r.OutputMonth>12||r.PaymentDate.HasValue&&(r.PaymentDate.Value.Year<2020||r.PaymentDate.Value.Year>2050))throw new InvalidDataException("복원자료의 금액·시간 범위를 확인하세요.");
  foreach(var day in r.Days){DateTime date;if(!DateTime.TryParseExact(day.Key,"yyyy-MM-dd",Inv,DateTimeStyles.None,out date)||date<r.Start||date>r.End||day.Value<1||day.Value>4)throw new InvalidDataException("복원자료의 근무일을 확인하세요.");}
 }
}
}
