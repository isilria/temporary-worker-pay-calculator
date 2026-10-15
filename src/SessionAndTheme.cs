using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace ShortPay {
public partial class MainForm {
 bool closingSession;string lastImportNote="";Card weeklyTint;ToolTip importTooltip;
 internal bool OffscreenTest;
 protected override bool ShowWithoutActivation {get{return OffscreenTest||base.ShowWithoutActivation;}}
 void CloseSession(object sender,FormClosingEventArgs e){
  // Only institution/preferences are persisted. Calculation inputs live in this form.
  closingSession=true;AutoValidate=AutoValidate.Disable;e.Cancel=false;FlushSettings();
 }
 internal void LoadWorkFile(string path){
  bool excel=String.Equals(Path.GetExtension(path),".xlsx",StringComparison.OrdinalIgnoreCase);
  var imported=excel?WageWorkbook.Load(path,jobs):new WageImport{Record=Engine.Load(path)};
  var loaded=imported.Record;WageWorkbook.Validate(loaded,jobs);
  if(imported.Legacy){loaded.Recipient=recipient.Text;loaded.Manager=manager.Text;loaded.Theme=theme.SelectedIndex;if(String.IsNullOrWhiteSpace(loaded.Institution))loaded.Institution=institution.Text;if(String.IsNullOrWhiteSpace(loaded.Contact))loaded.Contact=contact.Text;}
  if(loaded.Other!=0||loaded.Override||loaded.Separate){imported.Note+=" 이전 자료의 기타공제·주휴 수동확정·별도계약 설정은 해제하여 다시 계산했습니다.";loaded.Other=0;loaded.Override=false;loaded.Separate=false;}
  // Read and validate completely before replacing the current editing session.
  Engine.Calculate(loaded,jobs[loaded.JobIndex]);
  r=loaded;currentFile=excel?null:path;lastContractWarning="";lastImportNote=imported.Note??"";
  LoadUI();RefreshAll();dirty=false;tabs.SelectedIndex=1;SyncNavigation();
  if(importTooltip==null)importTooltip=new ToolTip{AutoPopDelay=20000,InitialDelay=350};importTooltip.SetToolTip(notice,lastImportNote);
 }
 void ApplySectionColors(){
  for(int index=0;index<tabs.TabCount;index++){
   var page=tabs.TabPages[index].Controls.OfType<FlowLayoutPanel>().First();var cards=page.Controls.OfType<Card>().ToArray();
   for(int n=0;n<cards.Length;n++){
    Color color=Palette.BlueTint;
    if(index==0&&n==1)color=Palette.Cream;
    if(index==2)color=n==0?Palette.Cream:n==1?Palette.Mint:Palette.Blush;
    if(index==3)color=Palette.Mint;
    if(index==5)color=n==1?Palette.Mint:n==3?Palette.Cream:Palette.BlueTint;
    cards[n].BackColor=color;
   }
  }
  sidebar.BackColor=Color.FromArgb(231,236,248);calendar.BackColor=Palette.BlueTint;
  weeklyTint=new Card{BackColor=Palette.Cream,Padding=Padding.Empty};calendarHost.Controls.Add(weeklyTint);weeklyTint.Controls.Add(weeklyHeading);weeklyTint.Controls.Add(weeks);weeklyTint.Controls.Add(holidayFormula);
  weeklyHeading.BackColor=holidayFormula.BackColor=Palette.Cream;
  weeks.BackgroundColor=Palette.Cream;weeks.DefaultCellStyle.BackColor=Palette.Cream;weeks.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(255,248,232);weeks.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(244,229,199);
  foreach(var card in tabs.TabPages[3].Controls.OfType<FlowLayoutPanel>().First().Controls.OfType<Card>())foreach(var grid in card.Controls.OfType<TableLayoutPanel>())foreach(var pane in grid.Controls.OfType<Card>())pane.BackColor=Color.FromArgb(239,248,242);
 }
}
}
