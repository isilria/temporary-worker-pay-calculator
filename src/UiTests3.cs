using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
namespace ShortPay {
public partial class MainForm {
 int uiChecks;void AssertUI(bool condition,string text){uiChecks++;if(!condition)throw new Exception("UI TEST: "+text);}
 internal void ExerciseTest3(string folder){
  var alerts=new List<string>();ContractAlert=alerts.Add;
  r=new Record{Name="홍길동",OriginalWorker="김담당",Reason="휴직 대체",Start=new DateTime(2026,9,7),End=new DateTime(2026,9,20)};Engine.Fill(r);LoadUI();RefreshAll();Show();Application.DoEvents();
  tabs.SelectedIndex=1;calendar.Controls.OfType<DayButton>().First(x=>x.AccessibleName.StartsWith("2026-09-09")).PerformClick();AssertUI(result.Days==9&&result.HolidayHours==8,"근무 → 결근 및 주휴 제외");
  calendar.Controls.OfType<DayButton>().First(x=>x.AccessibleName.StartsWith("2026-09-09")).PerformClick();AssertUI(result.Days==9&&result.HolidayHours==14.4m,"결근 → 비근무 및 소정일 제외");
  calendar.Controls.OfType<DayButton>().First(x=>x.AccessibleName.StartsWith("2026-09-09")).PerformClick();AssertUI(result.Days==10&&result.HolidayHours==16,"비근무 → 근무 복원");
  month.SelectedIndex=9;AssertUI(result.Days==10,"월 탐색으로 데이터 유지");month.SelectedIndex=8;
  start.Value=new DateTime(2026,9,8);end.Value=new DateTime(2026,9,14);AssertUI(result.HolidayHours==8&&weeks.Rows.Count==1,"화요일 시작 1주차");
  AssertUI(calendar.Controls.OfType<DayButton>().First(x=>x.AccessibleName.StartsWith("2026-09-14")).Pink,"주휴일 분홍 표시");
  tabs.SelectedIndex=0;end.Value=new DateTime(2026,10,8);AssertUI(alerts.Count==0,"no warning while editing dates");tabs.SelectedIndex=1;AssertUI(tabs.SelectedIndex==0&&alerts[0].StartsWith("계약기간이 1개월 이상으로 설정되어 있습니다."),"navigation validates contract and stays for review");AssertUI(alerts.Count==1&&end.Value==new DateTime(2026,9,30)&&Engine.ContractWarning(r)=="","초과 계약 월말 보정");
  start.Value=new DateTime(2026,9,1);AssertUI(alerts.Count==1,"start-date editing has no popup");AssertUI(Engine.ContractWarning(r)!=""&&!result.Ready,"1일~말일 월급제 계산 차단");
  start.Value=new DateTime(2026,9,8);end.Value=new DateTime(2026,9,21);AssertUI(weeks.Rows.Count==2,"2주 동적 행");end.Value=new DateTime(2026,9,29);AssertUI(weeks.Rows.Count==4,"3주와 나머지 동적 행");
  AssertUI(calendar.Controls.OfType<DayButton>().First(x=>x.AccessibleName.StartsWith("2026-09-25")).Pink,"추석 분홍 표시");
  start.SetTextForTest("20260909");AssertUI(start.Commit()&&start.Value==new DateTime(2026,9,9),"직접 날짜 입력");start.SetTextForTest("2026-02-30");AssertUI(!start.Commit()&&start.Value==new DateTime(2026,9,9),"잘못된 날짜 거부");
  end.Value=new DateTime(2026,9,22);person.Text="홍길동";original.Text="김담당";reason.Text="휴직 대체";identity.Text="900101-1234567";ReadUI();AssertUI(r.IdentityMode=="주민등록번호"&&r.OriginalWorker=="김담당","식별 자동 판단 및 원근로자");
  job.SelectedIndex=11;AssertUI(N("allowance")==70000,"조리실무사 위험수당 참조값");AssertUI(r.Employment&&r.Other==0&&!r.Override&&!r.Separate,"제거된 입력 설정 및 고용보험 자동");
  string saved=Path.Combine(folder,"ui-roundtrip.stpay");Engine.Save(saved,r);AssertUI(Engine.Load(saved).OriginalWorker=="김담당","원근로자 저장 재열기");
  var dateCopy=Engine.Load(saved);AssertUI(dateCopy.Start==r.Start&&dateCopy.End==r.End,"현지 날짜 저장 왕복");
  r.Start=new DateTime(2026,9,2);r.End=new DateTime(2026,9,30);Engine.Fill(r);LoadUI();RefreshAll();AssertUI(weeks.Rows.Count==5,"5주차 동적 생성");AssertUI(holidayFormula.Bottom<=calendarHost.Height,"5주차 산식 영역 표시");
  tabs.SelectedIndex=1;CaptureScreen(Path.Combine(folder,"five-weeks.png"));Show();
  File.WriteAllText(Path.Combine(folder,"test3-report.html"),Reports.Html(r,jobs[r.JobIndex],result,"임금산정표"));
  foreach(int index in new[]{0,1,2,3,4,5}){tabs.SelectedIndex=index;Application.DoEvents();var page=tabs.TabPages[index].Controls.OfType<FlowLayoutPanel>().First();CaptureScreen(Path.Combine(folder,"layout-"+index+".png"));Show();AssertUI(!page.AutoScroll&&page.Controls.Cast<Control>().All(x=>x.Bottom<=page.ClientSize.Height),"스크롤 없는 페이지 "+index+" height="+page.ClientSize.Height+" bottom="+page.Controls.Cast<Control>().Max(x=>x.Bottom));CaptureScreen(Path.Combine(folder,"test3-page-"+index+".png"));Show();}
  r.Start=new DateTime(2026,6,29);r.End=new DateTime(2026,7,15);Engine.Fill(r);LoadUI();RefreshAll();tabs.SelectedIndex=1;Application.DoEvents();
  AssertUI(calendar.Controls.OfType<DayButton>().Count()==17&&calendar.RowCount==4,"contract only: 17 dates and 3 weeks");
  AssertUI(calendar.Controls.OfType<Label>().Any(x=>x.Text.Contains("6월"))&&calendar.Controls.OfType<Label>().Any(x=>x.Text.Contains("7월")),"both month labels");
  AssertUI(!NeedsOutputOptions("임금산정표")&&!NeedsOutputOptions("급여명세서"),"wage export skips metadata");CaptureScreen(Path.Combine(folder,"contract-calendar.png"));
  AssertUI(calendar.Controls.OfType<Label>().Count(x=>x.Text.Contains("6월"))==1&&calendar.Controls.OfType<Label>().Count(x=>x.Text.Contains("7월"))==1,"each month appears once");
  r.Start=new DateTime(2026,8,14);r.End=new DateTime(2026,8,25);Engine.Fill(r);LoadUI();RefreshAll();AssertUI(calendar.Controls.OfType<Label>().Count(x=>x.Text.Contains("8월"))==1,"August appears once");CaptureScreen(Path.Combine(folder,"august-calendar.png"));
  institution.Text="예시초등학교";recipient.Text="가123";manager.Text="김담당";requestRound.SelectedIndex=3;payment.Value=new DateTime(2026,9,25);paymentKnown.Checked=true;ReadUI();var options=CurrentOutputOptions();
  AssertUI(options.Round==4&&options.Recipient=="가123"&&options.Manager=="김담당"&&options.PaymentDate==new DateTime(2026,9,25)&&options.Month==8,"inline export inputs");
  AssertUI(OutputFileName("인건비 신청내역",r,options)=="(가123)예시초등학교_(4차)2026. 8월 - 1개월 미만 기간제근로자(대체근로자)-인건비 신청.xlsx","original request filename pattern");
  string settingsRoundtrip=Path.Combine(folder,"output-options.stpay");Engine.Save(settingsRoundtrip,r);var restored=Engine.Load(settingsRoundtrip);AssertUI(restored.Recipient=="가123"&&restored.Manager=="김담당"&&restored.RequestRound==4&&restored.PaymentDate.HasValue&&restored.OutputMonth==0,"export settings saved with record");
  AssertUI(!reference.Text.Contains("위험수당")&&reference.Font.Size==10,"larger wage comparison without allowance");
  ReadUI();AssertUI(CurrentOutputOptions().Year==2026&&CurrentOutputOptions().Month==8,"automatic contract year/month");
  AssertUI(navButtons.Select(x=>x.MenuIcon).SequenceEqual(Enumerable.Range(0,6)),"six drawn menu icons");AssertUI(Text.Contains("1개월 미만 대체근로자 임금계산기"),"new program name");AssertUI(reference.Text.Contains("적용기준: 통상시급")&&reference.Left>450,"wage comparison right and applied basis");
  tabs.SelectedIndex=3;CaptureScreen(Path.Combine(folder,"output-inline.png"));tabs.SelectedIndex=5;CaptureScreen(Path.Combine(folder,"settings-inline.png"));
  AssertUI(theme.Parent==null&&themeChoices.Count==6,"theme moved from sidebar to six settings cards");
  tabs.SelectedIndex=5;for(int i=0;i<6;i++){themeChoices[i].PerformClick();AssertUI(r.Theme==i&&themeChoices.Count(x=>x.Active)==1&&themeChoices[i].Active,"theme card selection "+i);CaptureScreen(Path.Combine(folder,"test3-theme-"+i+".png"));Show();}
  CaptureScreen(Path.Combine(folder,"theme-settings.png"));
  using(var popup=new CalendarPopup(DateTime.Today,d=>{})){popup.Show(start);Application.DoEvents();using(var bmp=new Bitmap(popup.Width,popup.Height)){popup.DrawToBitmap(bmp,new Rectangle(0,0,bmp.Width,bmp.Height));bmp.Save(Path.Combine(folder,"date-popup.png"));}popup.Close();}
  tabs.SelectedIndex=2;Show();Application.DoEvents();var amount=numbers["within"];amount.Text="４．２５";AssertUI(amount.Text=="4.25"&&amount.ImeMode==ImeMode.Disable,"fullwidth numeric paste normalized");AssertUI(amount.Value==4.25m&&amount.Commit()&&result.OvertimePay>0,"decimal edit updates calculation");amount.Text="-1";AssertUI(!amount.Commit(),"negative amount rejected");amount.Text="0";AssertUI(amount.Commit(),"valid correction");amount.Text="";AssertUI(amount.Commit()&&amount.Value==0,"blank amount commits zero");amount.Focus();amount.SelectAll();Application.DoEvents();CaptureScreen(Path.Combine(folder,"focused-input.png"));
  AssertUI(amount.Controls.Count==0,"numeric input has no native spinner children");AssertUI(reference.Parent.Controls.OfType<FlowLayoutPanel>().All(x=>!x.Bounds.IntersectsWith(reference.Bounds)),"comparison has no overlapping input panel");
  for(int i=0;i<6;i++){tabs.SelectedIndex=i;Show();Application.DoEvents();AssertUI(summary.Visible==(i==3)&&notice.Visible&&notice.Parent==this,"result only in output page, common full-width footer "+i);}
  AssertUI(summary.Text.Contains("임금총액:")&&summary.Text.Contains("공제액:")&&summary.Text.Contains("실지급액:"),"result labels");
  tabs.SelectedIndex=5;institution.Text="자동저장 검증학교";recipient.Text="저장01";manager.Text="검증담당자";contact.Text="1234";theme.SelectedIndex=4;FlushSettings();var persisted=Engine.Parse<Settings>(File.ReadAllText(SettingsFile));AssertUI(persisted.Institution==institution.Text&&persisted.Recipient==recipient.Text&&persisted.Manager==manager.Text&&persisted.Theme==4,"settings atomic automatic persistence");using(var reopened=new MainForm()){AssertUI(reopened.institution.Text==institution.Text&&reopened.theme.SelectedIndex==4,"settings restored after restart");}ResetSettings();persisted=Engine.Parse<Settings>(File.ReadAllText(SettingsFile));AssertUI(persisted.Institution==""&&persisted.Manager==""&&persisted.Theme==4,"institution reset preserves selected theme");
  AssertUI(theme.Items[3].ToString()=="파랑"&&theme.Items[4].ToString()=="살구"&&theme.Items[5].ToString()=="장미","renamed themes");
  for(int i=0;i<6;i++){theme.SelectedIndex=i;tabs.SelectedIndex=1;CaptureScreen(Path.Combine(folder,"calendar-theme-"+i+".png"));tabs.SelectedIndex=3;CaptureScreen(Path.Combine(folder,"result-theme-"+i+".png"));Show();}
  using(var b=new DayButton{State=1,Text="8",Size=new Size(60,48)})using(var bmp=new Bitmap(60,48)){b.DrawToBitmap(bmp,new Rectangle(0,0,60,48));AssertUI(bmp.GetPixel(0,0).ToArgb()==Color.White.ToArgb()&&bmp.GetPixel(59,47).ToArgb()==Color.White.ToArgb(),"first-paint circle corners opaque white");}
  var parsed=PayrollUpdates.Parse("{\"appId\":\"shortpay\",\"version\":\"0.9.0.0\",\"url\":\"https://example.com/release\"}");AssertUI(parsed.Version>new Version(0,8,0,8),"new release detected");bool bad=false;try{PayrollUpdates.Parse("{\"appId\":\"other-app\",\"version\":\"9.0\",\"url\":\"https://example.com\"}");}catch{bad=true;}AssertUI(bad,"other application update rejected");bad=false;try{PayrollUpdates.Parse("{\"appId\":\"shortpay\",\"version\":\"9.0\",\"url\":\"file:///bad.exe\"}");}catch{bad=true;}AssertUI(bad,"invalid release URL rejected");
  ClientSize=new Size(1184,761);tabs.SelectedIndex=2;Application.DoEvents();var moneyPage=tabs.TabPages[2].Controls.OfType<FlowLayoutPanel>().First();AssertUI(moneyPage.Controls.Cast<Control>().All(x=>x.Bottom<=moneyPage.ClientSize.Height),"최소 창 높이 임금 화면 height="+moneyPage.ClientSize.Height+" bottom="+moneyPage.Controls.Cast<Control>().Max(x=>x.Bottom));
  CaptureScreen(Path.Combine(folder,"minimum-money.png"));tabs.SelectedIndex=5;Show();Application.DoEvents();var settingsPage=tabs.TabPages[5].Controls.OfType<FlowLayoutPanel>().First();AssertUI(settingsPage.Controls.Cast<Control>().All(x=>x.Bottom<=settingsPage.ClientSize.Height),"settings including updates fits minimum window");CaptureScreen(Path.Combine(folder,"minimum-settings.png"));tabs.SelectedIndex=3;Show();Application.DoEvents();var outputPage=tabs.TabPages[3].Controls.OfType<FlowLayoutPanel>().First();AssertUI(outputPage.Controls.Cast<Control>().All(x=>x.Bottom<=outputPage.ClientSize.Height),"results and export fit minimum window");CaptureScreen(Path.Combine(folder,"minimum-output.png"));ExerciseStorage(folder);ExerciseGuardUI(folder);File.WriteAllText(Path.Combine(folder,"ui-test-result.txt"),"PASS "+uiChecks+" UI checks, six pages, six themes, popup and minimum window renders");dirty=false;Hide();
 }
}
}
