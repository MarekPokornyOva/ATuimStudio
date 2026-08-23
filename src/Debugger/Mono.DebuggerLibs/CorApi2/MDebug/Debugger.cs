//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;

using ClrDebug;
using Microsoft.Samples.Debugging.Extensions;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using System.Linq;
using ManagedCallbackType = ClrDebug.CorDebugManagedCallbackKind;

namespace Microsoft.Samples.Debugging.CorDebug
{
    /**
     * Wraps the native CLR Debugger.
     * Note that we don't derive the class from WrapperBase, becuase this
     * class will never be returned in any callback.
     */
    public sealed  class CorDebugger : MarshalByRefObject
    {
      internal const ManagedCallbackType ManagedCallbackType_ExceptionInCallback = (ManagedCallbackType)int.MaxValue;

		  private const int MaxVersionStringLength = 256; // == MAX_PATH
        
        public static string GetDebuggerVersionFromFile(string pathToExe)
        {
            Debug.Assert( !string.IsNullOrEmpty(pathToExe) );
            if( string.IsNullOrEmpty(pathToExe) )
                throw new ArgumentException("Value cannot be null or empty.", "pathToExe");
            int neededSize;
            StringBuilder sb = new StringBuilder(MaxVersionStringLength);
            NativeMethods.GetRequestedRuntimeVersion(pathToExe, sb, sb.Capacity, out neededSize);
            return sb.ToString();
        }

        public static string GetDebuggerVersionFromPid(int pid)
        {
            using(ProcessSafeHandle ph = NativeMethods.OpenProcess((int)(NativeMethods.ProcessAccessOptions.PROCESS_VM_READ |
                                                                         NativeMethods.ProcessAccessOptions.PROCESS_QUERY_INFORMATION |
                                                                         NativeMethods.ProcessAccessOptions.PROCESS_DUP_HANDLE |
                                                                         NativeMethods.ProcessAccessOptions.SYNCHRONIZE),
                                                                   false, // inherit handle
                                                                   pid) )
            {
                if( ph.IsInvalid )
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                int neededSize;
                StringBuilder sb = new StringBuilder(MaxVersionStringLength);
                NativeMethods.GetVersionFromProcess(ph, sb, sb.Capacity, out neededSize);
                return sb.ToString();
            }
        }

        public static List<string> GetProcessLoadedRuntimes (int pid)
        {
            using (ProcessSafeHandle ph = NativeMethods.OpenProcess (
                (int) (NativeMethods.ProcessAccessOptions.PROCESS_VM_READ |
                       NativeMethods.ProcessAccessOptions.PROCESS_QUERY_INFORMATION |
                       NativeMethods.ProcessAccessOptions.PROCESS_DUP_HANDLE |
                       NativeMethods.ProcessAccessOptions.SYNCHRONIZE),
                false, // inherit handle
                pid)) {
                if (ph.IsInvalid)
                    return new List<string> ();
                int neededSize = MaxVersionStringLength;
                ICLRMetaHost host;
                NativeMethods.CLRCreateInstance (ref NativeMethods.CLSID_CLRMetaHost,
                    ref NativeMethods.IID_ICLRMetaHost, out host);
                var result = new List<string> ();
                host.EnumerateLoadedRuntimes (ph.DangerousGetHandle(), out IEnumUnknown runtimes);
                int count;
                while (runtimes.Next (1, out object item, out count) == 0) {
                    var info = item as ICLRRuntimeInfo;
                    if (info == null)
                        continue;
                    var stringBuilder = new StringBuilder (MaxVersionStringLength);
                    info.GetVersionString (stringBuilder, ref neededSize);
                    result.Add (stringBuilder.ToString ());
                }
                return result;
            }
        }

        public static string GetDefaultDebuggerVersion()
        {
            int size;
            NativeMethods.GetCORVersion(null,0,out size);
            Debug.Assert(size>0);
            StringBuilder sb = new StringBuilder(size);
            int hr = NativeMethods.GetCORVersion(sb,sb.Capacity,out size);
            Marshal.ThrowExceptionForHR(hr);
            return sb.ToString();
        }
     

        /// <summary>Creates a debugger wrapper from Guid.</summary>
        public CorDebugger(Guid debuggerGuid)
        {
            ICorDebug rawDebuggingAPI;
            NativeMethods.CoCreateInstance(ref debuggerGuid,
                                           IntPtr.Zero, // pUnkOuter
                                           1, // CLSCTX_INPROC_SERVER
                                           ref NativeMethods.IIDICorDebug,
                                           out rawDebuggingAPI);
            InitFromICorDebug(rawDebuggingAPI);
        }
        /// <summary>Creates a debugger interface that is able debug requested verison of CLR</summary>
        /// <param name="debuggerVersion">Version number of the debugging interface.</param>
        /// <remarks>The version number is usually retrieved either by calling one of following mscoree functions:
        /// GetCorVerison, GetRequestedRuntimeVersion or GetVersionFromProcess.</remarks>
        public CorDebugger (string debuggerVersion)
        {
            InitFromVersion(debuggerVersion);
        }

      
        [CLSCompliant(false)]
        public CorDebugger(ICorDebug corDebug)
        {
            InitFromICorDebug(corDebug);
        }


        ~CorDebugger()
        {
            if(m_debugger!=null)
                try 
                {
                    Terminate();
                } 
                catch
                {
                    // sometimes we cannot terminate because GC collects object in wrong
                    // order. But since the whole process is shutting down, we really
                    // don't care.
                    
                }
        }


        /**
         * Closes the debugger.  After this method is called, it is an error
         * to call any other methods on this object.
         */
        public void Terminate ()
        {
            Debug.Assert(m_debugger!=null);
            ICorDebug d= m_debugger;
            m_debugger = null;
            d.Terminate ();
        }

        /**
         * Specify the callback object to use for managed events.
         */
        internal void SetManagedHandler (ICorDebugManagedCallback managedCallback)
        {
            m_debugger.SetManagedHandler (managedCallback);
        }

        /**
         * Specify the callback object to use for unmanaged events.
         */
        internal void SetUnmanagedHandler (ICorDebugUnmanagedCallback nativeCallback)
        {
            m_debugger.SetUnmanagedHandler (nativeCallback);
        }

        /**
         * Launch a process under the control of the debugger.
         *
         * Parameters are the same as the Win32 CreateProcess call.
         */
        public CorProcess CreateProcess (
                                         String applicationName,
                                         String commandLine
                                         )
        {
            return CreateProcess (applicationName, commandLine, ".");
        }

        /**
         * Launch a process under the control of the debugger.
         *
         * Parameters are the same as the Win32 CreateProcess call.
         */
        public CorProcess CreateProcess (
                                         String applicationName,
                                         String commandLine,
                                         String currentDirectory
                                         )
		{
			// [Xamarin] ASP.NET Debugging.
			return CreateProcess (applicationName, commandLine, currentDirectory, null, 0);
        }

		/**
		 * Launch a process under the control of the debugger.
		 *
		 * Parameters are the same as the Win32 CreateProcess call.
		 */
		// [Xamarin] ASP.NET Debugging.
		public CorProcess CreateProcess (
										 String applicationName,
										 String commandLine,
										 String currentDirectory,
										 IDictionary<string,string> environment
										 )
		{
			return CreateProcess (applicationName, commandLine, currentDirectory, environment, 0);
		}

