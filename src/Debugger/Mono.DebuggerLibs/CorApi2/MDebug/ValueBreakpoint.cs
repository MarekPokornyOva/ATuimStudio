//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;

using ClrDebug;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public sealed class CorValueBreakpoint : CorBreakpoint
    {
        private CorDebugValueBreakpoint m_br;
        
        internal CorValueBreakpoint (CorDebugValueBreakpoint breakpoint) : base(breakpoint)
        {
            m_br = breakpoint;
        }

        public CorValue Value
        {
            get 
            {
                CorDebugValue m = m_br.Value;
                return new CorValue (m);
            }
        }
    } /* class ValueBreakpoint */
} /* namespace */
