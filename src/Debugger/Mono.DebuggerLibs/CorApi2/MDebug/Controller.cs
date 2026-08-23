//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using ClrDebug;

namespace Microsoft.Samples.Debugging.CorDebug
{
    /**
     * Represents a scope at which program execution can be controlled.
     */
    public class CorController : WrapperBase
    {
        internal CorController (CorDebugController controller)
            :base(controller)
        {
            m_controller = controller;
        }

        /**
         * Cooperative stop on all threads running managed code in the process.
         */
        public virtual void Stop (int timeout)
        {
            m_controller.Stop (timeout);
        }

        /**
         * Continue processes after a call to Stop.
         *
         * outOfBand is true if continuing from an unmanaged event that
         * was sent with the outOfBand flag in the unmanaged callback;
         * false if continueing from a managed event or normal unmanaged event.
         */
        public virtual void Continue (bool outOfBand)
        {
            m_controller.Continue (outOfBand);
        }

        /**
         * Are the threads in the process running freely?
         */
        public bool IsRunning ()
        {
            return m_controller.IsRunning;
        }

        /**
         * Are there managed callbacks queued up for the requested thread?
         */
        public bool HasQueuedCallbacks (CorThread managedThread)
        {
            return m_controller.HasQueuedCallbacks(managedThread.GetInterface().Raw);
        }

        /** Enumerate over all threads in active in the process. */
        public IEnumerable<CorThread> Threads
        {
            get 
            {
                return m_controller.Threads.Select(static x => new CorThread(x));
            }
        }

        /**
         * Set the current debug state of each thread.
         */
        [CLSCompliant(false)]
        public void SetAllThreadsDebugState (CorDebugThreadState state, CorThread exceptThis)
        {
            m_controller.SetAllThreadsDebugState (state, exceptThis != null ? exceptThis.GetInterface().Raw : null);
        }

        /** Detach the debugger from the process/appdomain. */
        public void Detach ()
        {
            m_controller.Detach ();
        }
    
        /** Terminate the current process. */
        public void Terminate (int exitCode)
        {
            m_controller.Terminate (exitCode);
        }

        /* Can the delta PEs be applied to the running process? */
        /*
        public IEnumerable CanCommitChanges (uint number, EditAndContinueSnapshot[] snapshots)
        {
            ICorDebugErrorInfoEnum error = null;
            m_controller.CanCommitChanges (number, snapshots, out error);
            if (error == null)
                return null;
            return new ErrorInfoEnumerator (error);
        }
        */

        /* Apply the delta PEs to the running process. */
        /*
        public IEnumerable CommitChanges (uint number, EditAndContinueSnapshot[] snapshots)
        {
            ICorDebugErrorInfoEnum error = null;
            m_controller.CommitChanges (number, snapshots, out error);
            if (error == null)
                return null;
            return new ErrorInfoEnumerator (error);
        }
        */
        [CLSCompliant(false)]
        protected CorDebugController GetController ()
        {
            return m_controller;
        }
        
        private CorDebugController m_controller;
    }
}
