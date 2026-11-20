using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace SchoolWorkdayCalculator {
internal static class FlatDesign {
 internal static readonly Color Ink=Color.FromArgb(28,39,77),Muted=Color.FromArgb(88,99,127),Blue=Color.FromArgb(24,113,102),Background=Color.FromArgb(244,245,249),Edge=Color.FromArgb(223,227,236);
 internal static readonly Color HeaderLavender=Color.FromArgb(236,232,250),HeaderPink=Color.FromArgb(253,232,239),HeaderPeach=Color.FromArgb(253,238,222),HeaderMint=Color.FromArgb(227,244,238);
 internal static Label Text(string text,int x,int y,int width,int height,float size,bool bold){return new Label{Text=text,Location=new Point(x,y),Size=new Size(width,height),Font=new Font("맑은 고딕",size,bold?FontStyle.Bold:FontStyle.Regular),ForeColor=Ink,BackColor=Color.Transparent,TextAlign=ContentAlignment.MiddleLeft};}
 internal static void Button(Button button,bool primary){button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=1;button.FlatAppearance.BorderColor=Edge;button.FlatAppearance.MouseOverBackColor=primary?Color.FromArgb(174,225,211):Color.FromArgb(246,248,252);button.FlatAppearance.MouseDownBackColor=primary?Color.FromArgb(160,216,200):Color.FromArgb(232,236,244);button.UseVisualStyleBackColor=false;button.BackColor=primary?Color.FromArgb(189,233,221):Color.White;button.ForeColor=Ink;button.Cursor=Cursors.Hand;button.Font=new Font("맑은 고딕",10F,primary?FontStyle.Bold:FontStyle.Regular);var pastel=button as PastelButton;if(pastel!=null)pastel.Primary=primary;button.Invalidate();}
 internal static void Dialog(Form form){form.BackColor=Background;form.ForeColor=Ink;foreach(var button in form.Controls.OfType<Button>())Button(button,button.DialogResult==DialogResult.OK||button.DialogResult==DialogResult.Yes);foreach(var text in form.Controls.OfType<TextBox>())text.BorderStyle=BorderStyle.FixedSingle;}
}
internal sealed partial class MainForm {
 internal bool Offscreen;protected override bool ShowWithoutActivation{get{return Offscreen||base.ShowWithoutActivation;}}
 Label fiscalCaption,modeHeading,modeSubheading,vacationRuleNote,holidayRuleNote,extraEmpty,ruleValue,planValue;
 Button consentMenu,nonConsentMenu,clearAgreedButton;bool nonConsentMode,guardBusy;
 CalendarView calendarView;CalendarDetail liveDetail;CardPanel discCard;Panel pastelBody;
 readonly HashSet<DateTime> agreedWork=new HashSet<DateTime>();

 CardPanel MakeCard(string title,string sub,Color header,int x,int y,int width,int height){return new CardPanel{Title=title,Sub=sub,HeaderFill=header,Bounds=new Rectangle(x,y,width,height)};}
 static CardPanel MakeTile(Color fill){return new CardPanel{HasHeader=false,BodyFill=fill,Padding=new Padding(14,0,14,0)};}
 static Label TileText(string text,float size,bool bold){var label=FlatDesign.Text(text,0,0,100,64,size,bold);label.Dock=DockStyle.Fill;return label;}

 void BuildFlatInterface(){
  SuspendLayout();
  var workArea=Screen.PrimaryScreen.WorkingArea;
  Text="교육공무직원 320일 근무 편성기 · "+AppRelease.DisplayVersion;Icon=LoadAppIcon();Font=new Font("맑은 고딕",10F);ForeColor=FlatDesign.Ink;BackColor=FlatDesign.Background;
  ClientSize=new Size(1280,Math.Min(860,workArea.Height-60));MinimumSize=new Size(1200,700);StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;DoubleBuffered=true;

  // ── 상단 바: 제목 · 모드 전환 · 공휴일 자료
  var top=new BufferedPanel{Dock=DockStyle.Top,Height=64,BackColor=Color.White};
  top.Paint+=(s,e)=>{using(var pen=new Pen(FlatDesign.Edge))e.Graphics.DrawLine(pen,0,top.Height-1,top.Width,top.Height-1);};
  var logo=new Label{Text="320",Bounds=new Rectangle(24,16,36,32),BackColor=FlatDesign.Blue,ForeColor=Color.White,Font=new Font("맑은 고딕",9F,FontStyle.Bold),TextAlign=ContentAlignment.MiddleCenter};
  var title=FlatDesign.Text("교육공무직원 320일 근무 편성기",68,12,320,40,14,true);
  var segment=new BufferedPanel{Bounds=new Rectangle(400,13,272,38),BackColor=Color.FromArgb(238,240,246)};
  var seg1=new SegButton{Text="320일 동의자",Bounds=new Rectangle(4,4,132,30),Font=new Font("맑은 고딕",10F,FontStyle.Bold)};
  var seg2=new SegButton{Text="비동의자",Bounds=new Rectangle(136,4,132,30),Font=new Font("맑은 고딕",10F,FontStyle.Bold)};
  consentMenu=seg1;nonConsentMenu=seg2;segment.Controls.Add(seg1);segment.Controls.Add(seg2);
  top.Controls.Add(logo);top.Controls.Add(title);top.Controls.Add(segment);
  BuildHolidayPanel(top);
  BuildAppUpdateButton(top);
  foreach(Control control in new Control[]{holidaySourceLabel,holidayDataStatus,holidayCheckButton,holidayHistoryButton,appUpdateButton})control.Anchor=AnchorStyles.Top|AnchorStyles.Left;
  holidaySourceLabel.AutoEllipsis=true;holidayDataStatus.AutoEllipsis=true;
  Action layoutTop=()=>{float scale=top.Height/64F;int r=top.Width-(int)(24*scale),gap=(int)(8*scale),wide=(int)(112*scale),small=(int)(100*scale),y=(int)(15*scale),h=(int)(34*scale);appUpdateButton.SetBounds(r-wide,y,wide,h);holidayHistoryButton.SetBounds(appUpdateButton.Left-gap-wide,y,wide,h);holidayCheckButton.SetBounds(holidayHistoryButton.Left-gap-small,y,small,h);int lx=segment.Right+gap,lw=holidayCheckButton.Left-gap-lx;if(lw<0){lx=holidayCheckButton.Left-gap;lw=0;}holidaySourceLabel.SetBounds(lx,(int)(9*scale),lw,(int)(22*scale));holidayDataStatus.SetBounds(lx,(int)(31*scale),lw,(int)(24*scale));holidaySourceLabel.TextAlign=ContentAlignment.MiddleRight;holidayDataStatus.TextAlign=ContentAlignment.MiddleRight;};
  top.Resize+=(s,e)=>layoutTop();layoutTop();

  // ── 왼쪽: 입력 카드
  var left=new BufferedPanel{Dock=DockStyle.Left,Width=346,AutoScroll=true,BackColor=FlatDesign.Background};
  var basis=MakeCard("학교회계연도","",FlatDesign.HeaderLavender,0,0,314,88);
  year.Minimum=Holidays.EarliestSchoolYear;year.Maximum=2040;year.Value=Math.Max(Holidays.EarliestSchoolYear,Math.Min(2040,DateTime.Today.Month>=3?DateTime.Today.Year:DateTime.Today.Year-1));year.Font=new Font("맑은 고딕",10F);
  PlaceInput(year,basis,16,46,82,30);
  var yearSuffix=FlatDesign.Text("학년도",104,46,56,30,10F,false);yearSuffix.Name="SchoolYearSuffix";basis.Controls.Add(yearSuffix);
  fiscalCaption=FlatDesign.Text("",164,46,138,30,8F,false);fiscalCaption.ForeColor=FlatDesign.Muted;basis.Controls.Add(fiscalCaption);

  var vacations=MakeCard("방학 기간","",FlatDesign.HeaderPink,0,98,314,172);
  vacations.Controls.Add(FlatDesign.Text("여름방학",16,44,120,20,9.5F,true));vacations.Controls.Add(FlatDesign.Text("겨울방학",16,108,120,20,9.5F,true));
  foreach(var picker in new[]{summerStart,summerEnd,winterStart,winterEnd}){picker.Font=new Font("맑은 고딕",10.5F);picker.AccessibleName=picker==summerStart?"여름방학 시작일":picker==summerEnd?"여름방학 종료일":picker==winterStart?"겨울방학 시작일":"겨울방학 종료일";}
  PlaceInput(summerStart,vacations,16,66,128,34);vacations.Controls.Add(FlatDesign.Text("~",146,66,22,34,11F,false));PlaceInput(summerEnd,vacations,170,66,128,34);
  PlaceInput(winterStart,vacations,16,130,128,34);vacations.Controls.Add(FlatDesign.Text("~",146,130,22,34,11F,false));PlaceInput(winterEnd,vacations,170,130,128,34);
  foreach(Control c in vacations.Controls)if(c is Label&&c.Text=="~")((Label)c).TextAlign=ContentAlignment.MiddleCenter;

  discCard=MakeCard("재량휴업일","최대 4일",FlatDesign.HeaderPeach,0,280,314,220);discCard.Chip="0 / 4";
  for(int i=0;i<4;i++){
   discretionary[i]=DatePicker();discretionary[i].ShowCheckBox=true;discretionary[i].Checked=false;discretionary[i].AccessibleName="재량휴업일 "+(i+1);
   var optionalDate=discretionary[i];optionalDate.ValueChanged+=(s,e)=>{GuardDiscretionary(optionalDate);};
   discretionaryNotes[i]=new HintTextBox{BorderStyle=BorderStyle.FixedSingle,Font=new Font("맑은 고딕",10),AccessibleName="재량휴업일 "+(i+1)+" 메모"};
   discretionaryNotes[i].TextChanged+=(s,e)=>QueuePreview();
   PlaceInput(discretionary[i],discCard,16,46+i*42,142,34);PlaceInput(discretionaryNotes[i],discCard,166,46+i*42,132,34);
  }

  var extra=MakeCard("기타 휴일","",FlatDesign.HeaderLavender,0,510,314,156);
  extraList.BorderStyle=BorderStyle.FixedSingle;extraList.IntegralHeight=false;extraList.ItemHeight=28;extraList.Font=new Font("맑은 고딕",10);extraList.AccessibleName="기타 휴일 목록";
  PlaceInput(extraList,extra,16,44,282,64);
  extraEmpty=FlatDesign.Text("등록된 휴일이 없습니다.",24,52,266,48,10F,false);extraEmpty.TextAlign=ContentAlignment.MiddleCenter;extraEmpty.BackColor=Color.White;extraEmpty.ForeColor=FlatDesign.Muted;extra.Controls.Add(extraEmpty);extraEmpty.BringToFront();
  var add=ButtonAt("추가",16,116,136,34);((PastelButton)add).Glyph=4;var remove=ButtonAt("삭제",162,116,136,34);((PastelButton)remove).Glyph=5;extra.Controls.Add(add);extra.Controls.Add(remove);
  left.Controls.Add(basis);left.Controls.Add(vacations);left.Controls.Add(discCard);left.Controls.Add(extra);

  // ── 오른쪽: 요약 · 범례 · 12개월 달력
  var right=new BufferedPanel{Dock=DockStyle.Fill,BackColor=FlatDesign.Background};
  var strip=new BufferedPanel{Dock=DockStyle.Top,Height=74,BackColor=FlatDesign.Background};
  var tile1=MakeTile(FlatDesign.HeaderMint);var tile2=MakeTile(Color.White);var tile3=MakeTile(Color.White);var tile4=MakeTile(Color.White);
  ruleValue=TileText("320일",24F,true);ruleValue.TextAlign=ContentAlignment.MiddleRight;
  var caption1=FlatDesign.Text("유급 산정일수",0,0,92,64,9.5F,false);caption1.Dock=DockStyle.Left;caption1.ForeColor=FlatDesign.Muted;
  tile1.Controls.Add(ruleValue);tile1.Controls.Add(caption1);
  modeSubheading=TileText("",9.5F,false);tile2.Controls.Add(modeSubheading);
  modeHeading=TileText("",10.5F,true);tile3.Controls.Add(modeHeading);
  planValue=TileText("",9.5F,false);tile4.Controls.Add(planValue);
  strip.Controls.Add(tile1);strip.Controls.Add(tile2);strip.Controls.Add(tile3);strip.Controls.Add(tile4);
  Action layoutStrip=()=>{int w=strip.Width,gap=Math.Max(1,(int)Math.Round(strip.Height*10F/74F)),height=strip.Height-gap;float unit=(w-gap*3)/4.3F;int w1=(int)(unit*1.3F),w2=(int)unit,x=0;tile1.SetBounds(x,0,w1,height);x+=w1+gap;tile2.SetBounds(x,0,w2,height);x+=w2+gap;tile3.SetBounds(x,0,w2,height);x+=w2+gap;tile4.SetBounds(x,0,Math.Max(10,w-x),height);};
  strip.Resize+=(s,e)=>layoutStrip();layoutStrip();

  var legendPanel=new BufferedPanel{Dock=DockStyle.Top,Height=36,Padding=new Padding(0,3,0,3),BackColor=FlatDesign.Background};
  var legendBar=new LegendBar{Dock=DockStyle.Fill};
  clearAgreedButton=ButtonAt("약정근무일 모두 해제",0,0,190,30);clearAgreedButton.Dock=DockStyle.Right;clearAgreedButton.Font=new Font("맑은 고딕",9F);
  legendPanel.Controls.Add(legendBar);legendPanel.Controls.Add(clearAgreedButton);

  var notes=new BufferedPanel{Dock=DockStyle.Bottom,Height=50,Padding=new Padding(0,6,0,0),BackColor=FlatDesign.Background};
  holidayRuleNote=FlatDesign.Text("",0,0,100,22,8.5F,false);holidayRuleNote.Dock=DockStyle.Top;holidayRuleNote.ForeColor=FlatDesign.Muted;
  vacationRuleNote=FlatDesign.Text("",0,0,100,22,8.5F,false);vacationRuleNote.Dock=DockStyle.Top;vacationRuleNote.ForeColor=FlatDesign.Muted;
  notes.Controls.Add(holidayRuleNote);notes.Controls.Add(vacationRuleNote);

  calendarView=new CalendarView{Dock=DockStyle.Fill};calendarView.DateRightClick+=OnCalendarRightClick;
  right.Controls.Add(calendarView);right.Controls.Add(notes);right.Controls.Add(legendPanel);right.Controls.Add(strip);

  var body=new BufferedPanel{Dock=DockStyle.Fill,BackColor=FlatDesign.Background,Padding=new Padding(24,16,24,8)};pastelBody=body;
  body.Controls.Add(right);body.Controls.Add(left);

  // ── 하단: 안내 문구 + 저장 위치/실행 버튼
  var notice=new BufferedPanel{Dock=DockStyle.Bottom,Height=28,BackColor=FlatDesign.Background};
  var noticeText=FlatDesign.Text("개인이 제작한 프로그램입니다. 사용 전 관련 지침·규정을 반드시 확인한 후 활용하시기 바랍니다.",24,4,700,20,8F,false);noticeText.Name="PersonalProgramNotice";noticeText.ForeColor=FlatDesign.Muted;
  status.ForeColor=FlatDesign.Muted;status.Font=new Font("맑은 고딕",8.5F);status.TextAlign=ContentAlignment.MiddleRight;status.AutoSize=false;
  var version=FlatDesign.Text("Ver. "+AppRelease.DisplayVersion,0,4,100,20,8F,false);version.Name="ReleaseVersion";version.ForeColor=FlatDesign.Muted;version.TextAlign=ContentAlignment.MiddleRight;
  notice.Controls.Add(noticeText);notice.Controls.Add(status);notice.Controls.Add(version);
  notice.Resize+=(s,e)=>{int r=notice.Width-24;version.SetBounds(r-100,4,100,20);status.SetBounds(r-100-8-380,4,380,20);noticeText.Width=Math.Max(100,r-100-8-380-24-24);};

  var foot=new BufferedPanel{Dock=DockStyle.Bottom,Height=72,BackColor=Color.White};
  foot.Paint+=(s,e)=>{using(var pen=new Pen(FlatDesign.Edge))e.Graphics.DrawLine(pen,0,0,foot.Width,0);};
  var folderLabel=FlatDesign.Text("저장 위치",24,0,70,72,10F,true);foot.Controls.Add(folderLabel);
  outputFolder.Font=new Font("Segoe UI",10);outputFolder.AccessibleName="결과 저장 폴더";PlaceInput(outputFolder,foot,100,17,400,38);
  var choose=ButtonAt("폴더 선택",0,15,110,42);((PastelButton)choose).Glyph=2;foot.Controls.Add(choose);
  createButton.Text="엑셀 생성";FlatDesign.Button(createButton,true);createButton.Font=new Font("맑은 고딕",11,FontStyle.Bold);((PastelButton)createButton).Glyph=7;foot.Controls.Add(createButton);
  var open=ButtonAt("저장 위치 열기",0,15,150,42);((PastelButton)open).Glyph=2;foot.Controls.Add(open);
  foot.Resize+=(s,e)=>{int r=foot.Width-24;createButton.SetBounds(r-270,15,270,42);open.SetBounds(r-270-8-150,15,150,42);choose.SetBounds(r-270-8-150-8-110,15,110,42);PlaceInput(outputFolder,foot,100,17,Math.Max(120,choose.Left-12-100),38);};

  Controls.Add(body);Controls.Add(notice);Controls.Add(foot);Controls.Add(top);

  seg1.Click+=(s,e)=>SelectWorkerMode(false);seg2.Click+=(s,e)=>SelectWorkerMode(true);
  year.ValueChanged+=(s,e)=>{SetDefaults();UpdateFiscalCaption();RefreshHolidayStatus();};
  choose.Click+=ChooseFolder;add.Click+=AddExtra;remove.Click+=RemoveExtra;createButton.Click+=CreateWorkbook;open.Click+=OpenFolder;
  clearAgreedButton.Click+=(s,e)=>{agreedWork.Clear();RefreshLivePreview();};
  UpdateFiscalCaption();SelectWorkerMode(false);
  ResumeLayout(true);
 }

 void SelectWorkerMode(bool nonConsent){
  nonConsentMode=nonConsent;((SegButton)consentMenu).SetActive(!nonConsent);((SegButton)nonConsentMenu).SetActive(nonConsent);
  vacationRuleNote.Text=nonConsent?"방학 중 자동 근무일은 없습니다. 약정 근무는 달력 날짜를 우클릭해 '약정근무일'로 지정하세요.":"방학 중 첫 3개·마지막 5개 평일은 자동 근무일입니다. 추가 근무는 달력 날짜를 우클릭해 '약정근무일'로 지정하세요.";
  holidayRuleNote.Text=nonConsent?"방학 중 일반 공휴일은 무급 · 근로자의 날과 근무가 있는 주의 토·일은 별도 반영합니다.":"법정공휴일·대체공휴일은 공식 자료로 반영하며, 방학 중 공휴일은 유급, 근로자의 날 기준은 유지합니다.";
  createButton.Text="엑셀 생성";
  status.Text=nonConsent?"비동의자 계산 모드입니다. 입력값은 유지됩니다.":"320일 근로자 계산 모드입니다. 입력값은 유지됩니다.";
  RefreshLivePreview();
 }
 void UpdateFiscalCaption(){int y=(int)year.Value;fiscalCaption.Text=y+". 3. 1. ~ "+(y+1)+". 2. "+DateTime.DaysInMonth(y+1,2)+".";}

 // 재량휴업일은 방학 기간에 지정할 수 없다. 날짜 입력으로 방학 중 날짜를 고르면 안내 후 선택을 취소한다.
 void GuardDiscretionary(DateBox picker){
  if(guardBusy||!picker.Checked)return;
  DateTime date=picker.Value.Date;string label=null;
  if(date>=summerStart.Value.Date&&date<=summerEnd.Value.Date)label="여름방학("+summerStart.Value.ToString("yyyy-MM-dd")+" ~ "+summerEnd.Value.ToString("yyyy-MM-dd")+")";
  else if(date>=winterStart.Value.Date&&date<=winterEnd.Value.Date)label="겨울방학("+winterStart.Value.ToString("yyyy-MM-dd")+" ~ "+winterEnd.Value.ToString("yyyy-MM-dd")+")";
  if(label==null)return;
  guardBusy=true;try{picker.Checked=false;}finally{guardBusy=false;}
  Notify(date.ToString("yyyy-MM-dd")+"은(는) "+label+" 기간입니다.\r\n방학 중에는 재량휴업일을 지정할 수 없으니 학기 중 평일로 다시 선택해 주세요.","입력값을 확인해 주세요",MessageBoxIcon.Warning);
 }
}
}
