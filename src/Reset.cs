using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShortPay {
public partial class MainForm {
 SoftButton resetAllButton;
 internal Func<bool> ConfirmReset = () => MessageBox.Show("현재 입력한 대상자·계약·근무 달력·임금·공제와 기관 정보를 모두 초기화할까요?\n저장하지 않은 입력은 사라집니다.\n\n저장된 파일과 출력 문서는 삭제하지 않으며, 테마와 저장 폴더 설정은 유지합니다.","전체 초기화",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.Yes;
 void BuildResetButton(Panel top){
  resetAllButton=B("초기화",ResetAll,150);resetAllButton.Name="resetAllButton";resetAllButton.AccessibleName="전체 초기화";resetAllButton.Active=true;resetAllButton.CausesValidation=false;
  resetAllButton.SetBounds(Math.Max(620,top.ClientSize.Width-150),0,150,38);resetAllButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;top.Controls.Add(resetAllButton);
  top.Resize+=(s,e)=>resetAllButton.Left=top.ClientSize.Width-resetAllButton.Width;
 }
 void ResetAll(){
  if(!ConfirmReset())return;
  if(settingsTimer!=null)settingsTimer.Stop();
  int selectedTheme=theme.SelectedIndex;
  r=new Record{Start=DateTime.Today,End=DateTime.Today.AddDays(6),Theme=selectedTheme};
  var j=jobs[r.JobIndex];r.MonthlyBase=j.@base;r.Meal=j.meal;r.Allowance=j.allowances.Where(x=>x.Key!="기관근무수당").Sum(x=>x.Value);
  currentFile=null;lastContractWarning="";
  LoadUI();RefreshAll();dirty=false;tabs.SelectedIndex=0;
  ScheduleSettingsSave();FlushSettings();person.Focus();
 }
 internal void ExerciseReset(string folder){
  Show();Application.DoEvents();int checks=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("Reset test failed: "+name);checks++;};
  string existing=Path.Combine(folder,"reset-preserved.stpay");File.WriteAllText(existing,"saved-file-must-stay");
  job.SelectedIndex=jobs.FindIndex(x=>x.name.Contains("당직전담실무원"));person.Text="초기화 검사";identity.Text="9001011234567";bank.Text="은행";account.Text="1234";holder.Text="예금주";reason.Text="사유";original.Text="원근로자";
  institution.Text="기관";contact.Text="연락처";recipient.Text="수신";manager.Text="담당";Set("tax",1234);Set("extra",4567);Set("nightDays",2);Set("publicDays",1);employmentExcluded.Checked=true;employmentReason.Text="검사 사유";paymentKnown.Checked=true;requestRound.SelectedIndex=2;theme.SelectedIndex=3;
  string savedFolder=outputFolders[0].Text;bool auto=automaticUpdates.Checked;currentFile=existing;r.Days[Engine.Key(r.Start)]=2;string before=Engine.Json(r);ConfirmReset=()=>false;resetAllButton.PerformClick();check(Engine.Json(r)==before&&currentFile==existing,"cancel preserves current work");
  ConfirmReset=()=>true;resetAllButton.PerformClick();
  check(person.Text==""&&identity.Text==""&&bank.Text==""&&account.Text==""&&holder.Text==""&&reason.Text==""&&original.Text=="","person fields cleared");
  check(r.Days.Count==0&&result.Days==0&&result.Gross==0,"calendar and result cleared");
  check(N("tax")==0&&N("extra")==0&&N("nightDays")==0&&N("publicDays")==0,"pay inputs cleared");
  check(!employmentExcluded.Checked&&employmentReason.Text==""&&r.Employment,"exemption cleared");
  check(!paymentKnown.Checked&&requestRound.SelectedIndex==0,"output options reset");
  check(start.Value.Date==DateTime.Today&&end.Value.Date==DateTime.Today.AddDays(6)&&job.SelectedIndex==2&&hours.Value==8,"contract defaults restored");
  check(institution.Text==""&&contact.Text==""&&recipient.Text==""&&manager.Text=="","institution cleared");
  var settings=Engine.Parse<Settings>(File.ReadAllText(SettingsFile));check(settings.Institution==""&&settings.Contact==""&&settings.Manager==""&&settings.Recipient=="","cleared settings persisted");
  check(theme.SelectedIndex==3&&outputFolders[0].Text==savedFolder&&automaticUpdates.Checked==auto,"preferences preserved");
  check(currentFile==null&&!dirty&&File.ReadAllText(existing)=="saved-file-must-stay","saved file preserved and detached");
  using(var reopened=new MainForm()){check(reopened.institution.Text==""&&reopened.manager.Text=="","no stale institution after restart");}
  check(resetAllButton.Size==new Size(150,38)&&resetAllButton.Active&&!resetAllButton.CausesValidation,"calendar button style and size");
  CaptureScreen(Path.Combine(folder,"reset-empty-screen.png"));ClientSize=new Size(1200,800);PerformLayout();check(resetAllButton.Right<=resetAllButton.Parent.ClientSize.Width&&resetAllButton.Left>610,"minimum width header layout");CaptureScreen(Path.Combine(folder,"reset-minimum-screen.png"));
  File.WriteAllText(Path.Combine(folder,"reset-tests.txt"),"PASS "+checks+" reset checks");
 }
}
}
