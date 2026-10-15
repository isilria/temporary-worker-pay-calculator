using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ShortPay {
class PlanDialog:Form {
 public bool Offscreen;
 protected override bool ShowWithoutActivation {get{return Offscreen||base.ShowWithoutActivation;}}
}
public partial class MainForm {
 ComboBox calendarEditMode;SoftButton planButton;ToolTip planTips=new ToolTip{AutoPopDelay=20000};internal Action<Form> InspectPlanDialog;
 void PreserveAttendanceAfterDates(){if(Engine.IsGuard(jobs[r.JobIndex])){Engine.Fill(r,jobs[r.JobIndex]);return;}foreach(string key in r.Days.Keys.ToArray()){DateTime day;if(!DateTime.TryParse(key,out day)||day<r.Start.Date||day>r.End.Date)r.Days.Remove(key);}WorkPlan.PrepareComparison(r);}
 void BuildPlanControls(Card card){var heading=card.Controls.OfType<Label>().First();heading.Text="근무 달력";heading.Width=110;planButton=B("계약·주휴 기준",EditWorkPlan,142);planButton.SetBounds(130,8,142,32);card.Controls.Add(planButton);planButton.BringToFront();calendarEditMode=Combo(new[]{"출근 상태","계약 근무일","통상 비교일"},130);calendarEditMode.AccessibleName="달력 편집 대상";calendarEditMode.SetBounds(280,10,130,28);card.Controls.Add(calendarEditMode);calendarEditMode.BringToFront();calendarEditMode.SelectedIndexChanged+=(s,e)=>{if(!loading)DrawCalendar();};}
 void RefreshPlanControls(){if(planButton==null)return;bool guard=Engine.IsGuard(jobs[r.JobIndex]);planButton.Enabled=calendarEditMode.Enabled=!guard;planButton.Active=!guard&&WorkPlan.Problems(r).Any();planButton.Invalidate();planTips.SetToolTip(planButton,guard?"당직은 기존 별도 계산 기준을 사용합니다.":WorkPlan.Summary(r));if(guard&&calendarEditMode.SelectedIndex!=0)calendarEditMode.SelectedIndex=0;}
 string CalendarDayText(DateTime d,int state,bool prescribed){return Engine.Key(d)+" "+(calendarEditMode!=null&&calendarEditMode.SelectedIndex==1?(prescribed?"계약 근무일":"약정 비근무일"):calendarEditMode!=null&&calendarEditMode.SelectedIndex==2?(WorkPlan.Comparison(r).ContainsKey(d)?"통상 비교근로일":"비교 제외일"):state==1?"근무":state==2?"결근":state==4?"유급휴일":prescribed&&Holidays.Name(d)==""?"출근 미확인":"비근무");}
 void EditCalendarDay(DateTime day){
  string key=Engine.Key(day);int old;r.Days.TryGetValue(key,out old);bool guard=Engine.IsGuard(jobs[r.JobIndex]);int mode=guard?0:calendarEditMode.SelectedIndex;
  if(mode==1){if(r.ScheduleMode==0){OutputMessage("고정 근무의 요일은 ‘계약·주휴 기준’에서 변경하세요. 일자마다 약정일이 다르면 ‘날짜별로 다른 일정’을 선택하세요.");return;}if(r.ContractDays.ContainsKey(key))r.ContractDays.Remove(key);else r.ContractDays[key]=1;}
  else if(mode==2){if(r.ScheduleMode==0){OutputMessage("고정 근무는 확인한 통상근로자의 주 근무요일로 비례 계산합니다. 날짜별 비교일 편집은 날짜별 계약에서 사용하세요.");return;}if(Holidays.Name(day)!=""){OutputMessage("날짜별 비교일의 공휴일 제외 전제를 사용합니다. 이 날짜에 근로의무가 있는 별도 기준이면 자동 확정하지 말고 적용 기준을 확인하세요.");return;}WorkPlan.PrepareComparison(r);if(r.ComparisonDays.ContainsKey(key))r.ComparisonDays.Remove(key);else r.ComparisonDays[key]=1;}
  else if(!guard){bool planned=WorkPlan.Contract(r).ContainsKey(day);if(!planned){if(r.Days.ContainsKey(key))r.Days.Remove(key);else{OutputMessage("먼저 ‘계약 근무일’에서 약정일을 지정하세요. 고정 계약은 ‘계약·주휴 기준’의 근무요일을 확인하세요.");return;}}else if(Holidays.Name(day)!="")r.Days[key]=old==4?1:4;else if(old==1)r.Days[key]=2;else if(old==2||old==3)r.Days.Remove(key);else r.Days[key]=1;}
  else{if(old==1)r.Days[key]=2;else if(old==2||old==3||old==4)r.Days.Remove(key);else r.Days[key]=1;}
  ResetReview();Changed();
 }
 void EditWorkPlan(){
  if(!ValidateChildren())return;ReadUI();
  using(var dialog=new PlanDialog{Text="계약·주휴 기준",ClientSize=new Size(720,395),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,Font=Font,BackColor=Palette.Background,ShowInTaskbar=false}){
   Func<string,int,Label> caption=(text,y)=>{var label=L(text,10,true);label.SetBounds(20,y,680,27);dialog.Controls.Add(label);return label;};
   caption("근로계약서의 근무일과 사업장의 주휴일을 확인하세요.",14);
   var mode=Combo(new[]{"매주 같은 요일로 근무","날짜별로 다른 일정"},280);mode.SetBounds(20,48,280,30);dialog.Controls.Add(mode);mode.SelectedIndex=r.ScheduleMode;
   var rest=Combo(new[]{"일요일","월요일","화요일","수요일","목요일","금요일","토요일"},160);rest.SetBounds(500,48,160,30);dialog.Controls.Add(rest);rest.SelectedIndex=r.WeeklyRestDay;var restLabel=L("실제 주휴일",9);restLabel.SetBounds(396,48,100,30);dialog.Controls.Add(restLabel);
   caption("계약상 근무요일 (고정 근무)",94);var contractChecks=DayChecks(dialog,r.ContractWeekdays,126);
   caption("같은 업무 통상근로자의 근무요일",171);var normalChecks=DayChecks(dialog,r.NormalWeekdays,203);
   var normalHours=new NumericUpDown{Minimum=.25m,Maximum=8,DecimalPlaces=2,Increment=.25m,Value=r.NormalDayHours};normalHours.SetBounds(395,245,100,28);dialog.Controls.Add(normalHours);var hoursLabel=L("통상근로자 1일 소정근로시간",9);hoursLabel.SetBounds(20,243,360,30);dialog.Controls.Add(hoursLabel);
   var explanation=L("",9);explanation.SetBounds(20,283,680,62);dialog.Controls.Add(explanation);
   Action update=()=>{foreach(var check in contractChecks)check.Enabled=mode.SelectedIndex==0;explanation.Text=mode.SelectedIndex==0?"고정 약정: 약정 주시간 ÷ 통상 주 근무일수로 1회 시간을 계산합니다.\n결근·공휴일로 실제 출근일이 달라져도 약정 주시간을 줄이지 않습니다.\n발생 횟수는 입사일부터 연속 7일 구간으로 판단합니다.":"날짜별 약정: 4주 이하 계약은 전체 약정시간 ÷ 같은 기간 통상 비교일수입니다.\n한 계약의 주휴 1회분에는 같은 기준시간을 적용합니다. 법정 공휴일은\n비교일·약정시간에서 제외하며, 발생 여부는 각 7일 구간의 개근 등으로 판단합니다.";};mode.SelectedIndexChanged+=(s,e)=>update();update();
   var apply=B("적용",()=>{},110);apply.SetBounds(466,350,110,32);var cancel=B("취소",()=>dialog.Close(),110);cancel.SetBounds(590,350,110,32);dialog.Controls.Add(apply);dialog.Controls.Add(cancel);dialog.CancelButton=cancel;
   apply.Click+=(s,e)=>{var candidate=Engine.Parse<Record>(Engine.Json(r));candidate.Start=r.Start;candidate.End=r.End;candidate.PaymentDate=r.PaymentDate;bool normalChanged=candidate.NormalWeekdays!=Mask(normalChecks);bool becameIrregular=candidate.ScheduleMode==0&&mode.SelectedIndex==1;candidate.ScheduleMode=mode.SelectedIndex;candidate.ContractWeekdays=Mask(contractChecks);candidate.NormalWeekdays=Mask(normalChecks);candidate.NormalDayHours=normalHours.Value;candidate.WeeklyRestDay=rest.SelectedIndex;if(becameIrregular)candidate.ContractDays=WorkPlan.Contract(r).ToDictionary(x=>Engine.Key(x.Key),x=>1);WorkPlan.PrepareComparison(candidate,normalChanged);var problems=WorkPlan.Problems(candidate);if(problems.Length>0){MessageBox.Show(dialog,String.Join("\n",problems),"기준 확인");return;}r=candidate;dirty=true;dialog.DialogResult=DialogResult.OK;dialog.Close();};
   if(InspectPlanDialog!=null){dialog.Offscreen=true;dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(SystemInformation.VirtualScreen.Right+200,SystemInformation.VirtualScreen.Top);dialog.Show();dialog.PerformLayout();Application.DoEvents();InspectPlanDialog(dialog);dialog.Hide();if(dialog.DialogResult==DialogResult.OK)RefreshAll();return;}
   if(dialog.ShowDialog(this)==DialogResult.OK)RefreshAll();
  }
 }
 CheckBox[] DayChecks(Form owner,int mask,int y){string[] names={"일","월","화","수","목","금","토"};return Enumerable.Range(0,7).Select(i=>{var box=new CheckBox{Text=names[i],Checked=(mask&(1<<i))!=0,AutoSize=false};box.SetBounds(20+i*95,y,86,30);owner.Controls.Add(box);return box;}).ToArray();}
 static int Mask(CheckBox[] boxes){return Enumerable.Range(0,7).Where(i=>boxes[i].Checked).Sum(i=>1<<i);}
}
}
