using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;

namespace ShortPay {
static class Program {
 [STAThread] static void Main(string[] args){
  ExcelReports.RegisterDependencies();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try {if(args.Contains("--update-live-test")){UpdateTests.Live(args.Last());return;}if(args.Contains("--update-test")){UpdateTests.Run(args.Last());return;}if(args.Contains("--xlsx-test")){ExcelTests.Run(args.Last());return;}if(args.Contains("--test")){Tests.Run(args.Last());return;}Application.Run(new MainForm());}
  catch(Exception ex){File.WriteAllText(Path.Combine(Path.GetTempPath(),"ShortPay-error.txt"),ex.ToString());MessageBox.Show("실행 중 오류가 발생했습니다.\n"+ex.Message,"임금 도우미");}
 }
}
class Palette {
 public static Color Accent=Color.FromArgb(76,76,210), Soft=Color.FromArgb(241,240,255), Edge=Color.FromArgb(219,220,239), Ink=Color.FromArgb(39,44,72), Muted=Color.FromArgb(112,119,143), Background=Color.FromArgb(247,248,253);
 public static void Select(int i){Color[] a={Color.FromArgb(76,76,210),Color.FromArgb(33,128,110),Color.FromArgb(147,84,172),Color.FromArgb(44,117,181),Color.FromArgb(178,105,66),Color.FromArgb(159,86,114)};Accent=a[Math.Max(0,Math.Min(5,i))];Soft=Color.FromArgb(242+Accent.R/30,242+Accent.G/30,242+Accent.B/30);}
 public static Color Surface(Control c){while(c!=null&&c.BackColor.A<255)c=c.Parent;return c==null?Background:c.BackColor;}
 public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
}
class Card:Panel {
 public bool Input;public Color InputColor=Color.Empty,LineColor=Color.Empty;
 public Card(){SetStyle(ControlStyles.ResizeRedraw,true);DoubleBuffered=true;BackColor=Color.White;Padding=new Padding(16);}
 protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Palette.Surface(Parent));using(var p=Palette.Round(new RectangleF(.5f,.5f,Width-2,Height-2),12)){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var b=new SolidBrush(Input?(InputColor.IsEmpty?Palette.Soft:InputColor):BackColor))e.Graphics.FillPath(b,p);using(var pen=new Pen(Input&&ContainsFocus?Palette.Accent:LineColor.IsEmpty?Palette.Edge:LineColor,1))e.Graphics.DrawPath(pen,p);}}
 protected override void OnEnter(EventArgs e){base.OnEnter(e);Invalidate();}protected override void OnLeave(EventArgs e){base.OnLeave(e);Invalidate();}
}
class SoftButton:Button {
 public bool Active;public Color ActionColor=Color.Empty;public int MenuIcon=-1;public bool ExcelIcon,PdfIcon;
 public SoftButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);UseVisualStyleBackColor=false;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.White;ForeColor=Palette.Ink;Cursor=Cursors.Hand;Height=36;TabStop=true;}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(Palette.Surface(Parent));e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var p=Palette.Round(new RectangleF(1,1,Width-3,Height-3),9)){using(var b=new SolidBrush(!Enabled?Color.FromArgb(246,247,250):Active?(ActionColor.IsEmpty?Palette.Accent:ActionColor):Palette.Soft))e.Graphics.FillPath(b,p);if(Focused)using(var pen=new Pen(Palette.Accent,1))e.Graphics.DrawPath(pen,p);}if(PdfIcon){using(var pen=new Pen(Color.White,1.6f)){e.Graphics.DrawRectangle(pen,15,(Height-24)/2,19,24);e.Graphics.DrawLine(pen,19,Height/2,30,Height/2);}}if(ExcelIcon)ExcelGlyph.Draw(e.Graphics,new Rectangle(19,(Height-28)/2,28,28));if(MenuIcon>=0)MenuGlyph.Draw(e.Graphics,MenuIcon,new Rectangle(12,(Height-20)/2,20,20),Active?Color.White:Palette.Accent);TextRenderer.DrawText(e.Graphics,Text,Font,(ExcelIcon||PdfIcon)?new Rectangle(45,0,Width-50,Height):MenuIcon>=0?new Rectangle(36,0,Width-40,Height):ClientRectangle,!Enabled?Color.FromArgb(186,189,199):Active?Color.White:Palette.Accent,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);}
}
class PastelCombo:ComboBox {
 public PastelCombo(){DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=24;}
 protected override void OnDrawItem(DrawItemEventArgs e){if(e.Index<0)return;bool selected=(e.State&DrawItemState.Selected)!=0;using(var b=new SolidBrush(selected?Palette.Accent:Palette.Soft))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,GetItemText(Items[e.Index]),Font,new Rectangle(e.Bounds.X+6,e.Bounds.Y,e.Bounds.Width-8,e.Bounds.Height),selected?Color.White:Palette.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);}
}
public partial class MainForm:Form {
 Record r=new Record(); List<Job> jobs; Result result; bool loading=true,dirty=false;string currentFile,lastContractWarning="";Label holidayFormula;
 internal Action<string> ContractAlert=message=>MessageBox.Show(message,"계약기간 확인",MessageBoxButtons.OK,MessageBoxIcon.Warning);
 Panel main,sidebar; FlowLayoutPanel content; Label summary,notice; TabControl tabs; TableLayoutPanel calendar; ComboBox year,month,removeMode,theme,job,identityMode,relation; DateInput start,end; PlainNumber hours; TextBox person,identity,bank,account,holder,reason,institution,contact,note,original;
 DataGridView weeks; Dictionary<string,PlainNumber> numbers=new Dictionary<string,PlainNumber>(); CheckBox manual,employment,rateConfirm; WageComparison reference; Panel calendarHost;
 public MainForm(){
  DoubleBuffered=true;
  using(var sr=new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("jobs.json")))jobs=Engine.Parse<List<Job>>(sr.ReadToEnd());
  Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
  Text="1개월 미만 대체근로자 임금계산기 · "+Assembly.GetExecutingAssembly().GetName().Version.ToString(2);Font=new Font("맑은 고딕",10);StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(1280,800);MinimumSize=new Size(1200,800);AutoScaleMode=AutoScaleMode.Dpi;BackColor=Palette.Background;
  r.Start=DateTime.Today;r.End=r.Start.AddDays(6);Engine.Fill(r,jobs[r.JobIndex]);Build();LoadUI();loading=false;RefreshAll();dirty=false;
  Shown+=(a,e)=>{BeginInvoke((Action)(()=>{PerformLayout();Invalidate(true);Update();job.Refresh();}));if(!Environment.GetCommandLineArgs().Contains("--test")&&automaticUpdates.Checked)CheckUpdates(false);};
  FormClosing+=(s,e)=>{if(!CanLeave())e.Cancel=true;else FlushSettings();};
 }
 Label L(string text,int size=10,bool bold=false){return new Label{Text=text,AutoSize=false,Height=27,ForeColor=Palette.Ink,BackColor=Color.Transparent,Font=new Font("맑은 고딕",size,bold?FontStyle.Bold:FontStyle.Regular),TextAlign=ContentAlignment.MiddleLeft};}
 SoftButton B(string text,Action action,int width=135){var b=new SoftButton{Text=text,Width=width,Margin=new Padding(3)};b.Click+=(s,e)=>action();return b;}
 ComboBox Combo(string[] values,int width=170){var c=new PastelCombo{DropDownStyle=ComboBoxStyle.DropDownList,Width=width,FlatStyle=FlatStyle.Flat,BackColor=Palette.Soft};c.Items.AddRange(values);c.SelectedIndex=0;return c;}
 FlowLayoutPanel Row(params Control[] controls){var f=new FlowLayoutPanel{AutoSize=false,WrapContents=true,Dock=DockStyle.Top,Padding=new Padding(0,5,0,5),BackColor=Color.Transparent};f.Controls.AddRange(controls);return f;}
 Card Section(Control parent,string title,int height){var c=new Card{Width=960,Height=height,Margin=new Padding(0,0,0,10)};var label=L(title,12,true);label.SetBounds(18,8,850,28);c.Controls.Add(label);parent.Controls.Add(c);return c;}
 Panel Field(string caption,Control control,int width=205){var p=new Panel{Width=width,Height=66,Margin=new Padding(4),BackColor=Color.Transparent};var l=L(caption,9);l.SetBounds(2,0,width-4,25);p.Controls.Add(l);var bg=new Card{Input=true,Padding=control is ComboBox?new Padding(10,2,10,2):new Padding(11,8,11,5)};bg.SetBounds(0,25,width,38);control.Dock=DockStyle.Fill;if(control is TextBox){((TextBox)control).BorderStyle=BorderStyle.None;control.BackColor=Palette.Soft;}bg.Controls.Add(control);p.Controls.Add(bg);return p;}
 TextBox TextInput(){var t=new TextBox();t.TextChanged+=(s,e)=>Changed();return t;}
 PlainNumber Num(string key,decimal max=100000000, int places=0){var n=new PlainNumber{Minimum=0,Maximum=max,DecimalPlaces=places,ThousandsSeparator=true,BorderStyle=BorderStyle.None,BackColor=Palette.Soft};n.ValueChanged+=(s,e)=>Changed();numbers[key]=n;return n;}
 void Build(){
  sidebar=new Panel{Dock=DockStyle.Left,Width=204,BackColor=Color.White,Padding=new Padding(18)};Controls.Add(sidebar);
  var brand=L("1개월 미만\n대체근로자\n임금계산기",17,true);brand.SetBounds(20,18,180,108);brand.ForeColor=Palette.Accent;sidebar.Controls.Add(brand);
  var sub=L("2026 기준",9);sub.SetBounds(20,128,180,26);sidebar.Controls.Add(sub);
  string[] nav={"대상자 · 계약","근무일 · 주휴","임금 · 공제","명세서 · 신청서","직종 · 계산 기준","설정"};for(int i=0;i<nav.Length;i++){int idx=i;var b=B(nav[i],()=>tabs.SelectedIndex=idx,170);b.MenuIcon=i;b.SetBounds(16,162+i*52,172,43);sidebar.Controls.Add(b); navButtons.Add(b);}
  theme=Combo(new[]{"라벤더","민트","라일락","파랑","살구","장미"},165);theme.SelectedIndexChanged+=(s,e)=>{if(loading)return;r.Theme=theme.SelectedIndex;ApplyTheme();dirty=true;};
  var tag=L("ver. "+Assembly.GetExecutingAssembly().GetName().Version.ToString(2)+"\ne-mail: isilria@ice.go.kr",9);tag.SetBounds(18,485,184,58);sidebar.Controls.Add(tag);
  main=new Panel{Dock=DockStyle.Fill,Padding=new Padding(20,12,20,12),BackColor=Palette.Background};Controls.Add(main);main.BringToFront();
  var top=new Panel{Dock=DockStyle.Top,Height=65};var title=L("1개월 미만 대체근로자 임금계산기",19,true);title.SetBounds(0,0,590,38);title.ForeColor=Palette.Accent;top.Controls.Add(title);var subtitle=L("근무한 날짜를 선택하면 임금과 주휴 검토 결과가 함께 바뀝니다.",9);subtitle.SetBounds(2,43,610,25);top.Controls.Add(subtitle);
  notice=L("본 프로그램은 개인 제작 프로그램입니다. 사용 전 현행 지침과 규정을 확인하시기 바랍니다",9);notice.Dock=DockStyle.Bottom;notice.Height=30;notice.Padding=new Padding(18,0,0,0);notice.ForeColor=Color.FromArgb(28,45,92);notice.BackColor=Palette.Background;Controls.Add(notice);notice.SendToBack();
  tabs=new PageTabs{Dock=DockStyle.Fill};foreach(string n in nav)tabs.TabPages.Add(n);
  tabs.Deselecting+=(s,e)=>{if(!loading&&e.TabPageIndex==0&&!CheckContractDeparture())e.Cancel=true;};
  tabs.SelectedIndexChanged+=(s,e)=>{for(int i=0;i<navButtons.Count;i++){navButtons[i].Active=i==tabs.SelectedIndex;navButtons[i].Invalidate();}};main.Controls.Add(tabs);main.Controls.Add(top);BuildResetButton(top);
  for(int i=0;i<tabs.TabCount;i++){tabs.TabPages[i].BackColor=Palette.Background;tabs.TabPages[i].Padding=new Padding(12);}
  BuildPerson();BuildCalendar();BuildMoney();BuildOutput();BuildReference();BuildSettings();tabs.SelectedIndex=0;navButtons[0].Active=true;
 }
 FlowLayoutPanel Page(int index){var f=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=false,Padding=new Padding(0)};tabs.TabPages[index].Controls.Add(f);f.Resize+=(s,e)=>{foreach(Control c in f.Controls)c.Width=f.ClientSize.Width-4;};return f;}
 void Fields(Card card,int y,params Control[] fields){var row=Row(fields);row.Padding=Padding.Empty;foreach(Control f in fields)f.Margin=new Padding(4,f is Button?25:0,4,0);row.Dock=DockStyle.None;row.SetBounds(14,y,card.Width-28,72);row.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;card.Controls.Add(row);}

 void BuildPerson(){
  content=Page(0);var c=Section(content,"대상자 정보",282);
  person=TextInput();identity=TextInput();bank=TextInput();account=TextInput();holder=TextInput();reason=TextInput();original=TextInput();
  identityMode=Combo(new[]{"생년월일","주민등록번호"});relation=Combo(new[]{"계약 유지","별도"});
  job=Combo(jobs.Select(x=>x.name).ToArray());job.SelectedIndexChanged+=(s,e)=>{if(loading)return;loading=true;var j=jobs[job.SelectedIndex];r.Meal=j.meal;r.Allowance=j.allowances.Where(x=>x.Key!="기관근무수당").Sum(x=>x.Value);Set("base",j.@base);Set("meal",j.meal);Set("allowance",j.allowances.Where(x=>x.Key!="기관근무수당").Sum(x=>x.Value));hours.Maximum=Engine.IsGuard(j)?24:8;Set("hours",Engine.IsGuard(j)?8.5m:8);Set("weekend",12);Set("nightDays",0);Set("publicDays",0);UpdateContractFields();r.JobIndex=job.SelectedIndex;Engine.Fill(r,j);loading=false;Changed();};
  Fields(c,42,Field("직종",job,315),Field("대상자 성명",person,215),Field("생년월일 / 주민번호",identity,310));
  Fields(c,116,Field("은행명",bank,215),Field("계좌번호",account,415),Field("예금주",holder,210));
  Fields(c,190,Field("원근로자 성명",original,260),Field("사유",reason,588));
  BuildContractFields(content);
  institution=TextInput();contact=TextInput();
 }

 void ResetReview(){if(!manual.Checked)return;loading=true;manual.Checked=false;loading=false;r.Override=false;}
 void DateChanged(){if(loading)return;r.Start=start.Value.Date;r.End=end.Value.Date;
  if(r.End<r.Start){loading=true;end.Value=r.Start;r.End=r.Start;loading=false;}
  ResetReview();Engine.Fill(r,jobs[r.JobIndex]);loading=true;year.SelectedItem=r.Start.Year.ToString();month.SelectedIndex=r.Start.Month-1;loading=false;Changed();
 }

 bool CheckContractDeparture(){
  if(!start.Commit()||!end.Commit())return false;
  ReadUI();if(Engine.ContractWarning(r)=="")return true;
  DateTime corrected=new DateTime(r.Start.Year,r.Start.Month,DateTime.DaysInMonth(r.Start.Year,r.Start.Month));
  if(corrected>=Engine.OneMonthEnd(r.Start))corrected=Engine.OneMonthEnd(r.Start).AddDays(-1);
  ContractAlert("계약기간이 1개월 이상으로 설정되어 있습니다.\n종료일을 "+corrected.ToString("yyyy-MM-dd")+"로 조정했습니다. 계약기간을 확인해 주세요.");
  loading=true;end.Value=corrected;r.End=corrected;loading=false;Engine.Fill(r,jobs[r.JobIndex]);Changed();return false;
 }

 void WarnContractPeriod(){string message=r.End<r.Start?"":Engine.ContractWarning(r);if(message!=""&&message!=lastContractWarning)ContractAlert(message);lastContractWarning=message;}
 void BuildCalendar(){
  var p=Page(1);var c=Section(p,"근무일 · 주휴수당",495);calendarHost=c;
  year=Combo(Enumerable.Range(2020,31).Select(x=>x.ToString()).ToArray(),95);month=Combo(Enumerable.Range(1,12).Select(x=>x+"월").ToArray(),78);
  year.SelectedIndexChanged+=(s,e)=>{if(!loading)DrawCalendar();};month.SelectedIndexChanged+=(s,e)=>{if(!loading)DrawCalendar();};
  contractCaption=L("",11,true);contractCaption.SetBounds(22,47,880,31);c.Controls.Add(contractCaption);
  calendar=new TableLayoutPanel{ColumnCount=8,RowCount=1,BackColor=Color.White};calendar.SetBounds(18,96,454,320);c.Controls.Add(calendar);
  var legend=new CalendarLegend();legend.SetBounds(22,438,450,50);c.Controls.Add(legend);
  weeks=new DataGridView{ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,EnableHeadersVisualStyles=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false};weeks.SetBounds(494,96,447,220);weeks.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
  weeks.ColumnHeadersDefaultCellStyle.BackColor=Palette.Soft;weeks.ColumnHeadersDefaultCellStyle.Font=new Font("맑은 고딕",9,FontStyle.Bold);weeks.ColumnHeadersHeight=40;weeks.RowTemplate.Height=38;weeks.DefaultCellStyle.Font=new Font("맑은 고딕",9);weeks.DefaultCellStyle.WrapMode=DataGridViewTriState.True;weeks.DefaultCellStyle.SelectionBackColor=Palette.Soft;weeks.DefaultCellStyle.SelectionForeColor=Palette.Ink;weeks.GridColor=Palette.Edge;
  foreach(string title in new[]{"주차 / 기간","소정일수","실근로일수","주휴수당"})weeks.Columns.Add(title,title);weeks.Columns[0].FillWeight=145;weeks.Columns[1].FillWeight=90;weeks.Columns[2].FillWeight=100;weeks.Columns[3].FillWeight=160;c.Controls.Add(weeks);
  holidayFormula=L("",9);holidayFormula.SetBounds(498,345,440,125);holidayFormula.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;c.Controls.Add(holidayFormula);
  manual=new CheckBox();note=TextInput();Num("holiday",80,2);removeMode=Combo(new[]{"비근무","결근","유급"});
 }

 void BuildMoney(){
  var p=Page(2);var c=Section(p,"통상시급 및 지급 기준",235);c.BackColor=Color.FromArgb(255,252,233);c.Margin=new Padding(0,0,0,5);
  Fields(c,38,Field("월 기본급 (8시간 기준)",Num("base"),275),Field("정액급식비 (월)",Num("meal"),275),Field("적용 직종수당 (월)",Num("allowance"),280));
  Fields(c,108,Field("최저시급",Num("minimum",100000,2),195),Field("시급 직접 지정 (0=자동)",Num("hourly",1000000,2),225));
  var leftInputs=c.Controls.OfType<FlowLayoutPanel>().Last();leftInputs.Width=451;leftInputs.Anchor=AnchorStyles.Top|AnchorStyles.Left;
  rateConfirm=new CheckBox();
  reference=new WageComparison{Font=new Font("맑은 고딕",10,FontStyle.Bold)};reference.SetBounds(473,115,465,76);reference.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;c.Controls.Add(reference);reference.BringToFront();
  c=Section(p,"추가 지급",106);c.BackColor=Color.FromArgb(227,244,231);c.Margin=new Padding(0,0,0,5);Fields(c,30,Field("연장근로(1배) · 시간",Num("within",300,2),275),Field("연장근로(1.5배) · 시간",Num("overtime",300,2),275),Field("기타 지급액",Num("extra"),280));
  c=Section(p,"공제 내역",210);c.BackColor=Color.FromArgb(252,229,237);Fields(c,30,Field("식대",Num("food"),275),Field("소득세",Num("tax"),275),Field("지방소득세",Num("local"),280));Fields(c,97,Field("건강보험",Num("health"),275),Field("장기요양보험",Num("care"),275),Field("국민연금",Num("pension"),280));
  employment=new CheckBox{Checked=true};Num("other");Num("rate",100,3);employmentLine=L("",9);employmentLine.SetBounds(20,167,900,23);c.Controls.Add(employmentLine);AddEmploymentExclusion(c);
  foreach(Card card in p.Controls.OfType<Card>())ToneInputs(card);
 }

 TextBox recipient,manager;DateInput payment;CheckBox paymentKnown;ComboBox requestRound;
 void BuildOutput(){
  var p=Page(3);var resultCard=Section(p,"임금 계산 결과",300);summary=new PayrollSummary();summary.SetBounds(18,42,920,242);summary.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;resultCard.Controls.Add(summary);
  var c=Section(p,"자료 생성",292);c.BackColor=Color.FromArgb(227,244,231);
  var info=L("필요한 문서의 엑셀 또는 PDF 버튼을 누르면 지정한 폴더에 저장됩니다.",10);info.SetBounds(22,44,900,32);c.Controls.Add(info);
  var columns=new TableLayoutPanel{ColumnCount=3,RowCount=1,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};columns.SetBounds(18,80,920,198);for(int i=0;i<3;i++)columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));c.Controls.Add(columns);
  string[] titles={"임금산정표","급여명세서","인건비 신청서"};string[] kinds={"임금산정표","급여명세서","인건비 신청내역"};
  for(int i=0;i<3;i++){int index=i;var pane=new Card{Dock=DockStyle.Fill,Margin=new Padding(5),BackColor=Color.FromArgb(246,250,247)};columns.Controls.Add(pane,i,0);
   var heading=L(titles[i],11,true);heading.SetBounds(17,3,248,28);pane.Controls.Add(heading);
   var buttons=new TableLayoutPanel{ColumnCount=i<2?2:1,RowCount=1,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,Padding=Padding.Empty,Margin=Padding.Empty};buttons.SetBounds(10,34,269,46);for(int bi=0;bi<buttons.ColumnCount;bi++)buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/buttons.ColumnCount));pane.Controls.Add(buttons);
   var button=B("엑셀",()=>Export(kinds[index]));button.ExcelIcon=true;button.Active=true;button.ActionColor=Color.FromArgb(43,126,91);button.Font=new Font("맑은 고딕",11,FontStyle.Bold);button.Dock=DockStyle.Fill;button.Margin=new Padding(2,0,3,0);buttons.Controls.Add(button,0,0);
   if(i<2){var pdf=B("PDF",()=>ExportPdf(kinds[index]));pdf.PdfIcon=true;pdf.Active=true;pdf.ActionColor=Color.FromArgb(171,65,76);pdf.Font=button.Font;pdf.Dock=DockStyle.Fill;pdf.Margin=new Padding(3,0,2,0);buttons.Controls.Add(pdf,1,0);}
   var label=L(i==0?"A4 가로 · 한 페이지\n지급·공제와 근무내역":i==1?"지급예정일":"신청 차수",10);label.SetBounds(17,81,248,i==0?60:28);pane.Controls.Add(label);
   if(i==1){payment=new DateInput();var dateBox=new Card{Input=true,Padding=new Padding(11,8,11,5)};dateBox.SetBounds(17,111,240,32);dateBox.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;payment.Dock=DockStyle.Fill;dateBox.Controls.Add(payment);pane.Controls.Add(dateBox);payment.ValueChanged+=(a,e)=>Changed();paymentKnown=new CheckBox{Text="지급예정일 입력 (미정이면 해제)",Font=new Font("맑은 고딕",9),BackColor=Color.Transparent};paymentKnown.SetBounds(17,147,257,32);paymentKnown.CheckedChanged+=(a,e)=>{payment.Enabled=paymentKnown.Checked;Changed();};pane.Controls.Add(paymentKnown);}
   if(i==2){requestRound=Combo(new[]{"1차","2차","3차","4차"});var roundBox=new Card{Input=true,Padding=new Padding(10,2,10,2)};roundBox.SetBounds(17,111,240,32);roundBox.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;requestRound.Dock=DockStyle.Fill;roundBox.Controls.Add(requestRound);pane.Controls.Add(roundBox);requestRound.SelectedIndexChanged+=(a,e)=>Changed();}
  }
 }
 void BuildReference(){var p=Page(4);var c=Section(p,"직종 참조표 · 23개 직종",430);var grid=new DataGridView{ReadOnly=true,AllowUserToAddRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,EnableHeadersVisualStyles=false,Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top};grid.SetBounds(18,45,920,365);foreach(string t in new[]{"직종","월 기본급","급식비","직종수당"})grid.Columns.Add(t,t);grid.Columns[0].FillWeight=160;grid.Columns[3].FillWeight=250;grid.DefaultCellStyle.WrapMode=DataGridViewTriState.True;grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells;grid.ColumnHeadersDefaultCellStyle.BackColor=Palette.Soft;foreach(var j in jobs)grid.Rows.Add(j.name,j.@base.ToString("N0"),j.meal.ToString("N0"),String.Join(" / ",j.allowances.Select(x=>x.Key+" "+x.Value.ToString("N0"))));c.Controls.Add(grid);
  var t1=L("2026 직종참조표 · 2025 보수교육 자료 · 1개월 미만 대체근로자 인건비 산출 자료 기준\n당직전담실무원은 평일·주말 근로시간과 야간·공휴일 지급일을 구분하여 산정합니다.",9);t1.Width=930;t1.Height=58;p.Controls.Add(t1);
 }
 readonly List<ThemeTile> themeChoices=new List<ThemeTile>();
 void BuildThemeChoices(Control page){var card=Section(page,"색상 테마 선택",110);var row=new TableLayoutPanel{ColumnCount=6,RowCount=1,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};row.SetBounds(17,39,925,65);for(int i=0;i<6;i++)row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/6));card.Controls.Add(row);Color[] colors={Color.FromArgb(76,76,210),Color.FromArgb(33,128,110),Color.FromArgb(147,84,172),Color.FromArgb(44,117,181),Color.FromArgb(178,105,66),Color.FromArgb(159,86,114)};for(int i=0;i<6;i++){int index=i;var button=new ThemeTile{Text=theme.Items[i].ToString(),ThemeIndex=i,Swatch=colors[i],Dock=DockStyle.Fill,Margin=new Padding(4),Active=i==r.Theme};button.Click+=(a,e)=>theme.SelectedIndex=index;row.Controls.Add(button,i,0);themeChoices.Add(button);}}
 void BuildSettings(){var p=Page(5);var c=Section(p,"기관 · 신청서 정보",190);recipient=TextInput();manager=TextInput();Fields(c,40,Field("학교 / 기관명",institution,300),Field("수신자 기호",recipient,300));Fields(c,112,Field("담당자명 (신청서)",manager,300),Field("담당자 / 연락처",contact,300));
  foreach(var row in c.Controls.OfType<FlowLayoutPanel>()){row.Width=622;row.Anchor=AnchorStyles.Top|AnchorStyles.Left;}
  var reset=B("설정값 초기화",ResetSettings,190);reset.SetBounds(710,72,190,38);reset.Anchor=AnchorStyles.Top|AnchorStyles.Right;c.Controls.Add(reset);
  var hint=L("입력한 정보는 자동 저장됩니다.",9);hint.SetBounds(700,116,235,48);hint.Anchor=AnchorStyles.Top|AnchorStyles.Right;c.Controls.Add(hint);
  BuildStorageSettings(p);
  BuildThemeChoices(p);
  BuildUpdateSettings(p);
  LoadSettings();foreach(var input in new[]{institution,contact,recipient,manager})input.TextChanged+=(s,e)=>ScheduleSettingsSave();theme.SelectedIndexChanged+=(s,e)=>ScheduleSettingsSave();automaticUpdates.CheckedChanged+=(s,e)=>ScheduleSettingsSave();}


 void LoadUI(){loading=true;person.Text=r.Name;identity.Text=r.Identity;identityMode.SelectedIndex=r.IdentityMode=="주민등록번호"?1:0;identity.UseSystemPasswordChar=false;bank.Text=r.Bank;account.Text=r.Account;holder.Text=r.Holder;reason.Text=r.Reason;original.Text=r.OriginalWorker;institution.Text=r.Institution;contact.Text=r.Contact;recipient.Text=r.Recipient;manager.Text=r.Manager;requestRound.SelectedIndex=Math.Max(0,Math.Min(3,r.RequestRound-1));payment.Value=r.PaymentDate??DateTime.Today;paymentKnown.Checked=r.PaymentDate.HasValue;payment.Enabled=paymentKnown.Checked;job.SelectedIndex=r.JobIndex;start.Value=r.Start;end.Value=r.End;relation.SelectedIndex=r.Separate?1:0;note.Text=r.ReviewNote;manual.Checked=r.Override;employment.Checked=r.Employment;rateConfirm.Checked=r.RateConfirmed;theme.SelectedIndex=r.Theme;
  LoadGuardFields();Set("hours",r.Hours);Set("holiday",r.HolidayHours);Set("base",r.MonthlyBase);Set("meal",r.Meal);Set("allowance",r.Allowance);Set("minimum",r.Minimum);Set("hourly",r.HourlyOverride);Set("within",r.Within);Set("overtime",r.Overtime);Set("extra",r.ExtraPay);Set("food",r.Food);Set("tax",r.Tax);Set("local",r.LocalTax);Set("health",r.Health);Set("care",r.Care);Set("pension",r.Pension);Set("other",r.Other);Set("rate",r.Rate);year.SelectedItem=r.Start.Year.ToString();month.SelectedIndex=r.Start.Month-1;loading=false;ApplyTheme();
 }
 void Set(string key,decimal value){var n=numbers[key];n.Value=Math.Max(n.Minimum,Math.Min(n.Maximum,value));}
 decimal N(string key){return numbers[key].Value;}
 void ReadUI(){r.Name=person.Text;r.Identity=identity.Text;r.IdentityMode=new string(identity.Text.Where(Char.IsDigit).ToArray()).Length>8?"주민등록번호":"생년월일";r.Bank=bank.Text;r.Account=account.Text;r.Holder=holder.Text;r.Reason=reason.Text;r.OriginalWorker=original.Text;r.Institution=institution.Text;r.Contact=contact.Text;r.Recipient=recipient.Text;r.Manager=manager.Text;r.RequestRound=requestRound.SelectedIndex+1;r.PaymentDate=paymentKnown.Checked?(DateTime?)payment.Value.Date:null;r.ExtraIsAdjustment=false;r.OutputYear=0;r.OutputMonth=0;r.JobIndex=job.SelectedIndex;r.Start=start.Value.Date;r.End=end.Value.Date;r.Hours=hours.Value;r.Separate=false;r.Override=false;r.ReviewNote=note.Text;r.HolidayHours=N("holiday");r.MonthlyBase=N("base");if(numbers["meal"].Enabled)r.Meal=N("meal");if(numbers["allowance"].Enabled)r.Allowance=N("allowance");r.Minimum=N("minimum");r.HourlyOverride=N("hourly");r.Within=N("within");r.Overtime=N("overtime");r.ExtraPay=N("extra");r.Food=N("food");r.Tax=N("tax");r.LocalTax=N("local");r.Health=N("health");r.Care=N("care");r.Pension=N("pension");r.Other=0;r.Rate=N("rate");ReadGuardFields();r.RateConfirmed=rateConfirm.Checked;}
 void Changed(){if(loading)return;if(r.Hours!=hours.Value||r.JobIndex!=job.SelectedIndex||r.Separate!=(relation.SelectedIndex==1))ResetReview();dirty=true;ReadUI();RefreshAll();}
 void RefreshAll(){result=Engine.Calculate(r,jobs[r.JobIndex]);summary.Text="임금총액: "+result.Gross.ToString("N0")+"원     공제액: "+result.Deductions.ToString("N0")+"원     실지급액: "+result.Net.ToString("N0")+"원";
  ((PayrollSummary)summary).UpdateResult(r,result);UpdateAllowanceInputs();
  if(Engine.ContractWarning(r)!="")summary.Text="계약기간 확인 필요 · 단기 인건비 계산 중지";

  weeks.Rows.Clear();int wi=0;foreach(var w in result.Weeks){wi++;weeks.Rows.Add(wi+"주차  "+w.Period,w.PlannedDays,r.Days.Count(d=>d.Value==1&&String.CompareOrdinal(d.Key,Engine.Key(w.Start))>=0&&String.CompareOrdinal(d.Key,Engine.Key(w.End))<=0).ToString(),w.Holiday>0?"O · "+w.Holiday.ToString("0.##")+"시간":"X · "+w.Status);}weeks.Height=40+Math.Max(1,weeks.Rows.Count)*38+3;weeks.ClearSelection();
  holidayFormula.Top=weeks.Bottom+15;
  holidayFormula.Text="주휴 1회 = "+result.ReferenceHours.ToString("0.##")+"시간 ÷ "+result.ReferenceDays+"일 = "+result.HolidayUnit.ToString("0.####")+"시간\n시작일부터 7일 · 주 15시간 이상 · 개근\n분홍색 주휴 표시는 각 7일 구간의 마지막 날입니다.\n공휴일 표시는 2026년 기준이며 유급 여부는 별도 확인합니다.";

  reference.Text="최저임금: "+r.Minimum.ToString("N2")+"원  |  통상시급: "+result.OrdinaryHourly.ToString("N2")+"원\n\n적용기준: "+(r.HourlyOverride>0?"직접 지정 ("+result.Hourly.ToString("N2")+"원)":result.OrdinaryHourly>=r.Minimum?"통상시급":"최저임금");
  if(Engine.IsGuard(jobs[r.JobIndex]))holidayFormula.Text="당직전담실무원 · 주휴수당 미산정\n월 임금산정시간 "+result.MonthlyHours+"시간\n평일 "+r.Hours+"시간 / 주말 "+r.WeekendHours+"시간\n야간가산: 지급일 × 1.5시간 × 통상시급 × 50%";
  employmentLine.Text=r.Employment?"고용보험 자동 공제  "+result.Employment.ToString("N0")+"원 ("+r.Rate+"%)":"고용보험 적용 제외 · 0원";DrawCalendar();
 }

 Label contractCaption;
 void DrawCalendar(){
  if(calendar==null)return;contractCaption.Text=r.Start.ToString("yyyy. M. d.")+" ~ "+r.End.ToString("yyyy. M. d.");calendar.SuspendLayout();
  while(calendar.Controls.Count>0){var c=calendar.Controls[0];calendar.Controls.Remove(c);c.Dispose();}calendar.ColumnStyles.Clear();calendar.RowStyles.Clear();
  DateTime first=r.Start.Date.AddDays(-(int)r.Start.DayOfWeek);int count=Math.Max(1,Math.Min(6,((r.End.Date-first).Days+7)/7));calendar.RowCount=count+1;calendar.Height=30+count*52;
  calendar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,58));for(int i=0;i<7;i++)calendar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/7));calendar.RowStyles.Add(new RowStyle(SizeType.Absolute,30));for(int i=0;i<count;i++)calendar.RowStyles.Add(new RowStyle(SizeType.Absolute,52));
  string[] names={"일","월","화","수","목","금","토"};for(int i=0;i<7;i++){var l=L(names[i],10,true);l.Dock=DockStyle.Fill;l.TextAlign=ContentAlignment.MiddleCenter;calendar.Controls.Add(l,i+1,0);}
  var seenMonths=new HashSet<int>();
  for(int row=0;row<count;row++){
   DateTime begin=first.AddDays(row*7);var shown=Enumerable.Range(0,7).Select(i=>begin.AddDays(i)).Where(d=>d>=r.Start.Date&&d<=r.End.Date).ToList();
   var label=L(String.Join("\n",shown.Select(d=>d.Month).Distinct().Where(m=>seenMonths.Add(m)).Select(m=>m+"월")),10,true);label.Dock=DockStyle.Fill;label.TextAlign=ContentAlignment.MiddleCenter;calendar.Controls.Add(label,0,row+1);
   for(int i=0;i<7;i++){DateTime d=begin.AddDays(i);if(d<r.Start.Date||d>r.End.Date)continue;int state;r.Days.TryGetValue(Engine.Key(d),out state);bool paid=result!=null&&result.Weeks.Any(w=>w.Holiday>0&&w.End==d);var b=new DayButton{Dock=DockStyle.Fill,Margin=new Padding(2),State=state,Pink=paid||Holidays.Name(d)!="",Text=d.Day.ToString(),AccessibleName=d.ToString("yyyy-MM-dd")+" "+(state==1?"근무":state==2?"결근":"비근무")+(paid?" 주휴":"")};
    b.Click+=(sender,e)=>{int old;r.Days.TryGetValue(Engine.Key(d),out old);if(old==1)r.Days[Engine.Key(d)]=2;else if(old==2||old==3)r.Days.Remove(Engine.Key(d));else r.Days[Engine.Key(d)]=1;ResetReview();Changed();};calendar.Controls.Add(b,i+1,row+1);
   }
  }calendar.ResumeLayout();
 }
 static Color InputBackground(Control c){for(Control p=c.Parent;p!=null;p=p.Parent){var card=p as Card;if(card!=null&&card.Input&&card.InputColor!=Color.Empty)return card.InputColor;}return Palette.Soft;}
 void ToneInputs(Card section){Color fill=section.BackColor.G>245?Color.FromArgb(255,247,212):section.BackColor.G>235?Color.FromArgb(215,236,221):Color.FromArgb(248,216,228);Color line=section.BackColor.G>245?Color.FromArgb(230,214,166):section.BackColor.G>235?Color.FromArgb(177,207,187):Color.FromArgb(223,185,200);ToneChildren(section,fill,line);}
 void ToneChildren(Control parent,Color fill,Color line){foreach(Control c in parent.Controls){var card=c as Card;if(card!=null&&card.Input){card.InputColor=fill;card.LineColor=line;}if(c is TextBox||c is PlainNumber)c.BackColor=fill;ToneChildren(c,fill,line);}}
 void ApplyTheme(){foreach(var choice in themeChoices){choice.Active=choice.ThemeIndex==r.Theme;choice.Invalidate();}Palette.Select(r.Theme);Walk(this);RefreshAll();Invalidate(true);}
 void Walk(Control p){foreach(Control c in p.Controls){if(c is Label&&c.Font.Size>=15)c.ForeColor=Palette.Accent;if(c is TextBox||c is PlainNumber||c is ComboBox)c.BackColor=InputBackground(c);if(c is SoftButton)c.Invalidate();if(c is DataGridView)((DataGridView)c).ColumnHeadersDefaultCellStyle.BackColor=Palette.Soft;Walk(c);}summary.ForeColor=Palette.Accent;}
 bool CanLeave(){if(!dirty)return true;var answer=MessageBox.Show("수정한 대상자 자료를 저장할까요?","저장 확인",MessageBoxButtons.YesNoCancel);if(answer==DialogResult.Cancel)return false;if(answer==DialogResult.No)return true;return SaveCore();}
 void NewRecord(){if(!CanLeave())return;r=new Record{Start=DateTime.Today,End=DateTime.Today.AddDays(6),Theme=r.Theme,Institution=r.Institution,Contact=r.Contact,Recipient=r.Recipient,Manager=r.Manager};Engine.Fill(r,jobs[r.JobIndex]);currentFile=null;lastContractWarning="";LoadUI();RefreshAll();dirty=false;tabs.SelectedIndex=0;}
 void Save(){SaveCore();}
 bool SaveCore(){if(!ValidateChildren())return false;ReadUI();if(Engine.ContractWarning(r)!=""){ContractAlert(Engine.ContractWarning(r));return false;}using(var d=new SaveFileDialog{Filter="대상자 자료|*.stpay",FileName=currentFile==null?SafeName(r.Name+"_"+r.Start.ToString("yyyyMM")+".stpay"):Path.GetFileName(currentFile),InitialDirectory=currentFile==null?Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments):Path.GetDirectoryName(currentFile)}){if(d.ShowDialog()!=DialogResult.OK)return false;try{Engine.Save(d.FileName,r);currentFile=d.FileName;dirty=false;return true;}catch(Exception ex){MessageBox.Show("저장하지 못했습니다.\n"+ex.Message);return false;}}}
 void Open(){if(!CanLeave())return;using(var d=new OpenFileDialog{Filter="대상자 자료|*.stpay"})if(d.ShowDialog()==DialogResult.OK)try{var loaded=Engine.Load(d.FileName);if(loaded.Other!=0||loaded.Override||loaded.Separate)MessageBox.Show("이전 자료를 테스트3 기준으로 다시 계산합니다. 기타공제·주휴 수동확정·별도계약은 해제하고 고용보험 제외 설정은 유지합니다.");loaded.Other=0;loaded.Override=false;loaded.Separate=false;r=loaded;currentFile=d.FileName;LoadUI();RefreshAll();dirty=false;lastContractWarning="";WarnContractPeriod();}catch(Exception ex){MessageBox.Show("자료를 열지 못했습니다. 올바른 대상자 저장파일인지 확인하세요.\n"+ex.Message);}}
 static string SafeName(string s){foreach(char c in Path.GetInvalidFileNameChars())s=s.Replace(c,'_');return s;}
 void Export(string kind,bool pdf=false){if(!ValidateChildren())return;ReadUI();RefreshAll();if(Engine.ContractWarning(r)!=""){ContractAlert(Engine.ContractWarning(r));return;}if(String.IsNullOrWhiteSpace(r.Name)){MessageBox.Show("대상자 성명을 입력하세요.");tabs.SelectedIndex=0;return;}
  var selected=CurrentOutputOptions();if(selected.Year<2020||selected.Year>2050){MessageBox.Show("설정의 귀속 연도를 2020~2050 또는 자동(0)으로 입력하세요.");tabs.SelectedIndex=5;return;}
  SaveAndOpenDocument(kind,pdf);
 }
 internal static bool NeedsOutputOptions(string kind){return false;}
 internal ExportOptions CurrentOutputOptions(){return new ExportOptions{Year=r.End.Year,Month=r.End.Month,Round=r.RequestRound,Recipient=r.Recipient,Manager=r.Manager,PaymentDate=r.PaymentDate,ExtraIsAdjustment=false};}
 internal static string OutputFileName(string kind,Record record,ExportOptions o){return SafeName(kind=="인건비 신청내역"?"("+o.Recipient+")"+record.Institution+"_("+o.Round+"차)"+o.Year+". "+o.Month+"월 - 1개월 미만 기간제근로자(대체근로자)-인건비 신청.xlsx":record.Name+"_"+kind+".xlsx");}
 public void CaptureScreen(string path){Show();Application.DoEvents();using(var b=new Bitmap(Width,Height)){DrawToBitmap(b,new Rectangle(0,0,Width,Height));b.Save(path);}Hide();}
 public void ExerciseUI(string folder){ExerciseTest3(folder);}
 public void ExerciseHolidayUI(string folder){}


}
}
