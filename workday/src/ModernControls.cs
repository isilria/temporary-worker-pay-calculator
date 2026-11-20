using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
namespace SchoolWorkdayCalculator {
// 직접 그리는 입력 컨트롤 모음. 윈도우 기본 DateTimePicker / NumericUpDown 을 대체한다.
internal static class Ui {
 internal static readonly Color InputEdge=Color.FromArgb(205,211,226),InputHot=Color.FromArgb(160,172,198),Mint=Color.FromArgb(222,247,240),MintHot=Color.FromArgb(234,248,243),MintDown=Color.FromArgb(205,239,228),Teal=Color.FromArgb(24,113,102),Sun=Color.FromArgb(205,72,84),Sat=Color.FromArgb(66,100,192),Faint=Color.FromArgb(176,184,202),Disabled=Color.FromArgb(241,243,247);
 const TextFormatFlags Left=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine;
 internal static TextFormatFlags LeftFlags{get{return Left;}}
 internal static Color Ground(Control c){return c.Parent==null?FlatDesign.Background:c.Parent.BackColor;}
 internal static void Chevron(Graphics g,float cx,float cy,float r,int dir,Color color,float width){
  using(var pen=new Pen(color,width)){
   pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;
   PointF a,b,c;
   if(dir==0){a=new PointF(cx-r,cy+r/2);b=new PointF(cx,cy-r/2);c=new PointF(cx+r,cy+r/2);}
   else if(dir==1){a=new PointF(cx-r,cy-r/2);b=new PointF(cx,cy+r/2);c=new PointF(cx+r,cy-r/2);}
   else if(dir==2){a=new PointF(cx+r/2,cy-r);b=new PointF(cx-r/2,cy);c=new PointF(cx+r/2,cy+r);}
   else{a=new PointF(cx-r/2,cy-r);b=new PointF(cx+r/2,cy);c=new PointF(cx-r/2,cy+r);}
   g.DrawLines(pen,new[]{a,b,c});
  }
 }
 // 달력 아이콘 (18 x 18 기준)
 internal static void CalendarGlyph(Graphics g,float x,float y,float size,Color color){
  var state=g.Save();g.TranslateTransform(x,y);g.ScaleTransform(size/18F,size/18F);
  using(var pen=new Pen(color,1.5F))using(var brush=new SolidBrush(color)){
   pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;
   using(var body=Pastel.Round(new RectangleF(1.5F,3.5F,15F,13F),3F))g.DrawPath(pen,body);
   g.DrawLine(pen,1.5F,7.5F,16.5F,7.5F);
   g.DrawLine(pen,5.5F,1.2F,5.5F,4.8F);g.DrawLine(pen,12.5F,1.2F,12.5F,4.8F);
   g.FillRectangle(brush,5F,10F,2F,2F);g.FillRectangle(brush,8F,10F,2F,2F);g.FillRectangle(brush,11F,10F,2F,2F);g.FillRectangle(brush,5F,13F,2F,2F);g.FillRectangle(brush,8F,13F,2F,2F);
  }
  g.Restore(state);
 }
 internal static void CheckBox(Graphics g,RectangleF r,bool on,bool hot){
  using(var path=Pastel.Round(r,4F)){
   using(var fill=new SolidBrush(on?Teal:Color.White))g.FillPath(fill,path);
   using(var pen=new Pen(on?Teal:(hot?Teal:InputHot),1.3F))g.DrawPath(pen,path);
  }
  if(on)using(var tick=new Pen(Color.White,1.9F)){
   tick.StartCap=LineCap.Round;tick.EndCap=LineCap.Round;tick.LineJoin=LineJoin.Round;
   g.DrawLines(tick,new[]{new PointF(r.X+r.Width*0.24F,r.Y+r.Height*0.52F),new PointF(r.X+r.Width*0.43F,r.Y+r.Height*0.72F),new PointF(r.X+r.Width*0.76F,r.Y+r.Height*0.30F)});
  }
 }
 internal static float Scale(Control c){using(var g=c.CreateGraphics())return g.DpiX/96F;}
}

// 날짜 입력칸: 둥근 입력칸 + 달력 아이콘, 클릭하면 직접 그린 달력이 열린다. 선택 체크박스(선택 안 함 상태)를 지원한다.
internal sealed class DateBox:Control {
 internal static readonly DateTime MinimumDateTime=new DateTime(1753,1,1);
 DateTime value=DateTime.Today,min=MinimumDateTime,max=new DateTime(9998,12,31);
 bool showCheck,isChecked=true,hot,hotCheck,open;int closedTick=-1000;
 internal event EventHandler ValueChanged;
 internal DateBox(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable|ControlStyles.StandardClick,true);TabStop=true;Font=new Font("맑은 고딕",10.5F);Size=new Size(140,34);}
 internal DateTime Value{get{return value;}set{DateTime v=Clamp(value.Date);if(this.value==v)return;this.value=v;Invalidate();RaiseChanged();}}
 internal DateTime MinDate{get{return min;}set{min=value.Date;if(max<min)max=min;if(this.value<min){this.value=min;RaiseChanged();}Invalidate();}}
 internal DateTime MaxDate{get{return max;}set{max=value.Date;if(min>max)min=max;if(this.value>max){this.value=max;RaiseChanged();}Invalidate();}}
 internal bool ShowCheckBox{get{return showCheck;}set{if(showCheck==value)return;showCheck=value;Invalidate();}}
 internal bool Checked{get{return isChecked;}set{if(isChecked==value)return;isChecked=value;Invalidate();RaiseChanged();}}
 DateTime Clamp(DateTime v){return v<min?min:(v>max?max:v);}
 void RaiseChanged(){if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}
 Rectangle CheckRect(){return new Rectangle(10,(Height-16)/2,16,16);}
 bool OverCheck(Point p){return showCheck&&p.X<32;}
 protected override bool IsInputKey(Keys keyData){Keys k=keyData&Keys.KeyCode;return k==Keys.Up||k==Keys.Down||k==Keys.PageUp||k==Keys.PageDown||base.IsInputKey(keyData);}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.Clear(Ui.Ground(this));g.SmoothingMode=SmoothingMode.AntiAlias;
  bool active=Focused||open;
  using(var path=Pastel.Round(new RectangleF(0.5F,0.5F,Width-2,Height-2),8F)){
   using(var fill=new SolidBrush(Enabled?Color.White:Ui.Disabled))g.FillPath(fill,path);
   using(var pen=new Pen(active?Ui.Teal:(hot&&Enabled?Ui.InputHot:Ui.InputEdge),active?1.6F:1F))g.DrawPath(pen,path);
  }
  int x=12;bool off=showCheck&&!isChecked;
  if(showCheck){Rectangle box=CheckRect();Ui.CheckBox(g,new RectangleF(box.X,box.Y,box.Width,box.Height),isChecked,hotCheck&&Enabled);x=36;}
  string text=off?"날짜 선택":value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
  TextRenderer.DrawText(g,text,Font,new Rectangle(x,0,Math.Max(10,Width-x-34),Height),off||!Enabled?FlatDesign.Muted:FlatDesign.Ink,Ui.LeftFlags);
  Ui.CalendarGlyph(g,Width-28,(Height-18)/2F,18F,off||!Enabled?Ui.Faint:Ui.Teal);
 }
 protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hot=true;Invalidate();}
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hot=false;hotCheck=false;Invalidate();}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);bool over=OverCheck(e.Location);if(over!=hotCheck){hotCheck=over;Invalidate();}Cursor=Cursors.Default;}
 protected override void OnMouseDown(MouseEventArgs e){
  base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||!Enabled)return;Focus();
  if(OverCheck(e.Location)){Checked=!Checked;return;}
  if(Environment.TickCount-closedTick<250)return;
  ShowPopup();
 }
 protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
 protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
 protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Invalidate();}
 protected override void OnKeyDown(KeyEventArgs e){
  base.OnKeyDown(e);
  if(e.KeyCode==Keys.Space&&showCheck){Checked=!Checked;e.Handled=true;}
  else if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter||e.KeyCode==Keys.F4||(e.Alt&&e.KeyCode==Keys.Down)){ShowPopup();e.Handled=true;}
  else if(e.KeyCode==Keys.Up||e.KeyCode==Keys.Down){Step(e.KeyCode==Keys.Up?1:-1,false);e.Handled=true;}
  else if(e.KeyCode==Keys.PageUp||e.KeyCode==Keys.PageDown){Step(e.KeyCode==Keys.PageUp?1:-1,true);e.Handled=true;}
 }
 void Step(int direction,bool month){if(showCheck&&!isChecked)return;Value=month?value.AddMonths(direction):value.AddDays(direction);}
 void ShowPopup(){
  if(open||!IsHandleCreated)return;
  var picker=new MonthPicker(value,min,max);float scale=Ui.Scale(this);picker.Size=new Size((int)Math.Round(284*scale),(int)Math.Round(318*scale));
  var host=new ToolStripControlHost(picker){AutoSize=false,Size=picker.Size,Margin=Padding.Empty,Padding=Padding.Empty};
  var drop=new ToolStripDropDown{AutoSize=true,Padding=Padding.Empty,Margin=Padding.Empty,DropShadowEnabled=true,BackColor=Color.White};
  drop.Items.Add(host);
  picker.DateChosen+=delegate(DateTime chosen){drop.Close();Value=chosen;if(showCheck)Checked=true;};
  drop.Closed+=delegate{open=false;closedTick=Environment.TickCount;Invalidate();if(IsHandleCreated)BeginInvoke(new Action(delegate{drop.Dispose();}));};
  open=true;Invalidate();drop.Show(this,new Point(0,Height+4));
 }
}

