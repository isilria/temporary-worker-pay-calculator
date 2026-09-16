using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace ShortPay {
public partial class MainForm {
 void ExerciseStorage(string folder){
  var opened=new List<string>();var messages=new List<string>();OpenGeneratedFile=path=>{AssertUI(File.Exists(path),"open only after completed save");opened.Add(path);};OutputMessage=messages.Add;
  r=new Record{Name="저장검증",OriginalWorker="원근로자",Reason="휴직",Start=new DateTime(2026,9,7),End=new DateTime(2026,9,20),Recipient="가01",Institution="예시학교"};Engine.Fill(r);LoadUI();RefreshAll();
  AssertUI(rateConfirm.Parent==null,"annual rate checkbox removed from UI");
  for(int i=0;i<3;i++)outputFolders[i].Text=Path.Combine(folder,"문서 저장 "+i);
  openGenerated.Checked=false;FlushSettings();
  using(var restored=new MainForm()){for(int i=0;i<3;i++)AssertUI(restored.outputFolders[i].Text==outputFolders[i].Text,"document folder persisted "+i);AssertUI(!restored.openGenerated.Checked,"open preference persisted off");}
  string[] kinds={"임금산정표","급여명세서","인건비 신청내역"};
  for(int i=0;i<3;i++){Export(kinds[i]);var files=Directory.GetFiles(outputFolders[i].Text,"*.xlsx");AssertUI(files.Length==1&&new FileInfo(files[0]).Length>1000,"generation button saves directly to matching folder "+i+" / "+messages.Last());}
  AssertUI(opened.Count==0&&messages.Count==3,"unchecked option saves without opening");
  string first=Directory.GetFiles(outputFolders[0].Text,"*.xlsx")[0];byte[] originalBytes=File.ReadAllBytes(first);Export(kinds[0]);AssertUI(File.ReadAllBytes(first).SequenceEqual(originalBytes)&&Directory.GetFiles(outputFolders[0].Text,"* (2).xlsx").Length==1,"duplicate filename preserves original");
  openGenerated.Checked=true;FlushSettings();using(var restored=new MainForm())AssertUI(restored.openGenerated.Checked,"open preference persisted on");
  Export(kinds[1]);AssertUI(opened.Count==1&&Path.GetDirectoryName(opened[0])==outputFolders[1].Text,"checked option opens correct generated file");
  OpenGeneratedFile=path=>{throw new InvalidOperationException("연결 프로그램 없음");};Export(kinds[1]);AssertUI(messages.Last().StartsWith("파일은 저장했지만 열지 못했습니다.")&&Directory.GetFiles(outputFolders[1].Text,"*.xlsx").Length==3,"open failure retains saved workbook and reports its location");
  string validFolder=outputFolders[0].Text;string fileInstead=Path.Combine(folder,"폴더가 아닌 파일");File.WriteAllText(fileInstead,"keep");outputFolders[0].Text=fileInstead;Export(kinds[0]);AssertUI(messages.Last().StartsWith("엑셀 파일을 저장하지 못했습니다.")&&File.ReadAllText(fileInstead)=="keep","invalid destination fails without replacing existing file");outputFolders[0].Text="relative-folder";Export(kinds[0]);AssertUI(messages.Last().Contains("올바른 저장 폴더"),"relative destination rejected");outputFolders[0].Text=validFolder;
  int before=Directory.GetFiles(outputFolders[2].Text).Length;Set("hourly",20000);Export(kinds[2]);AssertUI(messages.Last().StartsWith("엑셀 파일을 저장하지 못했습니다.")&&Directory.GetFiles(outputFolders[2].Text).Length==before,"formula failure leaves no partial export");Set("hourly",0);
  FlushSettings();tabs.SelectedIndex=5;CaptureScreen(Path.Combine(folder,"storage-settings.png"));tabs.SelectedIndex=3;CaptureScreen(Path.Combine(folder,"v1-output.png"));tabs.SelectedIndex=2;CaptureScreen(Path.Combine(folder,"v1-money.png"));dirty=false;
 }
}
}
