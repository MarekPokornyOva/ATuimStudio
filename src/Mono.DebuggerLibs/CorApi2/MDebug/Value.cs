//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Text;
using System.Diagnostics;

using ClrDebug;

namespace Microsoft.Samples.Debugging.CorDebug
{


    /** A value in the remote process. */
    public class CorValue : WrapperBase
    {
        internal CorValue (CorDebugValue value)
            :base(value)
        {
            m_val = value;
        }

        /** The simple type of the value. */
        public CorElementType Type
        {
            get 
            {
                return m_val.Type;
            }
        }

        /** Full runtime type of the object . */
        public CorType ExactType
        {
            get 
            {
				    CorDebugType dt = m_val.ExactType;
                return new CorType (dt);
            }
        }

        /** size of the value (in bytes). */
        public int Size
        {
            get 
            {
                return m_val.Size;
            }
        }

        /** Address of the value in the debuggee process. */
        public CORDB_ADDRESS Address
        {
            get 
            {
                return m_val.Address;
            }
        }

        /** Breakpoint triggered when the value is modified. */
        public CorValueBreakpoint CreateBreakpoint ()
        {
            CorDebugValueBreakpoint bp = m_val.CreateBreakpoint ();
            return new CorValueBreakpoint (bp);
        }

        // casting operations
        public CorReferenceValue CastToReferenceValue()
        {
            if(m_val is CorDebugReferenceValue corDebugReferenceValue)
                return new CorReferenceValue(corDebugReferenceValue);
            else
                return null;
        }

        public CorHandleValue CastToHandleValue()
        {
            if(m_val is CorDebugHandleValue corDebugHandleValue)
                return new CorHandleValue(corDebugHandleValue);
            else
                return null;
        }

        public CorStringValue CastToStringValue()
        {
            return new CorStringValue((CorDebugStringValue)m_val);
        }

        public CorObjectValue CastToObjectValue()
        {
            return new CorObjectValue((CorDebugObjectValue)m_val);
        }

        public CorGenericValue CastToGenericValue()
        {
            if(m_val is CorDebugGenericValue corDebugGenericValue)
                return new CorGenericValue(corDebugGenericValue);
            else
                return null;
        }
        
        public CorBoxValue CastToBoxValue()
        {
            if(m_val is CorDebugBoxValue corDebugBoxValue)
                return new CorBoxValue(corDebugBoxValue);
            else
                return null;
        }

        public CorArrayValue CastToArrayValue()
        {
            if(m_val is CorDebugArrayValue corDebugArrayValue)
                return new CorArrayValue(corDebugArrayValue);
            else
                return null;
        }

        public CorHeapValue CastToHeapValue()
        {
            if(m_val is CorDebugHeapValue corDebugHeapValue)
                return new CorHeapValue(corDebugHeapValue);
            else
                return null;
        }

        internal CorDebugValue  m_val=null;

    } /* class Value */


    public class CorReferenceValue : CorValue
    {

        internal CorReferenceValue(CorDebugReferenceValue referenceValue) : base(referenceValue)
        {
            m_refVal = referenceValue;
        }
        
        public CORDB_ADDRESS Value
        {
            get 
            {
                return m_refVal.Value;
            }
            set 
            {
                m_refVal.Value = value;
            }
        }

        public bool IsNull
        {
            get 
            {
                return m_refVal.IsNull;
            }
        }

        public CorValue Dereference()
        {
            CorDebugValue v = m_refVal.Dereference();
            return (v==null?null:new CorValue(v));
        }

        private CorDebugReferenceValue m_refVal = null;
    }


    public sealed class CorHandleValue : CorReferenceValue, System.IDisposable
    {

        internal CorHandleValue(CorDebugHandleValue handleValue) : base(handleValue)
        {
            m_handleVal = handleValue;
        }

        public void Dispose()
        {
            // The underlying ICorDebugHandle has a  Dispose() method which will free
            // its resources (a GC handle). We call that now to free things sooner.
            // If we don't call it now, it will still get freed at some random point after
            // the final release (which the finalizer will call).
            try
            {
                // This is just a best-effort to cleanup resources early.
                // If it fails, just swallow and move on.
                // May throw if handle was already disposed, or if process is not stopped.
                m_handleVal.Dispose();    
            }
            catch
            {
                // swallow all
            }
        }

