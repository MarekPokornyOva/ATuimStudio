//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Diagnostics;

using ClrDebug;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public abstract class CorBreakpoint : WrapperBase
    {
        [CLSCompliant(false)]
        protected CorBreakpoint(CorDebugBreakpoint managedBreakpoint) : base(managedBreakpoint)
        {
            Debug.Assert(managedBreakpoint!=null);
            m_corBreakpoint = managedBreakpoint;
        }

        public virtual void Activate(bool active)
        {
            m_corBreakpoint.Activate (active);
        }
          
        public virtual bool IsActive 
        { 
            get 
            {
                return m_corBreakpoint.IsActive;
            }
        }

        private CorDebugBreakpoint m_corBreakpoint;
    }
} /* namespace */
