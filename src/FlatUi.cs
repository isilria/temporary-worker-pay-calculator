using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShortPay {
class FlatCalendarLegend:CalendarLegend {
 public FlatCalendarLegend(){AccessibleName="근무 파랑, 비근무 흰색, 결근 노랑, 유급휴일 초록, 주휴 글자 표시";}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Palette.Surface(Parent));string[] names={"근무","비근무","결근","유급휴일","주휴"};Color[] colors={Palette.Accent,Color.White,Color.FromArgb(255,237,183),Color.FromArgb(220,247,230),Color.White};int part=Width/5;
  for(int i=0;i<5;i++){int x=i*part;using(var fill=new SolidBrush(colors[i]))g.FillRectangle(fill,x+1,5,13,13);using(var pen=new Pen(i==4?Palette.Accent:Palette.Edge))g.DrawRectangle(pen,x+1,5,13,13);if(i==4)using(var f=new Font("맑은 고딕",6))TextRenderer.DrawText(g,"주",f,new Rectangle(x+1,4,14,14),Palette.Accent,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);using(var f=new Font("맑은 고딕",9))TextRenderer.DrawText(g,names[i],f,new Rectangle(x+20,0,part-19,24),Palette.Ink,TextFormatFlags.VerticalCenter);}
  using(var f=new Font("맑은 고딕",8))TextRenderer.DrawText(g,"클릭: 근무 → 결근 → 비근무 · 공휴일은 유급휴일 선택 가능",f,new Rectangle(0,24,Width,20),Palette.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
 }
}
class FlatPayrollSummary:PayrollSummary {
 Record record;Result result;
 public override void UpdateResult(Record r,Result z){record=r;result=z;base.UpdateResult(r,z);}
 void Row(Graphics g,string name,decimal amount,Rectangle row,bool bold){using(var font=new Font("맑은 고딕",bold?10:9,bold?FontStyle.Bold:FontStyle.Regular)){TextRenderer.DrawText(g,name,font,new Rectangle(row.X+12,row.Y,row.Width-155,row.Height),Palette.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);TextRenderer.DrawText(g,amount.ToString("N0")+"원",font,new Rectangle(row.Right-145,row.Y,132,row.Height),Palette.Ink,TextFormatFlags.Right|TextFormatFlags.VerticalCenter);}}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Palette.Surface(Parent));if(result==null)return;if(Engine.ContractWarning(record)!=""){TextRenderer.DrawText(g,"계약기간 확인 필요 · 계산 중지",Font,ClientRectangle,Palette.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
  int gap=16,width=(Width-gap)/2,tableHeight=Height-56,totalY=tableHeight-30;var pay=PayrollItems.Pay(record,result);var deduct=PayrollItems.Deduct(record,result);int rows=Math.Max(1,Math.Max(pay.Count,deduct.Count)),rowHeight=Math.Min(27,(totalY-30)/rows);
  for(int side=0;side<2;side++){int x=side==0?0:width+gap;using(var fill=new SolidBrush(side==0?Palette.Mint:Palette.Blush))g.FillRectangle(fill,x,0,width,tableHeight);using(var fill=new SolidBrush(side==0?Color.FromArgb(203,229,215):Color.FromArgb(240,205,219)))g.FillRectangle(fill,x,0,width,28);using(var pen=new Pen(Palette.Edge)){g.DrawRectangle(pen,x,0,width-1,tableHeight-1);g.DrawLine(pen,x,28,x+width,28);g.DrawLine(pen,x,totalY,x+width,totalY);}using(var f=new Font("맑은 고딕",9,FontStyle.Bold)){TextRenderer.DrawText(g,side==0?"지급 내역":"공제 내역",f,new Rectangle(x+12,0,width-160,28),Palette.Ink,TextFormatFlags.VerticalCenter);TextRenderer.DrawText(g,"금액",f,new Rectangle(x+width-145,0,132,28),Palette.Ink,TextFormatFlags.Right|TextFormatFlags.VerticalCenter);}}
  for(int i=0;i<pay.Count;i++)Row(g,pay[i].Name=="기본급"?"근무·유급임금":pay[i].Name,pay[i].Amount,new Rectangle(0,30+i*rowHeight,width,rowHeight),false);
  for(int i=0;i<deduct.Count;i++)Row(g,deduct[i].Name,deduct[i].Amount,new Rectangle(width+gap,30+i*rowHeight,width,rowHeight),false);
  Row(g,"지급 합계",result.Gross,new Rectangle(0,totalY,width,30),true);Row(g,"공제 합계",result.Deductions,new Rectangle(width+gap,totalY,width,30),true);
  var net=new Rectangle(0,Height-46,Width,46);using(var fill=new SolidBrush(Palette.NetTint))g.FillRectangle(fill,net);using(var f=new Font("맑은 고딕",12,FontStyle.Bold))TextRenderer.DrawText(g,"실지급액",f,new Rectangle(15,net.Top,180,46),Palette.Accent,TextFormatFlags.VerticalCenter);using(var f=new Font("맑은 고딕",19,FontStyle.Bold))TextRenderer.DrawText(g,result.Net.ToString("N0")+"원",f,new Rectangle(Width-330,net.Top,312,46),Palette.Accent,TextFormatFlags.Right|TextFormatFlags.VerticalCenter);
 }
}
class CalendarTotals:Control {
 Result result;
 public CalendarTotals(){DoubleBuffered=true;BackColor=Color.White;}
 public void UpdateResult(Result value){result=value;Invalidate();}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Palette.Mint);using(var pen=new Pen(Palette.Edge)){g.DrawRectangle(pen,0,0,Width-1,Height-1);for(int i=1;i<4;i++)g.DrawLine(pen,i*Width/4,13,i*Width/4,Height-13);}if(result==null)return;
  string[] labels={"실제 근무","실근로","유급휴일","주휴"};string[] values={result.Days+"일",result.WorkHours.ToString("0.##")+"시간",result.PaidHolidayDays+"일",result.HolidayHours.ToString("0.####")+"시간"};
  for(int i=0;i<4;i++){var box=new Rectangle(i*Width/4,8,Width/4,24);using(var f=new Font("맑은 고딕",9))TextRenderer.DrawText(g,labels[i],f,box,Palette.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);box.Y=31;box.Height=30;using(var f=new Font("맑은 고딕",15,FontStyle.Bold))TextRenderer.DrawText(g,values[i],f,box,i==3?Palette.Accent:Palette.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
 }
}
public partial class MainForm {
 CalendarLegend calendarLegend;CalendarTotals calendarTotals;Label weeklyHeading,contextStrip;bool calendarLayoutBusy;
 void SyncNavigation(){for(int i=0;i<navButtons.Count;i++){navButtons[i].Active=i==tabs.SelectedIndex;navButtons[i].Invalidate();}}
 void BuildContextStrip(Panel top){var context=new Card{Input=false,BackColor=Color.FromArgb(242,245,249),Padding=new Padding(12,0,12,0),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};context.SetBounds(0,47,top.Width,38);top.Controls.Add(context);contextStrip=L("",10,true);contextStrip.Dock=DockStyle.Fill;contextStrip.Padding=new Padding(2,0,0,0);contextStrip.AutoEllipsis=true;context.Controls.Add(contextStrip);}
 void UpdateContextStrip(){if(contextStrip!=null)contextStrip.Text=(String.IsNullOrWhiteSpace(r.Name)?"대상자 미입력":r.Name)+" / "+jobs[r.JobIndex].name+"    |    계약기간  "+r.Start.ToString("yyyy.MM.dd")+" ~ "+r.End.ToString("yyyy.MM.dd")+"    |    1일 "+r.Hours.ToString("0.##")+"시간";UpdateOutputFolderCaption();}
 int LayoutFlatCalendar(int rows){int width=Math.Max(850,calendarHost.ClientSize.Width);int leftWidth=(width-56)/2;int cell=Math.Min(60,Math.Max(38,(leftWidth-36)/7));cell=Math.Min(cell,(calendarHost.Height-96-30-56)/rows);
  calendar.SetBounds(18,96,36+7*cell,30+rows*cell);contractCaption.SetBounds(18,46,calendar.Width,32);contractCaption.TextAlign=ContentAlignment.MiddleCenter;
  int right=width/2+20,areaWidth=width-right-18,areaHeight=calendarHost.Height;
  if(weeklyTint!=null){weeklyTint.SetBounds(width/2+8,1,width-width/2-10,calendarHost.Height-3);right=12;areaWidth=weeklyTint.Width-24;areaHeight=weeklyTint.Height;}
  weeklyHeading.SetBounds(right,8,areaWidth,28);weeks.SetBounds(right,62,areaWidth,40+Math.Max(1,weeks.Rows.Count)*40+3);holidayFormula.SetBounds(right,weeks.Bottom+14,areaWidth,areaHeight-weeks.Bottom-28);
  calendarLegend.SetBounds(18,calendar.Bottom+12,calendar.Width,44);return cell;
 }
 internal void LoadFlatExample(){r=HolidayTests.Example();r.Name="김대체";r.Institution="예시학교";r.PaymentDate=new DateTime(2026,10,25);r.Theme=0;LoadUI();automaticUpdates.Checked=false;RefreshAll();tabs.SelectedIndex=1;SyncNavigation();dirty=false;}
 ComboBox outputFolderKind;Label outputFolderCaption;
 void BuildOutputFolderBar(Card card){var label=L("저장 위치",9);label.SetBounds(20,253,75,28);card.Controls.Add(label);outputFolderKind=Combo(DocumentNames,155);outputFolderKind.SetBounds(100,254,155,28);card.Controls.Add(outputFolderKind);outputFolderCaption=L("",9);outputFolderCaption.AutoEllipsis=true;outputFolderCaption.SetBounds(265,253,card.Width-435,28);outputFolderCaption.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;card.Controls.Add(outputFolderCaption);outputFolderKind.SelectedIndexChanged+=(s,e)=>UpdateOutputFolderCaption();var open=B("폴더 열기",()=>{int index=outputFolderKind.SelectedIndex;string folder=outputFolders[index].Text.Trim();if(!Path.IsPathRooted(folder)){OutputMessage("설정에서 올바른 저장 폴더를 지정하세요.");return;}try{Directory.CreateDirectory(folder);System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(folder){UseShellExecute=true});}catch(Exception ex){OutputMessage("저장 폴더를 열지 못했습니다.\n"+ex.Message);}},125);open.SetBounds(card.Width-145,252,125,30);open.Anchor=AnchorStyles.Top|AnchorStyles.Right;card.Controls.Add(open);}
 void UpdateOutputFolderCaption(){if(outputFolderCaption==null||outputFolderKind==null||outputFolders[outputFolderKind.SelectedIndex]==null)return;string folder=outputFolders[outputFolderKind.SelectedIndex].Text.Trim();try{outputFolderCaption.Text=String.IsNullOrWhiteSpace(folder)?"설정에서 저장 폴더 지정":new DirectoryInfo(folder).Name;}catch{outputFolderCaption.Text="저장 폴더 확인 필요";}}
}
}
