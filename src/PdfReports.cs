using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ShortPay {
static class PdfReports {
 public static void Save(string path,string kind,Record r,Job j,Result z,ExportOptions options){
  if(kind!="급여명세서"&&kind!="임금산정표")throw new ArgumentException("PDF로 저장할 문서 종류가 올바르지 않습니다.","kind");
  string tempFolder=Path.Combine(Path.GetTempPath(),"ShortPayPdf_"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(tempFolder);
  try{
   string workbook=Path.Combine(tempFolder,"print.xlsx");
   // Use exactly the workbook, formulas and print settings produced by the Excel button.
   ExcelReports.Save(workbook,kind,r,j,z,options);
   ExportWorkbook(workbook,path);
  }finally{try{Directory.Delete(tempFolder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
 }
 internal static void ExportWorkbook(string workbookPath,string pdfPath){
  string target=Path.GetFullPath(pdfPath),folder=Path.GetDirectoryName(target);
  Directory.CreateDirectory(folder);
  string staging=Path.Combine(folder,".ShortPayPdf_"+Guid.NewGuid().ToString("N")+".pdf");
  dynamic excel=null,books=null,book=null;
  try{
   Type type=Type.GetTypeFromProgID("Excel.Application");
   if(type==null)throw new InvalidOperationException("Excel 양식 그대로 PDF를 저장하려면 Microsoft Excel이 설치되어 있어야 합니다. Excel 설치 상태를 확인해 주세요.");
   try{excel=Activator.CreateInstance(type);}catch(COMException ex){throw new InvalidOperationException("Microsoft Excel을 시작하지 못했습니다. Excel을 정상적으로 실행할 수 있는지 확인한 뒤 다시 저장해 주세요.",ex);}
   excel.Visible=false;excel.DisplayAlerts=false;excel.EnableEvents=false;excel.AutomationSecurity=3;excel.AskToUpdateLinks=false;
   books=excel.Workbooks;
   book=books.Open(Path.GetFullPath(workbookPath),0,true,Type.Missing,Type.Missing,Type.Missing,true);
   excel.CalculateFull();
   book.ExportAsFixedFormat(0,staging,0,true,false,Type.Missing,Type.Missing,false);
   using(var stream=File.OpenRead(staging)){
    byte[] signature=new byte[5];if(stream.Read(signature,0,5)!=5||System.Text.Encoding.ASCII.GetString(signature)!="%PDF-")throw new IOException("Excel에서 PDF 파일을 정상적으로 생성하지 못했습니다.");
   }
   if(File.Exists(target))File.Replace(staging,target,null);else File.Move(staging,target);
  }catch(COMException ex){throw new InvalidOperationException("Excel 양식을 PDF로 내보내지 못했습니다. Excel의 인쇄/PDF 저장 기능과 저장 위치를 확인해 주세요.",ex);}
  finally{
   if((object)book!=null)try{book.Close(false);}catch{}finally{Release((object)book);}
   Release((object)books);
   if((object)excel!=null)try{excel.Quit();}catch{}finally{Release((object)excel);}
   if(File.Exists(staging))try{File.Delete(staging);}catch(IOException){}catch(UnauthorizedAccessException){}
  }
 }
 static void Release(object value){if(value!=null&&Marshal.IsComObject(value))try{Marshal.FinalReleaseComObject(value);}catch{}}
}
}
