//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ClrDebug;
using ManagedCallbackType = ClrDebug.CorDebugManagedCallbackKind;

namespace Microsoft.Samples.Debugging.CorDebug
{
    /** A process running some managed code. */
    public sealed class CorProcess : CorController, IDisposable
    {
        [CLSCompliant(false)]
	     public static CorProcess GetCorProcess(CorDebugProcess process)
        {
            Debug.Assert(process!=null);
            return m_instances.GetOrAdd(process, static proc => new CorProcess(proc));
        }

	     [CLSCompliant(false)]
	     public static CorProcess GetCorProcess(ICorDebugProcess process)
		      => GetCorProcess(new CorDebugProcess(process));


        public void Dispose()
        {
            // Release event handlers. The event handlers are strong references and may keep
            // other high-level objects (such as things in the MdbgEngine layer) alive.
            m_callbacksArray = null;            
            
            // Remove ourselves from instances hash.
            lock(m_instances) 
            {
                m_instances.TryRemove(_p(), out _);
            }
        }

        private CorProcess (CorDebugProcess process)
            : base (process)
        {
            InitCallbacks ();
        }

        private void InitCallbacks ()
        {
            m_callbacksArray = new Dictionary<ManagedCallbackType, DebugEventHandler<CorEventArgs>> {
                {ManagedCallbackType.Breakpoint, (sender, args) => OnBreakpoint (sender, (CorBreakpointEventArgs) args)},
                {ManagedCallbackType.StepComplete, (sender, args) => OnStepComplete (sender, (CorStepCompleteEventArgs) args)},
                {ManagedCallbackType.Break, (sender, args) => OnBreak (sender, (CorThreadEventArgs) args)},
                {ManagedCallbackType.Exception, (sender, args) => OnException (sender, (CorExceptionEventArgs) args)},
                {ManagedCallbackType.EvalComplete, (sender, args) => OnEvalComplete (sender, (CorEvalEventArgs) args)},
                {ManagedCallbackType.EvalException, (sender, args) => OnEvalException (sender, (CorEvalEventArgs) args)},
                {ManagedCallbackType.CreateProcess, (sender, args) => OnCreateProcess (sender, (CorProcessEventArgs) args)},
                {ManagedCallbackType.ExitProcess, (sender, args) => OnProcessExit (sender, (CorProcessEventArgs) args)},
                {ManagedCallbackType.CreateThread, (sender, args) => OnCreateThread (sender, (CorThreadEventArgs) args)},
                {ManagedCallbackType.ExitThread, (sender, args) => OnThreadExit (sender, (CorThreadEventArgs) args)},
                {ManagedCallbackType.LoadModule, (sender, args) => OnModuleLoad (sender, (CorModuleEventArgs) args)},
                {ManagedCallbackType.UnloadModule, (sender, args) => OnModuleUnload (sender, (CorModuleEventArgs) args)},
                {ManagedCallbackType.LoadClass, (sender, args) => OnClassLoad (sender, (CorClassEventArgs) args)},
                {ManagedCallbackType.UnloadClass, (sender, args) => OnClassUnload (sender, (CorClassEventArgs) args)},
                {ManagedCallbackType.DebuggerError, (sender, args) => OnDebuggerError (sender, (CorDebuggerErrorEventArgs) args)},
                {ManagedCallbackType.LogMessage, (sender, args) => OnLogMessage (sender, (CorLogMessageEventArgs) args)},
                {ManagedCallbackType.LogSwitch, (sender, args) => OnLogSwitch (sender, (CorLogSwitchEventArgs) args)},
                {ManagedCallbackType.CreateAppDomain, (sender, args) => OnCreateAppDomain (sender, (CorAppDomainEventArgs) args)},
                {ManagedCallbackType.ExitAppDomain, (sender, args) => OnAppDomainExit (sender, (CorAppDomainEventArgs) args)},
                {ManagedCallbackType.LoadAssembly, (sender, args) => OnAssemblyLoad (sender, (CorAssemblyEventArgs) args)},
                {ManagedCallbackType.UnloadAssembly, (sender, args) => OnAssemblyUnload (sender, (CorAssemblyEventArgs) args)},
                {ManagedCallbackType.ControlCTrap, (sender, args) => OnControlCTrap (sender, (CorProcessEventArgs) args)},
                {ManagedCallbackType.NameChange, (sender, args) => OnNameChange (sender, (CorThreadEventArgs) args)},
                {ManagedCallbackType.UpdateModuleSymbols, (sender, args) => OnUpdateModuleSymbols (sender, (CorUpdateModuleSymbolsEventArgs) args)},
                {ManagedCallbackType.FunctionRemapOpportunity, (sender, args) => OnFunctionRemapOpportunity (sender, (CorFunctionRemapOpportunityEventArgs) args)},
                {ManagedCallbackType.FunctionRemapComplete, (sender, args) => OnFunctionRemapComplete (sender, (CorFunctionRemapCompleteEventArgs) args)},
                {ManagedCallbackType.BreakpointSetError, (sender, args) => OnBreakpointSetError (sender, (CorBreakpointEventArgs) args)},
                {ManagedCallbackType.Exception2, (sender, args) => OnException2 (sender, (CorException2EventArgs) args)},
                {ManagedCallbackType.ExceptionUnwind, (sender, args) => OnExceptionUnwind2 (sender, (CorExceptionUnwind2EventArgs) args)},
                {ManagedCallbackType.MDANotification, (sender, args) => OnMDANotification (sender, (CorMDAEventArgs) args)},
                {CorDebugger.ManagedCallbackType_ExceptionInCallback, (sender, args) => OnExceptionInCallback (sender, (CorExceptionInCallbackEventArgs) args)},
            };
        }

