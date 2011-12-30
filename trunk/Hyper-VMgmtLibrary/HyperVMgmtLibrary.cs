/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * @author David Luu
 */

using System;
using System.Collections.Generic;
using System.Management; //for WMI
using HyperVSamples; //to help with Hyper-V management, from Microsoft

namespace RobotFramework
{
	/// <summary>
	/// A (test) library to manage Hyper-V virtual machines within the 
	/// Robot Framework test automation framework. This library has
	/// methods to (remotely) stop/start virtual machines and take
	/// snapshots or revert back to them.
	/// </summary>
	public class HyperVMgmtLibrary
	{		
		private ManagementScope scope;
		private ManagementObject vm;
		private ManagementBaseObject inParams;
		private ManagementBaseObject outParams;
		private ManagementObject virtualSystemService;
		private ManagementObject vmSnapshot;
		
		/// <summary>
		/// Constructor
		/// </summary>
		public HyperVMgmtLibrary()
		{
			scope = new ManagementScope(@"\\.\root\virtualization", null);
			//scope = new ManagementScope(@"root\virtualization", null);
			vm = null;
			inParams = null;
			outParams = null;
			virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemManagementService");
			vmSnapshot = null;
		}		
		
		/// <summary>
		/// Start up a configured virtual machine in Hyper-V.
		/// </summary>
		/// <param name="name">Name of virtual machine to start up.</param>
		/// <returns>True if command succeeds, or false if it fails.</returns>
		public bool start_virtual_machine(string name)
		{
            vm = Utility.GetTargetComputer(name, scope);
            if (null == vm)
            {
                throw new ArgumentException(string.Format("The virtual machine '{0}' could not be found.",name));
            }

            inParams = vm.GetMethodParameters("RequestStateChange");
            inParams["RequestedState"] = 2; //enabled OR turn on
            outParams = vm.InvokeMethod("RequestStateChange",inParams,null);

            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
            {
                if (Utility.JobCompleted(outParams, scope))
                {
                    //Console.WriteLine("{0} was started successfully. Please allow time to fully boot VM.", name);
                	cleanupObjects();
                    return true;
                }
                else
                {
                    //Console.WriteLine("Failed to start {0}.",name);
                    cleanupObjects();
                    return false;
                }
            }
            else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {
                //Console.WriteLine("{0} was started successfully. Please allow time to fully boot VM.", name);
                cleanupObjects();
                return true;
            }
            else
            {
                //Console.WriteLine("Failed to start {0}, with error {1}.",name,outParams["ReturnValue"]);
                cleanupObjects();
                return false;
            }
		}
		
		/// <summary>
		/// Shut down a configured virtual machine (VM) in Hyper-V. 
		/// This is considered a hardware/forced shutdown, and
		/// is not the ideal way to shut down a VM. Use as a last resort.
		/// </summary>
		/// <param name="name">Name of virtual machine to shut down.</param>
		/// <returns>True if command succeeds, or false if it fails.</returns>
		public bool stop_virtual_machine(string name)
		{
            vm = Utility.GetTargetComputer(name, scope);
            if (null == vm)
            {
                throw new ArgumentException(string.Format("The virtual machine '{0}' could not be found.",name));
            }

            inParams = vm.GetMethodParameters("RequestStateChange");
            inParams["RequestedState"] = 3; //disabled OR turn off
            outParams = vm.InvokeMethod("RequestStateChange",inParams,null);

            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
            {
                if (Utility.JobCompleted(outParams, scope))
                {
                    //Console.WriteLine("{0} was stopped successfully.", name);
                	cleanupObjects();
                    return true;
                }
                else
                {
                    //Console.WriteLine("Failed to stop {0}.",name);
                    cleanupObjects();
                    return false;
                }
            }
            else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {
                //Console.WriteLine("{0} was stopped successfully.", name);
                cleanupObjects();
                return true;
            }
            else
            {
                //Console.WriteLine("Failed to stop {0}, with error {1}.",name,outParams["ReturnValue"]);
                cleanupObjects();
                return false;
            }			
		}
		
