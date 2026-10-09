using System;using System.IO;using System.Net;using System.Text;using System.Security.Cryptography;using System.Threading.Tasks;using System.Collections.Generic;using System.Web.Script.Serialization;
namespace FssLauncher {
 public sealed class AccountProfile {public string userId,displayName,email;}
 public sealed class AccountClient {
  readonly Uri origin;readonly CookieContainer cookies=new CookieContainer();
  public AccountClient(string url){origin=new Uri(CoreStudio.LauncherMatchSettings.NormalizeBackend(url));}
  public string Token {get{var cookie=cookies.GetCookies(origin)["backend_session"];return cookie==null?null:cookie.Value;}}
  public async Task<AccountProfile> Request(string action,object body){var data=await RequestData(action,body);object user;return data.TryGetValue("user",out user)?new JavaScriptSerializer().ConvertToType<AccountProfile>(user):null;}
  public void Restore(string token){if(!System.Text.RegularExpressions.Regex.IsMatch(token??"","^[a-f0-9]{64}$"))throw new ArgumentException("Invalid saved session.");cookies.Add(origin,new Cookie("backend_session",token,"/"));}
  public async Task<System.Collections.Generic.Dictionary<string,object>> RequestData(string action,object body){return await Task.Run(delegate{
   var request=(HttpWebRequest)WebRequest.Create(new Uri(origin,"/api/web/"+action));request.Proxy=null;request.AllowAutoRedirect=false;request.CookieContainer=cookies;request.Timeout=6000;request.ReadWriteTimeout=6000;
   if(body!=null){request.Method="POST";request.ContentType="application/json";request.Headers["Origin"]=origin.GetLeftPart(UriPartial.Authority);byte[] data=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(body));request.ContentLength=data.Length;using(var stream=request.GetRequestStream())stream.Write(data,0,data.Length);Array.Clear(data,0,data.Length);}
   try{using(var response=request.GetResponse())using(var reader=new StreamReader(response.GetResponseStream())){string text=reader.ReadToEnd();if(String.IsNullOrWhiteSpace(text))return new Dictionary<string,object>{{"success",true}};var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(text);return data;}}
   catch(WebException error){if(error.Response!=null){using(var response=error.Response)using(var reader=new StreamReader(response.GetResponseStream())){string message="Account request failed.";try{var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(reader.ReadToEnd());object value;if(data.TryGetValue("error",out value))message=Convert.ToString(value);}catch(ArgumentException){}throw new AccountFailure(message,((HttpWebResponse)response).StatusCode==HttpStatusCode.Unauthorized);}}throw new AccountFailure("Cannot connect to the selected backend. Check its address and that your friend has started their services.",false);}
  });}
 }
 public sealed class AccountFailure:Exception {public bool Expired;public AccountFailure(string message,bool expired):base(message){Expired=expired;}}
 public sealed class SessionVault {
  readonly string file;readonly byte[] binding;
  public SessionVault(string root,string origin){string scope=Path.GetFullPath(root).TrimEnd('\\').ToLowerInvariant()+"|"+origin;binding=Encoding.UTF8.GetBytes(scope);using(var hash=SHA256.Create())file=Path.Combine(root,"Launcher","sessions",BitConverter.ToString(hash.ComputeHash(binding)).Replace("-","")+".bin");}
  public string FilePath{get{return file;}}
  public void Save(string token){byte[] plain=Encoding.UTF8.GetBytes(token);try{byte[] data=ProtectedData.Protect(plain,binding,DataProtectionScope.CurrentUser);Directory.CreateDirectory(Path.GetDirectoryName(file));string temp=file+".tmp";File.WriteAllBytes(temp,data);if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);}catch(CryptographicException){throw new AccountFailure("Windows could not securely save this session. Try signing in with Remember Me unchecked.",false);}finally{Array.Clear(plain,0,plain.Length);}}
  public string Load(){try{if(!File.Exists(file))return null;byte[] plain=ProtectedData.Unprotect(File.ReadAllBytes(file),binding,DataProtectionScope.CurrentUser);try{return Encoding.UTF8.GetString(plain);}finally{Array.Clear(plain,0,plain.Length);}}catch(CryptographicException){Clear();return null;}catch(IOException){return null;}}
  public void Clear(){if(File.Exists(file))File.Delete(file);}
 }
}
