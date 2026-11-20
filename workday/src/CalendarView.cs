using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace SchoolWorkdayCalculator {
internal sealed class BufferedPanel:Panel {
 internal BufferedPanel(){DoubleBuffered=true;}
}
internal sealed class BufferedTable:TableLayoutPanel {
 internal BufferedTable(){DoubleBuffered=true;}
}
// 둥근 모서리 카드. 제목 띠가 있는 카드와 없는 요약 타일을 한 클래스로 처리한다.
internal sealed class CardPanel:Panel {
 internal string Title="",Sub="",Chip="";
 internal Color HeaderFill=Color.White,BodyFill=Color.White;
 internal bool HasHeader=true;
 static readonly Font TitleFont=new Font("맑은 고딕",10.5F,FontStyle.Bold),SubFont=new Font("맑은 고딕",8.5F),ChipFont=new Font("맑은 고딕",8.5F,FontStyle.Bold);
 internal CardPanel(){DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;}
 internal void SetChip(string text){if(Chip==text)return;Chip=text;Invalidate(new Rectangle(0,0,Width,38));}
 protected override void OnPaintBackground(PaintEventArgs e){
  var g=e.Graphics;g.Clear(Parent==null?FlatDesign.Background:Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
  using(var path=Pastel.Round(new RectangleF(0.5F,0.5F,Width-2,Height-2),12)){
   using(var body=new SolidBrush(BodyFill))g.FillPath(body,path);
   if(HasHeader){var state=g.Save();g.SetClip(path);using(var head=new SolidBrush(HeaderFill))g.FillRectangle(head,0,0,Width,36);g.Restore(state);}
   using(var pen=new Pen(FlatDesign.Edge))g.DrawPath(pen,path);
  }
  if(!HasHeader)return;
  var flags=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine;
  Size titleSize=TextRenderer.MeasureText(g,Title,TitleFont,new Size(400,36),flags);
  TextRenderer.DrawText(g,Title,TitleFont,new Rectangle(16,0,titleSize.Width+2,36),FlatDesign.Ink,flags);
  if(Sub.Length>0)TextRenderer.DrawText(g,Sub,SubFont,new Rectangle(16+titleSize.Width+10,0,Math.Max(10,Width-titleSize.Width-110),36),FlatDesign.Muted,flags);
  if(Chip.Length>0){
   Size chipSize=TextRenderer.MeasureText(g,Chip,ChipFont,new Size(200,24),flags|TextFormatFlags.HorizontalCenter);
   var pill=new Rectangle(Width-16-chipSize.Width-16,8,chipSize.Width+16,20);
   using(var path=Pastel.Round(pill,10))using(var brush=new SolidBrush(Color.White))g.FillPath(brush,path);
   TextRenderer.DrawText(g,Chip,ChipFont,pill,FlatDesign.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);
  }
 }
}
// 동의자 / 비동의자 전환 버튼
internal sealed class SegButton:Button {
 internal bool Active;
 internal SegButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;UseVisualStyleBackColor=false;Cursor=Cursors.Hand;}
 internal void SetActive(bool value){if(Active==value)return;Active=value;Invalidate();}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;Color ground=Parent==null?Color.White:Parent.BackColor;g.Clear(ground);g.SmoothingMode=SmoothingMode.AntiAlias;
  if(Active)using(var path=Pastel.Round(new RectangleF(0,0,Width-1,Height-1),9))using(var brush=new SolidBrush(FlatDesign.Blue))g.FillPath(brush,path);
  TextRenderer.DrawText(g,Text,Font,ClientRectangle,Active?Color.White:FlatDesign.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);
 }
}
// 범례 줄
internal sealed class LegendBar:Control {
 static readonly Font LabelFont=new Font("맑은 고딕",8.5F);
 static readonly string[] Names={"근로일","방학 자동","약정근무일","휴일·휴업","유급 토","유급 일","방학 무급","직접 지정"};
 internal LegendBar(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.Clear(Parent==null?FlatDesign.Background:Parent.BackColor);
  Color[] fills={CalendarView.CWork,CalendarView.CAuto,CalendarView.CAgreed,CalendarView.CHoliday,CalendarView.CSat,CalendarView.CSun,CalendarView.COff,Color.White};
  int x=0,cy=Height/2;
  for(int i=0;i<Names.Length;i++){
   var box=new RectangleF(x,cy-7,14,14);g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var path=Pastel.Round(box,4F)){
    using(var brush=new SolidBrush(fills[i]))g.FillPath(brush,path);
    using(var pen=new Pen(i==0?CalendarView.CWorkEdge:i==7?CalendarView.CMarkEdge:fills[i]))g.DrawPath(pen,path);
   }
   if(i==7)using(var brush=new SolidBrush(FlatDesign.Ink))g.FillEllipse(brush,box.Right-6,box.Top+2,4,4);
   Size size=TextRenderer.MeasureText(g,Names[i],LabelFont,new Size(300,20),TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);
   TextRenderer.DrawText(g,Names[i],LabelFont,new Rectangle(x+20,0,size.Width+2,Height),FlatDesign.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);
   x+=20+size.Width+16;
  }
 }
}
// 12개월 달력. 직접 그려서 빠르고 깜박임이 없다. 값이 바뀔 때만 다시 그린다.
internal sealed class CalendarView:Control {
 internal static readonly Color CWork=Color.FromArgb(255,249,222),CWorkEdge=Color.FromArgb(240,228,176),CMarkEdge=Color.FromArgb(201,207,221),CAuto=Color.FromArgb(24,113,102),CAgreed=Color.FromArgb(249,206,152),CHoliday=Color.FromArgb(250,221,232),CHolidayText=Color.FromArgb(138,28,20),CWeekend=Color.FromArgb(223,231,247),CSat=Color.FromArgb(206,223,252),CSatText=Color.FromArgb(38,70,160),CSun=Color.FromArgb(253,206,202),CSunText=Color.FromArgb(160,38,48),CHover=Color.FromArgb(226,216,250),COff=Color.FromArgb(231,233,238),COffText=Color.FromArgb(86,97,121);
 static readonly SolidBrush BWork=new SolidBrush(CWork),BAuto=new SolidBrush(CAuto),BAgreed=new SolidBrush(CAgreed),BHoliday=new SolidBrush(CHoliday),BWeekend=new SolidBrush(CWeekend),BSat=new SolidBrush(CSat),BSun=new SolidBrush(CSun),BHover=new SolidBrush(CHover),BOff=new SolidBrush(COff),BInk=new SolidBrush(FlatDesign.Ink),BBox=new SolidBrush(Color.White);
 static readonly Pen PWorkEdge=new Pen(CWorkEdge),PBox=new Pen(FlatDesign.Edge),PHover=new Pen(Color.FromArgb(24,113,102),2F),PTodayRing=new Pen(Color.FromArgb(24,113,102),1.2F),PShadow=new Pen(Color.FromArgb(234,237,244));
 static readonly Font TitleFont=new Font("맑은 고딕",9.5F,FontStyle.Bold),SubFont=new Font("맑은 고딕",8.5F),DayFont=new Font("맑은 고딕",8.5F),DayBold=new Font("맑은 고딕",8.5F,FontStyle.Bold),HeadFont=new Font("맑은 고딕",8F,FontStyle.Bold),MessageFont=new Font("맑은 고딕",11F);
 static readonly string[] WeekNames={"일","월","화","수","목","금","토"};
 const int Gap=12,Pad=10,TitleHeight=34,HeadHeight=18;
 static DateTime Today{get{return DateTime.Today;}}
 const TextFormatFlags Centered=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine;
 CalendarDetail detail;string error="";readonly int[] paid=new int[12];DateTime hover=DateTime.MinValue;
 readonly ToolTip tip=new ToolTip{InitialDelay=300,ReshowDelay=100,AutoPopDelay=8000};
 internal event Action<DateTime,Point> DateRightClick;
 internal CalendarView(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);AccessibleName="연간 근무 달력";AccessibleRole=AccessibleRole.Table;}
 protected override void Dispose(bool disposing){if(disposing)tip.Dispose();base.Dispose(disposing);}
 internal void SetDetail(CalendarDetail value){
  detail=value;error="";hover=DateTime.MinValue;Array.Clear(paid,0,paid.Length);
  if(value!=null)for(int i=0;i<value.Codes.Length;i++)if(value.Codes[i]!="N")paid[MonthIndex(value.Start.AddDays(i))]++;
  Invalidate();
 }
 internal void SetError(string message){detail=null;error=message??"";Invalidate();}
 static int MonthIndex(DateTime date){return (date.Month+9)%12;}
 static int MonthOfIndex(int index){return (index+2)%12+1;}
 RectangleF MonthBox(int index){float w=(Width-Gap*3)/4F,h=(Height-Gap*2)/3F;int col=index%4,row=index/4;return new RectangleF(col*(w+Gap),row*(h+Gap),w,h);}
 RectangleF GridArea(RectangleF box){float top=box.Y+TitleHeight+HeadHeight;return new RectangleF(box.X+Pad,top,box.Width-Pad*2,box.Bottom-Pad-top);}
 Rectangle CellRect(DateTime date){
  RectangleF area=GridArea(MonthBox(MonthIndex(date)));int offset=(int)new DateTime(date.Year,date.Month,1).DayOfWeek;int position=offset+date.Day-1;
  float cw=area.Width/7F,ch=area.Height/6F;
  return Rectangle.Round(new RectangleF(area.X+(position%7)*cw,area.Y+(position/7)*ch,cw,ch));
 }
 bool HitTest(Point point,out DateTime date){
  date=DateTime.MinValue;if(detail==null)return false;
  for(int i=0;i<12;i++){
   RectangleF box=MonthBox(i);if(!box.Contains(point))continue;
   RectangleF area=GridArea(box);if(!area.Contains(point))return false;
   int month=MonthOfIndex(i),year=month>=3?detail.Start.Year:detail.Start.Year+1;
   int col=(int)((point.X-area.X)/(area.Width/7F)),row=(int)((point.Y-area.Y)/(area.Height/6F));
   int day=row*7+col-(int)new DateTime(year,month,1).DayOfWeek+1;
   if(col<0||col>6||row<0||row>5||day<1||day>DateTime.DaysInMonth(year,month))return false;
   date=new DateTime(year,month,day);return true;
  }
  return false;
 }
 string Describe(DateTime date){
  string code=detail.CodeOn(date),name;string holiday=detail.Holidays.TryGetValue(date,out name)?name:"";
  string kind;bool weekend=date.DayOfWeek==DayOfWeek.Saturday||date.DayOfWeek==DayOfWeek.Sunday;
  if(detail.Discretionary.Contains(date))kind="재량휴업일";
  else if(detail.Extra.Contains(date))kind="임시·기타 휴일";
  else if(code=="W")kind=detail.Agreed.Contains(date)?"약정근무일":"근로일";
  else if(code=="A")kind="방학 근무일(자동 편성)";
  else if(code=="H")kind="공휴일 유급";
  else if(code=="S")kind="유급 토요일";
  else if(code=="U")kind="유급 일요일";
  else kind=weekend?"무급 휴무":"방학 무급";
  return holiday.Length>0?kind+" · "+holiday:kind;
 }
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.Clear(Parent==null?FlatDesign.Background:Parent.BackColor);
  if(detail==null){TextRenderer.DrawText(g,error.Length>0?error:"입력값을 확인해 주세요.",MessageFont,ClientRectangle,FlatDesign.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);return;}
  for(int i=0;i<12;i++){RectangleF box=MonthBox(i);if(e.ClipRectangle.IntersectsWith(Rectangle.Ceiling(box)))DrawMonth(g,i,box);}
 }
 void DrawMonth(Graphics g,int index,RectangleF box){
  int month=MonthOfIndex(index),year=month>=3?detail.Start.Year:detail.Start.Year+1;
  g.SmoothingMode=SmoothingMode.AntiAlias;
  using(var shadow=Pastel.Round(new RectangleF(box.X+0.5F,box.Y+1.5F,box.Width-1,box.Height-1),12))g.DrawPath(PShadow,shadow);
  using(var path=Pastel.Round(new RectangleF(box.X+0.5F,box.Y+0.5F,box.Width-1,box.Height-1),12)){g.FillPath(BBox,path);g.DrawPath(PBox,path);}
  var left=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine;
  TextRenderer.DrawText(g,year+"."+month+"월",TitleFont,new Rectangle((int)box.X+12,(int)box.Y+5,100,22),FlatDesign.Ink,left);
  string paidText="유급 "+paid[index]+"일";int pillW=TextRenderer.MeasureText(g,paidText,SubFont,new Size(200,20),TextFormatFlags.NoPadding|TextFormatFlags.SingleLine).Width+14;
  var pill=new Rectangle((int)box.Right-10-pillW,(int)box.Y+6,pillW,20);
  using(var pillPath=Pastel.Round(new RectangleF(pill.X,pill.Y,pill.Width,pill.Height),10F))using(var pillBrush=new SolidBrush(Ui.Mint))g.FillPath(pillBrush,pillPath);
  TextRenderer.DrawText(g,paidText,SubFont,pill,Ui.Teal,Centered);
  RectangleF area=GridArea(box);float cw=area.Width/7F,ch=area.Height/6F;
  for(int c=0;c<7;c++)TextRenderer.DrawText(g,WeekNames[c],HeadFont,new Rectangle((int)(area.X+c*cw),(int)area.Y-HeadHeight,(int)cw,HeadHeight),c==0?Ui.Sun:c==6?Ui.Sat:FlatDesign.Muted,Centered);
  int offset=(int)new DateTime(year,month,1).DayOfWeek,days=DateTime.DaysInMonth(year,month);
  for(int day=1;day<=days;day++){
   DateTime date=new DateTime(year,month,day);int position=offset+day-1;
   RectangleF cell=new RectangleF(area.X+(position%7)*cw+1.5F,area.Y+(position/7)*ch+1.5F,cw-3,ch-3);
   string code=detail.CodeOn(date);bool agreed=detail.Agreed.Contains(date),bold=false;SolidBrush fill;Color text=FlatDesign.Ink;
   if(code=="W"&&agreed){fill=BAgreed;bold=true;}
   else if(code=="W"){fill=BWork;}
   else if(code=="A"){fill=BAuto;text=Color.White;bold=true;}
   else if(code=="H"){fill=BHoliday;text=CHolidayText;bold=true;}
   else if(code=="S"){fill=BSat;text=CSatText;bold=detail.Holidays.ContainsKey(date);}
   else if(code=="U"){fill=BSun;text=CSunText;bold=detail.Holidays.ContainsKey(date);}
   else{fill=BOff;text=COffText;}
   if(date==hover){fill=BHover;text=FlatDesign.Ink;}
   using(var cellPath=Pastel.Round(cell,5F)){
    g.FillPath(fill,cellPath);
    if(code=="W"&&!agreed)g.DrawPath(PWorkEdge,cellPath);
    if(date==Today)g.DrawPath(PTodayRing,cellPath);
   }
   if(agreed||detail.Discretionary.Contains(date)||detail.Extra.Contains(date))g.FillEllipse(BInk,cell.Right-8,cell.Top+3,5,5);
   TextRenderer.DrawText(g,day.ToString(),bold?DayBold:DayFont,Rectangle.Round(cell),text,Centered);
  }
 }
 protected override void OnMouseMove(MouseEventArgs e){
  base.OnMouseMove(e);DateTime date;
  if(!HitTest(e.Location,out date)){ClearHover();return;}
  if(date==hover)return;
  DateTime old=hover;hover=date;
  if(old!=DateTime.MinValue)Invalidate(CellRect(old));
  Invalidate(CellRect(date));
  tip.SetToolTip(this,Describe(date));
 }
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);ClearHover();}
 void ClearHover(){if(hover==DateTime.MinValue)return;DateTime old=hover;hover=DateTime.MinValue;if(detail!=null)Invalidate(CellRect(old));tip.SetToolTip(this,"");}
 protected override void OnMouseUp(MouseEventArgs e){
  base.OnMouseUp(e);DateTime date;
  if(e.Button==MouseButtons.Right&&HitTest(e.Location,out date)&&DateRightClick!=null)DateRightClick(date,PointToScreen(e.Location));
 }
}
}