        /**
         * Launch a process under the control of the debugger.
         *
         * Parameters are the same as the Win32 CreateProcess call.
         */
        public CorProcess CreateProcess (
                                         String applicationName,
                                         String commandLine,
                                         String currentDirectory,
										 IDictionary<string,string> environment,
													  CreateProcessFlags flags
                                         )
        {
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION ();

            STARTUPINFOW si = new STARTUPINFOW ();
            si.cb = Marshal.SizeOf(si);

            // initialize safe handles 
			// [Xamarin] ASP.NET Debugging and output redirection.
			SafeFileHandle outReadPipe = null, errorReadPipe = null;
			DebuggerExtensions.SetupOutputRedirection (si, ref flags, out outReadPipe, out errorReadPipe);
			IntPtr env = DebuggerExtensions.SetupEnvironment (environment);

            CorProcess ret;

            //constrained execution region (Cer)
            System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions();
            try 
            {
            } 
            finally
            {
                ret = CreateProcess (
                                     applicationName,
                                     commandLine, 
                                     default,
                                     default,
                                     true,   // inherit handles
                                     flags,  // creation flags
									 env,      // environment
                                     currentDirectory,
                                     si,     // startup info
                                     ref pi, // process information
                                     CorDebugCreateProcessFlags.DEBUG_NO_SPECIAL_OPTIONS);
                NativeMethods.CloseHandle (pi.hProcess);
                NativeMethods.CloseHandle (pi.hThread);
            }

			DebuggerExtensions.TearDownEnvironment (env);
			DebuggerExtensions.TearDownOutputRedirection (outReadPipe, errorReadPipe, si, ret);

            return ret;
        }

        /**
         * Launch a process under the control of the debugger.
         *
         * Parameters are the same as the Win32 CreateProcess call.
         *
         * The caller should remember to execute:
         *
         *    Microsoft.Win32.Interop.Windows.CloseHandle (
         *      processInformation.hProcess);
         *
         * after CreateProcess returns.
         */
        [CLSCompliant(false)]
        public CorProcess CreateProcess (
                                         String                      applicationName,
                                         String                      commandLine,
                                         SECURITY_ATTRIBUTES         processAttributes,
                                         SECURITY_ATTRIBUTES         threadAttributes,
                                         bool                        inheritHandles,
													  CreateProcessFlags          creationFlags,
                                         IntPtr                      environment,  
                                         String                      currentDirectory,
                                         STARTUPINFOW                 STARTUPINFOW,
                                         ref PROCESS_INFORMATION     processInformation,
                                         CorDebugCreateProcessFlags  debuggingFlags)
        {
            /*
             * If commandLine is: <c:\a b\a arg1 arg2> and c:\a.exe does not exist, 
             *    then without this logic, "c:\a b\a.exe" would be tried next.
             * To prevent this ambiguity, this forces the user to quote if the path 
             *    has spaces in it: <"c:\a b\a" arg1 arg2>
             */
            if(null == applicationName && !commandLine.StartsWith("\""))
            {
                int firstSpace = commandLine.IndexOf(" ");
                if(firstSpace != -1)
                    commandLine = String.Format(CultureInfo.InvariantCulture, "\"{0}\" {1}", commandLine.Substring(0,firstSpace), commandLine.Substring(firstSpace, commandLine.Length-firstSpace));
            }

            ICorDebugProcess proc = null;

            m_debugger.CreateProcess (
                                  applicationName, 
                                  commandLine, 
                                  processAttributes,
                                  threadAttributes, 
                                  inheritHandles, 
                                  creationFlags, 
                                  environment, 
                                  currentDirectory, 
                                  STARTUPINFOW, 
                                  processInformation, 
                                  debuggingFlags,
                                  out proc);

            return CorProcess.GetCorProcess(proc);
        }

        /** 
         * Attach to an active process
         */
        public CorProcess DebugActiveProcess (int processId, bool win32Attach)
        {
            ICorDebugProcess proc = null;
            m_debugger.DebugActiveProcess (processId, win32Attach, out proc);
            return CorProcess.GetCorProcess(proc);
        }

        /**
         * Enumerate all processes currently being debugged.
         */
        public IEnumerable<CorProcess> Processes
        {
            get
            {
				   m_debugger.EnumerateProcesses(out ICorDebugProcessEnum ppProcess);
				   return new CorDebugProcessEnum(ppProcess).Select(static x => CorProcess.GetCorProcess(x));
            }
        }

        /**
         * Get the Process object for the given PID.
         */
        public CorProcess GetProcess (int processId)
        {
			   m_debugger.GetProcess(processId, out ICorDebugProcess ppProcess);
			   return CorProcess.GetCorProcess(ppProcess);
        }