        [CLSCompliant(false)]
        public CorDebugHandleType HandleType
        {
            get 
            {
                return m_handleVal.HandleType;
            }
        }
        private CorDebugHandleValue m_handleVal = null;
    }

    public sealed class CorStringValue : CorValue
    {

        internal CorStringValue(CorDebugStringValue stringValue) : base(stringValue)
        {
            m_strVal = stringValue;
        }
        
        public bool IsValid
        {
            get 
            {
                return m_strVal.IsValid;
            }
        }

        public string String
        {
            get 
            {
                return m_strVal.GetString(Length);
            }
        }

        public int Length
        {
            get 
            {
                return m_strVal.Length;
            }
        }

        private CorDebugStringValue m_strVal = null;
    }


    public sealed class CorObjectValue : CorValue
    {
        internal CorObjectValue(CorDebugObjectValue objectValue) : base(objectValue)
        {
            m_objVal = objectValue;
        }

        public CorClass Class
        {
            get 
            {
                CorDebugClass iclass = m_objVal.Class;
                return (iclass==null)?null:new CorClass(iclass);
            }
        }
        
        public CorValue GetFieldValue(CorClass managedClass,mdFieldDef fieldToken)
        {
            CorDebugValue val = m_objVal.GetFieldValue(managedClass.m_class.Raw,fieldToken);
            return new CorValue(val);
        }

        public CorType GetVirtualMethodAndType(mdMemberRef memberToken, out CorFunction managedFunction)
        {
			   GetVirtualMethodAndTypeResult res = m_objVal.GetVirtualMethodAndType(memberToken);
			   CorDebugFunction pfunc = res.ppFunction;
			   CorDebugType dt = res.ppType;
            if (pfunc == null)
                managedFunction = null;
            else
                managedFunction = new CorFunction (pfunc);
            return dt==null?null:new CorType (dt);
        }


        public bool IsValueClass 
        {
            get 
            {
                return m_objVal.IsValueClass;
            }
        }

        // public Object GetManagedCopy() -- deprecated, therefore we won't make it available at all.
        private CorDebugObjectValue m_objVal = null;
    }

    public sealed class CorGenericValue : CorValue
    {
        internal CorGenericValue(CorDebugGenericValue genericValue) : base(genericValue)
        {
            m_genVal = genericValue;
        }

        // Convert the supplied value to the type of this CorGenericValue using System.IConvertable.
        // Then store the value into this CorGenericValue.  Any compatible type can be supplied.
        // For example, if you supply a string and the underlying type is ELEMENT_TYPE_BOOLEAN,
        // Convert.ToBoolean will attempt to match the string against "true" and "false".
        public void SetValue(object value)
        {
            try
            {
                switch (this.Type)
                {
                    case CorElementType.Boolean:
                        bool v = Convert.ToBoolean( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &v ) );
                        }
                        break;
                        
                    case CorElementType.I1:
                        SByte sbv = Convert.ToSByte( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &sbv ) );
                        }
                        break;

