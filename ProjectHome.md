# Overview #

A (test) library to manage Hyper-V virtual machines within the Robot Framework test automation framework. This library has methods to (remotely) stop/start virtual machines and take snapshots or revert back to them.

The remote server should be able to run on Windows 7 and Windows Server 2008, though for Windows 7, you will need to first install [Remote Server Administration Tools for Windows 7 Service Pack 1](http://www.microsoft.com/download/en/details.aspx?displaylang=en&id=7887) but Hyper-V still needs to run via Windows Server 2008 or [Hyper-V Server 2008 R2](http://www.microsoft.com/download/en/details.aspx?displaylang=en&id=3512).

Also need to run server with UAC rights elevation for library to work (e.g. Run As Administrator).

It will not run on Windows XP, Vista, or Server 2003.

The default use case is running this remote server on the Windows Server 2008 Hyper-V server machine. And communicate with the remote server / library via Robot Framework tests.

# Downloads #

  * [Hyper-V Remote Library Server](http://code.google.com/p/robotframework-hypervlibrary/downloads/detail?name=RobotHyper-VMgmtSvrBinary.zip)

# Important Notes #

  * You can create an IronPython library version, so as not need a remote server. Create an IronPython wrapper that calls the Hyper-V library DLL, doing same thing that the remote server does. Or optionally, implement the Hyper-V library entirely in Hyper-V following the references in links section to the left.

  * I don't have access to Hyper-V server on a Windows Server 2008 box anymore. And until I have time and hardware to set up a Hyper-V Server 2008 [R2](https://code.google.com/p/robotframework-hypervlibrary/source/detail?r=2) server up, there likely won't be any updates to the source code and binary distribution of this library for quite some time. Feel free to update yourself and post patches to this project. As such, I don't have access to thoroughly test library either, but it was working last time I tested it.

# Contact #

For now, please direct all inquiries to the project admin. You could also post inquiries to [Robot Framework Users Google Group](http://groups.google.com/group/robotframework-users) as I am a member of that group and periodically check it. If there is enough inquiry activity, I may start a Google Group, etc. for it.