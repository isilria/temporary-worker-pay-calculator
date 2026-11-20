using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace SchoolWorkdayCalculator {
internal sealed partial class MainForm {
 Timer previewTimer;bool liveReady;internal CalculationResult liveResult;internal string previewError="";
 void InitializeLivePreview(){previewTimer=new Timer{Interval=60};previewTimer.Tick+=(s,e)=>{previewTimer.Stop();RefreshLivePreview();};liveReady=true;year.ValueChanged+=(s,e)=>QueuePreview();foreach(var date in new[]{summerStart,summerEnd,winterStart,winterEnd}.Concat(discretionary))date.ValueChanged+=(s,e)=>QueuePreview();RefreshLivePreview();}
 protected override void Dispose(bool disposing){if(disposing){liveReady=false;if(previewTimer!=null)previewTimer.Dispose();}base.Dispose(disposing);}
 void QueuePreview(){if(!liveReady)return;previewTimer.Stop();previewTimer.Start();}
 // 값이 같으면 다시 그리지 않아 깜박임을 줄인다.
 static void SetText(Label label,string text){if(label!=null&&label.Text!=text)label.Text=text;}
 void RefreshLivePreview(){
  if(!liveReady)return;previewTimer.Stop();
  try{
   var settings=BuildSettings(false);
   liveDetail=WorkbookBuilder.Analyze(settings);liveResult=liveDetail.Result;previewError="";
   SetText(ruleValue,liveResult.SeasonalPaidDays+"일");
   SetText(modeHeading,"소정근로일수\r\n"+liveResult.SeasonalWorkDays+"일");
   int difference=liveResult.SeasonalPaidDays-320;
   SetText(modeSubheading,nonConsentMode?"비동의자 · 입력 기준\r\n엑셀 추가 근무 제외":"320일 기준 "+(difference==0?"충족":Math.Abs(difference)+"일 "+(difference<0?"부족":"초과"))+"\r\n동의자 · 입력 기준");
   int agreed=liveDetail.Agreed.Count,auto=nonConsentMode?0:liveDetail.AutoWork.Count;
   SetText(planValue,nonConsentMode?"약정근무일 "+agreed+"일\r\n달력 우클릭으로 지정":"방학 근무 편성 "+(auto+agreed)+"일\r\n자동 "+auto+" · 약정 "+agreed);
   ruleValue.ForeColor=FlatDesign.Ink;
   calendarView.SetDetail(liveDetail);
  }
  catch(Exception ex){
   liveDetail=null;liveResult=null;previewError=ex.Message;
   SetText(ruleValue,"—");ruleValue.ForeColor=FlatDesign.Muted;SetText(modeHeading,"입력값을 확인해 주세요");SetText(modeSubheading,ex.Message);SetText(planValue,"");
   calendarView.SetError(ex.Message);
  }
  if(discCard!=null)discCard.SetChip(DiscretionaryUsed()+" / 4");
  if(clearAgreedButton!=null){bool any=liveDetail!=null&&liveDetail.Agreed.Count>0;if(clearAgreedButton.Enabled!=any)clearAgreedButton.Enabled=any;}
 }
}
}
