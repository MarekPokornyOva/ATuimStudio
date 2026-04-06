//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ClrDebug;

namespace Microsoft.Samples.Debugging.CorDebug
{
    /**
     * Information about an Assembly being debugged.
     */
    public sealed class CorAssembly : WrapperBase
    {
        private CorDebugAssembly m_asm;

        internal CorAssembly (CorDebugAssembly managedAssembly)
            :base(managedAssembly)
        { 
            m_asm = managedAssembly;
        }


        /** Get the process containing the Assembly. */
        public CorProcess Process
        {
            get 
            {
                return CorProcess.GetCorProcess(m_asm.Process);
            }
        }

        /** Get the AppDomain containing the assembly. */
        public CorAppDomain AppDomain
        {
            get 
            {
                return new CorAppDomain (m_asm.AppDomain);
            }
        }

        /** All the modules in the assembly. */
        public IEnumerable<CorModule> Modules
        {
            get 
            {
                return m_asm.Modules.Select(static x => new CorModule(x));
            }
        }
    
        /** Get the name of the code base used to load the assembly. */
        public String CodeBase
        {
            get 
            {
                return m_asm.CodeBase;
            }
        }

        /** The name of the assembly. */
        public String Name
        {
            get 
            {
                return m_asm.Name;
            }
        }

        public Boolean IsFullyTrusted
        {
            get
            {
                return m_asm.IsFullyTrusted;
            }
        }
    } /* class Assembly */
} /* namespace debugging */
