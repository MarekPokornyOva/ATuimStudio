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
    /** 
     * collects functionality which requires runnint code inside the debuggee.
     */
    public sealed  class CorEval : WrapperBase
    {
        private CorDebugEval m_eval;

        internal CorEval (CorDebugEval e)
            :base(e)
        {
            m_eval = e;
        }



        public void CallFunction (CorFunction managedFunction, CorValue[] arguments)
        {
            ICorDebugValue[] values = arguments == null
               ? null
               : Array.ConvertAll(arguments, static x => x.m_val.Raw);
            m_eval.CallFunction(managedFunction.m_function.Raw,
                                (values == null?0: values.Length),
                                values);
        }

        public void CallParameterizedFunction (CorFunction managedFunction, CorType[] argumentTypes, CorValue[] arguments)
        {
            ICorDebugType[] types = null;
            int typesLength = 0;
            ICorDebugValue[] values = null;
            int valuesLength = 0;
            
            if (argumentTypes != null)
            {
                types = Array.ConvertAll(argumentTypes, static x => x.m_type.Raw);
                typesLength = types.Length;
            }
            if (arguments != null)
            {
                values = Array.ConvertAll(arguments, static x => x.m_val.Raw);
                valuesLength = values.Length;
            }
            m_eval.CallParameterizedFunction(managedFunction.m_function.Raw, typesLength, types, valuesLength, values);
        }

        public CorValue CreateValueForType(CorType type)
        {
            CorDebugValue val = m_eval.CreateValueForType(type.m_type.Raw);
            return val==null?null:new CorValue (val);
        }

        public void NewParameterizedObject(CorFunction managedFunction, CorType[] argumentTypes, CorValue[] arguments)
        {
    
            ICorDebugType[] types = null;
            int typesLength = 0;
            ICorDebugValue[] values = null;
            int valuesLength = 0;

            if (argumentTypes != null)
            {
                types = Array.ConvertAll(argumentTypes, static x => x.m_type.Raw);
                typesLength = types.Length;
            }
            if (arguments != null)
            {
                values = Array.ConvertAll(arguments, static x => x.m_val.Raw);
                valuesLength = values.Length;
            }
            m_eval.NewParameterizedObject(managedFunction.m_function.Raw, typesLength, types, valuesLength, values);
        }

        public void NewParameterizedObjectNoConstructor(CorClass managedClass, CorType[] argumentTypes)
        {
            ICorDebugType[] types = null;
            int typesLength=0;
            if (argumentTypes != null)
            {
                types = Array.ConvertAll(argumentTypes, static x => x.m_type.Raw);
                typesLength = types.Length;
            }
            m_eval.NewParameterizedObjectNoConstructor(managedClass.m_class.Raw, typesLength, types);
        }

        public void NewParameterizedArray(CorType type, int rank, int[] dims, int[] lowBounds)
        {
            m_eval.NewParameterizedArray(type.m_type.Raw, rank, dims, lowBounds);
        }


        /** Create an object w/o invoking its constructor. */
        public void NewObjectNoContstructor (CorClass c)
        {
            m_eval.NewObjectNoConstructor (c.m_class.Raw);
        }

        /** allocate a string w/ the given contents. */
        public void NewString (string value)
        {
            m_eval.NewString (value);
        }

        public void NewArray (CorElementType type, CorClass managedClass, int rank, 
                              int[] dimensions, int[] lowBounds)
        {
            m_eval.NewArray (type, managedClass.m_class.Raw, rank, dimensions, lowBounds);
        }

        /** Does the Eval have an active computation? */
        public bool IsActive ()
        {
            return m_eval.IsActive;
        }

        /** Abort the current computation. */
        public void Abort ()
        {
            m_eval.Abort ();
        }

        /** Rude abort the current computation. */
        public void RudeAbort ()
        {
            ICorDebugEval2 eval2 = (ICorDebugEval2) m_eval;
            eval2.RudeAbort ();
        }

        /** Result of the evaluation.  Valid only after the eval is complete. */
        public CorValue Result
        {
            get
            {
                CorDebugValue v = m_eval.Result;
                return (v==null)?null:new CorValue (v);
            }
        }

        /** The thread that this eval was created in. */
        public CorThread Thread
        {
            get
            {
                CorDebugThread t = m_eval.Thread;
                return (t==null)?null:new CorThread (t);
            }
        }

        /** Create a Value to use it in a Function Evaluation. */
        public CorValue CreateValue (CorElementType type, CorClass managedClass)
        {
            CorDebugValue v = m_eval.CreateValue (type, managedClass==null?null:managedClass.m_class.Raw);
            return (v==null)?null:new CorValue (v);
        }
    } /* class Eval */
} /* namespace */
