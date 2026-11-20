using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;
namespace SchoolWorkdayCalculator {
internal static class Pastel {
 internal static readonly Color Mint=Color.FromArgb(222,247,240),Lavender=Color.FromArgb(241,235,255),Peach=Color.FromArgb(255,243,228),Pink=Color.FromArgb(254,245,249),Teal=Color.FromArgb(24,113,102);
 internal static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 internal static void Icon(Graphics g,int kind,Rectangle r,Color color){var state=g.Save();g.TranslateTransform(r.X,r.Y);g.ScaleTransform(r.Width/24f,r.Height/24f);using(var p=new Pen(color,1.7f)){p.StartCap=p.EndCap=LineCap.Round;if(kind==1){g.DrawEllipse(p,8,2,8,8);g.DrawArc(p,4,12,16,15,180,180);}else if(kind==2){g.DrawLines(p,new[]{new Point(2,20),new Point(2,6),new Point(9,6),new Point(11,9),new Point(22,9),new Point(22,20),new Point(2,20)});}else if(kind==3){g.DrawRectangle(p,5,2,14,20);g.DrawLine(p,9,7,15,7);g.DrawLine(p,9,11,15,11);g.DrawLine(p,9,15,15,15);}else if(kind==4){g.DrawEllipse(p,3,3,18,18);g.DrawLine(p,7,12,17,12);g.DrawLine(p,12,7,12,17);}else if(kind==5){g.DrawRectangle(p,6,7,12,15);g.DrawLine(p,4,5,20,5);g.DrawLine(p,10,2,14,2);g.DrawLine(p,10,10,10,18);g.DrawLine(p,14,10,14,18);}else if(kind==7){g.DrawLines(p,new[]{new Point(4,2),new Point(15,2),new Point(20,7),new Point(20,22),new Point(4,22),new Point(4,2)});g.DrawLines(p,new[]{new Point(15,2),new Point(15,7),new Point(20,7)});g.DrawRectangle(p,7,10,10,9);g.DrawLine(p,7,13,17,13);g.DrawLine(p,7,16,17,16);g.DrawLine(p,12,10,12,19);}else if(kind==8){g.DrawRectangle(p,3,4,18,18);g.DrawLine(p,3,10,21,10);g.DrawLine(p,7,1,7,7);g.DrawLine(p,17,1,17,7);using(var cross=new Pen(Color.FromArgb(206,67,80),2f)){cross.StartCap=cross.EndCap=LineCap.Round;g.DrawLine(cross,8,13,16,19);g.DrawLine(cross,16,13,8,19);}}else if(kind==6){g.DrawRectangle(p,3,4,18,18);g.DrawLine(p,3,10,21,10);g.DrawLine(p,7,1,7,7);g.DrawLine(p,17,1,17,7);g.DrawLines(p,new[]{new Point(7,15),new Point(10,18),new Point(17,12)});}else{g.DrawRectangle(p,3,4,18,18);g.DrawLine(p,3,10,21,10);g.DrawLine(p,7,1,7,7);g.DrawLine(p,17,1,17,7);}}g.Restore(state);}
}
internal sealed class PastelButton:Button {
 internal bool Primary,Navigation,Active;bool hot,down;internal int Glyph=-1,Number;internal Color Tint=Color.White;
 internal PastelButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;UseVisualStyleBackColor=false;Cursor=Cursors.Hand;}
 protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hot=true;Invalidate();}
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hot=false;down=false;Invalidate();}
 protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);down=true;Invalidate();}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);down=false;Invalidate();}
 protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Invalidate();}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent==null?FlatDesign.Background:Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;bool lit=hot&&Enabled,press=down&&Enabled;Color fill=!Enabled?Ui.Disabled:Navigation?Tint:Primary?(press?Color.FromArgb(160,216,200):lit?Color.FromArgb(174,225,211):Color.FromArgb(189,233,221)):(press?Color.FromArgb(232,236,244):lit?Color.FromArgb(246,248,252):Color.White),ink=Enabled?FlatDesign.Ink:FlatDesign.Muted;if(!Navigation&&Enabled&&!press)using(var shadow=Pastel.Round(new RectangleF(1,2,Width-3,Height-3),10))using(var shadowPen=new Pen(Color.FromArgb(226,230,239)))g.DrawPath(shadowPen,shadow);using(var path=Pastel.Round(new RectangleF(1,press?2:1,Width-3,Height-3),Navigation?14:10))using(var brush=new SolidBrush(fill))using(var pen=new Pen(Navigation&&Active?Color.FromArgb(124,202,180):lit&&!Primary?Color.FromArgb(150,205,190):Primary&&Enabled?Color.FromArgb(150,208,192):FlatDesign.Edge,Navigation&&Active?1.8f:1)){g.FillPath(brush,path);g.DrawPath(pen,path);}if(Navigation){Pastel.Icon(g,Glyph,new Rectangle(20,(Height-32)/2,32,32),Number==1?Pastel.Teal:Color.FromArgb(139,111,213));using(var small=new Font("맑은 고딕",10))TextRenderer.DrawText(g,Number.ToString("00"),small,new Rectangle(76,6,Width-94,18),FlatDesign.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);using(var title=new Font("맑은 고딕",14,FontStyle.Bold))TextRenderer.DrawText(g,Text,title,new Rectangle(76,26,Width-94,Height-30),ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);}else{if(Glyph==7){int textWidth=TextRenderer.MeasureText(g,Text,Font,new Size(Width,Height),TextFormatFlags.SingleLine|TextFormatFlags.NoPadding).Width;int left=(Width-textWidth-31)/2;Pastel.Icon(g,Glyph,new Rectangle(left,(Height-23)/2,23,23),Enabled?Pastel.Teal:Ui.Faint);TextRenderer.DrawText(g,Text,Font,new Rectangle(left+31,0,textWidth,Height),ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);}else{if(Glyph>=0)Pastel.Icon(g,Glyph,new Rectangle(14,(Height-23)/2,23,23),Enabled?Pastel.Teal:Ui.Faint);TextRenderer.DrawText(g,Text,Font,Glyph>=0?new Rectangle(42,0,Width-48,Height):ClientRectangle,ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}}if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(5,5,Width-11,Height-11));}
}
internal sealed class PastelGlyph:Control {internal int Kind;internal Color Ink=Pastel.Teal;protected override void OnPaint(PaintEventArgs e){Pastel.Icon(e.Graphics,Kind,ClientRectangle,Ink);}}
}

