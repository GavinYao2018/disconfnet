
  @echo off 
 rem 安装服务

 set p=%~dp0

 C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil  %p%Disconf.Net.WinServices.exe

 @echo on

 pause