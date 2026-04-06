//---------------------------------------------------------------------
//  This file is part of the CLR Managed Debugger (mdbg) Sample.
// 
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------
using System;
using System.Reflection;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Globalization;
using System.Diagnostics;

using ClrDebug; 

namespace Microsoft.Samples.Debugging.CorMetadata
{
    public sealed class MetadataParameterInfo : ParameterInfo
    {
        internal MetadataParameterInfo(IMetaDataImport importer,mdParamDef paramToken,
                                       MemberInfo memberImpl,Type typeImpl)
        {
            mdMethodDef parentToken;
            int pulSequence,pcchValue,size;
            CorParamAttr pdwAttr;
            CorElementType pdwCPlusTypeFlag;

				IntPtr ppValue;
            importer.GetParamProps(paramToken,
                                   out parentToken,
                                   out pulSequence,
                                   null,
                                   0,
                                   out size,
                                   out pdwAttr,
                                   out pdwCPlusTypeFlag,
                                   out ppValue,
                                   out pcchValue
                                   );
            StringBuilder szName = new StringBuilder(size);        
            importer.GetParamProps(paramToken,
                                   out parentToken,
                                   out pulSequence,
                                   szName,
                                   szName.Capacity,
                                   out size,
                                   out pdwAttr,
                                   out pdwCPlusTypeFlag,
                                   out ppValue,
                                   out pcchValue
                                   );
            NameImpl = szName.ToString();
            ClassImpl = typeImpl;
            PositionImpl = (int)pulSequence;
            AttrsImpl = (ParameterAttributes)pdwAttr;
            
            MemberImpl=memberImpl;
        }

        private MetadataParameterInfo(SerializationInfo info, StreamingContext context)
        {
            
        }

        public override string Name
        {
            get 
            {
                return NameImpl;
            }
        }

        public override int Position
        { 
            get 
            { 
                return PositionImpl; 
            }
        }
    }
}
