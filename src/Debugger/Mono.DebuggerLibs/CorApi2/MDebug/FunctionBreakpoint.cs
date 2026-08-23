//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;

using ClrDebug;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public sealed  class CorFunctionBreakpoint : CorBreakpoint
    {
        private CorDebugFunctionBreakpoint m_breakpoint;

        internal CorFunctionBreakpoint (CorDebugFunctionBreakpoint breakpoint) : base(breakpoint)
        {
            m_breakpoint = breakpoint;
        }

        public CorFunction Function
        {
            get
            {
                CorDebugFunction f = m_breakpoint.Function;
                return new CorFunction (f);
            }
        }

        public int Offset
        {
            get 
            {
                int off = m_breakpoint.Offset;
                return off;
            }
        }
    } /* class FunctionBreakpoint */
} /* namespace */
