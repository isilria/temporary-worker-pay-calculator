using System;using System.Drawing;using System.Windows.Forms;
namespace ShortPay {
class DocumentMenuIcon:Control {
 public int Kind;
 public DocumentMenuIcon(){SetStyle(ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;}
 protected override void OnPaint(PaintEventArgs e){MenuGlyph.Draw(e.Graphics,Kind,ClientRectangle,Palette.Accent);}
}
class DocumentPreview:Control {
 public string Heading="";public int Kind;static readonly Image[] pages=new Image[3];
 public DocumentPreview(){DoubleBuffered=true;ResizeRedraw=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.PushButton;AccessibleName="실제 출력 양식 예시 크게 보기";}
 internal Image PageImage {get{int index=Math.Max(0,Math.Min(2,Kind));if(pages[index]==null){using(var stream=new System.IO.MemoryStream(ExcelReports.Resource(new[]{"wage","payslip","request"}[index]+"-preview.png")))using(var source=Image.FromStream(stream))pages[index]=new Bitmap(source);}return pages[index];}}
 internal Form CreateViewer(){var form=new Form{Text=Heading+" · 출력 양식 예시 (가상 자료)",StartPosition=FormStartPosition.CenterParent,Size=new Size(1100,800),MinimumSize=new Size(650,500),BackColor=Color.FromArgb(241,244,247),ShowInTaskbar=false};var note=new Label{Text="실제 출력 양식을 축소한 예시입니다. 저장할 때는 현재 입력한 내용이 적용됩니다.",Dock=DockStyle.Bottom,Height=36,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("맑은 고딕",10),ForeColor=Palette.Muted};var page=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,Image=PageImage,Padding=new Padding(16)};form.Controls.Add(page);form.Controls.Add(note);return form;}
 protected override void OnClick(EventArgs e){base.OnClick(e);using(var viewer=CreateViewer())viewer.ShowDialog(FindForm());}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent.BackColor);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var path=Palette.Round(new RectangleF(1,1,Width-3,Height-3),9))using(var fill=new SolidBrush(Color.White))using(var pen=new Pen(Palette.Edge)){g.FillPath(fill,path);g.DrawPath(pen,path);}var image=PageImage;float scale=Math.Min((Width-16f)/image.Width,Math.Max(8,Height-31f)/image.Height);float w=image.Width*scale,h=image.Height*scale;g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(image,(Width-w)/2,7+(Math.Max(8,Height-31f)-h)/2,w,h);using(var f=new Font("맑은 고딕",8))TextRenderer.DrawText(g,"출력 양식 예시 · 클릭하여 확대",f,new Rectangle(4,Height-24,Width-8,21),Palette.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
}
}
