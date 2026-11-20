using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace SchoolWorkdayCalculator {
internal sealed partial class MainForm {
 static int flatChecks;
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);flatChecks++;}
 static System.Collections.Generic.IEnumerable<Control> Descendants(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var item in Descendants(child))yield return item;}}
 void Snapshot(string path){using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(Point.Empty,Size));bitmap.Save(path);}}
 internal static void TestFlat(string folder){Directory.CreateDirectory(folder);flatChecks=0;try{using(var form=new MainForm{Offscreen=true,ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new Point(SystemInformation.VirtualScreen.Right+200,SystemInformation.VirtualScreen.Top)}){
  form.Show();Application.DoEvents();form.year.Value=2026;form.outputFolder.Text=folder;
  string message="";form.TestMessage=(text,title,icon)=>{message=title+":"+text;};
  Check(Descendants(form).OfType<DateBox>().Count()==8,"all eight date inputs retained");
  Check(form.calendarView!=null&&form.calendarView.Visible&&form.calendarView.Width>600&&form.calendarView.Height>300,"twelve-month calendar fills the right side");
  Check(Descendants(form).OfType<Button>().All(x=>x.FlatStyle==FlatStyle.Flat),"buttons flat");
  Check(form.summerStart.Value.Date==new DateTime(2026,7,21)&&form.winterEnd.Value.Date==new DateTime(2027,2,28),"default vacation periods retained");
  Check(form.fiscalCaption.Text.Contains("2027. 2. 28."),"fiscal year caption");
  Check(form.holidaySourceLabel.Parent==form.holidayCheckButton.Parent&&form.holidayDataStatus.Parent==form.holidayHistoryButton.Parent,"holiday data controls live in the top bar");
  Check(form.holidayCheckButton.Text=="공휴일 확인"&&form.holidayHistoryButton.Text=="일정 변경 내역"&&form.holidayCheckButton.Right<form.holidayHistoryButton.Left,"holiday buttons aligned at right");
  Check(form.holidayDataStatus.Text=="작성 가능"&&form.holidayDataStatus.ForeColor==FlatDesign.Blue,"available fiscal year shown in blue");
  Check(!form.holidaySourceLabel.Bounds.IntersectsWith(form.holidayCheckButton.Bounds)&&!form.holidayDataStatus.Bounds.IntersectsWith(form.holidayHistoryButton.Bounds),"status and right buttons do not overlap");
  var notice=Descendants(form).OfType<Label>().Single(x=>x.Name=="PersonalProgramNotice");
  Check(notice.Text.Contains("개인이 제작한")&&notice.Text.Contains("관련 지침·규정")&&notice.Text.Contains("반드시 확인"),"personal-program warning kept");
  Check(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version==new Version(AppRelease.FileVersion)&&Descendants(form).OfType<Label>().Any(x=>x.Text=="Ver. "+AppRelease.DisplayVersion),"release version shown consistently");
  Check(form.Text.StartsWith("교육공무직원 320일 근무 편성기"),"approved application name");
  foreach(var size in new[]{new Size(1200,720),new Size(1440,960),new Size(1600,1000)}){form.ClientSize=size;Application.DoEvents();Check(!form.holidaySourceLabel.Bounds.IntersectsWith(form.holidayCheckButton.Bounds),"top bar stays clean at "+size);Check(form.createButton.Right<=form.createButton.Parent.Width&&form.createButton.Left>0,"generate button visible at "+size);Check(form.calendarView.Width>600,"calendar keeps room at "+size);}
  form.ClientSize=new Size(1440,960);Application.DoEvents();
  var settings=form.BuildSettings();Check(settings.Discretionary.Count==0&&settings.AgreedWork.Count==0,"nothing specified by default");
  form.Snapshot(Path.Combine(folder,"320일_기본화면.png"));
  // 달력 우클릭으로 지정하는 동작
  form.SetDiscretionary(new DateTime(2026,5,4));Check(form.discretionary[0].Checked&&form.discretionary[0].Value.Date==new DateTime(2026,5,4)&&form.liveDetail.Discretionary.Contains(new DateTime(2026,5,4)),"discretionary day set from calendar fills the left list");
  form.ClearDiscretionary(0);Check(!form.discretionary[0].Checked&&form.liveDetail.Discretionary.Count==0,"discretionary day cleared from calendar");
  form.SetExtra(new DateTime(2026,12,24));Check(form.extras.Count==1&&form.extraList.Items.Count==1&&form.liveDetail.CodeOn(new DateTime(2026,12,24))=="H","temporary holiday allowed inside vacation");
  form.ClearExtra(new DateTime(2026,12,24));Check(form.extras.Count==0&&form.liveDetail.CodeOn(new DateTime(2026,12,24))=="N","temporary holiday removed");
  int paidBefore=form.liveResult.SeasonalPaidDays,workBefore=form.liveResult.SeasonalWorkDays;
  form.agreedWork.Add(new DateTime(2027,1,12));form.RefreshLivePreview();Check(form.liveResult.SeasonalWorkDays==workBefore+1&&form.liveResult.SeasonalPaidDays==paidBefore+3&&form.liveDetail.Agreed.Count==1,"agreed work day counts as work and gives a paid weekend");
  Check(form.clearAgreedButton.Enabled,"clear-all enabled while agreed days exist");
  form.clearAgreedButton.PerformClick();Check(form.agreedWork.Count==0&&form.liveResult.SeasonalPaidDays==paidBefore&&!form.clearAgreedButton.Enabled,"clearing agreed days restores counts");
  // 방학 중 재량휴업일 차단
  form.discretionary[1].Value=new DateTime(2026,12,24);form.discretionary[1].Checked=true;
  Check(!form.discretionary[1].Checked&&message.StartsWith("입력값을 확인해 주세요")&&message.Contains("방학 중에는 재량휴업일을 지정할 수 없"),"discretionary day inside vacation shows an error and is cancelled");
  form.discretionary[0].Value=new DateTime(2026,5,4);form.discretionary[0].Checked=true;form.discretionaryNotes[0].Text="학교장 재량휴업";
  settings=form.BuildSettings();Check(settings.Discretionary.Count==1&&settings.DiscretionaryNames[new DateTime(2026,5,4)]=="학교장 재량휴업","date checkbox and memo binding");
  form.extras.Add(new ExtraHoliday{Date=new DateTime(2026,10,16),Name="학교 지정 휴일"});form.RefreshExtras();Check(form.extraList.Items.Count==1&&form.BuildSettings().Extras.Count==1,"extra holiday binding");
  form.extraList.SelectedIndex=0;Descendants(form).OfType<Button>().Single(x=>x.Text=="삭제").PerformClick();Check(form.extras.Count==0&&form.extraList.Items.Count==0,"delete button event");
  DateTime validEnd=form.summerEnd.Value;form.summerEnd.Value=form.summerStart.Value.AddDays(-1);bool rejected=false;try{form.BuildSettings();}catch{rejected=true;}Check(rejected,"reversed dates rejected");form.summerEnd.Value=validEnd;
  var result=WorkbookBuilder.Build(Path.Combine(folder,"flat-2026.xlsx"),form.BuildSettings());File.WriteAllText(Path.Combine(folder,"flat-2026.txt"),"REGULAR_WORK="+result.RegularWorkDays+"\r\nREGULAR_PAID="+result.RegularPaidDays+"\r\nSEASONAL_WORK="+result.SeasonalWorkDays+"\r\nSEASONAL_PAID="+result.SeasonalPaidDays+"\r\n");Check(result.RegularWorkDays>0&&result.SeasonalPaidDays>0,"default workbook generated");
  form.year.Value=2027;settings=form.BuildSettings();Check(form.discretionary.All(x=>!x.Checked)&&settings.DiscretionaryNames.Count==0&&settings.AgreedWork.Count==0,"year change resets optional dates, memos and agreed days");Check(settings.Winter.End==new DateTime(2028,2,29)&&form.fiscalCaption.Text.Contains("2028. 2. 29."),"leap year end retained");
  Check(form.holidayDataStatus.Text=="작성 가능"&&form.holidayDataStatus.ForeColor==FlatDesign.Blue,"next fiscal year uses both 2027 and 2028");form.Snapshot(Path.Combine(folder,"학년도_작성가능.png"));
  form.year.Value=2028;form.AwaitPreview();Check(form.holidayDataStatus.Text.Contains("작성불가")&&form.holidayDataStatus.Text.Contains("2029년 데이터 없음")&&form.holidayDataStatus.ForeColor==Color.FromArgb(192,45,45),"missing next-year data shown in red");Check(form.liveDetail==null&&form.previewError!="","missing holiday data clears the calendar");form.Snapshot(Path.Combine(folder,"학년도_작성불가.png"));
  form.year.Value=2029;Check(form.holidayDataStatus.Text.Contains("2029년·2030년 데이터 없음"),"both missing years reported");
  form.year.Value=2027;
 }
 using(var dialog=new ExtraHolidayDialog(new DateTime(2026,3,1),new DateTime(2027,2,28))){Check(dialog.Controls.OfType<Button>().All(b=>b.FlatStyle==FlatStyle.Flat),"extra holiday dialog styled");Check(dialog.AcceptButton!=null&&dialog.CancelButton!=null,"extra holiday keyboard actions");}
 using(var dialog=new ExistingFileDialog("example.xlsx")){Check(dialog.Controls.OfType<Button>().All(b=>b.FlatStyle==FlatStyle.Flat),"overwrite dialog styled");Check(dialog.Controls.OfType<Button>().Select(b=>b.DialogResult).OrderBy(v=>(int)v).SequenceEqual(new[]{DialogResult.Yes,DialogResult.No,DialogResult.Retry}.OrderBy(v=>(int)v)),"overwrite/cancel/rename preserved");}
 File.WriteAllText(Path.Combine(folder,"flat-ui-tests.txt"),"PASS "+flatChecks+" layout, calendar right-click actions, validation, button action, year/leap-year, dialogs and XLSX generation checks");
 }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"flat-ui-tests.txt"),ex.ToString());Environment.ExitCode=1;}}
}
}