        private static ConcurrentDictionary<CorDebugProcess, CorProcess> m_instances = new ConcurrentDictionary<CorDebugProcess, CorProcess>();

        private CorDebugProcess _p ()
        {
            return (CorDebugProcess) GetController();
        }



        /** The OS ID of the process. */
        public int Id
        {
            get 
            {
                return _p().Id;
            }
        }

        /** Returns a handle to the process. */
        public IntPtr Handle
        {
            get 
            {
                return _p().Handle;
            }
        }

        public Version Version
        {
            get 
            {
                var cv = _p().Version;
                return new Version(cv.dwMajor,cv.dwMinor,cv.dwBuild,cv.dwSubBuild);
            }
        }

        /** All managed objects in the process. */
        public IReadOnlyCollection<CORDB_ADDRESS> Objects
        {
            get 
            {
                return _p().Objects;
            }
        }

        /** Is the address inside a transition stub? */
        public bool IsTransitionStub (CORDB_ADDRESS address)
        {
            return _p().IsTransitionStub (address);
        }

        /** Has the thread been suspended? */
        public bool IsOSSuspended (int tid)
        {
            return _p().IsOSSuspended (tid);
        }

        /** Gets managed thread for threadId. 
         * Returns NULL if tid is not a managed thread. That's very common in interop-debugging cases.
         */
        public CorThread GetThread(int threadId)
        {
            CorDebugThread thread = null;
            try
            {
                thread = _p().GetThread(threadId);
            }
            catch (ArgumentException)
            {
            }
            return (thread == null) ? null : (new CorThread(thread));
        }

        /* Get the context for the given thread. */
        // See WIN32_CONTEXT structure declared in context.il
        public void GetThreadContext ( int threadId, IntPtr contextPtr, int context_size )
        {

            _p().GetThreadContext( threadId,  context_size, contextPtr );
            return;
        }

        /* Set the context for a given thread. */
        public void SetThreadContext (int threadId, IntPtr contextPtr, int context_size)
        {
            _p().SetThreadContext( threadId,  context_size, contextPtr );
        }

        /** Read memory from the process. */
        public long ReadMemory (CORDB_ADDRESS address, byte[] buffer)
        {
            Debug.Assert(buffer!=null);
		      using (AutoPinner ap = new AutoPinner(buffer))
			      return _p().ReadMemory(address, buffer.Length, ap);
        }

