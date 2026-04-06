//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ClrDebug;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public sealed class CorAppDomain : CorController
    {
        /** Create an CorAppDomain object. */
        internal CorAppDomain (CorDebugAppDomain appDomain)
            : base (appDomain)
        {
        }

        /** Get the ICorDebugAppDomain interface back from the Controller. */
        private CorDebugAppDomain _ad ()
        {
            return (CorDebugAppDomain) GetController();
        }

        /** Get the process containing the CorAppDomain. */
        public CorProcess Process
        {
            get
            {
                return  CorProcess.GetCorProcess (_ad().Process);
            }
        }

        /** Get all Assemblies in the CorAppDomain. */
        public IEnumerable<CorAssembly> Assemblies
        {
            get
            {
                return _ad().Assemblies.Select(static x => new CorAssembly(x));
            }
        }


        /** All active breakpoints in the CorAppDomain */
        public IEnumerable<CorBreakpoint> Breakpoints
        {
            get
            {
                return _ad().Breakpoints.Select<CorDebugBreakpoint, CorBreakpoint>(static x =>
                {
						 if (x is CorDebugFunctionBreakpoint corDebugFunctionBreakpoint)
							 return new CorFunctionBreakpoint(corDebugFunctionBreakpoint);
						 if (x is CorDebugModuleBreakpoint corDebugModuleBreakpoint)
							 return new CorModuleBreakpoint(corDebugModuleBreakpoint);
						 if (x is CorDebugValueBreakpoint corDebugValueBreakpoint)
							 return new CorValueBreakpoint(corDebugValueBreakpoint);
                   throw new NotSupportedException("Unknown breakpoint type.");
					 });
            }
        }

        /** All active steppers in the CorAppDomain */
        public IEnumerable<CorStepper> Steppers
        {
            get
            {
                return _ad().Steppers.Select(static x => new CorStepper(x));
            }
        }

        /** Is the debugger attached to the CorAppDomain? */
        public bool IsAttached ()
        {
            return _ad().IsAttached;
        }

        /** The name of the CorAppDomain */
        public String Name
        {
            get 
            {
                return _ad().Name;
            }
        }

        /** Get the runtime App domain object */
        public CorValue AppDomainVariable
        {
            get
            {
                return new CorValue (_ad().Object);
            }
        }

        /** 
         * Attach the AppDomain to receive all CorAppDomain related events (e.g.
         * load assembly, load module, etc.) in order to debug the AppDomain.
         */
        public void Attach ()
        {
            _ad().Attach ();
        }

        /** Get the ID of this CorAppDomain */
        public int Id
        {
            get
            {
                return _ad().Id;
            }
        }

        /** Returns CorType object for an array of or pointer to the given type */
        public CorType GetArrayOrPointerType(CorElementType elementType, int rank, CorType parameterTypes)
        {
            CorDebugType ct = _ad().GetArrayOrPointerType(elementType, rank, parameterTypes.m_type.Raw);
            return ct==null?null:new CorType (ct);
        }
        
        /** Returns CorType object for a pointer to a function */
        public CorType GetFunctionPointerType(CorType[] parameterTypes)
        {
            ICorDebugType[] types = null;
            int len = 0;
            if (parameterTypes != null)
            {
                types = Array.ConvertAll(parameterTypes, static x => x.m_type.Raw);
                len = types.Length;
            }

            CorDebugType ct = _ad().GetFunctionPointerType(len, types==null ? null : types[0]);
            return ct==null?null:new CorType (ct);
        }

    }
}