		/// <summary>
		/// Reboot a configured virtual machine (VM) in Hyper-V.
		/// This is considered a hardware/forced reset or hard reset
		/// and is not the ideal way to restart a VM. Use as a last resort.
		/// </summary>
		/// <param name="name">Name of virtual machine to reset.</param>
		/// <returns>True if command succeeds, or false if it fails.</returns>
		public bool hard_reset_virtual_machine(string name)
		{
            vm = Utility.GetTargetComputer(name, scope);
            if (null == vm)
            {
                throw new ArgumentException(string.Format("The virtual machine '{0}' could not be found.",name));
            }

            inParams = vm.GetMethodParameters("RequestStateChange");
            inParams["RequestedState"] = 10; //(hard) reset of machine
            outParams = vm.InvokeMethod("RequestStateChange",inParams,null);

            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
            {
                if (Utility.JobCompleted(outParams, scope))
                {
                    //Console.WriteLine("{0} was stopped successfully.", name);
                	cleanupObjects();
                    return true;
                }
                else
                {
                    //Console.WriteLine("Failed to stop {0}.",name);
                    cleanupObjects();
                    return false;
                }
            }
            else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {
                //Console.WriteLine("{0} was stopped successfully.", name);
                cleanupObjects();
                return true;
            }
            else
            {
                //Console.WriteLine("Failed to stop {0}, with error {1}.",name,outParams["ReturnValue"]);
                cleanupObjects();
                return false;
            }
		}
		
		/// <summary>
		/// Revert a configured virtual machine, running or not in Hyper-V,
		/// back to last saved snapshot. Useful for restoring saved state or
		/// "reimaging" or "restoring" system back to known base state.
		/// </summary>
		/// <param name="vmName">Name of virtual machine to revert.</param>
		/// <returns>True if command succeeds, or false if it fails.</returns>
		public bool revert_to_last_snapshot(string vmName)
		{            
            inParams = virtualSystemService.GetMethodParameters("ApplyVirtualSystemSnapshot");
            vm = Utility.GetTargetComputer(vmName, scope);
            vmSnapshot = GetLastVirtualSystemSnapshot(vm);
            inParams["SnapshotSettingData"] = vmSnapshot.Path.Path;
            inParams["ComputerSystem"] = vm.Path.Path;
            outParams = virtualSystemService.InvokeMethod("ApplyVirtualSystemSnapshot", inParams, null);

            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Started)
            {
                if (Utility.JobCompleted(outParams, scope))
                {
                    //Console.WriteLine("Successfully reverted {0} back to last snapshot.",vmName);
                    cleanupObjects();
                    return true;

                }
                else
                {
                    //Console.WriteLine("Failed to revert {0} back to last snapshot.",vmName);
                    cleanupObjects();
                    return false;
                }
            }
            else if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {
                //Console.WriteLine("Successfully reverted {0} back to last snapshot.",vmName);
                cleanupObjects();
                return true;
            }
            else
            {
                //Console.WriteLine("Failed to revert {0} back to last snapshot, with error {1}",vmName,outParams["ReturnValue"]);
                cleanupObjects();
                return false;
            }            
		}
		
