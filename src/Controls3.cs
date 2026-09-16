using System;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Globalization;
namespace ShortPay {
class PageTabs:TabControl {
 protected override void WndProc(ref Message m){if(m.Msg==0x1328 && !DesignMode){m.Result=(IntPtr)1;return;}base.WndProc(ref m);}
 public PageTabs(){Appearance=TabAppearance.FlatButtons;ItemSize=new Size(0,1);SizeMode=TabSizeMode.Fixed;}
}
class PlainNumber:TextBox {
 decimal current;bool formatting;public decimal Minimum=0,Maximum=100000000;public int DecimalPlaces;public bool ThousandsSeparator=true;public event EventHandler ValueChanged;
 public PlainNumber(){BorderStyle=BorderStyle.None;ImeMode=ImeMode.Disable;Text="0";}
 public decimal Value{get{return current;}set{decimal next=Math.Max(Minimum,Math.Min(Maximum,value));bool changed=current!=next;current=next;FormatValue();if(changed&&ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
 void FormatValue(){formatting=true;Text=current.ToString((ThousandsSeparator?"N":"F")+DecimalPlaces,CultureInfo.CurrentCulture);formatting=false;}
 internal static string NormalizeDigits(string text){return new string(text.Select(c=>c>='\uFF01'&&c<='\uFF5E'?(char)(c-0xFEE0):c=='\u3000'?' ':c).ToArray());}
 protected override void OnTextChanged(EventArgs e){if(formatting){base.OnTextChanged(e);return;}string normalized=NormalizeDigits(Text);if(normalized!=Text){int caret=SelectionStart;formatting=true;Text=normalized;SelectionStart=Math.Min(caret,TextLength);formatting=false;}base.OnTextChanged(e);decimal next;if(Decimal.TryParse(Text,NumberStyles.Number,CultureInfo.CurrentCulture,out next)&&next>=Minimum&&next<=Maximum&&next!=current){current=next;if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
 internal bool Commit(){decimal next;if(String.IsNullOrWhiteSpace(Text))next=0;else if(!Decimal.TryParse(Text,NumberStyles.Number,CultureInfo.CurrentCulture,out next))return false;if(next<Minimum||next>Maximum)return false;Value=Decimal.Round(next,DecimalPlaces,MidpointRounding.AwayFromZero);return true;}
 protected override void OnValidating(System.ComponentModel.CancelEventArgs e){if(!Commit()){e.Cancel=true;SelectAll();System.Media.SystemSounds.Beep.Play();}base.OnValidating(e);}
}
class DayButton:Button {
 public int State;public bool Pink;
 public DayButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);UseVisualStyleBackColor=false;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.White;Cursor=Cursors.Hand;Font=new Font("맑은 고딕",14,FontStyle.Bold);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Color.White);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float d=Math.Min(Width,Height)-8;var r=new RectangleF((Width-d)/2,(Height-d)/2,d,d);Color blue=Color.FromArgb(65,120,211);Color ink=Enabled?blue:Color.FromArgb(190,195,207);
  if(Text!=""&&(State!=0||Pink)){Color fill=State==1?blue:State==2?Color.FromArgb(248,204,83):Color.FromArgb(252,218,230);using(var b=new SolidBrush(fill))e.Graphics.FillEllipse(b,r);ink=State==1?Color.White:State==2?Color.FromArgb(118,84,15):Color.FromArgb(171,89,121);if(Pink&&State!=0){using(var pen=new Pen(Color.FromArgb(235,150,183),3))e.Graphics.DrawEllipse(pen,r);}}
  if(Focused)using(var pen=new Pen(Palette.Accent,1))e.Graphics.DrawEllipse(pen,r);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
 }
}
class DateInput:UserControl {
 TextBox input;SoftButton button;DateTime value=DateTime.Today;public event EventHandler ValueChanged;
 public DateTime Value{get{return value;}set{if(value.Date<this.MinDate||value.Date>this.MaxDate)return;bool changed=this.value!=value.Date;this.value=value.Date;input.Text=this.value.ToString("yyyy-MM-dd");if(changed&&ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
 public DateTime MinDate{get{return new DateTime(2020,1,1);}}public DateTime MaxDate{get{return new DateTime(2050,12,31);}}
 public DateInput(){BackColor=Palette.Soft;input=new TextBox{ImeMode=ImeMode.Disable,BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,BackColor=Palette.Soft,Text=value.ToString("yyyy-MM-dd")};button=new SoftButton{Text="▦",Dock=DockStyle.Right,Width=32,AccessibleName="달력 열기"};Controls.Add(input);Controls.Add(button);input.Validating+=(s,e)=>{if(!Commit()){e.Cancel=true;input.SelectAll();}};input.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){Commit();e.SuppressKeyPress=true;}};button.Click+=(s,e)=>ShowCalendar();}
 internal bool Commit(){DateTime d;string raw=PlainNumber.NormalizeDigits(input.Text).Trim();if(DateTime.TryParseExact(raw,new[]{"yyyy-MM-dd","yyyyMMdd","yyyy.MM.dd","yyyy/MM/dd"},CultureInfo.InvariantCulture,DateTimeStyles.None,out d)&&d>=MinDate&&d<=MaxDate){Value=d;return true;}input.Text=value.ToString("yyyy-MM-dd");return false;}
 internal void SetTextForTest(string text){input.Text=text;}
 internal void ShowCalendar(){var popup=new CalendarPopup(Value,d=>Value=d);popup.Show(this);}
}
class CalendarPopup:ToolStripDropDown {
 Panel panel;TableLayoutPanel days;Label title;DateTime view;Action<DateTime> choose;
 public CalendarPopup(DateTime selected,Action<DateTime> callback){choose=callback;view=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);Padding=new Padding(8);BackColor=Color.White;AutoClose=true;panel=new Panel{Size=new Size(294,310),BackColor=Color.White};title=new Label{TextAlign=ContentAlignment.MiddleCenter,Font=new Font("맑은 고딕",12,FontStyle.Bold)};title.SetBounds(52,7,188,33);panel.Controls.Add(title);
  var prev=new SoftButton{Text="‹"};prev.SetBounds(4,6,38,34);prev.Click+=(s,e)=>{if(view.Year>2020||view.Month>1){view=view.AddMonths(-1);Draw();}};panel.Controls.Add(prev);var next=new SoftButton{Text="›"};next.SetBounds(252,6,38,34);next.Click+=(s,e)=>{if(view.Year<2050||view.Month<12){view=view.AddMonths(1);Draw();}};panel.Controls.Add(next);
  days=new TableLayoutPanel{ColumnCount=7,RowCount=7,Location=new Point(2,49),Size=new Size(290,252)};for(int i=0;i<7;i++){days.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/7));days.RowStyles.Add(new RowStyle(SizeType.Percent,100f/7));}panel.Controls.Add(days);Items.Add(new ToolStripControlHost(panel){Margin=Padding.Empty,Padding=Padding.Empty,AutoSize=false,Size=panel.Size});Draw();}
 void Draw(){while(days.Controls.Count>0){var c=days.Controls[0];days.Controls.Remove(c);c.Dispose();}title.Text=view.ToString("yyyy년 M월");string[] names={"일","월","화","수","목","금","토"};for(int i=0;i<7;i++)days.Controls.Add(new Label{Text=names[i],Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter},i,0);for(int i=0;i<42;i++){DateTime date=view.AddDays(i-(int)view.DayOfWeek);var b=new DayButton{Text=date.Month==view.Month?date.Day.ToString():"",Dock=DockStyle.Fill,Margin=Padding.Empty,Enabled=date.Month==view.Month,Pink=Holidays.Name(date)!="",Font=new Font("맑은 고딕",10)};b.Click+=(s,e)=>{choose(date);Close();};days.Controls.Add(b,i%7,i/7+1);}}
 public void Show(Control anchor){Show(anchor,new Point(0,anchor.Height+5));}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var p=Palette.Round(new RectangleF(1,1,Width-3,Height-3),12))using(var pen=new Pen(Palette.Edge))e.Graphics.DrawPath(pen,p);}
}
static class Holidays {
 // 2026 KASA calendar data. These labels do not decide paid-leave eligibility.
 static Dictionary<string,string> dates=new Dictionary<string,string>{{"01-01","신정"},{"02-16","설 연휴"},{"02-17","설날"},{"02-18","설 연휴"},{"03-01","삼일절"},{"03-02","대체공휴일"},{"05-05","어린이날"},{"05-24","부처님오신날"},{"05-25","대체공휴일"},{"06-03","지방선거일"},{"06-06","현충일"},{"08-15","광복절"},{"08-17","대체공휴일"},{"09-24","추석 연휴"},{"09-25","추석"},{"09-26","추석 연휴"},{"10-03","개천절"},{"10-05","대체공휴일"},{"10-09","한글날"},{"12-25","성탄절"}};
 public static string Name(DateTime date){string name;return date.Year==2026&&dates.TryGetValue(date.ToString("MM-dd"),out name)?name:"";}
}
public partial class MainForm {
 List<SoftButton> navButtons=new List<SoftButton>();Label employmentLine;
 class Settings{public string WageFolder="",PayslipFolder="",RequestFolder="";public bool OpenGenerated=false;public bool AutomaticUpdates=true;public int Theme=0;public string Institution="",Contact="",Recipient="",Manager="";}
 internal static string SettingsTestFile;
 Timer settingsTimer;bool settingsPending;
 string SettingsFile{get{return SettingsTestFile??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ShortPay_Test3","settings.json");}}
 void LoadSettings(){try{if(File.Exists(SettingsFile)){var settings=Engine.Parse<Settings>(File.ReadAllText(SettingsFile));r.Institution=settings.Institution;r.Contact=settings.Contact;r.Recipient=settings.Recipient;r.Manager=settings.Manager;r.Theme=Math.Max(0,Math.Min(5,settings.Theme));automaticUpdates.Checked=settings.AutomaticUpdates;LoadStorageSettings(settings);}}catch{}}
 void ScheduleSettingsSave(){if(loading)return;settingsPending=true;if(settingsTimer==null){settingsTimer=new Timer{Interval=350};settingsTimer.Tick+=(s,e)=>FlushSettings();}settingsTimer.Stop();settingsTimer.Start();}
 void FlushSettings(){if(settingsTimer!=null)settingsTimer.Stop();if(!settingsPending)return;string temp=SettingsFile+"."+Guid.NewGuid().ToString("N")+".tmp";try{Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));File.WriteAllText(temp,Engine.Json(new Settings{Institution=institution.Text,Contact=contact.Text,Recipient=recipient.Text,Manager=manager.Text,Theme=r.Theme,AutomaticUpdates=automaticUpdates.Checked,WageFolder=outputFolders[0].Text,PayslipFolder=outputFolders[1].Text,RequestFolder=outputFolders[2].Text,OpenGenerated=openGenerated.Checked}),System.Text.Encoding.UTF8);if(File.Exists(SettingsFile))File.Replace(temp,SettingsFile,null);else File.Move(temp,SettingsFile);settingsPending=false;}catch(Exception e){MessageBox.Show("설정을 자동 저장하지 못했습니다. "+e.Message);}finally{if(File.Exists(temp))File.Delete(temp);}}
 void ResetSettings(){loading=true;institution.Text=contact.Text=recipient.Text=manager.Text="";loading=false;Changed();ScheduleSettingsSave();FlushSettings();}
 protected override void Dispose(bool disposing){if(disposing&&settingsTimer!=null)settingsTimer.Dispose();base.Dispose(disposing);}
}
}
namespace ShortPay {
static class MenuGlyph {
 public static void Draw(System.Drawing.Graphics g,int kind,System.Drawing.Rectangle bounds,System.Drawing.Color color){
  var state=g.Save();g.TranslateTransform(bounds.X,bounds.Y);g.ScaleTransform(bounds.Width/20f,bounds.Height/20f);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
  using(var p=new System.Drawing.Pen(color,1.6f)){p.StartCap=p.EndCap=System.Drawing.Drawing2D.LineCap.Round;p.LineJoin=System.Drawing.Drawing2D.LineJoin.Round;
   if(kind==0){g.DrawEllipse(p,6,2,8,8);g.DrawArc(p,3,11,14,12,180,180);}
   if(kind==1){g.DrawRectangle(p,2,4,16,14);g.DrawLine(p,2,8,18,8);g.DrawLine(p,6,2,6,6);g.DrawLine(p,14,2,14,6);g.DrawLine(p,6,12,8,14);g.DrawLine(p,8,14,14,10);}
   if(kind==2){g.DrawRectangle(p,4,1,12,18);g.DrawRectangle(p,7,4,6,3);for(int y=10;y<=16;y+=3){g.DrawLine(p,7,y,8,y);g.DrawLine(p,12,y,13,y);}}
   if(kind==3){g.DrawLines(p,new[]{new System.Drawing.Point(4,1),new System.Drawing.Point(12,1),new System.Drawing.Point(17,6),new System.Drawing.Point(17,19),new System.Drawing.Point(4,19),new System.Drawing.Point(4,1)});g.DrawLine(p,12,1,12,6);g.DrawLine(p,12,6,17,6);g.DrawLine(p,7,10,14,10);g.DrawLine(p,7,14,14,14);}
   if(kind==4){g.DrawRectangle(p,2,4,16,14);g.DrawLine(p,10,4,10,18);g.DrawLine(p,5,8,7,8);g.DrawLine(p,13,8,15,8);g.DrawLine(p,5,12,7,12);g.DrawLine(p,13,12,15,12);}
   if(kind==5){var vertices=new System.Drawing.PointF[6];for(int i=0;i<6;i++){double a=(i*60-30)*System.Math.PI/180;vertices[i]=new System.Drawing.PointF(10+(float)System.Math.Cos(a)*8.5f,10+(float)System.Math.Sin(a)*8.5f);}g.DrawPolygon(p,vertices);g.DrawEllipse(p,6.5f,6.5f,7,7);}
  }g.Restore(state);
 }
}
class CalendarLegend:System.Windows.Forms.Control {
 public CalendarLegend(){DoubleBuffered=true;BackColor=System.Drawing.Color.White;Font=new System.Drawing.Font("맑은 고딕",10);AccessibleName="근무 파랑, 결근 노랑, 주휴 및 공휴일 분홍. 클릭: 근무, 결근, 비근무 순서";}
 protected override void OnPaint(System.Windows.Forms.PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;var colors=new[]{System.Drawing.Color.FromArgb(65,120,211),System.Drawing.Color.FromArgb(248,204,83),System.Drawing.Color.FromArgb(252,218,230)};string[] labels={"근무","결근","주휴·공휴일"};int[] positions={0,83,166};for(int i=0;i<3;i++){using(var b=new System.Drawing.SolidBrush(colors[i]))e.Graphics.FillEllipse(b,positions[i],5,14,14);System.Windows.Forms.TextRenderer.DrawText(e.Graphics,labels[i],Font,new System.Drawing.Rectangle(positions[i]+20,0,135,26),Palette.Ink,System.Windows.Forms.TextFormatFlags.Left|System.Windows.Forms.TextFormatFlags.VerticalCenter);}System.Windows.Forms.TextRenderer.DrawText(e.Graphics,"클릭: 근무 → 결근 → 비근무 → 근무",Font,new System.Drawing.Rectangle(0,26,Width,24),Palette.Ink,System.Windows.Forms.TextFormatFlags.Left|System.Windows.Forms.TextFormatFlags.VerticalCenter);}
}
}

