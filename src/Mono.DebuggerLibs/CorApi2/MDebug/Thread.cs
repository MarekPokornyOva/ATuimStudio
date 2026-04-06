//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Diagnostics;

using ClrDebug;
using System.Runtime.Serialization;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Samples.Debugging.CorDebug
{

    public struct CorActiveFunction
    {
        public int ILoffset
        { 
            get 
            {
                return m_ilOffset; 
            } 
        }
        private int m_ilOffset;
            
        public CorFunction Function
        { 
            get 
            {
                return m_function; 
            } 
        }
        private CorFunction m_function;
            
        public CorModule Module
        { 
            get 
            {
                return m_module; 
            } 
        }
        private CorModule m_module;

        internal CorActiveFunction(int ilOffset,CorFunction managedFunction,CorModule managedModule)
        {
            m_ilOffset = ilOffset;
            m_function = managedFunction;
            m_module = managedModule;
        }
    }

    public enum CorStackWalkType
    {
        PureV3StackWalk,        // true representation of the V3 ICorDebugStackWalk API
        ExtendedV3StackWalk     // V3 ICorDebugStackWalk API with internal frames interleaved
    }

    /** A thread in the debugged process. */
    public sealed class CorThread : WrapperBase
    {
        internal CorThread (CorDebugThread thread)
            :base(thread)
        {
            m_th = thread;
        }

        internal CorDebugThread GetInterface ()
        {
            return m_th;
        }

        
        /** The process that this thread is in. */
        public CorProcess Process
        {
            get 
            {
                return CorProcess.GetCorProcess(m_th.Process);
            }
        }

        /** the OS id of the thread. */
        public int Id
        {
            get 
            {
                return m_th.Id;
            }
        }

        /** The handle of the active part of the thread. */
        public IntPtr Handle
        {
            get 
            {
                return m_th.Handle;
            }
        }

        /** The AppDomain that owns the thread. */
        public CorAppDomain AppDomain
        {
            get 
            {
                return new CorAppDomain (m_th.AppDomain);
            }
        }

        /** Set the current debug state of the thread. */
        [CLSCompliant(false)]
        public CorDebugThreadState DebugState
        {
            get 
            {
                return m_th.DebugState;
            }
            set 
            {
                m_th.DebugState = value;
            }
        }

        /** the user state. */
        [CLSCompliant(false)]
        public CorDebugUserState UserState
        {
            get 
            {
                return m_th.UserState;
            }
        }

        /** the exception object which is currently being thrown by the thread. */
        public CorValue CurrentException
        {
            get 
            {
			       HRESULT hr =m_th.TryGetCurrentException(out var v);
			       if (hr != HRESULT.S_FALSE)
				       hr.ThrowOnNotOK();
			       return v == null ? null! : new CorValue(v);
            }
        }

        /** 
         * Clear the current exception object, preventing it from being thrown.
         */
        public void ClearCurrentException ()
        {
            m_th.ClearCurrentException ();
        }

        /** 
         * Intercept the current exception.
         */
        public void InterceptCurrentException(CorFrame frame)
        {
            m_th.InterceptCurrentException(frame.m_frame.Raw);
        }

        /** 
         * create a stepper object relative to the active frame in this thread.
         */
        public CorStepper CreateStepper ()
        {
            CorDebugStepper s = m_th.CreateStepper ();
            return new CorStepper (s);
        }

        /** All stack chains in the thread. */
        public IEnumerable<CorChain> Chains
        {
            get 
            {
                return m_th.Chains.Select(static x => new CorChain(x));
            }
        }
        
        /** The most recent chain in the thread, if any. */
        public CorChain ActiveChain
        {
            get 
            {
                CorDebugChain ch = m_th.ActiveChain;
                return ch == null ? null : new CorChain (ch);
            }
        }

        /** Get the active frame. */
        public CorFrame ActiveFrame
        {
            get 
            {
                CorDebugFrame f = m_th.ActiveFrame;
                return f==null ? null : new CorFrame (f);
            }
        }

        /** Get the register set for the active part of the thread. */
        public CorRegisterSet RegisterSet
        {
            get 
            {
                CorDebugRegisterSet r = m_th.RegisterSet;
                return r==null?null:new CorRegisterSet (r);
            }
        }

        /** Creates an evaluation object. */
        public CorEval CreateEval ()
        {
            CorDebugEval e = m_th.CreateEval ();
            return e==null?null:new CorEval (e);
        }

        /** Get the runtime thread object. */
        public CorValue ThreadVariable
        {
            get 
            {
                CorDebugValue v = m_th.Object;
                return new CorValue (v);
            }
        }

        public CorActiveFunction[] GetActiveFunctions()
        {
            return Array.ConvertAll(m_th.ActiveFunctions, static x =>
							new CorActiveFunction(x.ilOffset,
								new CorFunction((CorDebugFunction)x.pFunction),
								x.pModule == null ? null : new CorModule((CorDebugModule)x.pModule)
								)
					);
        }
        private CorDebugThread m_th;

    } /* class Thread */



    public enum CorFrameType
    {
        ILFrame, NativeFrame, InternalFrame,
    }

    
    public sealed class CorFrame : WrapperBase
    {
        internal CorFrame(CorDebugFrame frame)
            :base(frame)
        {
            m_frame = frame;
        }

        
        public CorStepper CreateStepper()
        {
            CorDebugStepper istepper = m_frame.CreateStepper();
            return ( istepper==null ? null : new CorStepper(istepper) );
        }

        public CorFrame Callee
        {
            get 
            {
                CorDebugFrame iframe = m_frame.Callee;
                return ( iframe==null ? null : new CorFrame(iframe) );
            }
        }

        public CorFrame Caller
        {
            get 
            {
                CorDebugFrame iframe = m_frame.Caller;
                return ( iframe==null ? null : new CorFrame(iframe) );
            }
        }

        public CorChain Chain
        {
            get 
            {
                CorDebugChain ichain = m_frame.Chain;
                return ( ichain==null ? null : new CorChain(ichain) );
            }
        }

        public CorCode Code
        {
            get
            {
                CorDebugCode icode = m_frame.Code;
                return ( icode==null ? null : new CorCode(icode) );
            }
        }

        public CorFunction Function
        {
            get 
            {
                CorDebugFunction ifunction = m_frame.Function;
                return ( ifunction==null ? null : new CorFunction(ifunction) );
            }
        }

        public int FunctionToken
        {
            get 
            {
                return m_frame.FunctionToken;
            }
        }

        public CorFrameType FrameType
        {
            get 
            {
                CorDebugILFrame ilframe = GetILFrame();
                if (ilframe != null)
                    return CorFrameType.ILFrame;
                
                CorDebugInternalFrame iframe = GetInternalFrame();
                if (iframe != null)
                    return CorFrameType.InternalFrame;

                return CorFrameType.NativeFrame;
            }
        }
        
        [CLSCompliant(false)]
        public CorDebugInternalFrameType InternalFrameType
        {
            get
            {
                CorDebugInternalFrame iframe = GetInternalFrame();
                
                if(iframe==null)
                    throw new CorException("Cannot get frame type on non-internal frame");
                
                return iframe.FrameType;
            }
        }

    
        [CLSCompliant(false)]
        public void GetStackRange(out CORDB_ADDRESS startOffset,out CORDB_ADDRESS endOffset)
        {
            GetStackRangeResult sr = m_frame.StackRange;
            startOffset = sr.pStart;
            endOffset = sr.pEnd;
        }

        [CLSCompliant(false)]
        public void GetIP(out int offset, out CorDebugMappingResult mappingResult)
        {
            CorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null) 
            {
                offset = 0;
                mappingResult = CorDebugMappingResult.MAPPING_NO_INFO;
            }
            else
            {
                var ip = ilframe.IP;
                offset = ip.pnOffset;
                mappingResult = ip.pMappingResult;
            }
        }

        public void SetIP(int offset)
        {
            CorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                throw new CorException("Cannot set an IP on non-il frame");
            ilframe.SetIP(offset);
        }

        public bool CanSetIP(int offset)
        {
            CorDebugILFrame ilframe = GetILFrame();
            if( ilframe==null )
                return false;
            return ilframe.Raw.CanSetIP(offset)==HRESULT.S_OK;
        }

        public bool CanSetIP(int offset, out HRESULT hresult)
        {
            CorDebugILFrame ilframe = GetILFrame();
            if( ilframe==null )
            {
                hresult = HRESULT.E_FAIL;
                return false;
            }
            hresult = ilframe.Raw.CanSetIP(offset);
            return (hresult==HRESULT.S_OK);
        }

        [CLSCompliant(false)]
        public void GetNativeIP(out int offset)
        {
            CorDebugNativeFrame nativeFrame = m_frame as CorDebugNativeFrame;
            Debug.Assert( nativeFrame!=null );
            offset = nativeFrame.IP;
        }
    
        public CorValue GetLocalVariable(int index)
        {
            CorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                return null;
            
            CorDebugValue value;
            try
            {
                value = ilframe.GetLocalVariable(index);
            }
            catch(System.Runtime.InteropServices.COMException e)
            {
                // If you are stopped in the Prolog, the variable may not be available.
                // CORDBG_E_IL_VAR_NOT_AVAILABLE is returned after dubugee triggers StackOverflowException
                if((HRESULT)e.ErrorCode == HRESULT.CORDBG_E_IL_VAR_NOT_AVAILABLE)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
            return (value==null)?null:new CorValue(value);
        }

        public int GetLocalVariablesCount()
        {
            CorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                return -1;
            return ilframe.LocalVariables.Length;
        }

        public CorValue GetArgument(int index)
        {
            CorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                return null;


            CorDebugValue value = ilframe.GetArgument(index);
            return (value==null)?null:new CorValue(value);
        }

        public int GetArgumentCount()
        {
            CorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                return -1;
            return ilframe.Arguments.Length;
        }

        public void RemapFunction(int newILOffset)
        {
            CorDebugILFrame ilframe = GetILFrame();
            if(ilframe==null)
                throw new CorException("Cannot remap on non-il frame.");
            ilframe.RemapFunction(newILOffset);
        }

        private CorDebugILFrame GetILFrame()
        {
            if(!m_ilFrameCached) 
            {
                m_ilFrameCached = true;               
                m_ilFrame = m_frame as CorDebugILFrame;
                
            }
            return m_ilFrame;
        }

        private CorDebugInternalFrame GetInternalFrame()
        {
            if(!m_iFrameCached) 
            {
                m_iFrameCached = true;
                
                m_iFrame = m_frame as CorDebugInternalFrame;
            }
            return m_iFrame;
        }

        // 'TypeParameters' returns an enumerator that goes yields generic args from
        // both the class and the method. To enumerate just the generic args on the 
        // method, we need to skip past the class args. We have to get that skip value
        // from the metadata. This is a helper function to efficiently get an enumerator that skips
        // to a given spot (likely past the class generic args). 
        public IEnumerable<CorType> GetTypeParamEnumWithSkip(int skip)
        {
            if (skip < 0)
            {
                throw new ArgumentException("Skip parameter must be positive");
            }
            return this.TypeParameters.Skip(skip);
        }
    
        public IEnumerable<CorType> TypeParameters
        {
            get 
            {
                CorDebugILFrame ilf = GetILFrame();
                
                return ilf.TypeParameters.Select(static x => new CorType(x));
            }
        }


        
        private CorDebugILFrame m_ilFrame = null;
        private bool m_ilFrameCached = false;

        private CorDebugInternalFrame m_iFrame = null;
        private bool m_iFrameCached = false;

        internal CorDebugFrame m_frame;
    }

    public sealed class CorChain : WrapperBase
    {
        internal CorChain(CorDebugChain chain)
            :base(chain)
        {
            m_chain = chain;
        }


        public CorFrame ActiveFrame
        {
            get 
            {
                CorDebugFrame iframe = m_chain.ActiveFrame;
                return ( iframe==null ? null : new CorFrame(iframe) );
            }
        }

        public CorChain Callee
        {
            get 
            {
                CorDebugChain ichain = m_chain.Callee;
                return ( ichain==null ? null : new CorChain(ichain) );
            }
        }
      
        public CorChain Caller
        {
            get 
            {
                CorDebugChain ichain = m_chain.Caller;
                return ( ichain==null ? null : new CorChain(ichain) );
            }
        }
      
        public CorContext Context
        {
            get 
            {
                CorDebugContext icontext = m_chain.Context;
                return ( icontext==null ? null : new CorContext(icontext) );
            }
        }
      
        public CorChain Next
        {
            get 
            {
                CorDebugChain ichain = m_chain.Next;
                return ( ichain==null ? null : new CorChain(ichain) );
            }
        }

        public CorChain Previous
        {
            get 
            {
                CorDebugChain ichain = m_chain.Previous;
                return ( ichain==null ? null : new CorChain(ichain) );
            }
        }

        [CLSCompliant(false)]
        public CorDebugChainReason Reason
        {
            get 
            {
                return m_chain.Reason;
            }
        }

        public CorRegisterSet RegisterSet
        {
            get 
            {
                CorDebugRegisterSet r = m_chain.RegisterSet;
                return r==null?null:new CorRegisterSet (r);
            }
        }
      
        public void GetStackRange(out CORDB_ADDRESS pStart, out CORDB_ADDRESS pEnd)
        {
            GetStackRangeResult sr = m_chain.StackRange;
            pStart = sr.pStart;
            pEnd = sr.pEnd;
        }
      
        public CorThread Thread
        {
            get 
            {
                CorDebugThread ithread = m_chain.Thread;
                return ( ithread==null ? null : new CorThread(ithread) );
            }
        }

        public bool IsManaged
        {
            get 
            {
                return m_chain.IsManaged;
            }
        }

        public IEnumerable<CorFrame> Frames
        {
            get 
            {
                return m_chain.Frames.Select(static x => new CorFrame(x));
            }
        }

        private CorDebugChain m_chain;
    }

    public sealed class CorCode : WrapperBase
    {
        internal CorCode(CorDebugCode code)
            :base(code)
        {
            m_code = code;
        }


        public CorFunctionBreakpoint CreateBreakpoint(int offset)
        {
            CorDebugFunctionBreakpoint ibreakpoint = m_code.CreateBreakpoint(offset);
            return ( ibreakpoint==null ? null : new CorFunctionBreakpoint(ibreakpoint) );
        }

        [CLSCompliant(false)]
        public CORDB_ADDRESS Address
        {
            get 
            {
                return m_code.Address;
            }
        }

        public CorDebugJITCompilerFlags CompilerFlags
        {
            get 
            {
                return m_code.CompilerFlags;
            }
        }
      
        public byte[] GetCode()
        {
            int codeSize = this.Size;
   			return m_code.GetCode(0, codeSize, codeSize);
        }

        [CLSCompliant(false)]
        public CodeChunkInfo[] GetCodeChunks()
        {
            return m_code.CodeChunks;
        }
        
        public CorFunction GetFunction()
        {
            CorDebugFunction ifunction = m_code.Function;
            return ( ifunction==null ? null : new CorFunction(ifunction) );
        }

        public COR_DEBUG_IL_TO_NATIVE_MAP[] GetILToNativeMapping()
        {
            return m_code.ILToNativeMapping;
        }
      
        [CLSCompliant(false)]
        public int Size
        {
            get 
            {
                return m_code.Size;
            }
        }
      
        public int VersionNumber
        {
            get 
            {
                return m_code.VersionNumber;
            }
        }
      
        public bool IsIL
        {
            get 
            {
                return m_code.IsIL;
            }
        }

        private CorDebugCode m_code;
    }

    public sealed class CorFunction : WrapperBase
    {
        internal CorFunction(CorDebugFunction managedFunction)
            :base(managedFunction)
        {
            m_function = managedFunction;
        }


        public CorFunctionBreakpoint CreateBreakpoint()
        {
            CorDebugFunctionBreakpoint ifuncbreakpoint = m_function.CreateBreakpoint();
            return ( ifuncbreakpoint==null ? null : new CorFunctionBreakpoint(ifuncbreakpoint) );
        }
      
        public CorClass Class
        {
            get
            {
                CorDebugClass iclass = m_function.Class;
                return ( iclass==null ? null : new CorClass(iclass) );
            }
        }
      
        
        public CorCode ILCode
        {
            get 
            {
                CorDebugCode icode = m_function.ILCode;
                return ( icode==null ? null : new CorCode(icode) );
            }
        }
      
        public CorCode NativeCode
        {
            get 
            {
                CorDebugCode icode = m_function.NativeCode;
                return ( icode==null ? null : new CorCode(icode) );
            }
        }
        
        
        public CorModule Module
        {
            get 
            {
                CorDebugModule imodule = m_function.Module;
                return ( imodule==null ? null : new CorModule(imodule) );
            }
        }
      
        public mdMethodDef Token
        {
            get 
            {
                return m_function.Token;
            }
        }

        public int Version
        {
            get 
            {
                return m_function.VersionNumber;
            }
        }

        public bool JMCStatus
        {
            get
            {
                return m_function.JMCStatus;
            }
            set
            {
                m_function.JMCStatus = value;
            }
        }
        internal CorDebugFunction m_function;
    }

    public sealed class CorContext : WrapperBase
    {
        internal CorContext(CorDebugContext context)
            :base(context)
        {
            m_context = context;
        }


        
        // Following functions are not implemented
        /*
          void CreateBreakpoint(ref CORDBLib.ICorDebugValueBreakpoint ppBreakpoint);
          void GetAddress(ref UInt64 pAddress);
          void GetClass(ref CORDBLib.ICorDebugClass ppClass);
          void GetContext(ref CORDBLib.ICorDebugContext ppContext);
          void GetFieldValue(CORDBLib.ICorDebugClass pClass, UInt32 fieldDef, ref CORDBLib.ICorDebugValue ppValue);
          void GetManagedCopy(ref Object ppObject);
          void GetSize(ref UInt32 pSize);
          void GetType(ref UInt32 pType);
          void GetVirtualMethod(UInt32 memberRef, ref CORDBLib.ICorDebugFunction ppFunction);
          void IsValueClass(ref Int32 pbIsValueClass);
          void SetFromManagedCopy(object pObject);
        */
        private CorDebugContext m_context;
    }

    [Serializable]
    public class CorException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the CorException.
        /// </summary>
        public CorException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the CorException with the specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CorException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CorException with the specified error message and inner Exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public CorException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the CorException class with serialized data. 
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected CorException(SerializationInfo info, StreamingContext context)
            : base(info,context)
        {
        }
    }

} /* namespace */