        /** Write memory in the process. */
        public long WriteMemory (CORDB_ADDRESS address, byte[] buffer)
        {
		      using (AutoPinner ap = new AutoPinner(buffer))
			      return _p().WriteMemory(address, buffer.Length, ap);
        }

        /** Clear the current unmanaged exception on the given thread. */
        public void ClearCurrentException (int threadId)
        {
            _p().ClearCurrentException (threadId);
        }

        /** enable/disable sending of log messages to the debugger for logging. */
        public void EnableLogMessages (bool value)
        {
            _p().EnableLogMessages (value);
        }

        /** Modify the specified switches severity level */
        public void ModifyLogSwitch (String name, int level)
        {
            _p().ModifyLogSwitch (name,level);
        }

        /** All appdomains in the process. */
        public IEnumerable<CorAppDomain> AppDomains
        {
            get
            {
                return _p().AppDomains.Select(static x => new CorAppDomain(x));
            }
        }

        /** Get the runtime proces object. */
        public CorValue ProcessVariable
        {
            get
            {
                return new CorValue (_p().Object);
            }
        }

        /** These flags set things like TrackJitInfo, PreventOptimization, IgnorePDBs, and EnableEnC */
        /**  Any combination of bits in this DWORD flag enum is ok, but if its not a valid set, you may get an error */
        public CorDebugJITCompilerFlags DesiredNGENCompilerFlags
        {
            get
            {
                return _p().DesiredNGENCompilerFlags;
            }
            set
            {
                _p().DesiredNGENCompilerFlags = value;
            }
        }

        public CorReferenceValue GetReferenceValueFromGCHandle(IntPtr gchandle)
        {
		return new CorReferenceValue(_p().GetReferenceValueFromGCHandle(gchandle));
        }

        /** get the thread for a cookie. */
        public CorThread ThreadForFiberCookie (int cookie)
        {
            CorDebugThread thread = _p().ThreadForFiberCookie (cookie);
            return (thread==null)?null:(new CorThread (thread));
        }

        /** set a BP in native code */
        public byte[] SetUnmanagedBreakpoint( CORDB_ADDRESS address )
        {
            return _p().SetUnmanagedBreakpoint( address, 1);
        }

        /** clear a previously set BP in native code */
        public void ClearUnmanagedBreakpoint( CORDB_ADDRESS address )
        {
            _p().ClearUnmanagedBreakpoint( address );
        }

        public override void Stop (int timeout)
        {
            _p().Stop (timeout);
        }

        public override void Continue (bool outOfBand)
        {
            if( !outOfBand &&                               // OOB event can arrive anytime (we just ignore them).
                (m_callbackAttachedEvent!=null) )
            {
                // first special call to "Continue" -- this fake continue will start delivering
                // callbacks.
                Debug.Assert( !outOfBand );
                ManualResetEvent ev = m_callbackAttachedEvent;
                // we set the m_callbackAttachedEvent to null first to prevent races.
                m_callbackAttachedEvent = null;
                ev.Set();
            }
            else
                base.Continue(outOfBand);
        }
        
        // when process is first created wait till callbacks are enabled.
        private ManualResetEvent m_callbackAttachedEvent = new ManualResetEvent(false);