		/// <summary>
		/// Get info on a configured virtual machine in Hyper-V. Things like
		/// VM name, notes, VM state, current memory usage, CPU load, guest OS
		/// running inside VM, etc.
		/// </summary>
		/// <param name="name">Name of virtual machine to get information about.</param>
		/// <returns>A string with VM information found.</returns>
		public string get_virtual_machine_information(string name)
        {
			ManagementObject virtualSystemSettings = GetVirtualSystemSetting(name);
            inParams = virtualSystemService.GetMethodParameters("GetSummaryInformation");
            string[] settingPaths = new string[1];
            for (int i = 0; i < settingPaths.Length; ++i)
            {
                settingPaths[i] = virtualSystemSettings.Path.Path;
            }
            inParams["SettingData"] = settingPaths;
            //build array of info to request
            UInt32[] requestedInfo = new UInt32[7]; //[8]
            requestedInfo[0] = 0; //VM name
            requestedInfo[1] = 3; //VM notes
            requestedInfo[2] = 100; //state of VM, on/off
            requestedInfo[3] = 101; //CPU load
            requestedInfo[4] = 103; //memory usage
            requestedInfo[5] = 105; //VM uptime
            requestedInfo[6] = 106; //guest OS running in VM
            //requestedInfo[7] = 102; //CPU load history, last 100 samples in array
            inParams["RequestedInformation"] = requestedInfo;
            outParams = virtualSystemService.InvokeMethod("GetSummaryInformation", inParams, null);

            string summaryInfo = "";
            if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
            {            	
                //Console.WriteLine("Summary information was retrieved successfully.");
                ManagementBaseObject[] summaryInformationArray = (ManagementBaseObject[])outParams["SummaryInformation"];
                foreach (ManagementBaseObject summaryInformation in summaryInformationArray)
                {
                    summaryInfo = summaryInfo + "VM name: " + summaryInformation["Name"].ToString() + "\n";
                    summaryInfo = summaryInfo + "VM guest OS: " + summaryInformation["GuestOperatingSystem"].ToString() + "\n";
                    summaryInfo = summaryInfo + "VM notes: " + summaryInformation["Notes"].ToString() + "\n";
                    summaryInfo = summaryInfo + "VM state: " + summaryInformation["EnabledState"].ToString() + "\n";
                    summaryInfo = summaryInfo + "VM uptime: " + summaryInformation["Uptime"].ToString() + "\n";
                    summaryInfo = summaryInfo + "VM memory usage: " + summaryInformation["MemoryUsage"].ToString() + "\n";
                    summaryInfo = summaryInfo + "VM CPU load: " + summaryInformation["ProcessorLoad"].ToString() + "\n";
                    //add processor history in future by summing array of ProcessorLoadHistory then take average
                    //summaryInfo = summaryInfo + summaryInformation["ProcessorLoadHistory"].ToString() + "\n";
                    //Console.WriteLine(summaryInfo);                    
                }
            }
            else
            {                
                summaryInfo = "Failed to retrieve VM info.\n";
                //Console.WriteLine(summaryInfo);
            }
            cleanupObjects();
            return summaryInfo;
        }
		
		//internal helper method for revert_to_last_snapshot()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="vm"></param>
		/// <returns></returns>
		private ManagementObject GetLastVirtualSystemSnapshot(ManagementObject vm)
        {
            ManagementObjectCollection settings = vm.GetRelated(
                "Msvm_VirtualSystemsettingData",
                "Msvm_PreviousSettingData",
                null,
                null,
                "SettingData",
                "ManagedElement",
                false,
                null);
            ManagementObject virtualSystemsetting = null;
            foreach (ManagementObject setting in settings)
            {
                //Console.WriteLine(setting.Path.Path);
                //Console.WriteLine(setting["ElementName"]);
                virtualSystemsetting = setting;
            }
            return virtualSystemsetting;
        }
		
		//internal helper method for get_virtual_machine_information()
		/// <summary>
		/// 
		/// </summary>
		/// <param name="vmName"></param>
		/// <returns></returns>
		private ManagementObject GetVirtualSystemSetting(string vmName)
        {
            ManagementObject virtualSystem = Utility.GetTargetComputer(vmName, scope);
            ManagementObjectCollection virtualSystemSettings = virtualSystem.GetRelated
             (
                 "Msvm_VirtualSystemSettingData",
                 "Msvm_SettingsDefineState",
                 null,
                 null,
                 "SettingData",
                 "ManagedElement",
                 false,
                 null
             );
            ManagementObject virtualSystemSetting = null;

            foreach (ManagementObject instance in virtualSystemSettings)
            {
                virtualSystemSetting = instance;
                break;
            }
            return virtualSystemSetting;
        }
		
		//internal helper method for all methods
		/// <summary>
		/// 
		/// </summary>
		private void cleanupObjects()
		{
			if(inParams != null) inParams.Dispose();
			if(outParams != null) outParams.Dispose();
			if(vmSnapshot != null) vmSnapshot.Dispose();
			if(vm != null) vm.Dispose();
			if(virtualSystemService != null) virtualSystemService.Dispose();
		}
	}
}