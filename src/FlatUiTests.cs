using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShortPay {
public static class FlatUiTests {
 [STAThread] public static void Main(string[] args){ExcelReports.RegisterDependencies();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Run(args.Last());}
 internal static void PrepareContext(string folder){Directory.CreateDirectory(folder);MainForm.SettingsTestFile=Path.Combine(folder,"isolated-settings.json");File.WriteAllText(MainForm.SettingsTestFile,Engine.Json(new {AutomaticUpdates=false,Theme=0,WageFolder=Path.Combine(folder,"임금산정표"),PayslipFolder=Path.Combine(folder,"급여명세서"),RequestFolder=Path.Combine(folder,"신청서")}));}
 public static void Run(string folder){PrepareContext(folder);try{using(var form=new MainForm()){form.CheckFlatLayout(folder);} }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"flat-ui-tests.txt"),ex.ToString());Environment.ExitCode=1;}}
}
public static class FlatPreview {
 [STAThread] public static void Main(){ExcelReports.RegisterDependencies();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);var folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview-qa");FlatUiTests.PrepareContext(folder);using(var form=new MainForm()){form.LoadFlatExample();Application.Run(form);}}
}
public partial class MainForm {
 internal void CheckFlatLayout(string folder){int checks=0;Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception("FLAT UI: "+label);checks++;};
  LoadFlatExample();OffscreenTest=true;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Location=new Point(SystemInformation.VirtualScreen.Right+200,SystemInformation.VirtualScreen.Top);Show();PerformLayout();Application.DoEvents();
  foreach(var size in new[]{new Size(1280,800),new Size(1200,800),new Size(1600,950)}){
   ClientSize=size;PerformLayout();Application.DoEvents();tabs.SelectedIndex=1;DrawCalendar();Application.DoEvents();
   var days=calendar.Controls.OfType<DayButton>().ToList();check(days.Count==24,"single contract calendar keeps all 24 dates: "+size);check(days.All(b=>b.Width==b.Height),"every day hit target is square: "+size);check(days.All(b=>b.Width>=38),"date text has usable size: "+size);check(calendarLegend.Bottom<=calendarHost.Height&&holidayFormula.Bottom<=calendarHost.Height,"calendar and formula stay inside card: "+size);
   check(days.Single(b=>b.AccessibleName.StartsWith("2026-09-24")).State==4&&days.Single(b=>b.AccessibleName.StartsWith("2026-10-05")).State==4,"paid holidays survive month boundary: "+size);
   check(days.Where(b=>b.WeeklyHoliday).Count()==3&&calendarTotals!=null,"three weekly tags and totals strip: "+size);
   for(int index=0;index<6;index++){tabs.SelectedIndex=index;PerformLayout();Application.DoEvents();var page=tabs.TabPages[index].Controls.OfType<FlowLayoutPanel>().First();check(page.Controls.Cast<Control>().All(c=>c.Bottom<=page.ClientSize.Height),"page "+index+" fits "+size+"; height "+page.ClientSize.Height+" bottom "+page.Controls.Cast<Control>().Max(c=>c.Bottom));var sections=page.Controls.Cast<Control>().ToArray();check(Enumerable.Range(1,Math.Max(0,sections.Length-1)).All(n=>sections[n].Top>=sections[n-1].Bottom),"page sections do not overlap "+index+" "+size);}
   check(resetAllButton.Right<=resetAllButton.Parent.Width&&resetAllButton.Left>=620,"header actions fit: "+size);
  }
  tabs.SelectedIndex=1;check(result.HolidayHours==16.8m&&result.Net==1429260m,"approved example calculation survives new UI");
  r.Start=new DateTime(2026,8,1);r.End=new DateTime(2026,8,30);Engine.Fill(r,jobs[r.JobIndex]);LoadUI();RefreshAll();Application.DoEvents();check(calendar.RowCount==7&&calendar.Controls.OfType<DayButton>().All(b=>b.Width==b.Height),"six calendar rows still have square cells");check(calendarLegend.Bottom<=calendarHost.Height,"six-row legend stays within card");
  dirty=false;Hide();File.WriteAllText(Path.Combine(folder,"flat-ui-tests.txt"),"PASS "+checks+" layout checks: square dates, three window sizes, all six pages, month boundary, six calendar rows and preserved example money.");
 }
}
}