        private Dictionary<ManagedCallbackType, DebugEventHandler<CorEventArgs>> m_callbacksArray;
        
        
        internal void DispatchEvent(ManagedCallbackType callback,CorEventArgs e)
        {
            try
            {
                if( m_callbackAttachedEvent!=null )
                    m_callbackAttachedEvent.WaitOne(); // waits till callbacks are enabled
                var d = m_callbacksArray[callback];
                d(this,e);
            }
            catch(Exception ex)
            {
                CorExceptionInCallbackEventArgs e2 = new CorExceptionInCallbackEventArgs(e.Controller,ex);
                Debug.Assert(false,"Exception in callback: "+ex.ToString());
                try 
                {
                    // we need to dispatch the exceptin in callback error, but we cannot
                    // use DispatchEvent since throwing exception in ExceptionInCallback
                    // would lead to infinite recursion.
                    Debug.Assert( m_callbackAttachedEvent==null);
                    var d = m_callbacksArray[CorDebugger.ManagedCallbackType_ExceptionInCallback];
                    d(this, e2);
                } 
                catch(Exception ex2)
                {
                    Debug.Assert(false,"Exception in Exception notification callback: "+ex2.ToString());
                    // ignore it -- there is nothing we can do.
                }
                e.Continue = e2.Continue;
            }
        }

        public event DebugEventHandler<CorBreakpointEventArgs> OnBreakpoint = delegate { };
        public event DebugEventHandler<CorBreakpointEventArgs> OnBreakpointSetError = delegate { };
        public event DebugEventHandler<CorStepCompleteEventArgs> OnStepComplete = delegate { };
        public event DebugEventHandler<CorThreadEventArgs> OnBreak = delegate { };
        public event DebugEventHandler<CorExceptionEventArgs> OnException = delegate { };
        public event DebugEventHandler<CorEvalEventArgs> OnEvalComplete = delegate { };
        public event DebugEventHandler<CorEvalEventArgs> OnEvalException = delegate { };
        public event DebugEventHandler<CorProcessEventArgs> OnCreateProcess = delegate { };
        public event DebugEventHandler<CorProcessEventArgs> OnProcessExit = delegate { };
        public event DebugEventHandler<CorThreadEventArgs> OnCreateThread = delegate { };
        public event DebugEventHandler<CorThreadEventArgs> OnThreadExit = delegate { };
        public event DebugEventHandler<CorModuleEventArgs> OnModuleLoad = delegate { };
        public event DebugEventHandler<CorModuleEventArgs> OnModuleUnload = delegate { };
        public event DebugEventHandler<CorClassEventArgs> OnClassLoad = delegate { };
        public event DebugEventHandler<CorClassEventArgs> OnClassUnload = delegate { };
        public event DebugEventHandler<CorDebuggerErrorEventArgs> OnDebuggerError = delegate { };
        public event DebugEventHandler<CorMDAEventArgs> OnMDANotification = delegate { };
        public event DebugEventHandler<CorLogMessageEventArgs> OnLogMessage = delegate { };
        public event DebugEventHandler<CorLogSwitchEventArgs> OnLogSwitch = delegate { };
        public event DebugEventHandler<CorAppDomainEventArgs> OnCreateAppDomain = delegate { };
        public event DebugEventHandler<CorAppDomainEventArgs> OnAppDomainExit = delegate { };
        public event DebugEventHandler<CorAssemblyEventArgs> OnAssemblyLoad = delegate { };
        public event DebugEventHandler<CorAssemblyEventArgs> OnAssemblyUnload = delegate { };
        public event DebugEventHandler<CorProcessEventArgs> OnControlCTrap = delegate { };
        public event DebugEventHandler<CorThreadEventArgs> OnNameChange = delegate { };
        public event DebugEventHandler<CorUpdateModuleSymbolsEventArgs> OnUpdateModuleSymbols = delegate { };
        public event DebugEventHandler<CorFunctionRemapOpportunityEventArgs> OnFunctionRemapOpportunity = delegate { };
        public event DebugEventHandler<CorFunctionRemapCompleteEventArgs> OnFunctionRemapComplete = delegate { };
        public event DebugEventHandler<CorException2EventArgs> OnException2 = delegate { };
        public event DebugEventHandler<CorExceptionUnwind2EventArgs> OnExceptionUnwind2 = delegate { };
        public event DebugEventHandler<CorExceptionInCallbackEventArgs> OnExceptionInCallback = delegate { };

    }

    public delegate void DebugEventHandler<in TArgs> (Object sender, TArgs args) where TArgs : CorEventArgs;

/* class Process */
} /* namespace */