namespace SchoolWorkdayCalculator {
internal sealed class InputFrame:Panel {
 readonly Control input;
 internal InputFrame(Control value){input=value;DoubleBuffered=true;ResizeRedraw=true;BackColor=Color.White;if(value is TextBox){((TextBox)value).BorderStyle=BorderStyle.None;((TextBox)value).AutoSize=false;}if(value is NumericUpDown)((NumericUpDown)value).BorderStyle=BorderStyle.None;if(value is ListBox)((ListBox)value).BorderStyle=BorderStyle.None;Controls.Add(value);value.Enter+=delegate{Invalidate();};value.Leave+=delegate{Invalidate();};}
 protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(input==null)return;int inset=9;int h=input is TextBox?Math.Min(Height-8,input.PreferredSize.Height):Height-8;input.SetBounds(inset,Math.Max(2,(Height-h)/2),Math.Max(10,Width-inset*2),h);}
 protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?FlatDesign.Background:Parent.BackColor);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var p=Pastel.Round(new RectangleF(1,1,Width-3,Height-3),8))using(var b=new SolidBrush(Color.White))using(var pen=new Pen(input!=null&&input.ContainsFocus?Ui.Teal:Ui.InputEdge,input!=null&&input.ContainsFocus?1.6F:1F)){e.Graphics.FillPath(b,p);e.Graphics.DrawPath(pen,p);}}
}
internal sealed class HintTextBox:TextBox {
 [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wParam,string value);
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);SendMessage(Handle,0x1501,new IntPtr(1),"메모");}
}
internal sealed class PastelListBox:ListBox {
 internal PastelListBox(){DrawMode=DrawMode.OwnerDrawFixed;}
 protected override void OnDrawItem(DrawItemEventArgs e){
  if(e.Index<0||e.Index>=Items.Count)return;
  var g=e.Graphics;using(var back=new SolidBrush(Color.White))g.FillRectangle(back,e.Bounds);
  bool selected=(e.State&DrawItemState.Selected)!=0;
  var r=new RectangleF(e.Bounds.X+4,e.Bounds.Y+2,e.Bounds.Width-8,e.Bounds.Height-4);
  if(selected){g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Pastel.Round(r,7F))using(var fill=new SolidBrush(Ui.Mint))g.FillPath(fill,path);}
  TextRenderer.DrawText(g,Items[e.Index].ToString(),Font,new Rectangle((int)r.X+8,(int)r.Y,(int)r.Width-10,(int)r.Height),selected?Ui.Teal:FlatDesign.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
 }
 protected override void WndProc(ref Message m){base.WndProc(ref m);if((m.Msg==15||m.Msg==0x317||m.Msg==0x318)&&Items.Count==0){using(var g=m.Msg==15?Graphics.FromHwnd(Handle):Graphics.FromHdc(m.WParam))using(var font=new Font("맑은 고딕",11))TextRenderer.DrawText(g,"등록된 휴일이 없습니다.",font,ClientRectangle,FlatDesign.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}}
}
internal sealed partial class MainForm {
 void PlaceInput(Control input,Control parent,int x,int y,int width,int height){if(input is DateBox||input is StepBox){if(input.Parent!=parent)parent.Controls.Add(input);input.SetBounds(x,y,width,height);return;}var frame=input.Parent as InputFrame;if(frame==null){frame=new InputFrame(input);parent.Controls.Add(frame);}frame.SetBounds(x,y,width,height);frame.PerformLayout();}
}
}
