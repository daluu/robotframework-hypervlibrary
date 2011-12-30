﻿/* Licensed under the Apache License, Version 2.0 (the "License");
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
using System.IO;
using System.Net;		//used for XML-RPC server
using CookComputing.XmlRpc; //get from www.xml-rpc.net
using System.Reflection; //for the get_keyword methods and run_keyword method
using System.Xml;		//for use with get_keyword_documentation
using System.Xml.XPath; //to generate documentation for remote library
using System.Threading; //to shutdown server from remote request and at same time, return XML-RPC response

namespace RobotFramework
{	
	/// <summary>
	/// Customized Robot Framework remote server in .NET for automated
	/// Hyper-V management.
	/// </summary>
	class RemoteServer
	{
		public static bool enableStopServer;
		
		public static void Main(string[] args)
		{
			//set defaults for RobotFramework XML-RPC server spec
			string host = "127.0.0.1"; //localhost
			string port = "8270";
			enableStopServer = true;
			//params to load test library and documentation
			string remoteLibrary = "HyperVMgmtLibrary.dll";
			string className = "RobotFramework.HyperVMgmtLibrary";
			string docFile = "HyperVMgmtLibrary_doc.xml";

			//parse arguments
			for(int i = 0; i < args.Length; i++){
				if(args[i] == "--host")
					host = args[i+1];
				if(args[i] == "--port")
					port = args[i+1];
				if(args[i] == "--nostopsvr")
					enableStopServer = false;
			}
			
			Console.WriteLine("");
			Console.WriteLine("Robot Framework Hyper-V remote library started at {0} on port {1}, on {2}",host,port,System.DateTime.Now.ToString());
			Console.WriteLine("");
			Console.WriteLine("To stop server/library, send XML-RPC method request 'run_keyword' with");
			Console.WriteLine("single argument of 'stop_remote_server' to do so, or hit Ctrl + C, etc.");
			Console.WriteLine("");
			
			//Using .NET HTTP listener to remove dependence on IIS, etc. using code snippet from
			// http://www.xml-rpc.net/faq/xmlrpcnetfaq.html#3.12
			HttpListener listener = new HttpListener();
			listener.Prefixes.Add("http://"+host+":"+port+"/");
			listener.Start();
			while (true)
			{
				HttpListenerContext context = listener.GetContext();
				XmlRpcListenerService svc;
				if(docFile == "")
				{
					svc = new XmlRpcMethods(remoteLibrary,className);
				}
				else
				{
					svc = new XmlRpcMethods(remoteLibrary,className,docFile);
				}				
				svc.ProcessRequest(context);
			}
		}
		
		/// <summary>
		/// Display the usage information for server.
		/// </summary>
		public static void displayUsage()
		{
			Console.WriteLine("");
			Console.WriteLine("RobotHyperVMgmtSvr - v1.0");
			Console.WriteLine("");
			Console.WriteLine("Usage Info:");
			Console.WriteLine("");
			Console.WriteLine("  RobotHyperVMgmtSvr [--host address] [--port portNumber] [--nostopsvr]");
			Console.WriteLine("");
			Console.WriteLine("  Optionally specify IP address or host name to bind remote server to.");
			Console.WriteLine("    Default of 127.0.0.1 (localhost).");
			Console.WriteLine("  Optionally specify port to bind remote server to. Default of 8270.");
			Console.WriteLine("  Optionally set whether to allow remote shut down of server. Default yes.");
			Console.WriteLine("");
			Console.WriteLine("Example:");
			Console.WriteLine("");
			Console.WriteLine("  RobotHyperVMgmtSvr --host 192.168.0.10 --port 8080");
		}
	}
	
	/// <summary>
	/// Class of XML-RPC methods for remote library (server)
	/// that conforms to RobotFramework remote library API
	/// </summary>
	public class XmlRpcMethods : XmlRpcListenerService
	{
		private Assembly library;
		private string libraryClass;
		private XPathDocument doc;
		
		/// <summary>
		/// Default constructor for XML-RPC method class.
		/// Not really to be used.
		/// </summary>
		public XmlRpcMethods()
		{
			library = null;
			libraryClass = null;
			doc = null;
		}
		
		/// <summary>
		/// Basic constructor for XML-RPC method class.
		/// Load specified library (assembly) for use.
		/// No XML documentation provided to Robot Framework.
		/// </summary>
		/// <param name="libraryFile">Path to .NET assembly (DLL) file that contains the remote library class to load.</param>
		/// <param name="libraryClassName">Name of remote library class to load, specified in the format of "NamespaceName.ClassName" without the quotes.</param>
		public XmlRpcMethods(string libraryFile, string libraryClassName)
		{
			library = Assembly.LoadFrom(libraryFile);			
			libraryClass = libraryClassName;
			doc = null;
		}
		
		/// <summary>
		/// Best constructor for XML-RPC method class.
		/// Load specified library (assembly) for use
		/// and provide XML documentation to Robot Framework.
		/// </summary>
		/// <param name="libraryFile">Path to .NET assembly (DLL) file that contains the remote library class to load.</param>
		/// <param name="libraryClassName">Name of remote library class to load, specified in the format of "NamespaceName.ClassName" without the quotes.</param>
		/// <param name="docFile">Path to XML documentation file for the specified .NET class assembly file.</param>
		public XmlRpcMethods(string libraryFile, string libraryClassName, string docFile)
		{
			library = Assembly.LoadFrom(libraryFile);			
			libraryClass = libraryClassName;
			try{
				doc = new XPathDocument(docFile);
			}catch{
				doc = null; //failed to load XML documentation file, set null for further processing
			}			
		}
		
		/// <summary>
		/// Get a list of RobotFramework keywords available in remote library for use.
		/// 
		/// NOTE: Current implementation will return 
		/// extra unanticipated keywords from .NET remote class library, just ignore them
		/// for now, until we can optimize this .NET implementation.
		/// </summary>
		/// <returns>A string array of RobotFramework keywords available in remote library for use.</returns>
		[XmlRpcMethod]
		public string[] get_keyword_names()
  		{
			Type classType = library.GetType(libraryClass);			
			//MethodInfo[] mis = classType.GetMethods(BindingFlags.Public | BindingFlags.Static);
			//seem to have issue when trying to only get public & static methods, so get all instead
			MethodInfo[] mis = classType.GetMethods();
			
			//add one more for stop server that's part of the server
			string[] keyword_names = new string[mis.Length+1];
			int i = 0;			
			foreach(MethodInfo mi in mis)
			{
				keyword_names[i++] = mi.Name;
			}
			keyword_names[i] = "stop_remote_server";
			return keyword_names;
		}
		
		/// <summary>
		/// Run specified Robot Framework keyword from remote server.
		/// </summary>
		/// <param name="keyword">Keyword class library method to run for Robot Framework.</param>
		/// <param name="args">Arguments, if any, to pass to keyword method.</param>
		/// <returns></returns>
		[XmlRpcMethod]
		public keyword_results run_keyword(string keyword, object[] args)
  		{
			keyword_results kr = new keyword_results();
			if(keyword == "stop_remote_server")
			{				
				if(RemoteServer.enableStopServer){
					//spawn new thread to do a delayed server shutdown
					//and return XML-RPC response before delay is over
					new Thread(stop_remote_server).Start();
					Console.WriteLine("Shutting down remote server/library in 5 seconds, from Robot Framework remote");
					Console.WriteLine("library/XML-RPC request.");
					Console.WriteLine("");
					kr.output = "NOTE: remote server shutting/shut down.";					
				}else{
					kr.output = "NOTE: remote server not configured to allow remote shutdowns. Your request has been ignored.";
					//in case RF spec changes to report failure in this case in future
					//kr.status = "FAIL");
					//kr.error = "NOTE: remote server not configured to allow remote shutdowns. Your request has been ignored.";
				}
				kr.Return = "1";
				kr.status = "PASS";
				kr.error = "";
				kr.traceback = "";
				return kr;
			}
			Type classType = library.GetType(libraryClass);
			object libObj = Activator.CreateInstance(classType);
			MethodInfo mi = classType.GetMethod(keyword);			
			
			try
			{
				string retval = "";
				if(mi.ReturnType == typeof(System.Int32) ||
				   mi.ReturnType == typeof(System.String) ||
				   mi.ReturnType == typeof(System.Boolean))
				{
					//***case to handle basic data types
					//due to strict data typing by .NET, return value will
					//always be cast as a string in this implementation.
					//Until we can fix/optimize it to return value in any
					//one of the basic types: int, string, boolean, etc.
					retval = (string) mi.Invoke(libObj, args).ToString();
					if(mi.ReturnType == typeof(System.Boolean))
					{
						if(retval == "True")
							kr.status = "PASS";
						else //retval == "False"
							kr.status = "FAIL";						
					}
					else //return type ~int, string, etc. so always pass, if no exception
						kr.status = "PASS";					
				}
				else if(mi.ReturnType == typeof(System.Int32[]) ||
				   	mi.ReturnType == typeof(System.String[]) ||
				   	mi.ReturnType == typeof(System.Boolean[]))
				{
					//***case to handle array of basic data types
					//due to strict data typing by .NET, return value will
					//always be cast as a special delimited string value in this implementation.
					//Until we can fix/optimize it to return value in array of any
					//one of the basic types: int, string, boolean, etc.
					object[] tmpretval;
					tmpretval = (object[]) mi.Invoke(libObj, args);
					//return results in this string format: {item1, item2, ...}
					retval = "{";
					foreach(object obj in tmpretval)
					{
						retval = retval + (string) obj.ToString() + ",";
					}
					retval = retval + "}";
					kr.status = "PASS";
				}
				else
				{	
					//***case to handle keywords that don't return values, and all other cases
					//expect no return value from keyword, so always pass, if no exception					
					mi.Invoke(libObj, args);
					kr.status = "PASS";
				}
				kr.Return = retval;
				//due to limitation of .NET (I think) in not being able to redirect
				//standard (or stream) output from reflected/loaded library
				//output will always be empty with this implementation. Until we can
				//fix/optimize this deficiency.
				kr.output = "";
				kr.error = "";
				kr.traceback = "";						
				return kr;
			}
			catch(System.Exception ex)
			{
				kr.traceback = ex.StackTrace;
				kr.error = ex.Message;
				kr.output = ex.Message;
				kr.status = "FAIL";
				kr.Return = "";
				return kr;
			}					
		}
		
		/// <summary>
		/// As defined by Robot Framework spec, this keyword will remotely stop remote library server.
		/// To be called by Robot Framework remote library interface
		/// or by XML-RPC request to run_keyword() XML-RPC method,
		/// passing it "stop_remote_server" as single argument.
		/// 
		/// NOTE: Currently will not return any XML-RPC response after being called, unlike the Python implementation.
		/// </summary>
		private static void stop_remote_server()
  		{
			//delay shutdown for some time so can return XML-RPC response
			int delay = 5000; //let's arbitrarily set delay at 5 seconds
			Thread.Sleep(delay);
			Console.WriteLine("Remote server/library shut down at {0}",System.DateTime.Now.ToString());
			System.Environment.Exit(0);
		}
		
		/// <summary>
		/// Get list of arguments for specified Robot Framework keyword.
		/// </summary>
		/// <param name="keyword">The keyword to get a list of arguments for.</param>
		/// <returns>A string array of arguments for the given keyword.</returns>
		[XmlRpcMethod]
		public string[] get_keyword_arguments(string keyword)
		{
			if(keyword == "stop_remote_server") return new String[0];
			Type classType = library.GetType(libraryClass);
			MethodInfo mi = classType.GetMethod(keyword);
			ParameterInfo[] pis = mi.GetParameters();
			string[] args = new String[pis.Length];
			int i = 0;
			foreach(ParameterInfo pi in pis)
			{
				args[i++] = pi.Name;
			}
			return args;		
		}		

		/// <summary>
		/// Get documentation for specified Robot Framework keyword.
		/// Done by reading the .NET compiler generated XML documentation
		/// for the loaded class library.
		/// </summary>
		/// <param name="keyword">The keyword to get documentation for.</param>
		/// <returns>A documentation string for the given keyword.</returns>
		[XmlRpcMethod]
		public string get_keyword_documentation(string keyword)
		{
			string retval = ""; //start off with no documentation, in case keyword is not documented
			
			if(keyword == "stop_remote_server"){
				retval = "Remotely shut down remote server/library w/ Robot Framework keyword.\n\n";
				retval += "If server is configured to not allow remote shutdown, keyword 'request' is ignored by server.\n\n";
				retval += "Always returns status of PASS with return value of 1. Output value contains helpful info and may indicate whether remote shut down is allowed or not.";
				return retval;
			}
			if(doc == null)
			{
				return retval; //no XML documentation provided, return blank doc
			}//else return keyword (class method) documentation from XML file
			
			XPathNavigator docFinder;
			XPathNodeIterator docCol;
			try{
				docFinder = doc.CreateNavigator();
			}catch{
				docFinder = null; //failed to load XML documentation file, set null
			}			
			string branch = "/doc/members/member[starts-with(@name,'M:"+libraryClass+"."+keyword+"')]/summary";
			try
			{
				retval = docFinder.SelectSingleNode(branch).Value + System.Environment.NewLine + System.Environment.NewLine;
			}
			catch
			{
				//no summary info provided for .NET class method
			}
			try
			{
				branch = "/doc/members/member[starts-with(@name,'M:"+libraryClass+"."+keyword+"')]/param";
				docCol = docFinder.Select(branch);
				while (docCol.MoveNext())
				{
					retval = retval + docCol.Current.GetAttribute("name","") + ": " + docCol.Current.Value + System.Environment.NewLine;
				};
				retval = retval + System.Environment.NewLine;
			}
			catch
			{
				//no parameter info provided or some parameter info missing for .NET class method
			}
			try
			{
				branch = "/doc/members/member[starts-with(@name,'M:"+libraryClass+"."+keyword+"')]/returns";
				retval = retval + "Returns: " + docFinder.SelectSingleNode(branch).Value;
			}
			catch
			{
				//.NET class method either does not return a value (e.g. void) or documentation not provided
			}			
			return retval; //return whatever documentation was found for the keyword
		}
	}
	
	/// <summary>
	/// Robot Framework run_keyword return value data structure, based on spec at
	/// http://robotframework.googlecode.com/svn/tags/robotframework-2.5.6/doc/userguide/RobotFrameworkUserGuide.html#remote-library-interface
	/// 
	/// Due to strict data typing by .NET, return value will always be cast as a string in this implementation.
	/// Until we can fix/optimize it to return value in any one of the basic types: int, string, array of strings/ints, etc.
	/// </summary>
	public struct keyword_results
	{
		public string status; //Mandatory execution status. Either PASS or FAIL.
		public string output; //Possible output to write into the RobotFramework log file. Must be given as a single string but can contain multiple messages and different log levels in format *INFO* First message\n*INFO* Second\n*WARN* Another message.
		public string traceback; //Possible stack trace to write into the RobotFramework log file using DEBUG level when the execution fails.
		public string error; //Possible error message. Used only when the execution fails.
		[XmlRpcMember("return")]
		public string Return; //Possible return value. Must be one of the supported RobotFramework/Python data types.
		//due to strict data typing by .NET, return value will always be cast as a string in this implementation.
		//Until we can fix/optimize it to return value in any one of the basic types: int, string, array of strings/ints, etc.
	}
}