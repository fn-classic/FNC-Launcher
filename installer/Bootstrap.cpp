#define UNICODE
#define _UNICODE
#include <windows.h>
#include <shellapi.h>
#include <shlobj.h>
#include <bcrypt.h>
#include <string>
#include <vector>
#include "PayloadHashes.h"
#pragma comment(lib,"shell32.lib")
#pragma comment(lib,"advapi32.lib")
#pragma comment(lib,"ole32.lib")
#pragma comment(lib,"bcrypt.lib")
static std::wstring Failure;
static bool Extract(int id,const std::wstring& file,const char* expected){
 auto resource=FindResourceW(nullptr,MAKEINTRESOURCEW(id),RT_RCDATA);
 if(!resource){Failure=L"Setup payload is missing. Download the complete installer again.";return false;}
 auto size=SizeofResource(nullptr,resource);auto bytes=(const unsigned char*)LockResource(LoadResource(nullptr,resource));
 BCRYPT_ALG_HANDLE alg=nullptr;BCRYPT_HASH_HANDLE hash=nullptr;unsigned char digest[32];
 if(BCryptOpenAlgorithmProvider(&alg,BCRYPT_SHA256_ALGORITHM,nullptr,0)<0||BCryptCreateHash(alg,&hash,nullptr,0,nullptr,0,0)<0){Failure=L"Cannot verify setup payload.";return false;}
 auto handle=CreateFileW(file.c_str(),GENERIC_WRITE,0,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
 bool ok=handle!=INVALID_HANDLE_VALUE;
 for(DWORD offset=0;ok&&offset<size;){DWORD count=(size-offset>1048576)?1048576:size-offset,written=0;
  ok=BCryptHashData(hash,(PUCHAR)bytes+offset,count,0)>=0&&WriteFile(handle,bytes+offset,count,&written,nullptr)&&written==count;offset+=count;}
 if(handle!=INVALID_HANDLE_VALUE)CloseHandle(handle);
 ok=ok&&BCryptFinishHash(hash,digest,32,0)>=0;BCryptDestroyHash(hash);BCryptCloseAlgorithmProvider(alg,0);
 const char* hex="0123456789abcdef";
 for(int i=0;ok&&i<32;++i)ok=hex[digest[i]>>4]==expected[i*2]&&hex[digest[i]&15]==expected[i*2+1];
 if(!ok){DeleteFileW(file.c_str());Failure=L"Setup file verification failed or there is insufficient disk space. Download the complete installer again.";}
 return ok;
}
static DWORD Value(const std::wstring& key,const wchar_t* name,DWORD view=0){DWORD value=0,size=4;RegGetValueW(HKEY_LOCAL_MACHINE,key.c_str(),name,RRF_RT_REG_DWORD|view,nullptr,&value,&size);return value;}
static bool HasRuntime(const wchar_t* arch){
 std::wstring key=L"SOFTWARE\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\";key+=arch;
 for(DWORD view:{RRF_SUBKEY_WOW6432KEY,RRF_SUBKEY_WOW6464KEY}){
  if(Value(key,L"Installed",view)!=1)continue;
  auto major=Value(key,L"Major",view),minor=Value(key,L"Minor",view),build=Value(key,L"Bld",view);
  if(major>14||(major==14&&(minor>51||(minor==51&&build>=36231))))return true;
 }return false;
}
static DWORD Run(const std::wstring& file,const std::wstring& args,bool elevated){
 SHELLEXECUTEINFOW info={sizeof(info)};info.fMask=SEE_MASK_NOCLOSEPROCESS|SEE_MASK_NOASYNC;
 info.lpFile=file.c_str();info.lpParameters=args.c_str();info.lpVerb=elevated?L"runas":L"open";info.nShow=SW_SHOWNORMAL;
 if(!ShellExecuteExW(&info)||!info.hProcess){Failure=L"Setup was cancelled or Windows could not start a bundled component.";return 1;}
 WaitForSingleObject(info.hProcess,INFINITE);DWORD code=1;GetExitCodeProcess(info.hProcess,&code);CloseHandle(info.hProcess);return code;
}
int WINAPI wWinMain(HINSTANCE,HINSTANCE,PWSTR,int){
 int count=0;auto args=CommandLineToArgvW(GetCommandLineW(),&count);bool audit=count==3&&wcscmp(args[1],L"--extract-only")==0;
 wchar_t temp[MAX_PATH]={};GetTempPathW(MAX_PATH,temp);GUID id;CoCreateGuid(&id);wchar_t guid[48];StringFromGUID2(id,guid,48);
 std::wstring folder=audit?args[2]:std::wstring(temp)+L"FNClassicSetup-"+guid;LocalFree(args);
 int directoryResult=SHCreateDirectoryExW(nullptr,folder.c_str(),nullptr);
 if(directoryResult!=ERROR_SUCCESS&&directoryResult!=ERROR_ALREADY_EXISTS&&directoryResult!=ERROR_FILE_EXISTS){Failure=L"Cannot create the setup folder.";goto fail;}
 {
  OSVERSIONINFOW version={sizeof(version)};
  auto getVersion=(LONG(WINAPI*)(OSVERSIONINFOW*))GetProcAddress(GetModuleHandleW(L"ntdll.dll"),"RtlGetVersion");
  if(!audit&&(!getVersion||getVersion(&version)<0||version.dwMajorVersion<10||version.dwBuildNumber<19041)){Failure=L"Fortnite Classic requires 64-bit Windows 10 version 2004 or newer, or Windows 11.";goto fail;}
  struct Payload{int id;const wchar_t* name;const char* hash;};
  const Payload files[]={{101,L"FNClassicLauncher.exe",LauncherHash},{102,L"vc_redist.x64.exe",VC64Hash},{103,L"vc_redist.x86.exe",VC86Hash},{104,L"NDP48-x86-x64-AllOS-ENU.exe",FrameworkHash},{105,L"directx_Jun2010_redist.exe",DirectXHash}};
  for(const auto& file:files)if(!Extract(file.id,folder+L"\\"+file.name,file.hash))goto fail;
  if(audit)return 0;
  bool restart=false;
  auto prerequisite=[&](const wchar_t* file){auto code=Run(folder+L"\\"+file,L"/passive /norestart",true);if(code==3010||code==1641){restart=true;return true;}if(code==0||code==1638)return true;Failure=L"A bundled runtime could not be installed (error "+std::to_wstring(code)+L"). Setup files remain in "+folder;return false;};
  if(Value(L"SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full",L"Release",RRF_SUBKEY_WOW6432KEY)<528040&&!prerequisite(L"NDP48-x86-x64-AllOS-ENU.exe"))goto fail;
  if(!HasRuntime(L"x64")&&!prerequisite(L"vc_redist.x64.exe"))goto fail;
  if(!HasRuntime(L"x86")&&!prerequisite(L"vc_redist.x86.exe"))goto fail;
  wchar_t system[MAX_PATH];GetSystemDirectoryW(system,MAX_PATH);
  bool hasDirectX=true;for(const wchar_t* dll:{L"XINPUT1_3.dll",L"X3DAudio1_7.dll",L"XAPOFX1_5.dll"})hasDirectX=hasDirectX&&GetFileAttributesW((std::wstring(system)+L"\\"+dll).c_str())!=INVALID_FILE_ATTRIBUTES;
  if(!hasDirectX){
   auto directX=folder+L"\\DirectX";CreateDirectoryW(directX.c_str(),nullptr);
   if(Run(folder+L"\\directx_Jun2010_redist.exe",L"/Q /T:\""+directX+L"\"",false)!=0){Failure=L"Could not extract the bundled DirectX runtime.";goto fail;}
   auto code=Run(directX+L"\\DXSETUP.exe",L"/silent",true);
   if(code!=0){Failure=L"The bundled DirectX runtime could not be installed (error "+std::to_wstring(code)+L").";goto fail;}
  }
  if(restart){MessageBoxW(nullptr,L"The required runtimes were installed. Restart Windows, then run this installer again to finish. Setup will not restart your PC automatically.",L"Fortnite Classic Setup",MB_OK|MB_ICONINFORMATION);return 3010;}
  auto code=Run(folder+L"\\FNClassicLauncher.exe",L"--setup",false);
  for(const auto& file:files)DeleteFileW((folder+L"\\"+file.name).c_str());RemoveDirectoryW(folder.c_str());return (int)code;
 }
fail:
 MessageBoxW(nullptr,Failure.c_str(),L"Fortnite Classic Setup",MB_OK|MB_ICONERROR);return 1;
}
