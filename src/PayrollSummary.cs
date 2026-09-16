using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace ShortPay {
class PayrollSummary:Label {
 Record record;Result result;
 internal int TotalTop{get{return Height-42;}}
 public PayrollSummary(){AutoSize=false;BackColor=Color.White;Font=new Font("맑은 고딕",10);SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);}
 public void UpdateResult(Record r,Result z){record=r;result=z;Invalidate();}
 void Box(Graphics g,Rectangle r,Color fill,Color line){using(var p=Palette.Round(r,10))using(var b=new SolidBrush(fill))using(var pen=new Pen(line,1)){g.FillPath(b,p);g.DrawPath(pen,p);}}
 void Line(Graphics g,string caption,decimal amount,Rectangle bounds,bool bold){using(var f=new Font(Font.FontFamily,caption.Length>8?9:10,bold?FontStyle.Bold:FontStyle.Regular)){var label=new Rectangle(bounds.X+12,bounds.Y,bounds.Width-118,bounds.Height);var value=new Rectangle(bounds.Right-100,bounds.Y,88,bounds.Height);TextRenderer.DrawText(g,caption,f,label,Palette.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);TextRenderer.DrawText(g,amount.ToString("N0")+"원",f,value,Palette.Ink,TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);}}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Palette.Surface(Parent));g.SmoothingMode=SmoothingMode.AntiAlias;if(result==null)return;int gap=12,w=(Width-gap*2)/3;var pay=new Rectangle(1,1,w-2,Height-3);var ded=new Rectangle(w+gap,1,w-2,Height-3);var net=new Rectangle((w+gap)*2,1,Width-(w+gap)*2-2,Height-3);
  Box(g,pay,Color.FromArgb(235,244,255),Color.FromArgb(196,216,239));Box(g,ded,Color.FromArgb(255,237,244),Color.FromArgb(232,201,213));Box(g,net,Color.FromArgb(255,248,218),Color.FromArgb(230,216,165));
  if(Engine.ContractWarning(record)!=""){TextRenderer.DrawText(g,"계약기간 확인 필요 · 계산 중지",Font,ClientRectangle,Palette.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
  var payItems=PayrollItems.Pay(record,result);var dedItems=PayrollItems.Deduct(record,result);int rows=Math.Max(1,Math.Max(payItems.Count,dedItems.Count)),rowHeight=Math.Min(30,(TotalTop-10)/rows);
  for(int i=0;i<payItems.Count;i++)Line(g,payItems[i].Name,payItems[i].Amount,new Rectangle(pay.X,6+i*rowHeight,pay.Width,rowHeight),false);
  for(int i=0;i<dedItems.Count;i++)Line(g,dedItems[i].Name,dedItems[i].Amount,new Rectangle(ded.X,6+i*rowHeight,ded.Width,rowHeight),false);
  var gross=new Rectangle(pay.X+5,TotalTop,pay.Width-10,Height-TotalTop-6);var total=new Rectangle(ded.X+5,TotalTop,ded.Width-10,Height-TotalTop-6);
  Box(g,gross,Color.FromArgb(255,243,190),Color.FromArgb(226,207,146));Box(g,total,Color.FromArgb(220,242,224),Color.FromArgb(178,209,185));Line(g,"임금 계",result.Gross,gross,true);Line(g,"공제 계",result.Deductions,total,true);
  TextRenderer.DrawText(g,"실지급액",Font,new Rectangle(net.X,Height/2-38,net.Width,28),Palette.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
  using(var f=new Font("맑은 고딕",19,FontStyle.Bold))TextRenderer.DrawText(g,result.Net.ToString("N0")+"원",f,new Rectangle(net.X+8,Height/2-5,net.Width-16,45),Palette.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
 }
}
}