        /**
         * Warn us of potentional problems in using debugging (eg. whether a kernel debugger is 
         * attached).  This API should probably be renamed or the warnings turned into errors
         * in CreateProcess/DebugActiveProcess
         */
        public void CanLaunchOrAttach(int processId, bool win32DebuggingEnabled)
        {
            m_debugger.CanLaunchOrAttach(processId,
                                         win32DebuggingEnabled?1:0);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // CorDebugger private implement part
        //
        ////////////////////////////////////////////////////////////////////////////////

        // called by constructors during initialization
        private void InitFromVersion(string debuggerVersion)
        {
            if( debuggerVersion.StartsWith("v1") )
            {
                throw new ArgumentException( "Can't debug a version 1 CLR process (\"" + debuggerVersion + 
                    "\").  Run application in a version 2 CLR, or use a version 1 debugger instead." );
            }
            
            ICorDebug rawDebuggingAPI;
			// [Xamarin] .NET 4 API Version.
#if MDBG_FAKE_COM
			// TODO: Ideally, there wouldn't be any difference in the corapi code for MDBG_FAKE_COM.
			// This would require puting this initialization logic into the wrapper and interop assembly, which doesn't seem right.
			// We should also release this pUnk, but doing that here would be difficult and we aren't done with it until
			// we shutdown anyway.
			IntPtr pUnk = NativeMethods.CreateDebuggingInterfaceFromVersion((int)CorDebuggerVersion.Whidbey, debuggerVersion);
			rawDebuggingAPI = new NativeApi.CorDebugClass(pUnk);
#else
			int apiVersion = debuggerVersion.StartsWith ("v4") ? 4 : 3;
			rawDebuggingAPI = NativeMethods.CreateDebuggingInterfaceFromVersion (apiVersion, debuggerVersion);
#endif
		    InitFromICorDebug(rawDebuggingAPI);
    	}
        
        private void InitFromICorDebug(ICorDebug rawDebuggingAPI)
        {
            Debug.Assert(rawDebuggingAPI!=null);
            if( rawDebuggingAPI==null )
                throw new ArgumentException("Cannot be null.","rawDebugggingAPI");
            
            m_debugger = rawDebuggingAPI;
            m_debugger.Initialize ();

			m_debugger = rawDebuggingAPI ?? throw new ArgumentException("Cannot be null.", "rawDebugggingAPI");
			m_debugger.Initialize();

			static ManagedCallbackType ConvertCallbackType(CorDebugManagedCallbackEventArgs args)
				=> (ManagedCallbackType)(int)args.Kind;
			static CorAppDomain ConvertAppDomain2(CorDebugAppDomain domain)
				=> domain == null ? null! : new CorAppDomain(domain);
			static CorAppDomain ConvertAppDomain(AppDomainThreadDebugCallbackEventArgs args)
				=> ConvertAppDomain2(args.AppDomain);
			static CorThread ConvertThread(AppDomainThreadDebugCallbackEventArgs args)
				=> ConvertThread2(args.Thread);
			static CorThread ConvertThread2(CorDebugThread thread)
				=> thread == null ? null! : new CorThread(thread);
			static CorProcess ConvertProcess(CorDebugProcess process)
				=> process == null ? null! : CorProcess.GetCorProcess(process);
			static CorModule ConvertModule(CorDebugModule module)
				=> module == null ? null! : new CorModule(module);
			static CorClass ConvertClass(CorDebugClass cls)
				=> cls == null ? null! : new CorClass(cls);
			static CorAssembly ConvertAssembly(CorDebugAssembly assembly)
				=> assembly == null ? null! : new CorAssembly(assembly);
			static CorBreakpoint ConvertBreakpoint(CorDebugBreakpoint breakpoint)
			{
				if (breakpoint == null)
					return null!;

				if (breakpoint is CorDebugFunctionBreakpoint funBp)
					return new CorFunctionBreakpoint(funBp);

				if (breakpoint is CorDebugModuleBreakpoint modBp)
					return new CorModuleBreakpoint(modBp);

				if (breakpoint is CorDebugValueBreakpoint valBp)
					return new CorValueBreakpoint(valBp);

				throw new NotImplementedException("Encountered an 'CorDebugBreakpoint' of an unknown type. Cannot create wrapper type.");
			}
			static CorFunction ConvertFunction(CorDebugFunction function)
				=> function == null ? null! : new CorFunction(function);
			static CorFrame ConvertFrame(CorDebugFrame frame)
				=> frame == null ? null! : new CorFrame(frame);
			static CorMDA ConvertMda(CorDebugMDA mda)
				=> mda == null ? null! : new CorMDA(mda);

			var managedCallback = new CorDebugManagedCallback();
			//managedCallback.OnAnyEvent += (sender, args) =>
			//{
			//OnAnyEvent is more or less duplicate for specific events.
			//	TOut CastArgs<TIn, TOut>(Func<TIn, TOut> converter) where TIn : CorDebugManagedCallbackEventArgs where  TOut : CorEventArgs
			//		=> converter((TIn)args);
			//
			//	CorEventArgs outArgs = args.Kind switch
			//	{
			//		CorDebugManagedCallbackKind.Breakpoint => CastArgs<BreakpointCorDebugManagedCallbackEventArgs, CorBreakpointEventArgs>(args => new CorBreakpointEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertBreakpoint(args.Breakpoint), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.StepComplete => CastArgs<StepCompleteCorDebugManagedCallbackEventArgs, CorStepCompleteEventArgs>(args => new CorStepCompleteEventArgs(ConvertAppDomain(args), ConvertThread(args), new CorStepper(args.Stepper), args.Reason, ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.Break => CastArgs<BreakCorDebugManagedCallbackEventArgs, CorThreadEventArgs>(args => new CorThreadEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.Exception => CastArgs<ExceptionCorDebugManagedCallbackEventArgs, CorExceptionEventArgs>(args => new CorExceptionEventArgs(ConvertAppDomain(args), ConvertThread(args), args.Unhandled != 0, ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.EvalComplete => CastArgs<EvalCompleteCorDebugManagedCallbackEventArgs, CorEvalEventArgs>(args => new CorEvalEventArgs(ConvertAppDomain(args), ConvertThread(args), args.Eval == null ? null! : new CorEval(args.Eval), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.EvalException => CastArgs<EvalExceptionCorDebugManagedCallbackEventArgs, CorEvalEventArgs>(args => new CorEvalEventArgs(ConvertAppDomain(args), ConvertThread(args), args.Eval == null ? null! : new CorEval(args.Eval), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.CreateProcess => CastArgs<CreateProcessCorDebugManagedCallbackEventArgs, CorProcessEventArgs>(args => new CorProcessEventArgs(ConvertProcess(args.Process), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.ExitProcess => CastArgs<ExitProcessCorDebugManagedCallbackEventArgs, CorProcessEventArgs>(args => new CorProcessEventArgs(ConvertProcess(args.Process), ConvertCallbackType(args)) { Continue = false }),
			//		CorDebugManagedCallbackKind.CreateThread => CastArgs<CreateThreadCorDebugManagedCallbackEventArgs, CorThreadEventArgs>(args => new CorThreadEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.ExitThread => CastArgs<ExitThreadCorDebugManagedCallbackEventArgs, CorThreadEventArgs>(args => new CorThreadEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.LoadModule => CastArgs<LoadModuleCorDebugManagedCallbackEventArgs, CorModuleEventArgs>(args => new CorModuleEventArgs(ConvertAppDomain2(args.AppDomain), ConvertModule(args.Module), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.UnloadModule => CastArgs<UnloadModuleCorDebugManagedCallbackEventArgs, CorModuleEventArgs>(args => new CorModuleEventArgs(ConvertAppDomain2(args.AppDomain), ConvertModule(args.Module), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.LoadClass => CastArgs<LoadClassCorDebugManagedCallbackEventArgs, CorClassEventArgs>(args => new CorClassEventArgs(ConvertAppDomain2(args.AppDomain), ConvertClass(args.C), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.UnloadClass => CastArgs<UnloadClassCorDebugManagedCallbackEventArgs, CorClassEventArgs>(args => new CorClassEventArgs(ConvertAppDomain2(args.AppDomain), ConvertClass(args.C), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.DebuggerError => CastArgs<DebuggerErrorCorDebugManagedCallbackEventArgs, CorDebuggerErrorEventArgs>(args => new CorDebuggerErrorEventArgs(ConvertProcess(args.Process), (int)args.ErrorHR, args.ErrorCode, ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.LogMessage => CastArgs<LogMessageCorDebugManagedCallbackEventArgs, CorLogMessageEventArgs>(args => new CorLogMessageEventArgs(ConvertAppDomain(args), ConvertThread(args), (int)args.LLevel, args.LogSwitchName, args.Message, ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.LogSwitch => CastArgs<LogSwitchCorDebugManagedCallbackEventArgs, CorLogSwitchEventArgs>(args => new CorLogSwitchEventArgs(ConvertAppDomain(args), ConvertThread(args), args.LLevel, (int)args.UlReason, args.LogSwitchName, args.ParentName, ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.CreateAppDomain => CastArgs<CreateAppDomainCorDebugManagedCallbackEventArgs, CorAppDomainEventArgs>(args => new CorAppDomainEventArgs(ConvertProcess(args.Process), ConvertAppDomain2(args.AppDomain), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.ExitAppDomain => CastArgs<ExitAppDomainCorDebugManagedCallbackEventArgs, CorAppDomainEventArgs>(args => new CorAppDomainEventArgs(ConvertProcess(args.Process), ConvertAppDomain2(args.AppDomain), ConvertCallbackType(args)) { Continue = false }),
			//		CorDebugManagedCallbackKind.LoadAssembly => CastArgs<LoadAssemblyCorDebugManagedCallbackEventArgs, CorAssemblyEventArgs>(args => new CorAssemblyEventArgs(ConvertAppDomain2(args.AppDomain), ConvertAssembly(args.Assembly), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.UnloadAssembly => CastArgs<UnloadAssemblyCorDebugManagedCallbackEventArgs, CorAssemblyEventArgs>(args => new CorAssemblyEventArgs(ConvertAppDomain2(args.AppDomain), ConvertAssembly(args.Assembly), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.ControlCTrap => CastArgs<ControlCTrapCorDebugManagedCallbackEventArgs, CorProcessEventArgs>(args => new CorProcessEventArgs(ConvertProcess(args.Process), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.NameChange => CastArgs<NameChangeCorDebugManagedCallbackEventArgs, CorThreadEventArgs>(args => new CorThreadEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.UpdateModuleSymbols => CastArgs<UpdateModuleSymbolsCorDebugManagedCallbackEventArgs, CorUpdateModuleSymbolsEventArgs>(args => new CorUpdateModuleSymbolsEventArgs(ConvertAppDomain2(args.AppDomain), ConvertModule(args.Module), ConvertStream(args.SymbolStream), ConvertCallbackType(args))),
			//		//CorDebugManagedCallbackKind.EditAndContinueRemap => CastArgs<,>(args => new (, ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.BreakpointSetError => CastArgs<BreakpointSetErrorCorDebugManagedCallbackEventArgs, CorBreakpointSetErrorEventArgs>(args => new CorBreakpointSetErrorEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertBreakpoint(args.Breakpoint), args.Error, ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.FunctionRemapOpportunity => CastArgs<FunctionRemapOpportunityCorDebugManagedCallbackEventArgs, CorFunctionRemapOpportunityEventArgs>(args => new CorFunctionRemapOpportunityEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertFunction(args.OldFunction), ConvertFunction(args.NewFunction), args.OldILOffset, ConvertCallbackType(args))),
			//		//CorDebugManagedCallbackKind.CreateConnection => CastArgs<,>(args => new (, ConvertCallbackType(args))),
			//		//CorDebugManagedCallbackKind.ChangeConnection => CastArgs<,>(args => new (, ConvertCallbackType(args))),
			//		//CorDebugManagedCallbackKind.DestroyConnection => CastArgs<,>(args => new (, ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.Exception2 => CastArgs<Exception2CorDebugManagedCallbackEventArgs, CorException2EventArgs>(args => new CorException2EventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertFrame(args.Frame), args.Offset, args.EventType, (int)args.Flags, ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.ExceptionUnwind => CastArgs<ExceptionUnwindCorDebugManagedCallbackEventArgs, CorExceptionUnwind2EventArgs>(args => new CorExceptionUnwind2EventArgs(ConvertAppDomain(args), ConvertThread(args), args.EventType, (int)args.Flags, ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.FunctionRemapComplete => CastArgs<FunctionRemapCompleteCorDebugManagedCallbackEventArgs, CorFunctionRemapCompleteEventArgs>(args => new CorFunctionRemapCompleteEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertFunction(args.Function), ConvertCallbackType(args))),
			//		CorDebugManagedCallbackKind.MDANotification => CastArgs<MDANotificationCorDebugManagedCallbackEventArgs, CorMDAEventArgs>(args => new CorMDAEventArgs(ConvertMda(args.MDA), ConvertThread2(args.Thread), ConvertProcess(args.Controller is CorDebugProcess dp ? dp : ((CorDebugAppDomain)args.Controller).Process), ConvertCallbackType(args))),
			//		//CorDebugManagedCallbackKind.CustomNotification => CastArgs<,>(args => new (, ConvertCallbackType(args))),
			//		//CorDebugManagedCallbackKind.BeforeGarbageCollection => CastArgs<,>(args => new (, ConvertCallbackType(args))),
			//		//CorDebugManagedCallbackKind.AfterGarbageCollection => CastArgs<,>(args => new (, ConvertCallbackType(args))),
			//		//CorDebugManagedCallbackKind.DataBreakpoint += CastArgs<,>(args => (sender, args) => new(, ConvertCallbackType(args))),
			//		_ => throw new NotImplementedException()
			//	};
			//	InternalFireEvent(ConvertCallbackType(args), outArgs);
			//};
			managedCallback.OnBreakpoint += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorBreakpointEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertBreakpoint(args.Breakpoint), ConvertCallbackType(args)));
			managedCallback.OnStepComplete += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorStepCompleteEventArgs(ConvertAppDomain(args), ConvertThread(args), new CorStepper(args.Stepper), args.Reason, ConvertCallbackType(args)));
			managedCallback.OnBreak += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorThreadEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertCallbackType(args)));
			managedCallback.OnException += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorExceptionEventArgs(ConvertAppDomain(args), ConvertThread(args), args.Unhandled != 0, ConvertCallbackType(args)));
			managedCallback.OnEvalComplete += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorEvalEventArgs(ConvertAppDomain(args), ConvertThread(args), args.Eval == null ? null! : new CorEval(args.Eval), ConvertCallbackType(args)));
			managedCallback.OnEvalException += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorEvalEventArgs(ConvertAppDomain(args), ConvertThread(args), args.Eval == null ? null! : new CorEval(args.Eval), ConvertCallbackType(args)));
			managedCallback.OnCreateProcess += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorProcessEventArgs(ConvertProcess(args.Process), ConvertCallbackType(args)));
			managedCallback.OnExitProcess += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorProcessEventArgs(ConvertProcess(args.Process), ConvertCallbackType(args)) { Continue = false });
			managedCallback.OnCreateThread += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorThreadEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertCallbackType(args)));
			managedCallback.OnExitThread += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorThreadEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertCallbackType(args)));
			managedCallback.OnLoadModule += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorModuleEventArgs(ConvertAppDomain2(args.AppDomain), ConvertModule(args.Module), ConvertCallbackType(args)));
			managedCallback.OnUnloadModule += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorModuleEventArgs(ConvertAppDomain2(args.AppDomain), ConvertModule(args.Module), ConvertCallbackType(args)));
			managedCallback.OnLoadClass += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorClassEventArgs(ConvertAppDomain2(args.AppDomain), ConvertClass(args.C), ConvertCallbackType(args)));
			managedCallback.OnUnloadClass += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorClassEventArgs(ConvertAppDomain2(args.AppDomain), ConvertClass(args.C), ConvertCallbackType(args)));
			managedCallback.OnDebuggerError += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorDebuggerErrorEventArgs(ConvertProcess(args.Process), (int)args.ErrorHR, args.ErrorCode, ConvertCallbackType(args)));
			managedCallback.OnLogMessage += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorLogMessageEventArgs(ConvertAppDomain(args), ConvertThread(args), (int)args.LLevel, args.LogSwitchName, args.Message, ConvertCallbackType(args)));
			managedCallback.OnLogSwitch += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorLogSwitchEventArgs(ConvertAppDomain(args), ConvertThread(args), args.LLevel, (int)args.UlReason, args.LogSwitchName, args.ParentName, ConvertCallbackType(args)));
			managedCallback.OnCreateAppDomain += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorAppDomainEventArgs(ConvertProcess(args.Process), ConvertAppDomain2(args.AppDomain), ConvertCallbackType(args)));
			managedCallback.OnExitAppDomain += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorAppDomainEventArgs(ConvertProcess(args.Process), ConvertAppDomain2(args.AppDomain), ConvertCallbackType(args)) { Continue = false });
			managedCallback.OnLoadAssembly += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorAssemblyEventArgs(ConvertAppDomain2(args.AppDomain), ConvertAssembly(args.Assembly), ConvertCallbackType(args)));
			managedCallback.OnUnloadAssembly += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorAssemblyEventArgs(ConvertAppDomain2(args.AppDomain), ConvertAssembly(args.Assembly), ConvertCallbackType(args)));
			managedCallback.OnControlCTrap += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorProcessEventArgs(ConvertProcess(args.Process), ConvertCallbackType(args)));
			managedCallback.OnNameChange += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorThreadEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertCallbackType(args)));
			managedCallback.OnUpdateModuleSymbols += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorUpdateModuleSymbolsEventArgs(ConvertAppDomain2(args.AppDomain), ConvertModule(args.Module), args.SymbolStream, ConvertCallbackType(args)));
			//managedCallback.OnEditAndContinueRemap += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new (, ConvertCallbackType(args)));
			managedCallback.OnBreakpointSetError += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorBreakpointSetErrorEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertBreakpoint(args.Breakpoint), args.Error, ConvertCallbackType(args)));
			managedCallback.OnFunctionRemapOpportunity += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorFunctionRemapOpportunityEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertFunction(args.OldFunction), ConvertFunction(args.NewFunction), args.OldILOffset, ConvertCallbackType(args)));
			//managedCallback.OnCreateConnection += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new (, ConvertCallbackType(args)));
			//managedCallback.OnChangeConnection += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new (, ConvertCallbackType(args)));
			//managedCallback.OnDestroyConnection += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new (, ConvertCallbackType(args)));
			managedCallback.OnException2 += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorException2EventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertFrame(args.Frame), args.Offset, args.EventType, (int)args.Flags, ConvertCallbackType(args)));
			managedCallback.OnExceptionUnwind += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorExceptionUnwind2EventArgs(ConvertAppDomain(args), ConvertThread(args), args.EventType, (int)args.Flags, ConvertCallbackType(args)));
			managedCallback.OnFunctionRemapComplete += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorFunctionRemapCompleteEventArgs(ConvertAppDomain(args), ConvertThread(args), ConvertFunction(args.Function), ConvertCallbackType(args)));
			managedCallback.OnMDANotification += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new CorMDAEventArgs(ConvertMda(args.MDA), ConvertThread2(args.Thread), ConvertProcess(args.Controller is CorDebugProcess dp ? dp : ((CorDebugAppDomain)args.Controller).Process), ConvertCallbackType(args)));
			//managedCallback.OnCustomNotification += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new (, ConvertCallbackType(args)));
			//managedCallback.OnBeforeGarbageCollection += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new (, ConvertCallbackType(args)));
			//managedCallback.OnAfterGarbageCollection += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new (, ConvertCallbackType(args)));
			//managedCallback.OnDataBreakpoint += (sender, args) => InternalFireEvent(ConvertCallbackType(args), new(, ConvertCallbackType(args)));

            m_debugger.SetManagedHandler (managedCallback);
    	}            

        /**
         * Helper for invoking events.  Checks to make sure that handlers
         * are hooked up to a handler before the handler is invoked.
         *
         * We want to allow maximum flexibility by our callers.  As such,
         * we don't require that they call <code>e.Controller.Continue</code>,
         * nor do we require that this class call it.  <b>Someone</b> needs
         * to call it, however.
         *
         * Consequently, if an exception is thrown and the process is stopped,
         * the process is continued automatically.
         */

        void InternalFireEvent(ManagedCallbackType callbackType,CorEventArgs e)
        {
            CorProcess owner;
            CorController c = e.Controller;
            Debug.Assert(c!=null);
            if(c is CorProcess)
                owner = (CorProcess)c ;
            else 
            {
                Debug.Assert(c is CorAppDomain);
                owner = (c as CorAppDomain).Process;
            }
            Debug.Assert(owner!=null);
            try 
            {
                owner.DispatchEvent(callbackType,e);
            }
            finally
            {
                if(e.Continue)
                {
                        e.Controller.Continue(false);
                }
            }
        }

        private ICorDebug m_debugger = null;
    } /* class Debugger */


  ////////////////////////////////////////////////////////////////////////////////
    //
    // CorEvent Classes & Corresponding delegates
    //
    ////////////////////////////////////////////////////////////////////////////////

    /**
     * All of the Debugger events make a Controller available (to specify
     * whether or not to continue the program, or to stop, etc.).
     *
     * This serves as the base class for all events used for debugging.
     *
     * NOTE: If you don't want <b>Controller.Continue(false)</b> to be
     * called after event processing has finished, you need to set the
     * <b>Continue</b> property to <b>false</b>.
     */

    public class CorEventArgs : EventArgs
    {
        private CorController m_controller;

        private bool m_continue;


        private ManagedCallbackType m_callbackType;

        private CorThread m_thread;

        public CorEventArgs(CorController controller)
        {
            m_controller = controller;
            m_continue = true;
        }

        public CorEventArgs(CorController controller, ManagedCallbackType callbackType)
        {
            m_controller = controller;
            m_continue = true;
            m_callbackType = callbackType;
        }

        /** The Controller of the current event. */
        public CorController Controller
        {
            get
            {
                return m_controller;
            }
        }

        /** 
         * The default behavior after an event is to Continue processing
         * after the event has been handled.  This can be changed by
         * setting this property to false.
         */
        public virtual bool Continue
        {
            get
            {
                return m_continue;
            }
            set
            {
                m_continue = value;
            }
        }

        /// <summary>
        /// The type of callback that returned this CorEventArgs object.
        /// </summary>
        public ManagedCallbackType CallbackType
        {
            get
            {
                return m_callbackType;
            }
        }

        /// <summary>
        /// The CorThread associated with the callback event that returned
        /// this CorEventArgs object. If here is no such thread, Thread is null.
        /// </summary>
        public CorThread Thread
        {
            get
            {
                return m_thread;
            }
            protected set
            {
                m_thread = value;
            }
        }

    }


    /**
     * This class is used for all events that only have access to the 
     * CorProcess that is generating the event.
     */
    public class CorProcessEventArgs : CorEventArgs
    {
        public CorProcessEventArgs(CorProcess process)
            : base(process)
        {
        }

        public CorProcessEventArgs(CorProcess process, ManagedCallbackType callbackType)
            : base(process, callbackType)
        {
        }

        /** The process that generated the event. */
        public CorProcess Process
        {
            get
            {
                return (CorProcess)Controller;
            }
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.CreateProcess:
                    return "Process Created";
                case ManagedCallbackType.ExitProcess:
                    return "Process Exited";
                case ManagedCallbackType.ControlCTrap:
                    break;
            }
            return base.ToString();
        }
    }


    /**
     * The event arguments for events that contain both a CorProcess
     * and an CorAppDomain.
     */
    public class CorAppDomainEventArgs : CorProcessEventArgs
    {
        private CorAppDomain m_ad;

        public CorAppDomainEventArgs(CorProcess process, CorAppDomain ad)
            : base(process)
        {
            m_ad = ad;
        }

        public CorAppDomainEventArgs(CorProcess process, CorAppDomain ad,
                                      ManagedCallbackType callbackType)
            : base(process, callbackType)
        {
            m_ad = ad;
        }

        /** The AppDomain that generated the event. */
        public CorAppDomain AppDomain
        {
            get
            {
                return m_ad;
            }
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.CreateAppDomain:
                    return "AppDomain Created: " + m_ad.Name;
                case ManagedCallbackType.ExitAppDomain:
                    return "AppDomain Exited: " + m_ad.Name;
            }
            return base.ToString();
        }
    }


    /**
     * The base class for events which take an CorAppDomain as their
     * source, but not a CorProcess.
     */
    public class CorAppDomainBaseEventArgs : CorEventArgs
    {
        public CorAppDomainBaseEventArgs(CorAppDomain ad)
            : base(ad)
        {
        }

        public CorAppDomainBaseEventArgs(CorAppDomain ad, ManagedCallbackType callbackType)
            : base(ad, callbackType)
        {
        }

        public CorAppDomain AppDomain
        {
            get
            {
                return (CorAppDomain)Controller;
            }
        }
    }


    /**
     * Arguments for events dealing with threads.
     */
    public class CorThreadEventArgs : CorAppDomainBaseEventArgs
    {
        public CorThreadEventArgs(CorAppDomain appDomain, CorThread thread)
            : base(appDomain != null ? appDomain : thread.AppDomain)
        {
            Thread = thread;
        }

        public CorThreadEventArgs(CorAppDomain appDomain, CorThread thread,
            ManagedCallbackType callbackType)
            : base(appDomain != null ? appDomain : thread.AppDomain, callbackType)
        {
            Thread = thread;
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.Break:
                    return "Break";
                case ManagedCallbackType.CreateThread:
                    return "Thread Created";
                case ManagedCallbackType.ExitThread:
                    return "Thread Exited";
                case ManagedCallbackType.NameChange:
                    return "Name Changed";
            }
            return base.ToString();
        }
    }


    /**
     * Arguments for events involving breakpoints.
     */
    public class CorBreakpointEventArgs : CorThreadEventArgs
    {
        private CorBreakpoint m_break;

        public CorBreakpointEventArgs(CorAppDomain appDomain,
                                       CorThread thread,
                                       CorBreakpoint managedBreakpoint)
            : base(appDomain, thread)
        {
            m_break = managedBreakpoint;
        }

        public CorBreakpointEventArgs(CorAppDomain appDomain,
                                       CorThread thread,
                                       CorBreakpoint managedBreakpoint,
                                       ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_break = managedBreakpoint;
        }

        /** The breakpoint involved. */
        public CorBreakpoint Breakpoint
        {
            get
            {
                return m_break;
            }
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.Breakpoint)
            {
                return "Breakpoint Hit";
            }
            return base.ToString();
        }
    }


    /**
     * Arguments for when a Step operation has completed.
     */
    public class CorStepCompleteEventArgs : CorThreadEventArgs
    {
        private CorStepper m_stepper;
        private CorDebugStepReason m_stepReason;

        [CLSCompliant(false)]
        public CorStepCompleteEventArgs(CorAppDomain appDomain, CorThread thread,
                                         CorStepper stepper, CorDebugStepReason stepReason)
            : base(appDomain, thread)
        {
            m_stepper = stepper;
            m_stepReason = stepReason;
        }

        [CLSCompliant(false)]
        public CorStepCompleteEventArgs(CorAppDomain appDomain, CorThread thread,
                                         CorStepper stepper, CorDebugStepReason stepReason,
                                         ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_stepper = stepper;
            m_stepReason = stepReason;
        }

        public CorStepper Stepper
        {
            get
            {
                return m_stepper;
            }
        }

        [CLSCompliant(false)]
        public CorDebugStepReason StepReason
        {
            get
            {
                return m_stepReason;
            }
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.StepComplete)
            {
                return "Step Complete";
            }
            return base.ToString();
        }
    }


    /**
     * For events dealing with exceptions.
     */
    public class CorExceptionEventArgs : CorThreadEventArgs
    {
        bool m_unhandled;

        public CorExceptionEventArgs(CorAppDomain appDomain,
                                      CorThread thread,
                                      bool unhandled)
            : base(appDomain, thread)
        {
            m_unhandled = unhandled;
        }

        public CorExceptionEventArgs(CorAppDomain appDomain,
                                      CorThread thread,
                                      bool unhandled,
                                      ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_unhandled = unhandled;
        }

        /** Has the exception been handled yet? */
        public bool Unhandled
        {
            get
            {
                return m_unhandled;
            }
        }
    }


    /**
     * For events dealing the evaluation of something...
     */
    public class CorEvalEventArgs : CorThreadEventArgs
    {
        CorEval m_eval;

        public CorEvalEventArgs(CorAppDomain appDomain, CorThread thread,
                                 CorEval eval)
            : base(appDomain, thread)
        {
            m_eval = eval;
        }

        public CorEvalEventArgs(CorAppDomain appDomain, CorThread thread,
                                 CorEval eval, ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_eval = eval;
        }

        /** The object being evaluated. */
        public CorEval Eval
        {
            get
            {
                return m_eval;
            }
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.EvalComplete:
                    return "Eval Complete";
                case ManagedCallbackType.EvalException:
                    return "Eval Exception";
            }
            return base.ToString();
        }
    }


    /**
     * For events dealing with module loading/unloading.
     */
    public class CorModuleEventArgs : CorAppDomainBaseEventArgs
    {
        CorModule m_managedModule;

        public CorModuleEventArgs(CorAppDomain appDomain, CorModule managedModule)
            : base(appDomain)
        {
            m_managedModule = managedModule;
        }

        public CorModuleEventArgs(CorAppDomain appDomain, CorModule managedModule,
            ManagedCallbackType callbackType)
            : base(appDomain, callbackType)
        {
            m_managedModule = managedModule;
        }

        public CorModule Module
        {
            get
            {
                return m_managedModule;
            }
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.LoadModule:
                    return "Module loaded: " + m_managedModule.Name;
                case ManagedCallbackType.UnloadModule:
                    return "Module unloaded: " + m_managedModule.Name;
            }
            return base.ToString();
        }
    }


    /**
     * For events dealing with class loading/unloading.
     */
    public class CorClassEventArgs : CorAppDomainBaseEventArgs
    {
        CorClass m_class;

        public CorClassEventArgs(CorAppDomain appDomain, CorClass managedClass)
            : base(appDomain)
        {
            m_class = managedClass;
        }

        public CorClassEventArgs(CorAppDomain appDomain, CorClass managedClass,
            ManagedCallbackType callbackType)
            : base(appDomain, callbackType)
        {
            m_class = managedClass;
        }

        public CorClass Class
        {
            get
            {
                return m_class;
            }
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.LoadClass:
                    return "Class loaded: " + m_class;
                case ManagedCallbackType.UnloadClass:
                    return "Class unloaded: " + m_class;
            }
            return base.ToString();
        }
    }


    /**
     * For events dealing with debugger errors.
     */
    public class CorDebuggerErrorEventArgs : CorProcessEventArgs
    {
        int m_hresult;
        int m_errorCode;

        public CorDebuggerErrorEventArgs(CorProcess process, int hresult,
                                          int errorCode)
            : base(process)
        {
            m_hresult = hresult;
            m_errorCode = errorCode;
        }

        public CorDebuggerErrorEventArgs(CorProcess process, int hresult,
                                          int errorCode, ManagedCallbackType callbackType)
            : base(process, callbackType)
        {
            m_hresult = hresult;
            m_errorCode = errorCode;
        }

        public int HResult
        {
            get
            {
                return m_hresult;
            }
        }

        public int ErrorCode
        {
            get
            {
                return m_errorCode;
            }
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.DebuggerError)
            {
                return "Debugger Error";
            }
            return base.ToString();
        }
    }


    /**
     * For events dealing with Assemblies.
     */
    public class CorAssemblyEventArgs : CorAppDomainBaseEventArgs
    {
        private CorAssembly m_assembly;
        public CorAssemblyEventArgs(CorAppDomain appDomain,
                                     CorAssembly assembly)
            : base(appDomain)
        {
            m_assembly = assembly;
        }

        public CorAssemblyEventArgs(CorAppDomain appDomain,
                                     CorAssembly assembly, ManagedCallbackType callbackType)
            : base(appDomain, callbackType)
        {
            m_assembly = assembly;
        }

        /** The Assembly of interest. */
        public CorAssembly Assembly
        {
            get
            {
                return m_assembly;
            }
        }

        public override string ToString()
        {
            switch (CallbackType)
            {
                case ManagedCallbackType.LoadAssembly:
                    return "Assembly loaded: " + m_assembly.Name;
                case ManagedCallbackType.UnloadAssembly:
                    return "Assembly unloaded: " + m_assembly.Name;
            }
            return base.ToString();
        }
    }


    /**
     * For events dealing with logged messages.
     */
    public class CorLogMessageEventArgs : CorThreadEventArgs
    {
        int m_level;
        string m_logSwitchName;
        string m_message;

        public CorLogMessageEventArgs(CorAppDomain appDomain, CorThread thread,
                                       int level, string logSwitchName, string message)
            : base(appDomain, thread)
        {
            m_level = level;
            m_logSwitchName = logSwitchName;
            m_message = message;
        }

        public CorLogMessageEventArgs(CorAppDomain appDomain, CorThread thread,
                                       int level, string logSwitchName, string message,
                                       ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_level = level;
            m_logSwitchName = logSwitchName;
            m_message = message;
        }

        public int Level
        {
            get
            {
                return m_level;
            }
        }

        public string LogSwitchName
        {
            get
            {
                return m_logSwitchName;
            }
        }

        public string Message
        {
            get
            {
                return m_message;
            }
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.LogMessage)
            {
                return "Log message(" + m_logSwitchName + ")";
            }
            return base.ToString();
        }
    }


    /**
     * For events dealing with logged messages.
     */
    public class CorLogSwitchEventArgs : CorThreadEventArgs
    {
        int m_level;

        int m_reason;

        string m_logSwitchName;

        string m_parentName;

        public CorLogSwitchEventArgs(CorAppDomain appDomain, CorThread thread,
                                      int level, int reason, string logSwitchName, string parentName)
            : base(appDomain, thread)
        {
            m_level = level;
            m_reason = reason;
            m_logSwitchName = logSwitchName;
            m_parentName = parentName;
        }

        public CorLogSwitchEventArgs(CorAppDomain appDomain, CorThread thread,
                                      int level, int reason, string logSwitchName, string parentName,
                                      ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_level = level;
            m_reason = reason;
            m_logSwitchName = logSwitchName;
            m_parentName = parentName;
        }

        public int Level
        {
            get
            {
                return m_level;
            }
        }

        public int Reason
        {
            get
            {
                return m_reason;
            }
        }

        public string LogSwitchName
        {
            get
            {
                return m_logSwitchName;
            }
        }

        public string ParentName
        {
            get
            {
                return m_parentName;
            }
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.LogSwitch)
            {
                return "Log Switch" + "\n" +
                    "Level: " + m_level + "\n" +
                    "Log Switch Name: " + m_logSwitchName;
            }
            return base.ToString();
        }
    }


    /**
     * For events dealing with MDA messages.
     */
    public class CorMDAEventArgs : CorProcessEventArgs
    {
        // Thread may be null.
        public CorMDAEventArgs(CorMDA mda, CorThread thread, CorProcess proc)
            : base(proc)
        {
            m_mda = mda;
            Thread = thread;
            //m_proc = proc;
        }

        public CorMDAEventArgs(CorMDA mda, CorThread thread, CorProcess proc,
            ManagedCallbackType callbackType)
            : base(proc, callbackType)
        {
            m_mda = mda;
            Thread = thread;
            //m_proc = proc;
        }

        CorMDA m_mda;
        public CorMDA MDA { get { return m_mda; } }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.MDANotification)
            {
                return "MDANotification" + "\n" +
                    "Name=" + m_mda.Name + "\n" +
                    "XML=" + m_mda.XML;
            }
            return base.ToString();
        }

        //CorProcess m_proc;
        //CorProcess Process { get { return m_proc; } }
    }


    /**
     * For events dealing module symbol updates.
     */
    public class CorUpdateModuleSymbolsEventArgs : CorModuleEventArgs
    {
        ComStream m_stream;

        [CLSCompliant(false)]
        public CorUpdateModuleSymbolsEventArgs(CorAppDomain appDomain,
                                                CorModule managedModule,
                                                ComStream stream)
            : base(appDomain, managedModule)
        {
            m_stream = stream;
        }

        [CLSCompliant(false)]
        public CorUpdateModuleSymbolsEventArgs(CorAppDomain appDomain,
                                                CorModule managedModule,
                                                ComStream stream,
                                                ManagedCallbackType callbackType)
            : base(appDomain, managedModule, callbackType)
        {
            m_stream = stream;
        }

        [CLSCompliant(false)]
        public ComStream Stream
        {
            get
            {
                return m_stream;
            }
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.UpdateModuleSymbols)
            {
                return "Module Symbols Updated";
            }
            return base.ToString();
        }
    }

    public sealed class CorExceptionInCallbackEventArgs : CorEventArgs
    {
        public CorExceptionInCallbackEventArgs(CorController controller, Exception exceptionThrown)
            : base(controller)
        {
            m_exceptionThrown = exceptionThrown;
        }

        public CorExceptionInCallbackEventArgs(CorController controller, Exception exceptionThrown,
            ManagedCallbackType callbackType)
            : base(controller, callbackType)
        {
            m_exceptionThrown = exceptionThrown;
        }

        public Exception ExceptionThrown
        {
            get
            {
                return m_exceptionThrown;
            }
        }

        public override string ToString()
        {
            if (CallbackType == CorDebugger.ManagedCallbackType_ExceptionInCallback)
            {
                return "Callback Exception: " + m_exceptionThrown.Message;
            }
            return base.ToString();
        }

        private Exception m_exceptionThrown;
    }


    /**
     * Edit and Continue callbacks
     */
    public class CorEditAndContinueRemapEventArgs : CorThreadEventArgs
    {
        public CorEditAndContinueRemapEventArgs(CorAppDomain appDomain,
                                        CorThread thread,
                                        CorFunction managedFunction,
                                        int accurate)
            : base(appDomain, thread)
        {
            m_managedFunction = managedFunction;
            m_accurate = accurate;
        }

        public CorEditAndContinueRemapEventArgs(CorAppDomain appDomain,
                                        CorThread thread,
                                        CorFunction managedFunction,
                                        int accurate,
                                        ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_managedFunction = managedFunction;
            m_accurate = accurate;
        }

        public CorFunction Function
        {
            get
            {
                return m_managedFunction;
            }
        }

        public bool IsAccurate
        {
            get
            {
                return m_accurate != 0;
            }
        }

        private CorFunction m_managedFunction;
        private int m_accurate;
    }


    public class CorBreakpointSetErrorEventArgs : CorThreadEventArgs
    {
        public CorBreakpointSetErrorEventArgs(CorAppDomain appDomain,
                                        CorThread thread,
                                        CorBreakpoint breakpoint,
                                        int errorCode)
            : base(appDomain, thread)
        {
            m_breakpoint = breakpoint;
            m_errorCode = errorCode;
        }

        public CorBreakpointSetErrorEventArgs(CorAppDomain appDomain,
                                        CorThread thread,
                                        CorBreakpoint breakpoint,
                                        int errorCode,
                                        ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_breakpoint = breakpoint;
            m_errorCode = errorCode;
        }

        public CorBreakpoint Breakpoint
        {
            get
            {
                return m_breakpoint;
            }
        }

        public int ErrorCode
        {
            get
            {
                return m_errorCode;
            }
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.BreakpointSetError)
            {
                return "Error Setting Breakpoint";
            }
            return base.ToString();
        }

        private CorBreakpoint m_breakpoint;
        private int m_errorCode;
    }


    public sealed class CorFunctionRemapOpportunityEventArgs : CorThreadEventArgs
    {
        public CorFunctionRemapOpportunityEventArgs(CorAppDomain appDomain,
                                           CorThread thread,
                                           CorFunction oldFunction,
                                           CorFunction newFunction,
                                           int oldILoffset
                                           )
            : base(appDomain, thread)
        {
            m_oldFunction = oldFunction;
            m_newFunction = newFunction;
            m_oldILoffset = oldILoffset;
        }

        public CorFunctionRemapOpportunityEventArgs(CorAppDomain appDomain,
                                           CorThread thread,
                                           CorFunction oldFunction,
                                           CorFunction newFunction,
                                           int oldILoffset,
                                           ManagedCallbackType callbackType
                                           )
            : base(appDomain, thread, callbackType)
        {
            m_oldFunction = oldFunction;
            m_newFunction = newFunction;
            m_oldILoffset = oldILoffset;
        }

        public CorFunction OldFunction
        {
            get
            {
                return m_oldFunction;
            }
        }

        public CorFunction NewFunction
        {
            get
            {
                return m_newFunction;
            }
        }

        public int OldILOffset
        {
            get
            {
                return m_oldILoffset;
            }
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.FunctionRemapOpportunity)
            {
                return "Function Remap Opportunity";
            }
            return base.ToString();
        }

        private CorFunction m_oldFunction, m_newFunction;
        private int m_oldILoffset;
    }

    public sealed class CorFunctionRemapCompleteEventArgs : CorThreadEventArgs
    {
        public CorFunctionRemapCompleteEventArgs(CorAppDomain appDomain,
                                           CorThread thread,
                                           CorFunction managedFunction
                                           )
            : base(appDomain, thread)
        {
            m_managedFunction = managedFunction;
        }

        public CorFunctionRemapCompleteEventArgs(CorAppDomain appDomain,
                                           CorThread thread,
                                           CorFunction managedFunction,
                                           ManagedCallbackType callbackType
                                           )
            : base(appDomain, thread, callbackType)
        {
            m_managedFunction = managedFunction;
        }

        public CorFunction Function
        {
            get
            {
                return m_managedFunction;
            }
        }

        private CorFunction m_managedFunction;
    }


    public class CorExceptionUnwind2EventArgs : CorThreadEventArgs
    {

        [CLSCompliant(false)]
        public CorExceptionUnwind2EventArgs(CorAppDomain appDomain, CorThread thread,
                                            CorDebugExceptionUnwindCallbackType eventType,
                                            int flags)
            : base(appDomain, thread)
        {
            m_eventType = eventType;
            m_flags = flags;
        }

        [CLSCompliant(false)]
        public CorExceptionUnwind2EventArgs(CorAppDomain appDomain, CorThread thread,
                                            CorDebugExceptionUnwindCallbackType eventType,
                                            int flags,
                                            ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_eventType = eventType;
            m_flags = flags;
        }

        [CLSCompliant(false)]
        public CorDebugExceptionUnwindCallbackType EventType
        {
            get
            {
                return m_eventType;
            }
        }

        public int Flags
        {
            get
            {
                return m_flags;
            }
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.ExceptionUnwind)
            {
                return "Exception unwind\n" +
                    "EventType: " + m_eventType;
            }
            return base.ToString();
        }

        CorDebugExceptionUnwindCallbackType m_eventType;
        int m_flags;
    }


    public class CorException2EventArgs : CorThreadEventArgs
    {

        [CLSCompliant(false)]
        public CorException2EventArgs(CorAppDomain appDomain,
                                      CorThread thread,
                                      CorFrame frame,
                                      int offset,
                                      CorDebugExceptionCallbackType eventType,
                                      int flags)
            : base(appDomain, thread)
        {
            m_frame = frame;
            m_offset = offset;
            m_eventType = eventType;
            m_flags = flags;
        }

        [CLSCompliant(false)]
        public CorException2EventArgs(CorAppDomain appDomain,
                                      CorThread thread,
                                      CorFrame frame,
                                      int offset,
                                      CorDebugExceptionCallbackType eventType,
                                      int flags,
                                      ManagedCallbackType callbackType)
            : base(appDomain, thread, callbackType)
        {
            m_frame = frame;
            m_offset = offset;
            m_eventType = eventType;
            m_flags = flags;
        }

        public CorFrame Frame
        {
            get
            {
                return m_frame;
            }
        }

        public int Offset
        {
            get
            {
                return m_offset;
            }
        }

        [CLSCompliant(false)]
        public CorDebugExceptionCallbackType EventType
        {
            get
            {
                return m_eventType;
            }
        }

        public int Flags
        {
            get
            {
                return m_flags;
            }
        }

        public override string ToString()
        {
            if (CallbackType == ManagedCallbackType.Exception2)
            {
                return "Exception Thrown";
            }
            return base.ToString();
        }

        CorFrame m_frame;
        int m_offset;
        CorDebugExceptionCallbackType m_eventType;
        int m_flags;
    }
} /* namespace */
