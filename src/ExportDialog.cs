using System;
using System.Drawing;
using System.Windows.Forms;
namespace ShortPay {
class ExportDialog:Form {
 TextBox recipient,manager;NumericUpDown year,month,round;DateTimePicker payment;CheckBox extra;
 public ExportOptions Options{get{return new ExportOptions{Year=(int)year.Value,Month=(int)month.Value,Round=(int)round.Value,Recipient=recipient.Text.Trim(),Manager=manager.Text.Trim(),PaymentDate=payment.Checked?(DateTime?)payment.Value.Date:null,ExtraIsAdjustment=extra.Checked};}}
 public ExportDialog(string kind,Record r){Text="엑셀 출력 정보";ClientSize=new Size(540,330);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;Font=new Font("맑은 고딕",10);BackColor=Palette.Background;
  var title=new Label{Text=kind+" · "+r.Name,Font=new Font(Font,FontStyle.Bold),AutoSize=true,Location=new Point(20,20)};Controls.Add(title);
  year=Number(r.End.Year,2020,2050,20,80,110);month=Number(r.End.Month,1,12,145,80,95);round=Number(1,1,99,255,80,85);
  Label("귀속 연도",20,53);Label("월",145,53);Label("신청 차수",255,53);
  recipient=Input(20,150,160);manager=Input(200,150,300);manager.Text=r.Contact;Label("수신자 기호",20,123);Label("담당자명 (연락처 제외)",200,123);
  payment=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM-dd",ShowCheckBox=true,Checked=false,Location=new Point(20,212),Width=205};Controls.Add(payment);Label("지급(예정)일 · 미정이면 체크 해제",20,185);
  extra=new CheckBox{Text="기타 지급액은 소급·환수액입니다",AutoSize=true,Location=new Point(20,249),Visible=kind=="인건비 신청내역"&&r.ExtraPay!=0};Controls.Add(extra);
  var ok=new SoftButton{Active=true,Text="엑셀 저장",DialogResult=DialogResult.OK,Location=new Point(318,282),Width=105};var cancel=new SoftButton{Text="취소",DialogResult=DialogResult.Cancel,Location=new Point(433,282),Width=85};Controls.Add(ok);Controls.Add(cancel);AcceptButton=ok;CancelButton=cancel;
  bool request=kind=="인건비 신청내역";recipient.Enabled=request;manager.Enabled=request;round.Enabled=request;payment.Enabled=!request;
 }
 void Label(string text,int x,int y){Controls.Add(new Label{Text=text,Location=new Point(x,y),AutoSize=true});}
 TextBox Input(int x,int y,int w){var t=new TextBox{Location=new Point(x,y),Width=w};Controls.Add(t);return t;}
 NumericUpDown Number(int value,int min,int max,int x,int y,int w){var n=new NumericUpDown{Minimum=min,Maximum=max,Value=value,Location=new Point(x,y),Width=w};Controls.Add(n);return n;}
}
}