namespace ShortPay {
class ThemeTile:System.Windows.Forms.Button {
 public int ThemeIndex;public bool Active;public System.Drawing.Color Swatch;bool hovered;
 public ThemeTile(){SetStyle(System.Windows.Forms.ControlStyles.UserPaint|System.Windows.Forms.ControlStyles.AllPaintingInWmPaint|System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);UseVisualStyleBackColor=false;BackColor=System.Drawing.Color.White;FlatStyle=System.Windows.Forms.FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=System.Windows.Forms.Cursors.Hand;}
 protected override void OnMouseEnter(System.EventArgs e){hovered=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(System.EventArgs e){hovered=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnPaint(System.Windows.Forms.PaintEventArgs e){var g=e.Graphics;g.Clear(Palette.Surface(Parent));g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var path=Palette.Round(new System.Drawing.RectangleF(2,2,Width-5,Height-5),11))using(var brush=new System.Drawing.SolidBrush(Active?System.Drawing.Color.FromArgb(245,246,253):System.Drawing.Color.White))using(var pen=new System.Drawing.Pen(Active?Swatch:hovered?Palette.Muted:Palette.Edge,Active?2:1)){g.FillPath(brush,path);g.DrawPath(pen,path);}using(var brush=new System.Drawing.SolidBrush(Swatch))g.FillEllipse(brush,12,(Height-24)/2,24,24);if(Active)using(var pen=new System.Drawing.Pen(System.Drawing.Color.White,2)){g.DrawLine(pen,17,Height/2,22,Height/2+5);g.DrawLine(pen,22,Height/2+5,30,Height/2-5);}using(var font=new System.Drawing.Font("맑은 고딕",10,System.Drawing.FontStyle.Bold))System.Windows.Forms.TextRenderer.DrawText(g,Text,font,new System.Drawing.Rectangle(44,0,Width-50,Height),Palette.Ink,System.Windows.Forms.TextFormatFlags.Left|System.Windows.Forms.TextFormatFlags.VerticalCenter);}
}
}

namespace ShortPay {
static class ExcelGlyph {
 public static void Draw(System.Drawing.Graphics g,System.Drawing.Rectangle r){var save=g.Save();g.TranslateTransform(r.X,r.Y);g.ScaleTransform(r.Width/28f,r.Height/28f);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
  using(var path=Palette.Round(new System.Drawing.RectangleF(7,1,20,25),3))using(var b=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(237,251,241)))g.FillPath(b,path);
  using(var pen=new System.Drawing.Pen(System.Drawing.Color.FromArgb(83,160,113),1.3f)){for(int y=7;y<=21;y+=7)g.DrawLine(pen,11,y,24,y);g.DrawLine(pen,17,5,17,23);}
  using(var path=Palette.Round(new System.Drawing.RectangleF(0,7,16,17),2))using(var b=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(26,95,62)))g.FillPath(b,path);
  using(var pen=new System.Drawing.Pen(System.Drawing.Color.White,2.2f)){pen.StartCap=pen.EndCap=System.Drawing.Drawing2D.LineCap.Round;g.DrawLine(pen,4,11,12,20);g.DrawLine(pen,12,11,4,20);}g.Restore(save);
 }
}
}

