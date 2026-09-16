using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using OfficeOpenXml;
namespace ShortPay {
// Evaluates only the ordinary scalar formulas used in the supplied templates.
// Guard-specific working-hour formulas are supplied by ExcelReports before evaluation.
internal class TemplateFormula {
 ExcelWorksheet sheet;Dictionary<string,object> cache=new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);HashSet<string> visiting=new HashSet<string>();
 public TemplateFormula(ExcelWorksheet ws){sheet=ws;}
 public decimal NumberAt(string address){return Number(ValueAt(address));}
 object ValueAt(string address){address=address.Replace("$","").ToUpperInvariant();object value;if(cache.TryGetValue(address,out value))return value;if(!visiting.Add(address))throw new InvalidOperationException("순환 수식: "+address);try{string formula=sheet.Cells[address].Formula;value=String.IsNullOrEmpty(formula)?sheet.Cells[address].Value:new Parser(formula,this).Parse()();cache[address]=value;return value;}finally{visiting.Remove(address);}}
 static decimal Number(object value){if(value==null)return 0;if(value is bool)return (bool)value?1:0;decimal n;if(Decimal.TryParse(Convert.ToString(value,CultureInfo.InvariantCulture),NumberStyles.Any,CultureInfo.InvariantCulture,out n))return n;throw new InvalidOperationException("숫자가 아닌 수식 값: "+value);}
 static bool Bool(object value){return value is bool?(bool)value:Number(value)!=0;}
 static int Compare(object a,object b){decimal x,y;if(a is string||b is string)return String.Compare(Convert.ToString(a),Convert.ToString(b),StringComparison.OrdinalIgnoreCase);x=Number(a);y=Number(b);return x.CompareTo(y);}
 object Function(string name,List<Func<object>> a){name=name.ToUpperInvariant();if(name=="IF")return Bool(a[0]())?a[1]():a.Count>2?a[2]():false;if(name=="IFERROR"){try{return a[0]();}catch{return a[1]();}}if(name=="AND")return a.All(x=>Bool(x()));if(name=="OR")return a.Any(x=>Bool(x()));if(name=="SUM"||name=="SUBTOTAL"){int from=name=="SUBTOTAL"?1:0;if(from==1&&Number(a[0]())!=9)throw new InvalidOperationException("지원하지 않는 SUBTOTAL 코드");decimal sum=0;foreach(var f in a.Skip(from)){var v=f();var range=v as object[];if(range!=null){foreach(var item in range)if(!(item is string)&&item!=null)sum+=Number(item);}else sum+=Number(v);}return sum;}
  if(name=="ROUND"||name=="ROUNDDOWN"||name=="ROUNDUP"){decimal n=Number(a[0]());int places=(int)Number(a[1]());decimal scale=(decimal)Math.Pow(10,places),scaled=n*scale;return (name=="ROUND"?Math.Round(scaled,0,MidpointRounding.AwayFromZero):name=="ROUNDDOWN"?Math.Truncate(scaled):Math.Sign(scaled)*Math.Ceiling(Math.Abs(scaled)))/scale;}
  throw new InvalidOperationException("지원하지 않는 양식 수식 함수: "+name);
 }
 class Parser {
  string text;int pos;TemplateFormula owner;public Parser(string s,TemplateFormula o){text=s;owner=o;}
  void Space(){while(pos<text.Length&&Char.IsWhiteSpace(text[pos]))pos++;}
  bool Eat(string s){Space();if(pos+s.Length<=text.Length&&text.Substring(pos,s.Length)==s){pos+=s.Length;return true;}return false;}
  public Func<object> Parse(){Eat("=");var n=Comparison();Space();if(pos!=text.Length)throw new InvalidOperationException("수식 구문 확인: "+text.Substring(pos));return n;}
  Func<object> Comparison(){var left=Add();foreach(string op in new[]{"<=",">=","<>","=","<",">"})if(Eat(op)){var l=left;var r=Add();return ()=>{int c=Compare(l(),r());return op=="="?c==0:op=="<>"?c!=0:op=="<"?c<0:op==">"?c>0:op=="<="?c<=0:c>=0;};}return left;}
  Func<object> Add(){var n=Multiply();while(true){if(Eat("+")){var l=n;var r=Multiply();n=()=>Number(l())+Number(r());}else if(Eat("-")){var l=n;var r=Multiply();n=()=>Number(l())-Number(r());}else return n;}}
  Func<object> Multiply(){var n=Primary();while(true){if(Eat("*")){var l=n;var r=Primary();n=()=>Number(l())*Number(r());}else if(Eat("/")){var l=n;var r=Primary();n=()=>Number(l())/Number(r());}else return n;}}
  Func<object> Primary(){Space();if(Eat("-")){var n=Primary();return ()=>-Number(n());}if(Eat("+"))return Primary();if(Eat("(")){var n=Comparison();if(!Eat(")"))throw new FormatException(text);return n;}if(Eat("\"")){string str="";while(pos<text.Length){char c=text[pos++];if(c=='\"'){if(pos<text.Length&&text[pos]=='\"'){str+='\"';pos++;}else break;}else str+=c;}return ()=>str;}
   int start=pos;if(pos<text.Length&&(Char.IsDigit(text[pos])||text[pos]=='.')){while(pos<text.Length&&(Char.IsDigit(text[pos])||text[pos]=='.'))pos++;decimal value=Decimal.Parse(text.Substring(start,pos-start),CultureInfo.InvariantCulture);return ()=>value;}
   while(pos<text.Length&&(Char.IsLetterOrDigit(text[pos])||text[pos]=='$'||text[pos]=='_'))pos++;if(pos==start)throw new FormatException(text);string token=text.Substring(start,pos-start);
   if(Eat("(")){var args=new List<Func<object>>();if(!Eat(")")){do{args.Add(Comparison());}while(Eat(","));if(!Eat(")"))throw new FormatException(text);}return ()=>owner.Function(token,args);}
   if(token.ToUpperInvariant()=="TRUE")return ()=>true;if(token.ToUpperInvariant()=="FALSE")return ()=>false;
   if(Eat(":")){Space();int last=pos;while(pos<text.Length&&(Char.IsLetterOrDigit(text[pos])||text[pos]=='$'))pos++;string end=text.Substring(last,pos-last);return ()=>owner.sheet.Cells[token+":"+end].Select(c=>owner.ValueAt(c.Address)).ToArray();}
   return ()=>owner.ValueAt(token);
  }
 }
}
}
