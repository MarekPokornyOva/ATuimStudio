//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;

using ClrDebug;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public sealed  class CorClass : WrapperBase
    {
        internal CorDebugClass m_class;
        
        internal CorClass (CorDebugClass managedClass)
            : base(managedClass)
        {
            m_class = managedClass;
        }



        /** The module containing the class */
        public CorModule Module
        {
            get 
            {
                CorDebugModule m = m_class.Module;
                return new CorModule (m);
            }
        }

        /** The metadata typedef token of the class. */
        public mdTypeDef Token
        {
            get 
            {
                return m_class.Token;
            }
        }

        public bool JMCStatus
        {
            set
            {
                (m_class as ICorDebugClass2).SetJMCStatus(value);
            }
        }

        public CorType GetParameterizedType(CorElementType elementType, CorType[] typeArguments)
        {
            ICorDebugType[] types = null;
            int length = 0;
            if (typeArguments != null)
            {
                types = Array.ConvertAll(typeArguments, static x => x.m_type.Raw);
                length = typeArguments.Length;
            }

            CorDebugType pType = m_class.GetParameterizedType(elementType, length, types);
            return pType==null?null:new CorType (pType);
        }

        public CorValue GetStaticFieldValue(int fieldToken, CorFrame managedFrame)
        {
            CorDebugValue pValue = m_class.GetStaticFieldValue(fieldToken, (managedFrame==null)?null:managedFrame.m_frame.Raw);
            return pValue==null?null:new CorValue(pValue);
        }

    } /* class Class */

} /* namespace */