// DateBox 가 여는 한 달 달력
internal sealed class MonthPicker:Control {
 internal event Action<DateTime> DateChosen;
 DateTime shown,selected,min,max;int hotCell=-1,hotNav=-1;
 const int Pad=14;
 static readonly string[] Week={"일","월","화","수","목","금","토"};
 static readonly Font TitleF=new Font("맑은 고딕",11F,FontStyle.Bold),DayF=new Font("맑은 고딕",9.5F),DayB=new Font("맑은 고딕",9.5F,FontStyle.Bold),HeadF=new Font("맑은 고딕",8.5F,FontStyle.Bold),TodayF=new Font("맑은 고딕",9F,FontStyle.Bold);
 const TextFormatFlags Centered=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine;
 internal MonthPicker(DateTime selectedDate,DateTime minDate,DateTime maxDate){
  SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
  selected=selectedDate.Date;min=minDate.Date;max=maxDate.Date;shown=new DateTime(selected.Year,selected.Month,1);BackColor=Color.White;
 }
 float U{get{return Width/284F;}}
 Rectangle NavRect(int id){float u=U;int size=(int)(28*u),y=(int)(10*u);int x;
  if(id==0)x=(int)(Pad*u);else if(id==1)x=(int)((Pad+30)*u);else if(id==2)x=Width-(int)((Pad+58)*u);else x=Width-(int)((Pad+28)*u);
  return new Rectangle(x,y,size,size);}
 Rectangle TodayRect(){float u=U;return new Rectangle((int)(Pad*u),Height-(int)(44*u),Width-(int)(Pad*2*u),(int)(34*u));}
 RectangleF GridArea(){float u=U;float top=72*u,bottom=Height-52*u;return new RectangleF(Pad*u,top,Width-Pad*2*u,bottom-top);}
 DateTime FirstCell(){return shown.AddDays(-(int)shown.DayOfWeek);}
 Rectangle CellRect(int index){RectangleF a=GridArea();float cw=a.Width/7F,ch=a.Height/6F;return Rectangle.Round(new RectangleF(a.X+(index%7)*cw,a.Y+(index/7)*ch,cw,ch));}
 int CellAt(Point p){RectangleF a=GridArea();if(!a.Contains(p))return -1;int col=(int)((p.X-a.X)/(a.Width/7F)),row=(int)((p.Y-a.Y)/(a.Height/6F));return row*7+col;}
 int NavAt(Point p){for(int i=0;i<4;i++)if(NavRect(i).Contains(p))return i;return TodayRect().Contains(p)?4:-1;}
 DateTime MinMonth(){return new DateTime(min.Year,min.Month,1);}
 DateTime MaxMonth(){return new DateTime(max.Year,max.Month,1);}
 DateTime NavTarget(int id){
  DateTime t=id==0?shown.AddYears(-1):id==1?shown.AddMonths(-1):id==2?shown.AddMonths(1):shown.AddYears(1);
  if(t<MinMonth())t=MinMonth();if(t>MaxMonth())t=MaxMonth();return t;
 }
 bool InRange(DateTime d){return d>=min&&d<=max;}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.Clear(Color.White);g.SmoothingMode=SmoothingMode.AntiAlias;float u=U;
  using(var pen=new Pen(Ui.InputEdge))g.DrawRectangle(pen,0,0,Width-1,Height-1);
  // 머리: « ‹ 제목 › »
  for(int i=0;i<4;i++){
   Rectangle r=NavRect(i);bool can=NavTarget(i)!=shown;
   if(can&&hotNav==i)using(var path=Pastel.Round(new RectangleF(r.X,r.Y,r.Width,r.Height),8F))using(var brush=new SolidBrush(Ui.Mint))g.FillPath(brush,path);
   Color ink=can?FlatDesign.Ink:Ui.Faint;float cx=r.X+r.Width/2F,cy=r.Y+r.Height/2F,rad=4.2F*u;
   if(i==0||i==3){int dir=i==0?2:3;float off=3F*u;Ui.Chevron(g,cx-off,cy,rad,dir,ink,1.7F);Ui.Chevron(g,cx+off,cy,rad,dir,ink,1.7F);}
   else Ui.Chevron(g,cx,cy,rad,i==1?2:3,ink,1.9F);
  }
  TextRenderer.DrawText(g,shown.Year+"년 "+shown.Month+"월",TitleF,new Rectangle((int)((Pad+60)*u),(int)(10*u),Width-(int)((Pad+60)*2*u),(int)(28*u)),FlatDesign.Ink,Centered);
  // 요일
  RectangleF a=GridArea();float cw=a.Width/7F,ch=a.Height/6F;
  for(int c=0;c<7;c++)TextRenderer.DrawText(g,Week[c],HeadF,new Rectangle((int)(a.X+c*cw),(int)(48*u),(int)cw,(int)(22*u)),c==0?Ui.Sun:c==6?Ui.Sat:FlatDesign.Muted,Centered);
  // 날짜
  DateTime first=FirstCell();DateTime today=DateTime.Today;
  for(int i=0;i<42;i++){
   DateTime d=first.AddDays(i);bool inMonth=d.Month==shown.Month,ok=InRange(d),sel=d==selected;
   Rectangle cell=CellRect(i);cell.Inflate(-2,-2);
   var cellF=new RectangleF(cell.X,cell.Y,cell.Width,cell.Height);
   Color text;bool bold=false;
   if(sel){using(var path=Pastel.Round(cellF,8F))using(var brush=new SolidBrush(Ui.Teal))g.FillPath(brush,path);text=Color.White;bold=true;}
   else{
    if(i==hotCell&&ok)using(var path=Pastel.Round(cellF,8F))using(var brush=new SolidBrush(Ui.Mint))g.FillPath(brush,path);
    if(d==today)using(var path=Pastel.Round(cellF,8F))using(var pen=new Pen(Ui.Teal,1.4F))g.DrawPath(pen,path);
    text=!ok?Color.FromArgb(205,210,222):!inMonth?Ui.Faint:d.DayOfWeek==DayOfWeek.Sunday?Ui.Sun:d.DayOfWeek==DayOfWeek.Saturday?Ui.Sat:FlatDesign.Ink;
    bold=d==today&&ok;
   }
   TextRenderer.DrawText(g,d.Day.ToString(CultureInfo.InvariantCulture),bold?DayB:DayF,cell,text,Centered);
  }
  // 오늘 버튼
  Rectangle tr=TodayRect();
  using(var path=Pastel.Round(new RectangleF(tr.X,tr.Y,tr.Width,tr.Height),9F))using(var brush=new SolidBrush(hotNav==4?Ui.MintDown:Ui.Mint))g.FillPath(brush,path);
  TextRenderer.DrawText(g,"오늘  "+today.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),TodayF,tr,Ui.Teal,Centered);
 }
 protected override void OnMouseMove(MouseEventArgs e){
  base.OnMouseMove(e);int nav=NavAt(e.Location),cell=nav>=0?-1:CellAt(e.Location);
  if(nav!=hotNav||cell!=hotCell){hotNav=nav;hotCell=cell;Invalidate();}
 }
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);if(hotNav!=-1||hotCell!=-1){hotNav=-1;hotCell=-1;Invalidate();}}
 protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);Navigate(e.Delta>0?1:2);}
 void Navigate(int id){DateTime t=NavTarget(id);if(t!=shown){shown=t;Invalidate();}}
 protected override void OnMouseDown(MouseEventArgs e){
  base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;
  int nav=NavAt(e.Location);
  if(nav>=0&&nav<4){Navigate(nav);return;}
  if(nav==4){DateTime today=DateTime.Today;if(InRange(today)){if(DateChosen!=null)DateChosen(today);}else{shown=new DateTime(today.Year,today.Month,1);if(shown<MinMonth())shown=MinMonth();if(shown>MaxMonth())shown=MaxMonth();Invalidate();}return;}
  int cell=CellAt(e.Location);if(cell<0)return;
  DateTime d=FirstCell().AddDays(cell);if(!InRange(d))return;
  if(DateChosen!=null)DateChosen(d);
 }
}

