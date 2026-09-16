using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
namespace ShortPay {
static class PdfReports {
 static readonly Color Ink=Color.FromArgb(28,45,92),Line=Color.FromArgb(202,210,223);
 static string N(decimal n){return n.ToString("#,##0.####");}
 static void Text(Graphics g,string text,float x,float y,float width,float height,int size=22,bool bold=false,bool right=false){using(var font=new Font("맑은 고딕",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(Ink))using(var format=new StringFormat{Alignment=right?StringAlignment.Far:StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter})g.DrawString(text,font,brush,new RectangleF(x,y,width,height),format);}
 static void Box(Graphics g,float x,float y,float w,float h,Color fill){using(var brush=new SolidBrush(fill))g.FillRectangle(brush,x,y,w,h);using(var pen=new Pen(Line,1))g.DrawRectangle(pen,x,y,w,h);}
 static void Items(Graphics g,string title,List<PayItem> items,decimal total,int x,int y,int width,int height,Color color){Box(g,x,y,width,height,Color.White);Box(g,x,y,width,42,color);Text(g,title,x+16,y,width-32,42,23,true);int rowHeight=Math.Min(42,(height-98)/Math.Max(1,items.Count));for(int i=0;i<items.Count;i++){Text(g,items[i].Name,x+16,y+46+i*rowHeight,width-186,rowHeight,20);Text(g,N(items[i].Amount)+"원",x+width-174,y+46+i*rowHeight,158,rowHeight,22,false,true);}Box(g,x,y+height-48,width,48,color);Text(g,title+" 계",x+16,y+height-48,160,48,22,true);Text(g,N(total)+"원",x+width-226,y+height-48,210,48,25,true,true);}
 public static void Save(string path,string kind,Record r,Job j,Result z,ExportOptions o){Engine.ValidateExport(r,j,z);if(Engine.ContractWarning(r)!="")throw new InvalidOperationException(Engine.ContractWarning(r));if(String.IsNullOrWhiteSpace(r.Name))throw new InvalidOperationException("대상자 성명을 입력하세요.");bool wage=kind=="임금산정표";int width=wage?1684:1190,height=wage?1190:1684;
  using(var bitmap=new Bitmap(width*3/2,height*3/2)){
   using(var g=Graphics.FromImage(bitmap)){g.Clear(Color.White);g.ScaleTransform(1.5f,1.5f);g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
    Text(g,kind,60,40,width-120,65,40,true);Text(g,r.Institution+"  |  "+j.name+"  |  "+r.Name,60,110,width-120,38,23,true);Text(g,"계약기간  "+r.Start.ToString("yyyy.MM.dd")+" ~ "+r.End.ToString("yyyy.MM.dd")+"    생년월일  "+ExcelReports.Birth(r),60,150,width-120,38,21);Text(g,"지급계좌  "+r.Bank+"  "+r.Account+"  "+r.Holder,60,188,width-120,35,20);
    var pay=PayrollItems.Pay(r,z);var deduct=PayrollItems.Deduct(r,z);int column=(width-140)/2,tableHeight=wage?340:405;
    Items(g,"임금",pay,z.Gross,60,240,column,tableHeight,Color.FromArgb(234,243,255));Items(g,"공제",deduct,z.Deductions,80+column,240,column,tableHeight,Color.FromArgb(253,235,242));
    int netY=250+tableHeight;Box(g,60,netY,width-120,65,Color.FromArgb(255,246,205));Text(g,"실지급액",80,netY,240,65,25,true);Text(g,N(z.Net)+"원",width-510,netY,430,65,34,true,true);
    if(wage){int y=netY+86;Text(g,"주별 근무 · 주휴 검토",60,y,740,40,24,true);y+=45;foreach(var week in z.Weeks){Text(g,week.Period+"    "+N(week.Actual)+"시간    "+(week.Holiday>0?"주휴 "+N(week.Holiday)+"시간":week.Status),60,y,735,35,20);y+=38;}
     Text(g,"근무일 내역",850,netY+86,770,40,24,true);var days=r.Days.OrderBy(x=>x.Key).ToList();for(int i=0;i<days.Count;i++){var day=days[i];var date=DateTime.ParseExact(day.Key,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);int x=850+(i/16)*385,dy=netY+130+(i%16)*21;Text(g,date.ToString("MM.dd")+"  "+(day.Value==1?"근무":day.Value==2?"결근":"유급")+"  "+N(day.Value==2?0:Engine.DayHours(r,j,date))+"시간",x,dy,365,23,18);}
     Text(g,"통상시급 "+N(z.OrdinaryHourly)+"원 / 적용시급 "+N(z.Hourly)+"원 / 월 임금산정 "+N(z.MonthlyHours)+"시간",60,1060,width-120,35,20);
    }else{int y=netY+94;Text(g,"지급 산출 내역",60,y,width-120,42,25,true);y+=48;foreach(var item in pay){Text(g,item.Name,60,y,330,32,22,true);Text(g,item.Formula,60,y+31,width-120,40,20);y+=88;}Text(g,"지급예정일  "+(o.PaymentDate.HasValue?o.PaymentDate.Value.ToString("yyyy.MM.dd"):"미정"),60,1400,width-120,40,22);Text(g,"임금명세서를 교부받았습니다.     성명                         (인)",60,1470,width-120,45,22);}
    Text(g,r.Employment?"고용보험: 지급액 × "+N(r.Rate)+"% (10원 미만 절사)":"고용보험 적용 제외 사유: "+r.EmploymentExclusionReason,60,height-105,width-120,36,19);
    Text(g,"본 프로그램은 개인 제작 프로그램입니다. 사용 전 현행 지침과 규정을 확인하시기 바랍니다",60,height-60,width-160,32,17);Text(g,"1 / 1",width-105,height-60,55,32,17,false,true);
   }
   using(var memory=new MemoryStream()){var encoder=ImageCodecInfo.GetImageEncoders().First(x=>x.MimeType=="image/jpeg");using(var parameters=new EncoderParameters(1)){parameters.Param[0]=new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,95L);bitmap.Save(memory,encoder,parameters);}WritePdf(path,memory.ToArray(),bitmap.Width,bitmap.Height,wage?842:595,wage?595:842);}
  }
 }
 static void WritePdf(string path,byte[] jpeg,int pixelsWide,int pixelsHigh,int pageWidth,int pageHeight){using(var memory=new MemoryStream()){var offsets=new List<long>{0};Action<string> write=t=>{byte[] b=Encoding.ASCII.GetBytes(t);memory.Write(b,0,b.Length);};Action<int,string> obj=(n,t)=>{offsets.Add(memory.Position);write(n+" 0 obj\n"+t+"\nendobj\n");};write("%PDF-1.4\n");obj(1,"<< /Type /Catalog /Pages 2 0 R >>");obj(2,"<< /Type /Pages /Kids [3 0 R] /Count 1 >>");obj(3,"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 "+pageWidth+" "+pageHeight+"] /Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>");offsets.Add(memory.Position);write("4 0 obj\n<< /Type /XObject /Subtype /Image /Width "+pixelsWide+" /Height "+pixelsHigh+" /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length "+jpeg.Length+" >>\nstream\n");memory.Write(jpeg,0,jpeg.Length);write("\nendstream\nendobj\n");string content="q "+pageWidth+" 0 0 "+pageHeight+" 0 0 cm /Im0 Do Q\n";obj(5,"<< /Length "+Encoding.ASCII.GetByteCount(content)+" >>\nstream\n"+content+"endstream");long xref=memory.Position;write("xref\n0 6\n0000000000 65535 f \n");for(int i=1;i<6;i++)write(offsets[i].ToString("D10")+" 00000 n \n");write("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n"+xref+"\n%%EOF");File.WriteAllBytes(path,memory.ToArray());}}
}
}