                    case CorElementType.U1:                
                        Byte bv = Convert.ToByte( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &bv ) );
                        }
                        break;
                        
                    case CorElementType.Char:              
                        Char cv = Convert.ToChar( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &cv ) );
                        }
                        break;
                        
                    case CorElementType.I2:
                        Int16 i16v = Convert.ToInt16( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &i16v ) );
                        }
                        break;

                    case CorElementType.U2:
                        UInt16 u16v = Convert.ToUInt16( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &u16v ) );
                        }
                        break;
                        
                    case CorElementType.I4:
                        Int32 i32v = Convert.ToInt32( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &i32v ) );
                        }
                        break;
                        
                    case CorElementType.U4:
                        UInt32 u32v = Convert.ToUInt32( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &u32v ) );
                        }
                        break;

                    case CorElementType.I:
                        Int64 ip64v = Convert.ToInt64( value );
                        IntPtr ipv = new IntPtr( ip64v );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &ipv ) );
                        }
                        break;

                    case CorElementType.U:         
                        UInt64 ipu64v = Convert.ToUInt64( value );
                        UIntPtr uipv = new UIntPtr( ipu64v );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &uipv ) );
                        }
                        break;

                    case CorElementType.I8:                
                        Int64 i64v = Convert.ToInt64( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &i64v ) );
                        }
                        break;
                        
                    case CorElementType.U8:
                        UInt64 u64v = Convert.ToUInt64( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &u64v ) );
                        }
                        break;

                    case CorElementType.R4:                                
                        Single sv = Convert.ToSingle( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &sv ) );
                        }
                         break;

                    case CorElementType.R8:                                                            
                        Double dv = Convert.ToDouble( value );
                        unsafe
                        {
                            SetValueInternal( new IntPtr( &dv ) );
                        }
                        break;  
                        
                    case CorElementType.ValueType:                 
                        byte[] bav = (byte[]) value;
                        unsafe
                        {                       
                            fixed (byte* bufferPtr = &bav[0]) 
                            {
                                Debug.Assert(this.Size == bav.Length);          
                                m_genVal.SetValue(new IntPtr(bufferPtr));               
                            }
                        }
                        break;
                        
                    default:                
                        throw new InvalidOperationException("Type passed is not recognized.");
                }
            }
            catch (InvalidCastException e)
            {
                throw new InvalidOperationException("Wrong type used for SetValue command", e);
            }
        }
        
        public object GetValue()
        {
            return UnsafeGetValueAsType(this.Type);
        }

        /// <summary>
        /// Get the value as an array of IntPtrs.
        /// </summary>
        public IntPtr[] GetValueAsIntPtrArray()
        {
            int ptrsize=IntPtr.Size;            
            int cElem = (this.Size + ptrsize-1) / ptrsize;
            IntPtr[] buffer = new IntPtr[cElem];
            
            unsafe
            {
                fixed (IntPtr* bufferPtr = &buffer[0]) 
                {
                    this.GetValueInternal( new IntPtr(bufferPtr));               
                }
            }
            return buffer;
        }

        public Object UnsafeGetValueAsType(CorElementType type)
        {
            switch(type)
            {
                case CorElementType.Boolean:
                    byte bValue=4; // just initialize to avoid compiler warnings
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(byte));
                        this.GetValueInternal(new IntPtr(&bValue));
                    }
                    return (object) (bValue!=0);
                                    
                case CorElementType.Char:
                    char cValue='a'; // initialize to avoid compiler warnings
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(char));
                        this.GetValueInternal(new IntPtr(&cValue));
                    }
                    return (object) cValue;

                case CorElementType.I1:
                    SByte i1Value=4;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(SByte));
                        this.GetValueInternal(new IntPtr(&i1Value));
                    }
                    return (object) i1Value;
                    
                case CorElementType.U1:
                    Byte u1Value=4;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(Byte));
                        this.GetValueInternal(new IntPtr(&u1Value));
                    }
                    return (object) u1Value;
                    
                case CorElementType.I2:
                    Int16 i2Value=4;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(Int16));
                        this.GetValueInternal(new IntPtr(&i2Value));
                    }
                    return (object) i2Value;
                    
                case CorElementType.U2:
                    UInt16 u2Value=4;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(UInt16));
                        this.GetValueInternal(new IntPtr(&u2Value));
                    }
                    return (object) u2Value;
                    
                case CorElementType.I:
                    IntPtr ipValue=IntPtr.Zero;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(IntPtr));
                        this.GetValueInternal(new IntPtr(&ipValue));
                    }
                    return (object) ipValue;

                case CorElementType.U:
                    UIntPtr uipValue=UIntPtr.Zero;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(UIntPtr));
                        this.GetValueInternal(new IntPtr(&uipValue));
                    }
                    return (object) uipValue;
                    
                case CorElementType.I4:
                    Int32 i4Value=4;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(Int32));
                        this.GetValueInternal(new IntPtr(&i4Value));
                    }
                    return (object) i4Value;

                case CorElementType.U4:
                    UInt32 u4Value=4;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(UInt32));
                        this.GetValueInternal(new IntPtr(&u4Value));
                    }
                    return (object) u4Value;

                case CorElementType.I8:
                    Int64 i8Value=4;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(Int64));
                        this.GetValueInternal(new IntPtr(&i8Value));
                    }
                    return (object) i8Value;

                case CorElementType.U8:
                    UInt64 u8Value=4;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(UInt64));
                        this.GetValueInternal(new IntPtr(&u8Value));
                    }
                    return (object) u8Value;

                case CorElementType.R4:
                    Single r4Value=4;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(Single));
                        this.GetValueInternal(new IntPtr(&r4Value));
                    }
                    return (object) r4Value;

                case CorElementType.R8:
                    Double r8Value=4;
                    unsafe 
                    {
                        Debug.Assert(this.Size==sizeof(Double));
                        this.GetValueInternal(new IntPtr(&r8Value));
                    }
                    return (object) r8Value;


                case CorElementType.ValueType:
                    byte[] buffer = new byte[this.Size];
                    unsafe
                    {
                        fixed (byte* bufferPtr = &buffer[0]) 
                        {
                            Debug.Assert(this.Size == buffer.Length);           
                            this.GetValueInternal( new IntPtr(bufferPtr));               
                        }
                    }
                    return buffer;
                    
                default:
                    Debug.Assert(false,"Generic value should not be of any other type");
                    throw new NotSupportedException();
            }
        }


        private void SetValueInternal(IntPtr valPtr) 
        {
            m_genVal.SetValue(valPtr);
        }

        private void GetValueInternal( IntPtr valPtr )
        {
            m_genVal.GetValue(valPtr);
        }
        
        private CorDebugGenericValue m_genVal = null;
    }

    public sealed class CorBoxValue : CorValue
    {
        internal CorBoxValue(CorDebugBoxValue boxedValue): base(boxedValue)
        {
            m_boxVal = boxedValue;
        }

        public CorObjectValue GetObject()
        {
            CorDebugObjectValue ov = m_boxVal.Object;
            return (ov==null)?null:new CorObjectValue(ov);
        }

        private CorDebugBoxValue m_boxVal = null;
    }

    public sealed class CorArrayValue : CorValue
    {
        internal CorArrayValue(CorDebugArrayValue arrayValue): base(arrayValue)
        {
            m_arrayVal = arrayValue;
        }

        //void CreateRelocBreakpoint(ref CORDBLib.ICorDebugValueBreakpoint ppBreakpoint);
        //void GetBaseIndicies(UInt32 cdim, IntPtr indicies);

        public int Count
        {
            get 
            {
                return m_arrayVal.Count;
            }
        }

        
        public CorElementType ElementType
        {
            get 
            {
                return m_arrayVal.ElementType;
            }
        }

        public int Rank
        {
            get 
            {
                return m_arrayVal.Rank;
            }
        }

        public bool HasBaseIndicies
        {
            get 
            {
                return m_arrayVal.HasBaseIndicies();
            }
        }

        public bool IsValid
        {
            get 
            {
                return m_arrayVal.IsValid;
            }
        }

        public int[] GetDimensions()
        {
            Debug.Assert(Rank!=0);
            return m_arrayVal.GetDimensions(Rank);
        }

		public int[] GetBaseIndicies()
		{
			Debug.Assert(Rank != 0);
			return m_arrayVal.GetBaseIndicies(Rank);
		}

        public CorValue GetElement(int[] indices)
        {
            Debug.Assert(indices!=null);
            CorDebugValue ppValue = m_arrayVal.GetElement(indices.Length,indices);
            return ppValue==null?null:new CorValue(ppValue);
        }
        
        public CorValue GetElementAtPosition(int position)
        {
            CorDebugValue ppValue = m_arrayVal.GetElementAtPosition(position);
            return ppValue==null?null:new CorValue(ppValue);
        }
        private CorDebugArrayValue m_arrayVal = null;
    }

    public sealed class CorHeapValue : CorValue
    {
        internal CorHeapValue(CorDebugHeapValue heapValue): base(heapValue)
        {
            m_heapVal = heapValue;
        }

        //void CreateRelocBreakpoint(ref Microsoft.Samples.Debugging.CorDebug.NativeApi.ICorDebugValueBreakpoint ppBreakpoint);
        public CorValueBreakpoint CreateRelocBreakpoint()
        {
            CorDebugValueBreakpoint bp = m_heapVal.CreateRelocBreakpoint ();
            return new CorValueBreakpoint (bp);
        }

        //void IsValid(ref Int32 pbValid);
        public bool IsValid
        {
            get 
            {
                return m_heapVal.IsValid;
            }
        }

        [CLSCompliant(false)]
        public CorHandleValue CreateHandle(CorDebugHandleType type)
        {
            CorDebugHandleValue handle = m_heapVal.CreateHandle(type);
            return handle==null?null:new CorHandleValue(handle);
        }


        private CorDebugHeapValue m_heapVal = null;
    }
 
} /* namespace  */
