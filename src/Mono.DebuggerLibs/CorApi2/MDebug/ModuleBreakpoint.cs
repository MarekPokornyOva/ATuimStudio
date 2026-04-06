//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;

using ClrDebug;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public sealed class CorModuleBreakpoint : CorBreakpoint
    {
        private CorDebugModuleBreakpoint m_br;
          
        internal CorModuleBreakpoint (CorDebugModuleBreakpoint managedModule): base(managedModule)
        {
            m_br = managedModule;
        }
          
        public CorModule Module
        {
            get 
            {
                CorDebugModule m = m_br.Module;
                return new CorModule (m);
            }
        }
    } /* class ModuleBreakpoint */
} /* namespace */
