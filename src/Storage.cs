using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
namespace ShortPay {
public partial class MainForm {
 TextBox[] outputFolders=new TextBox[3];CheckBox openGenerated;
 internal Action<string> OpenGeneratedFile=path=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path){UseShellExecute=true});
 internal Action<string> OutputMessage=message=>MessageBox.Show(message,"자료 생성");
 static readonly string[] DocumentNames={"임금산정표","급여명세서","인건비 신청서"};
 static string DefaultOutputFolder(int index){return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"대체근로자 인건비",DocumentNames[index]);}
 void BuildStorageSettings(Control page){
  var card=Section(page,"저장 경로 설정",184);
  for(int i=0;i<3;i++){int index=i;var label=L(DocumentNames[i],9);label.SetBounds(20,37+i*35,120,30);card.Controls.Add(label);
   var box=new Card{Input=true,Padding=new Padding(10,7,10,3),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};box.SetBounds(145,37+i*35,650,31);card.Controls.Add(box);
   var input=new TextBox{Text=DefaultOutputFolder(i),BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,BackColor=Palette.Soft,AccessibleName=DocumentNames[i]+" 저장 폴더"};outputFolders[i]=input;box.Controls.Add(input);input.TextChanged+=(s,e)=>ScheduleSettingsSave();
   var browse=B("폴더 선택",()=>ChooseOutputFolder(index),125);browse.SetBounds(810,37+i*35,125,31);browse.Anchor=AnchorStyles.Top|AnchorStyles.Right;card.Controls.Add(browse);
  }
  openGenerated=new CheckBox{Text="생성 파일 바로 실행",AutoSize=true,BackColor=Color.Transparent};openGenerated.SetBounds(22,146,270,27);openGenerated.CheckedChanged+=(s,e)=>ScheduleSettingsSave();card.Controls.Add(openGenerated);
 }
 void ChooseOutputFolder(int index){using(var dialog=new FolderBrowserDialog{Description=DocumentNames[index]+" 저장 폴더를 선택하세요.",SelectedPath=outputFolders[index].Text,ShowNewFolderButton=true})if(dialog.ShowDialog(this)==DialogResult.OK){outputFolders[index].Text=dialog.SelectedPath;FlushSettings();}}
 void LoadStorageSettings(Settings settings){string[] paths={settings.WageFolder,settings.PayslipFolder,settings.RequestFolder};for(int i=0;i<3;i++)outputFolders[i].Text=String.IsNullOrWhiteSpace(paths[i])?DefaultOutputFolder(i):paths[i];openGenerated.Checked=settings.OpenGenerated;}
 void ExportPdf(string kind){Export(kind,true);}
 internal string CreateDocument(string kind,bool pdf=false){
  int index=kind=="임금산정표"?0:kind=="급여명세서"?1:2;string folder=outputFolders[index].Text.Trim();
  if(String.IsNullOrWhiteSpace(folder)||!Path.IsPathRooted(folder)||!String.Equals(Path.GetPathRoot(folder),Path.GetPathRoot(Path.GetFullPath(folder)),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("설정에서 "+DocumentNames[index]+"의 올바른 저장 폴더를 지정하세요.");
  folder=Path.GetFullPath(folder);Directory.CreateDirectory(folder);string name=Path.ChangeExtension(OutputFileName(kind,r,CurrentOutputOptions()),pdf?".pdf":".xlsx");string staging=Path.Combine(folder,"."+Guid.NewGuid().ToString("N")+(pdf?".pdf":".xlsx"));
  try{if(pdf)PdfReports.Save(staging,kind,r,jobs[r.JobIndex],result,CurrentOutputOptions());else ExcelReports.Save(staging,kind,r,jobs[r.JobIndex],result,CurrentOutputOptions());for(int number=1;;number++){string target=Path.Combine(folder,number==1?name:Path.GetFileNameWithoutExtension(name)+" ("+number+")"+Path.GetExtension(name));try{File.Move(staging,target);return target;}catch(IOException){if(!File.Exists(target))throw;}}}
  finally{if(File.Exists(staging))File.Delete(staging);}
 }
 void SaveAndOpenDocument(string kind,bool pdf=false){string type=pdf?"PDF":"엑셀";string path;try{path=CreateDocument(kind,pdf);}catch(Exception ex){OutputMessage(type+" 파일을 저장하지 못했습니다.\n"+ex.Message);return;}
  if(openGenerated.Checked){try{OpenGeneratedFile(path);}catch(Exception ex){OutputMessage("파일은 저장했지만 열지 못했습니다.\n"+path+"\n"+ex.Message);}}else OutputMessage(type+" 파일을 저장했습니다.\n"+path);
 }
}
}
