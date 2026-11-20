using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace SchoolWorkdayCalculator {
// 달력 날짜 우클릭: 재량휴업일 · 임시공휴일 · 약정근무일 지정/해제
internal sealed partial class MainForm {
 static readonly string[] WeekNames={"일","월","화","수","목","금","토"};
 bool InVacation(DateTime date){return (date>=summerStart.Value.Date&&date<=summerEnd.Value.Date)||(date>=winterStart.Value.Date&&date<=winterEnd.Value.Date);}
 int DiscretionarySlot(DateTime date){for(int i=0;i<discretionary.Length;i++)if(discretionary[i].Checked&&discretionary[i].Value.Date==date)return i;return -1;}
 int DiscretionaryUsed(){int used=0;for(int i=0;i<discretionary.Length;i++)if(discretionary[i].Checked)used++;return used;}

 void OnCalendarRightClick(DateTime date,Point screen){
  if(liveDetail==null)return;
  date=date.Date;
  bool weekday=date.DayOfWeek!=DayOfWeek.Saturday&&date.DayOfWeek!=DayOfWeek.Sunday;
  string officialName;bool official=liveDetail.Official.TryGetValue(date,out officialName);
  int slot=DiscretionarySlot(date);bool isDiscretionary=slot>=0;
  bool isExtra=extras.Any(x=>x.Date.Date==date);
  bool inVacation=InVacation(date);
  bool isAuto=liveDetail.AutoWork.Contains(date);
  bool isAgreed=agreedWork.Contains(date);

  var menu=PastelMenu.Create(Font);
  var heading=PastelMenu.Heading(date.ToString("yyyy-MM-dd")+" ("+WeekNames[(int)date.DayOfWeek]+")");
  menu.Items.Add(heading);menu.Items.Add(new ToolStripSeparator());
  int added=0;
  if(!weekday){
   AddInfo(menu,"토·일은 자동 반영됩니다");added++;
  }
  else if(official&&!isExtra&&!isDiscretionary){
   AddInfo(menu,"공휴일 · "+officialName);added++;
  }
  else{
   // 재량휴업일
   if(isDiscretionary){int target=slot;AddAction(menu,"재량휴업일 해제",true,(s,e)=>ClearDiscretionary(target));added++;}
   else if(inVacation){}
   else if(!isExtra){int used=DiscretionaryUsed();DateTime target=date;AddAction(menu,"재량휴업일로 지정 ("+used+"/4)",used<4,(s,e)=>SetDiscretionary(target));added++;}
   // 임시공휴일 (방학 중에도 지정 가능)
   if(isExtra){DateTime target=date;AddAction(menu,"임시공휴일 해제",true,(s,e)=>ClearExtra(target));added++;}
   else if(!isDiscretionary){DateTime target=date;AddAction(menu,"임시공휴일로 지정",true,(s,e)=>SetExtra(target));added++;}
   // 약정근무일 (방학 중 평일)
   if(inVacation&&!isExtra){
    if(isAuto){AddInfo(menu,"방학 자동 근무일");added++;}
    else if(isAgreed){DateTime target=date;AddAction(menu,"약정근무일 해제",true,(s,e)=>{agreedWork.Remove(target);RefreshLivePreview();});added++;}
    else{DateTime target=date;AddAction(menu,"약정근무일로 지정",true,(s,e)=>{agreedWork.Add(target);RefreshLivePreview();});added++;}
   }
  }
  if(added==0){menu.Dispose();return;}
  menu.Closed+=(s,e)=>{BeginInvoke(new Action(menu.Dispose));};
  menu.Show(screen);
 }

 static void AddInfo(ContextMenuStrip menu,string text){PastelMenu.Info(menu,text);}
 static void AddAction(ContextMenuStrip menu,string text,bool enabled,EventHandler handler){PastelMenu.Action(menu,text,enabled,handler);}

 void SetDiscretionary(DateTime date){
  for(int i=0;i<discretionary.Length;i++){
   if(discretionary[i].Checked)continue;
   discretionary[i].Value=date;discretionary[i].Checked=true;
   RefreshLivePreview();
   discretionaryNotes[i].Focus();
   return;
  }
 }
 void ClearDiscretionary(int slot){discretionary[slot].Checked=false;discretionaryNotes[slot].Text="";RefreshLivePreview();}
 void SetExtra(DateTime date){extras.Add(new ExtraHoliday{Date=date,Name="임시공휴일"});extras.Sort((a,b)=>a.Date.CompareTo(b.Date));RefreshExtras();RefreshLivePreview();}
 void ClearExtra(DateTime date){extras.RemoveAll(x=>x.Date.Date==date);RefreshExtras();RefreshLivePreview();}
}
}
