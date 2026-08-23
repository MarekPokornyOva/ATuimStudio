//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using ClrDebug;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public sealed class CorMDA : WrapperBase
    {
        private CorDebugMDA m_mda;
        internal CorMDA(CorDebugMDA mda)
            :base(mda)
        {
            m_mda = mda;
        }

        public CorDebugMDAFlags Flags
        {
            get
            {
                CorDebugMDAFlags flags = default;
                m_mda.GetFlags(flags);
                return flags;
            }
        }

        string m_cachedName = null;
        public string Name        
        {
            get 
            {
                // This is thread safe because even in a race, the loser will just do extra work.
                // but no harm done.
                if (m_cachedName == null)
                {
                    m_cachedName = m_mda.Name;
                }
                return m_cachedName;               
            } // end get
        }

        public string XML
        {
            get 
            {
                return m_mda.XML;
            }            
        }

        public string Description
        {
            get 
            {
                return m_mda.Description;
            }            
        }

        public int OsTid
        {
            get
            {
                return m_mda.OSThreadId;
            }            
        }
    } // end CorMDA

    public sealed class CorModule : WrapperBase
    {
        private CorDebugModule m_module;

        internal CorModule (CorDebugModule managedModule)
            :base(managedModule)
        {
            m_module = managedModule;
        }


        /** The process this module is in. */
        public CorProcess Process
        {
            get
            {
                CorDebugProcess proc = m_module.Process;
                return CorProcess.GetCorProcess (proc);
            }
        }

        /** The base address of this module */
        public CORDB_ADDRESS BaseAddress
        {
            get
            {
                return m_module.BaseAddress;
            }
        }

        /** The assembly this module is in. */
        public CorAssembly Assembly
        {
            get
            {
                CorDebugAssembly a = m_module.Assembly;
                return new CorAssembly (a);
            }
        }

        /** The name of the module. */
        public String Name
        {
            get
            {
                
                return m_module.Name;
            }
        }

        /** These flags set things like TrackJitInfo, PreventOptimization, IgnorePDBs, and EnableEnC
        * The setter here will sometimes not successfully set the EnableEnc bit (0x4) when asked to, and
        * we have hidden this detail from users of this layer.
        * If you are interested in handling this case, simply use the getter to check what the new value is after setting it.
        * If they don't match and no exception was thrown, you may assume that's what happened
        */
        public CorDebugJITCompilerFlags JITCompilerFlags
        {
            get
            {
                return m_module.JITCompilerFlags;
            }
            set
            {
                // ICorDebugModule2.SetJITCompilerFlags can return successful HRESULTS other than S_OK.
                // Since we have asked the COMInterop layer to preservesig, we need to marshal any failing HRESULTS.
                m_module.JITCompilerFlags = value;
            }
        }

        /** This is Debugging support for Type Forwarding */
        public CorAssembly ResolveAssembly(int tkAssemblyRef)
        {
            CorDebugAssembly assm = m_module.ResolveAssembly(tkAssemblyRef);
            return new CorAssembly(assm);
        }

        /** 
         * should the jitter preserve debugging information for methods 
         * in this module?
         */
        public void EnableJitDebugging (bool trackJitInfo, bool allowJitOpts)
        {
            m_module.EnableJITDebugging (trackJitInfo, 
                                      allowJitOpts);
        }

        /** Are ClassLoad callbacks called for this module? */
        public void EnableClassLoadCallbacks (bool value)
        {
            m_module.EnableClassLoadCallbacks (value);
        }

        /** Get the function from the metadata info. */
        public CorFunction GetFunctionFromToken (mdMethodDef functionToken)
        {
            CorDebugFunction corFunction = m_module.GetFunctionFromToken(functionToken);
            return (corFunction==null?null:new CorFunction(corFunction));
        }


        /** get the class from metadata info. */
        public CorClass GetClassFromToken (mdTypeDef classToken)
        {
            CorDebugClass c = m_module.GetClassFromToken (classToken);
            return new CorClass (c);
        }

        /** 
         * create a breakpoint which is triggered when code in the module
         * is executed.
         */
        public CorModuleBreakpoint CreateBreakpoint ()
        {
            CorDebugModuleBreakpoint mbr = m_module.CreateBreakpoint ();
            return new CorModuleBreakpoint (mbr);
        }

        /// <summary>
        /// Typesafe wrapper around GetMetaDataInterface. 
        /// </summary>
        /// <typeparam name="T">type of interface to query for</typeparam>
        /// <returns>interface to the metadata</returns>
        public T GetMetaDataInterface<T>()
        {
            // Ideally, this would be declared as Object to match the unmanaged
            // CorDebug.idl definition; but the managed wrappers we build
            // on import it as an IMetadataImport, so we need to start with
            // that. 
            return m_module.GetMetaDataInterface<T>();
        }
            

        /** Get the token for the module table entry of this object. */
        public mdModule Token
        {
            get
            {
                return m_module.Token;
            }
        }

        /** is this a dynamic module? */
        public bool IsDynamic
        {
            get 
            {
                return m_module.IsDynamic;
            }
        }

		/** is this an InMemory module? */
		public bool IsInMemory
		{
			get {
				return m_module.IsInMemory;
			}
		}


        /** get the value object for the given global variable. */
        public CorValue GetGlobalVariableValue (mdFieldDef fieldToken)
        {
            CorDebugValue v = m_module.GetGlobalVariableValue (fieldToken);
            return new CorValue (v);
        }


        /** The size (in bytes) of the module. */
        public int Size
        {
            get
            {
                return m_module.Size;
            }
        }

        public void ApplyChanges(byte[] deltaMetadata,byte[] deltaIL)
        {
		      using (AutoPinner ap = new AutoPinner(deltaMetadata))
		      using (AutoPinner ap2 = new AutoPinner(deltaIL))
		      	m_module.ApplyChanges(deltaMetadata.Length, ap, deltaIL.Length, ap2);
        }

        public void SetJmcStatus(bool isJustMyCOde,mdToken[] tokens)
        {
            Debug.Assert(tokens==null); 
            m_module.SetJMCStatus(isJustMyCOde, tokens == null ? 0 : tokens.Length, tokens);
        }
    } /* class Module */
} /* namespace */
