using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace ShortPay {
public partial class MainForm {
 Card contractCard;Panel weekdayField,weekendField,guardFields;Label contractHint;CheckBox employmentExcluded,unmannedSaturday,unmannedSunday;TextBox employmentReason;
 void BuildContractFields(Control page){
  contractCard=Section(page,"계약 및 근무 조건",174);start=new DateInput();end=new DateInput();start.ValueChanged+=(s,e)=>DateChanged();end.ValueChanged+=(s,e)=>DateChanged();hours=Num("hours",24,2);hours.Minimum=.25m;
  weekdayField=Field("일 소정근로시간",hours,165);weekendField=Field("주말 소정근로시간",Num("weekend",24,2),165);numbers["weekend"].Minimum=.25m;
  Fields(contractCard,42,Field("계약 시작일",start,165),Field("계약 종료일",end,165),weekdayField,weekendField);
  var row=contractCard.Controls.OfType<FlowLayoutPanel>().Last();row.Width=704;row.Anchor=AnchorStyles.Top|AnchorStyles.Left;
  var go=B("근무 달력 →",()=>tabs.SelectedIndex=1,150);go.Active=true;go.SetBounds(780,70,150,38);go.Anchor=AnchorStyles.Top|AnchorStyles.Right;contractCard.Controls.Add(go);
  var refill=B("근무일 자동 편성",()=>{ReadUI();ResetReview();Engine.Fill(r,jobs[r.JobIndex]);Changed();},150);refill.SetBounds(780,115,150,36);refill.Anchor=AnchorStyles.Top|AnchorStyles.Right;contractCard.Controls.Add(refill);
  var unmanned=new FlowLayoutPanel{BackColor=Color.Transparent,WrapContents=false,Padding=new Padding(4,3,0,0)};
  unmannedSaturday=new CheckBox{Text="토요일",Checked=true,AutoSize=true,BackColor=Color.Transparent};
  unmannedSunday=new CheckBox{Text="일요일",AutoSize=true,BackColor=Color.Transparent};
  unmanned.Controls.Add(unmannedSaturday);unmanned.Controls.Add(unmannedSunday);
  unmannedSaturday.CheckedChanged+=(s,e)=>SelectUnmannedDay(unmannedSaturday,unmannedSunday,6);
  unmannedSunday.CheckedChanged+=(s,e)=>SelectUnmannedDay(unmannedSunday,unmannedSaturday,0);
  Fields(contractCard,115,Field("야간가산수당 지급일",Num("nightDays",31),220),Field("휴일근무 가산수당 지급일",Num("publicDays",31),250),Field("무인경비요일",unmanned,240));guardFields=contractCard.Controls.OfType<FlowLayoutPanel>().Last();guardFields.Width=750;guardFields.Anchor=AnchorStyles.Top|AnchorStyles.Left;
  contractHint=L("날짜를 직접 입력하거나 달력 버튼으로 선택하세요. 계약기간 내 평일이 자동 선택됩니다.",9);contractHint.SetBounds(20,122,900,70);contractCard.Controls.Add(contractHint);
 }
 void UpdateContractFields(){bool guard=Engine.IsGuard(jobs[job.SelectedIndex]);hours.Maximum=guard?24:8;weekdayField.Controls.OfType<Label>().First().Text=guard?"평일 소정근로시간":"일 소정근로시간";weekendField.Visible=guard;guardFields.Visible=guard;contractCard.Height=guard?272:174;if(numbers.ContainsKey("base")){numbers["base"].Parent.Parent.Controls.OfType<Label>().First().Text=guard?"월 기본급 (당직 기준)":"월 기본급 (8시간 기준)";numbers["overtime"].Parent.Parent.Controls.OfType<Label>().First().Text=guard?"추가근로(1배) · 시간":"연장근로(1.5배) · 시간";}contractHint.Top=guard?190:122;contractHint.Height=guard?70:45;contractHint.Width=guard?900:740;contractHint.Text=guard?"야간가산: 지급일 × 1.5시간 × 통상시급 × 50%\n공휴일 추가근로: 지급일 × 기본급 ÷ 월 임금산정시간 × 3\n계약 시작일 기준 1인 매일 / 2인 격일 · 무인경비요일 월 1~4번째 및 설·추석 3일 제외":"날짜를 직접 입력하거나 달력 버튼으로 선택하세요. 계약기간 내 평일이 자동 선택됩니다.";}
 void SelectUnmannedDay(CheckBox selected,CheckBox other,int day){
  if(loading)return;loading=true;
  // Exactly one remains selected, including when the active checkbox is clicked again.
  selected.Checked=true;other.Checked=false;r.UnmannedDay=day;loading=false;
  ResetReview();Engine.Fill(r,jobs[job.SelectedIndex]);Changed();
 }
 void LoadGuardFields(){unmannedSaturday.Checked=r.UnmannedDay==6;unmannedSunday.Checked=r.UnmannedDay==0;hours.Maximum=Engine.IsGuard(jobs[r.JobIndex])?24:8;Set("weekend",r.WeekendHours);Set("nightDays",r.NightDays);Set("publicDays",r.PublicHolidayDays);employmentExcluded.Checked=!r.Employment;employmentReason.Text=r.EmploymentExclusionReason;employmentReason.Enabled=employmentExcluded.Checked;UpdateContractFields();}
 void ReadGuardFields(){r.UnmannedDay=unmannedSunday.Checked?0:6;r.WeekendHours=N("weekend");r.NightDays=N("nightDays");r.PublicHolidayDays=N("publicDays");r.NightHoursPerDay=1.5m;r.Employment=!employmentExcluded.Checked;r.EmploymentExclusionReason=employmentReason.Text;}
 void AddEmploymentExclusion(Card card){
  employmentLine.Width=330;employmentLine.Font=new Font("맑은 고딕",8);
  employmentExcluded=new CheckBox{Text="고용보험 적용 제외 대상",ForeColor=Color.FromArgb(163,38,48),Font=new Font("맑은 고딕",9,FontStyle.Bold),BackColor=Color.Transparent};employmentExcluded.SetBounds(352,167,193,24);card.Controls.Add(employmentExcluded);
  var box=new Card{Input=true,Padding=new Padding(10,6,10,5),LineColor=Color.FromArgb(204,164,168),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};box.SetBounds(549,162,384,34);card.Controls.Add(box);employmentReason=TextInput();employmentReason.BorderStyle=BorderStyle.None;employmentReason.AccessibleName="고용보험 적용 제외 사유";employmentReason.Dock=DockStyle.Fill;employmentReason.BackColor=Palette.Soft;employmentReason.ForeColor=Color.FromArgb(163,38,48);employmentReason.Font=new Font("맑은 고딕",9,FontStyle.Bold);box.Controls.Add(employmentReason);employmentReason.Enabled=false;
  employmentExcluded.CheckedChanged+=(s,e)=>{employmentReason.Enabled=employmentExcluded.Checked;Changed();};
 }
 void UpdateAllowanceInputs(){bool previous=loading;loading=true;bool eligible=result.Average>=15;foreach(var key in new[]{"meal","allowance"}){var input=numbers[key];bool wasEnabled=input.Enabled;input.Enabled=eligible;if(!eligible)input.Text="";else if(!wasEnabled)input.Value=key=="meal"?r.Meal:r.Allowance;}loading=previous;}
}
}
