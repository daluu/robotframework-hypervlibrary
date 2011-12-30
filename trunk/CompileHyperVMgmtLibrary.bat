rem .NET command line compiler example for compiling Hyper-V library
rem You may alternatively compile with SharpDevelop, Visual Studio, etc. instead
rem by creating new console application and/or class library projects with the source files

rem compile (from local path)
rem assumes XML-RPC.NET library DLL is precompiled and in local path
rem FYI get XML-RPC.NET library from
rem www.xml-rpc.net
rem change .NET framework path to desired version as needed

rem Needs to be compiled on Windows Server 2008 due to dependency on Hyper-V WMI provider
rem which does not exist in other OSes

rem However, remote server should be able to run on Windows 7 and Windows Server 2008,
rem though for Windows 7, you will need to first install
rem Remote Server Administration Tools for Windows 7
rem but Hyper-V still needs to run via Windows Server 2008 or Hyper-V Server 2008 R2
rem Also need to run server with UAC rights elevation for library to work (e.g. Run As Administrator)

rem for 64-bit binary compilation use
rem %windir%\Microsoft.NET\Framework64\v3.5\csc.exe ...

rem compile the Hyper-V management library
rem don't compile with XML documenation option as the HyperVSamples class will cause much compilation issues of the documentation
rem easier to manually edit/generate XML documentation for HyperVMgmtLibrary class
%windir%\Microsoft.NET\Framework\v3.5\csc.exe /out:HyperVMgmtLibrary.dll /target:library HyperVSamples.cs HyperVMgmtLibrary.cs

rem compile the remote server that runs the library
%windir%\Microsoft.NET\Framework\v3.5\csc.exe /out:RobotHyperVMgmtSvr.exe /target:exe /reference:CookComputing.XmlRpcV2.dll RobotHyperVMgmtSvr.cs

rem now be sure the remote server and all DLLs and XML documentation are all in one place after compilation for server to run correctly