// 숫자 입력칸(학년도): 둥근 입력칸 + 오른쪽 위/아래 화살표. 직접 입력(4자리), 휠, 방향키, 길게 누르기를 지원한다.
internal sealed class StepBox:Control {
 decimal min=0,max=100,value;string buffer="";int hotPart,downPart;readonly Timer repeat=new Timer();int repeatDirection;
 internal event EventHandler ValueChanged;
 const int SpinWidth=22;
 internal StepBox(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true);TabStop=true;Font=new Font("맑은 고딕",10F);Size=new Size(82,30);repeat.Tick+=delegate{repeat.Interval=70;Step(repeatDirection);};}
 protected override void Dispose(bool disposing){if(disposing)repeat.Dispose();base.Dispose(disposing);}
 internal decimal Minimum{get{return min;}set{min=value;if(max<min)max=min;if(this.value<min)Value=min;Invalidate();}}
 internal decimal Maximum{get{return max;}set{max=value;if(min>max)min=max;if(this.value>max)Value=max;Invalidate();}}
 internal decimal Value{get{return value;}set{decimal v=value<min?min:(value>max?max:value);buffer="";if(this.value==v){Invalidate();return;}this.value=v;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
 public override string Text{get{return ((int)value).ToString(CultureInfo.InvariantCulture);}set{int n;if(int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out n))Value=n;}}
 void Step(int direction){Value=value+direction;}
 int PartAt(Point p){if(p.X<Width-SpinWidth)return 0;return p.Y<Height/2?1:2;}
 Rectangle PartRect(int part){int half=Height/2;return part==1?new Rectangle(Width-SpinWidth,0,SpinWidth,half):new Rectangle(Width-SpinWidth,half,SpinWidth,Height-half);}
 protected override bool IsInputKey(Keys keyData){Keys k=keyData&Keys.KeyCode;return k==Keys.Up||k==Keys.Down||k==Keys.PageUp||k==Keys.PageDown||base.IsInputKey(keyData);}
 protected override void OnPaint(PaintEventArgs e){
  var g=e.Graphics;g.Clear(Ui.Ground(this));g.SmoothingMode=SmoothingMode.AntiAlias;
  using(var path=Pastel.Round(new RectangleF(0.5F,0.5F,Width-2,Height-2),8F)){
   using(var fill=new SolidBrush(Enabled?Color.White:Ui.Disabled))g.FillPath(fill,path);
   var state=g.Save();g.SetClip(path);
   for(int part=1;part<=2;part++){bool can=part==1?value<max:value>min;if(!can||!Enabled)continue;if(hotPart==part)using(var brush=new SolidBrush(downPart==part?Ui.MintDown:Ui.Mint))g.FillRectangle(brush,PartRect(part));}
   g.Restore(state);
   using(var pen=new Pen(Focused?Ui.Teal:(hotPart>0?Ui.InputHot:Ui.InputEdge),Focused?1.6F:1F))g.DrawPath(pen,path);
  }
  using(var divider=new Pen(Ui.InputEdge)){g.DrawLine(divider,Width-SpinWidth,6,Width-SpinWidth,Height-6);g.DrawLine(divider,Width-SpinWidth+5,Height/2,Width-6,Height/2);}
  float cx=Width-SpinWidth/2F;
  Ui.Chevron(g,cx,Height/4F+0.5F,2.8F,0,Enabled&&value<max?Ui.Teal:Ui.Faint,1.8F);
  Ui.Chevron(g,cx,Height*3/4F-0.5F,2.8F,1,Enabled&&value>min?Ui.Teal:Ui.Faint,1.8F);
  string text=buffer.Length>0?buffer:Text;
  Rectangle textRect=new Rectangle(10,0,Width-SpinWidth-12,Height);
  TextRenderer.DrawText(g,text,Font,textRect,Enabled?FlatDesign.Ink:FlatDesign.Muted,Ui.LeftFlags);
  if(buffer.Length>0&&Focused){Size size=TextRenderer.MeasureText(g,text,Font,new Size(200,40),Ui.LeftFlags);using(var caret=new Pen(FlatDesign.Ink))g.DrawLine(caret,10+size.Width+1,Height/2-8,10+size.Width+1,Height/2+8);}
 }
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);int part=PartAt(e.Location);if(part!=hotPart){hotPart=part;Invalidate();}}
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);if(hotPart!=0||downPart!=0){hotPart=0;Invalidate();}}
 protected override void OnMouseDown(MouseEventArgs e){
  base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||!Enabled)return;Focus();
  int part=PartAt(e.Location);if(part==0)return;
  downPart=part;repeatDirection=part==1?1:-1;Step(repeatDirection);repeat.Interval=400;repeat.Start();Invalidate();
 }
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);repeat.Stop();if(downPart!=0){downPart=0;Invalidate();}}
 protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);if(Focused&&Enabled)Step(e.Delta>0?1:-1);}
 protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
 protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Commit();Invalidate();}
 protected override void OnKeyDown(KeyEventArgs e){
  base.OnKeyDown(e);
  if(e.KeyCode==Keys.Up){Step(1);e.Handled=true;}else if(e.KeyCode==Keys.Down){Step(-1);e.Handled=true;}
  else if(e.KeyCode==Keys.PageUp){Step(10);e.Handled=true;}else if(e.KeyCode==Keys.PageDown){Step(-10);e.Handled=true;}
  else if(e.KeyCode==Keys.Enter){Commit();e.Handled=true;}
  else if(e.KeyCode==Keys.Escape){buffer="";Invalidate();e.Handled=true;}
  else if(e.KeyCode==Keys.Back&&buffer.Length>0){buffer=buffer.Substring(0,buffer.Length-1);Invalidate();e.Handled=true;}
 }
 protected override void OnKeyPress(KeyPressEventArgs e){
  base.OnKeyPress(e);
  if(e.KeyChar>='0'&&e.KeyChar<='9'){if(buffer.Length>=4)buffer="";buffer+=e.KeyChar;if(buffer.Length==4)Commit();else Invalidate();e.Handled=true;}
 }
 void Commit(){
  if(buffer.Length==0)return;
  int n;string text=buffer;buffer="";
  if(int.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out n))Value=n;else Invalidate();
 }
}
}
namespace SchoolWorkdayCalculator {
// 우클릭 메뉴: 둥근 호버, 색 점, 얇은 구분선
internal sealed class PastelMenuRenderer:ToolStripProfessionalRenderer {
 internal PastelMenuRenderer():base(new PastelColors()){RoundedEdges=false;}
 protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){using(var brush=new SolidBrush(Color.White))e.Graphics.FillRectangle(brush,e.AffectedBounds);}
 protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){using(var pen=new Pen(Ui.InputEdge))e.Graphics.DrawRectangle(pen,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);}
 protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
 protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){
  if(!e.Item.Selected||!e.Item.Enabled)return;
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
  var r=new RectangleF(4,e.Item.Bounds.Height>0?1:0,e.Item.Width-8,e.Item.Height-2);
  using(var path=Pastel.Round(r,7F))using(var brush=new SolidBrush(Ui.Mint))g.FillPath(brush,path);
 }
 protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e){using(var pen=new Pen(FlatDesign.Edge))e.Graphics.DrawLine(pen,12,e.Item.Height/2,e.Item.Width-12,e.Item.Height/2);}
 protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){
  if(e.Item is ToolStripLabel&&!(e.Item is ToolStripMenuItem))e.TextColor=FlatDesign.Ink;
  else e.TextColor=!e.Item.Enabled?FlatDesign.Muted:e.Item.Selected?Ui.Teal:FlatDesign.Ink;
  base.OnRenderItemText(e);
 }
 sealed class PastelColors:ProfessionalColorTable{
  public override Color ToolStripDropDownBackground{get{return Color.White;}}
  public override Color MenuBorder{get{return Ui.InputEdge;}}
  public override Color ImageMarginGradientBegin{get{return Color.White;}}
  public override Color ImageMarginGradientMiddle{get{return Color.White;}}
  public override Color ImageMarginGradientEnd{get{return Color.White;}}
 }
}
internal static class PastelMenu {
 static readonly Bitmap[] Dots={Dot(Color.FromArgb(240,150,165)),Dot(Color.FromArgb(226,110,125)),Dot(Color.FromArgb(240,180,90)),Dot(Color.FromArgb(190,198,214))};
 static Bitmap Dot(Color color){var bmp=new Bitmap(16,16);using(var g=Graphics.FromImage(bmp)){g.SmoothingMode=SmoothingMode.AntiAlias;using(var b=new SolidBrush(color))g.FillEllipse(b,3,3,10,10);}return bmp;}
 internal static ContextMenuStrip Create(Font font){
  return new ContextMenuStrip{Font=new Font(font.FontFamily,10F),Renderer=new PastelMenuRenderer(),ShowImageMargin=true,ShowCheckMargin=false,Padding=new Padding(0,4,0,4),DropShadowEnabled=true,BackColor=Color.White};
 }
 internal static ToolStripItem Heading(string text){return new ToolStripLabel(text){Font=new Font("맑은 고딕",10F,FontStyle.Bold),Padding=new Padding(10,6,10,6),ForeColor=FlatDesign.Ink};}
 internal static void Info(ContextMenuStrip menu,string text){var item=new ToolStripMenuItem(text){Enabled=false,Padding=new Padding(4,5,10,5),Image=Dots[3]};menu.Items.Add(item);}
 internal static void Action(ContextMenuStrip menu,string text,bool enabled,EventHandler handler){
  int tone=text.StartsWith("재량")?0:text.StartsWith("임시")?1:2;
  var item=new ToolStripMenuItem(text){Enabled=enabled,Padding=new Padding(4,5,10,5),Image=enabled?Dots[tone]:Dots[3]};
  item.Click+=handler;menu.Items.Add(item);
 }
}
}
