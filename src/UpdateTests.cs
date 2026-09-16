using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
namespace ShortPay {
static class UpdateTests {
 static int count;
 static void Check(bool ok,string name){if(!ok)throw new Exception("Update test failed: "+name);count++;}
 static void Reject(string json,string name){bool failed=false;try{PayrollUpdates.Parse(json);}catch{failed=true;}Check(failed,name);}
 internal static void Run(string folder){Directory.CreateDirectory(folder);try{count=0;
  Check(PayrollUpdates.IsNewer(new Version(1,1),new Version(1,0,0,0)),"new version");
  Check(!PayrollUpdates.IsNewer(new Version(1,0),new Version(1,0,0,0)),"equal normalized version");
  Check(!PayrollUpdates.IsNewer(new Version(0,9,9,9),new Version(1,0)),"no downgrade");
  Check(PayrollUpdates.IsNewer(new Version(1,0,1),new Version(1,0)),"patch update");
  var legacy=PayrollUpdates.Parse("{\"appId\":\"shortpay\",\"version\":\"1.0\",\"url\":\"https://github.com/isilria/temporary-worker-pay-calculator/releases\"}");Check(legacy.Version==new Version(1,0,0,0),"legacy manifest");
  const string prefix="{\"appId\":\"shortpay\",\"version\":\"1.1.0.0\",\"url\":\"https://github.com/isilria/temporary-worker-pay-calculator/releases/tag/v1.1.0\",";
  string asset="https://github.com/isilria/temporary-worker-pay-calculator/releases/download/v1.1.0/ShortTermPayroll-1.1.exe";
  string good=prefix+"\"downloadUrl\":\""+asset+"\",\"sha256\":\""+new string('a',64)+"\"}";
  Check(PayrollUpdates.Parse(good).DownloadUrl==asset,"valid release asset");
  Reject(good.Replace("shortpay","another-app"),"wrong application");
  Reject(good.Replace("1.1.0.0","banana"),"bad version");
  Reject(good.Replace("https://","http://"),"insecure transport");
  Reject(good.Replace("/releases/download/","/blob/"),"not a release asset");
  Reject(good.Replace("github.com","github.com.evil.example"),"untrusted host");
  Reject(good.Replace("temporary-worker-pay-calculator/releases/download","other-project/releases/download"),"wrong repository");
  Reject(good.Replace(new string('a',64),"abc"),"bad checksum");
  Reject(prefix+"\"downloadUrl\":\""+asset+"\"}","missing checksum");
  string f=Path.Combine(folder,"checksum-fixture.bin");File.WriteAllText(f,"update-check");string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(f))).Replace("-","");PayrollUpdates.Verify(f,hash.ToLowerInvariant());Check(true,"valid checksum");File.AppendAllText(f,"tampered");bool rejected=false;try{PayrollUpdates.Verify(f,hash);}catch(InvalidDataException){rejected=true;}Check(rejected,"corrupt download rejected");
  File.WriteAllText(Path.Combine(folder,"update-tests.txt"),"PASS "+count+" update checks");
 }catch(Exception ex){File.WriteAllText(Path.Combine(folder,"update-tests.txt"),ex.ToString());Environment.ExitCode=1;}}
 internal static void Live(string folder){Directory.CreateDirectory(folder);try{var info=PayrollUpdates.Fetch(PayrollUpdates.ManifestUrl);if(info.Version!=PayrollUpdates.Normalize(Assembly.GetExecutingAssembly().GetName().Version))throw new Exception("Published version mismatch");string file=PayrollUpdates.Download(info);PayrollUpdates.Verify(file,info.Sha256);File.WriteAllText(Path.Combine(folder,"update-live-test.txt"),"PASS published manifest, release download, SHA256, Windows executable header\nVersion "+info.Version+"\nFile "+file);}catch(Exception ex){File.WriteAllText(Path.Combine(folder,"update-live-test.txt"),ex.ToString());Environment.ExitCode=1;}}
}
}