namespace ShortPay {
class WageComparison:System.Windows.Forms.Label {
 public WageComparison(){SetStyle(System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer|System.Windows.Forms.ControlStyles.AllPaintingInWmPaint|System.Windows.Forms.ControlStyles.UserPaint,true);BackColor=System.Drawing.Color.Transparent;}
 protected override void OnPaint(System.Windows.Forms.PaintEventArgs e){var lines=Text.Split(new[]{'\n'},System.StringSplitOptions.RemoveEmptyEntries);var flags=System.Windows.Forms.TextFormatFlags.Left|System.Windows.Forms.TextFormatFlags.NoPadding; if(lines.Length>0)System.Windows.Forms.TextRenderer.DrawText(e.Graphics,lines[0],Font,new System.Drawing.Rectangle(0,8,Width,27),Palette.Ink,flags);if(lines.Length>1){const string prefix="적용기준: ";string value=lines[1].StartsWith(prefix)?lines[1].Substring(prefix.Length):lines[1];int w=System.Windows.Forms.TextRenderer.MeasureText(e.Graphics,prefix,Font,new System.Drawing.Size(Width,27),flags).Width;System.Windows.Forms.TextRenderer.DrawText(e.Graphics,prefix,Font,new System.Drawing.Rectangle(0,46,w,27),Palette.Ink,flags);System.Windows.Forms.TextRenderer.DrawText(e.Graphics,value,Font,new System.Drawing.Rectangle(w,46,Width-w,27),System.Drawing.Color.FromArgb(36,100,211),flags);}}
}
}

