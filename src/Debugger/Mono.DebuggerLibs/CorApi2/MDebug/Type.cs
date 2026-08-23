//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;

using ClrDebug;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public sealed class CorType : WrapperBase
    {
        internal CorDebugType m_type;
        
        internal CorType (CorDebugType type)
            : base(type)
        {
            m_type = type;
        }

        internal CorDebugType GetInterface ()
        {
            return m_type;
        }


        /** Element type of the type. */
        public CorElementType Type
        {
            get 
            {
                return m_type.Type;
            }
        }

        /** Class of the type */
        public CorClass Class
        {
            get 
            {
                CorDebugClass c = m_type.Class;
                return c==null?null:new CorClass (c);
            }
        }

        public int Rank
        {
            get 
            {
                return m_type.Rank;
            }
        }

        // Provide the first CorType parameter in the TypeParameters collection.
        // This is a convenience operator.
        public CorType FirstTypeParameter
        {
            get
            {
                CorDebugType dt = m_type.FirstTypeParameter;
                return dt==null?null:new CorType (dt);
            }
        }

        public CorType Base
        {
            get
            {
                CorDebugType dt = m_type.Base;
                return dt==null?null:new CorType (dt);
            }
        }

        public CorValue GetStaticFieldValue(mdFieldDef fieldToken, CorFrame frame)
        {
            CorDebugValue dv = m_type.GetStaticFieldValue(fieldToken, frame.m_frame.Raw);
            return dv==null?null:new CorValue (dv);
        }

		// [Xamarin] Expression evaluator.
        // Expose IEnumerable, which can be used with for-each constructs.
        // This will provide an collection of CorType parameters.
		public CorType[] TypeParameters
        {
            get
            {
            return Array.ConvertAll(m_type.TypeParameters, static x => new CorType(x));
            }
        }
    } /* class Type */
} /* namespace */
