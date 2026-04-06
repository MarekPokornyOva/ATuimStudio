using ClrDebug;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

#nullable enable

namespace Mono.Debugging.ClrDebug
{
	sealed class ATuimMetaDataImport : IMetaDataImport, IMetaDataImport2, IDisposable
	{
		readonly FileStream _asmFile;
		readonly PEReader _pEReader;
		readonly MetadataReader _metadataReader;
		public ATuimMetaDataImport(string filename)
		{
			_asmFile = File.OpenRead(filename);
			_pEReader = new PEReader(_asmFile);
			_metadataReader = _pEReader.GetMetadataReader();
		}

		#region Dispose
		bool _isDisposed;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_pEReader?.Dispose();
					_asmFile?.Dispose();
				}

				_isDisposed = true;
			}
		}

		~ATuimMetaDataImport()
		{
			Dispose(false);
		}
		#endregion Dispose

		#region IMetaDataImport
		public void CloseEnum(IntPtr hEnum)
		{
			//no need to dispose anything
		}

		public HRESULT CountEnum(IntPtr hEnum, out int pulCount)
		{
			throw new NotImplementedException();
		}

		public HRESULT ResetEnum(IntPtr hEnum, int ulPos)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumTypeDefs(ref IntPtr phEnum, mdTypeDef[] typeDefs, int cMax, out int pcTypeDefs)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumInterfaceImpls(ref IntPtr phEnum, mdTypeDef td, mdInterfaceImpl[] rImpls, int cMax, out int pcImpls)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumTypeRefs(ref IntPtr phEnum, mdTypeRef[] rTypeRefs, int cMax, out int pcTypeRefs)
		{
			throw new NotImplementedException();
		}

		public HRESULT FindTypeDefByName(string szTypeDef, mdToken tkEnclosingClass, out mdTypeDef typeDef)
		{
			TypeDefinitionHandle tdh = FindTypeDefByNameInternal(szTypeDef);

			if (!tdh.IsNil)
			{
				typeDef = MetadataTokens.GetToken(tdh);
				return HRESULT.S_OK;
			}

			throw new COMException("", unchecked((int)HRESULT.CLDB_E_RECORD_NOTFOUND));
		}

		TypeDefinitionHandle FindTypeDefByNameInternal(string szTypeDef)
		{
			int pos = szTypeDef.LastIndexOf('.');
			string name;
			string? ns;
			if (pos == -1)
			{
				ns = null;
				name = szTypeDef;
			}
			else
			{
				ns = szTypeDef[..pos];
				name = szTypeDef[(pos + 1)..];
			}

			string? nested;
			pos = name.IndexOf('+');
			if (pos == -1)
				nested = null;
			else
			{
				nested = name[(pos + 1)..];
				name = name[..pos];
			}

			foreach (TypeDefinitionHandle tdh in _metadataReader.TypeDefinitions)
			{
				TypeDefinition type = _metadataReader.GetTypeDefinition(tdh);
				if (!string.Equals(_metadataReader.GetString(type.Name), name, StringComparison.Ordinal))
					continue;
				if (ns == null && !type.Namespace.IsNil)
					continue;
				if (!string.Equals(_metadataReader.GetString(type.Namespace), ns, StringComparison.Ordinal))
					continue;

				if (nested == null)
					return tdh;

				foreach (TypeDefinitionHandle ntdh in type.GetNestedTypes())
				{
					type = _metadataReader.GetTypeDefinition(ntdh);
					if (string.Equals(_metadataReader.GetString(type.Name), nested, StringComparison.Ordinal))
						return ntdh;
				}

				return default;
			}

			return default;
		}

		public HRESULT GetScopeProps(char[] szName, int cchName, out int pchName, out Guid pmvid)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetModuleFromScope(out mdModule pmd)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetTypeDefProps(mdTypeDef td, char[] szTypeDef, int cchTypeDef, out int pchTypeDef, out CorTypeAttr pdwTypeDefFlags, out mdToken ptkExtends)
		{
			TypeDefinition type = _metadataReader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(td.Rid));
			string name = _metadataReader.GetString(type.Namespace);
			if (!string.IsNullOrEmpty(name))
				name += '.';
			name += _metadataReader.GetString(type.Name);
			pchTypeDef = name.Length + 1;
			pdwTypeDefFlags = (CorTypeAttr)type.Attributes;
			ptkExtends = type.BaseType.IsNil ? 0 : MetadataTokens.GetToken(type.BaseType);

			if (szTypeDef != null)
			{
				name.ToCharArray().CopyTo(szTypeDef);
				szTypeDef[cchTypeDef - 1] = '\0';
			}
			return HRESULT.S_OK;
		}

		public HRESULT GetInterfaceImplProps(mdInterfaceImpl iiImpl, out mdTypeDef pClass, out mdToken ptkIface)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetTypeRefProps(mdTypeRef tr, out mdToken ptkResolutionScope, char[] szName, int cchName, out int pchName)
		{
			TypeReference type = _metadataReader.GetTypeReference(MetadataTokens.TypeReferenceHandle(tr.Rid));
			string ns = _metadataReader.GetString(type.Namespace);
			string name = _metadataReader.GetString(type.Name);
			ptkResolutionScope = MetadataTokens.GetToken(type.ResolutionScope);
			int nsLen = ns.Length;
			pchName = nsLen + name.Length + 2;

			if (szName != null)
			{
				ns.ToCharArray().CopyTo(szName);
				name.ToCharArray().CopyTo(szName, nsLen + 1);
				szName[nsLen] = '.';
				szName[cchName - 1] = '\0';
			}
			return HRESULT.S_OK;
		}

		public HRESULT ResolveTypeRef(mdTypeRef tr, in Guid riid, out object ppIScope, out mdTypeDef ptd)
		{
			//https://stackoverflow.com/questions/8865239/problems-with-imetadataimportresolvetyperef-method
			//https://web.archive.org/web/20190114032248/https://blogs.msdn.microsoft.com/davbr/2011/10/17/metadata-tokens-run-time-ids-and-type-loading/
			throw new NotImplementedException();
		}

		public HRESULT EnumMembers(ref IntPtr phEnum, mdTypeDef cl, mdToken[] rMembers, int cMax, out int pcTokens)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumMembersWithName(ref IntPtr phEnum, mdTypeDef cl, string szName, mdToken[] rMembers, int cMax, out int pcTokens)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumMethods(ref IntPtr phEnum, mdTypeDef cl, mdMethodDef[] rMethods, int cMax, out int pcTokens)
		{
			if (cMax != 1)
			{
				pcTokens = 0;
				return HRESULT.ERROR_INVALID_PARAMETER;
			}

			int methIndex = (int)phEnum;
			TypeDefinition type = _metadataReader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(cl.Rid));
			MethodDefinitionHandleCollection methHandles = type.GetMethods();

			if (methHandles.Count < methIndex + 1)
			{
				pcTokens = 0;
				return HRESULT.S_FALSE;
			}

			phEnum++;
			rMethods[0] = MetadataTokens.GetToken(methHandles.ElementAt(methIndex));
			pcTokens = 1;
			return HRESULT.S_OK;
		}

		public HRESULT EnumMethodsWithName(ref IntPtr phEnum, mdTypeDef cl, string szName, mdMethodDef[] rMethods, int cMax, out int pcTokens)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumFields(ref IntPtr phEnum, mdTypeDef cl, mdFieldDef[] rFields, int cMax, out int pcTokens)
		{
			if (cMax != 1)
			{
				pcTokens = 0;
				return HRESULT.ERROR_INVALID_PARAMETER;
			}

			int parmIndex = (int)phEnum;
			TypeDefinition type = _metadataReader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(cl.Rid));
			FieldDefinitionHandleCollection fieldHandles = type.GetFields();

			if (fieldHandles.Count < parmIndex + 1)
			{
				pcTokens = 0;
				return HRESULT.S_FALSE;
			}

			phEnum++;
			rFields[0] = MetadataTokens.GetToken(fieldHandles.ElementAt(parmIndex));
			pcTokens = 1;
			return HRESULT.S_OK;
		}

		public HRESULT EnumFieldsWithName(ref IntPtr phEnum, mdTypeDef cl, string szName, mdFieldDef[] rFields, int cMax, out int pcTokens)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumParams(ref IntPtr phEnum, mdMethodDef mb, mdParamDef[] rParams, int cMax, out int pcTokens)
		{
			if (cMax != 1)
			{
				pcTokens = 0;
				return HRESULT.ERROR_INVALID_PARAMETER;
			}

			int parmIndex = (int)phEnum;
			MethodDefinition method = _metadataReader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(mb.Rid));
			ParameterHandleCollection parmHandles = method.GetParameters();

			if (parmHandles.Count < parmIndex + 1)
			{
				pcTokens = 0;
				return HRESULT.S_FALSE;
			}

			phEnum++;
			rParams[0] = MetadataTokens.GetToken(parmHandles.ElementAt(parmIndex));
			pcTokens = 1;
			return HRESULT.S_OK;
		}

		public HRESULT EnumMemberRefs(ref IntPtr phEnum, mdToken tkParent, mdMemberRef[] rMemberRefs, int cMax, out int pcTokens)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumMethodImpls(ref IntPtr phEnum, mdTypeDef td, mdToken[] rMethodBody, mdToken[] rMethodDecl, int cMax, out int pcTokens)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumPermissionSets(ref IntPtr phEnum, mdToken tk, CorDeclSecurity dwActions, mdPermission[] rPermission, int cMax, out int pcTokens)
		{
			throw new NotImplementedException();
		}

		public HRESULT FindMember(mdToken td, string szName, IntPtr pvSigBlob, int cbSigBlob, out mdToken pmb)
		{
			throw new NotImplementedException();
		}

		public HRESULT FindMethod(mdToken td, string szName, IntPtr pvSigBlob, int cbSigBlob, out mdMethodDef pmb)
		{
			throw new NotImplementedException();
		}

		public HRESULT FindField(mdToken td, string szName, IntPtr pvSigBlob, int cbSigBlob, out mdFieldDef pmb)
		{
			throw new NotImplementedException();
		}

		public HRESULT FindMemberRef(mdToken td, string szName, IntPtr pvSigBlob, int cbSigBlob, out mdMemberRef pmr)
		{
			throw new NotImplementedException();
		}

		public unsafe HRESULT GetMethodProps(mdMethodDef mb, out mdTypeDef pClass, char[] szMethod, int cchMethod, out int pchMethod, out CorMethodAttr pdwAttr, out IntPtr ppvSigBlob, out int pcbSigBlob, out int pulCodeRVA, out CorMethodImpl pdwImplFlags)
		{
			MethodDefinition method = _metadataReader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(mb.Rid));
			string name = _metadataReader.GetString(method.Name);
			BlobReader signatureReader = _metadataReader.GetBlobReader(method.Signature);
			pClass = MetadataTokens.GetToken(method.GetDeclaringType());
			pchMethod = name.Length + 1;
			pdwAttr = (CorMethodAttr)method.Attributes;
			ppvSigBlob = (IntPtr)signatureReader.StartPointer;
			pcbSigBlob = signatureReader.Length;
			pulCodeRVA = method.RelativeVirtualAddress;
			pdwImplFlags = (CorMethodImpl)method.ImplAttributes;

			if (szMethod != null)
			{
				name.ToCharArray().CopyTo(szMethod);
				szMethod[cchMethod - 1] = '\0';
			}
			return HRESULT.S_OK;
		}

		public HRESULT GetMemberRefProps(mdMemberRef mr, out mdToken ptk, char[] szMember, int cchMember, out int pchMember, out IntPtr ppvSigBlob, out int pbSig)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumProperties(ref IntPtr phEnum, mdTypeDef td, mdProperty[] rProperties, int cMax, out int pcProperties)
		{
			if (cMax != 1)
			{
				pcProperties = 0;
				return HRESULT.ERROR_INVALID_PARAMETER;
			}

			int propIndex = (int)phEnum;
			TypeDefinition type = _metadataReader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(td.Rid));
			PropertyDefinitionHandleCollection propHandles = type.GetProperties();

			if (propHandles.Count < propIndex + 1)
			{
				pcProperties = 0;
				return HRESULT.S_FALSE;
			}

			phEnum++;
			rProperties[0] = MetadataTokens.GetToken(propHandles.ElementAt(propIndex));
			pcProperties = 1;
			return HRESULT.S_OK;
		}

		public HRESULT EnumEvents(ref IntPtr phEnum, mdTypeDef td, mdEvent[] rEvents, int cMax, out int pcEvents)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetEventProps(mdEvent ev, out mdTypeDef pClass, char[] szEvent, int cchEvent, out int pchEvent, out CorEventAttr pdwEventFlags, out mdToken ptkEventType, out mdMethodDef pmdAddOn, out mdMethodDef pmdRemoveOn, out mdMethodDef pmdFire, mdMethodDef[] rmdOtherMethod, int cMax, out int pcOtherMethod)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumMethodSemantics(ref IntPtr phEnum, mdMethodDef mb, mdToken[] rEventProp, int cMax, out int pcEventProp)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetMethodSemantics(mdMethodDef mb, mdToken tkEventProp, out CorMethodSemanticsAttr pdwSemanticsFlags)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetClassLayout(mdTypeDef td, out int pdwPackSize, COR_FIELD_OFFSET[] rFieldOffset, int cMax, out int pcFieldOffset, out int pulClassSize)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetFieldMarshal(mdToken tk, out IntPtr ppvNativeType, out int pcbNativeType)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetRVA(mdToken tk, out int pulCodeRVA, out CorMethodImpl pdwImplFlags)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetPermissionSetProps(mdPermission pm, out CorDeclSecurity pdwAction, out IntPtr ppvPermission, out int pcbPermission)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetSigFromToken(mdSignature mdSig, out IntPtr ppvSig, out int pcbSig)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetModuleRefProps(mdModuleRef mur, char[] szName, int cchName, out int pchName)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumModuleRefs(ref IntPtr phEnum, mdModuleRef[] rModuleRefs, int cmax, out int pcModuleRefs)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetTypeSpecFromToken(mdTypeSpec typespec, out IntPtr ppvSig, out int pcbSig)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetNameFromToken(mdToken tk, out IntPtr pszUtf8NamePtr)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumUnresolvedMethods(ref IntPtr phEnum, mdToken[] rMethods, int cMax, out int pcTokens)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetUserString(mdString stk, char[] szString, int cchString, out int pchString)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetPinvokeMap(mdToken tk, out CorPinvokeMap pdwMappingFlags, char[] szImportName, int cchImportName, out int pchImportName, out mdModuleRef pmrImportDLL)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumSignatures(ref IntPtr phEnum, mdSignature[] rSignatures, int cmax, out int pcSignatures)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumTypeSpecs(ref IntPtr phEnum, mdTypeSpec[] rTypeSpecs, int cmax, out int pcTypeSpecs)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumUserStrings(ref IntPtr phEnum, mdString[] rStrings, int cmax, out int pcStrings)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetParamForMethodIndex(mdMethodDef md, int ulParamSeq, out mdParamDef ppd)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumCustomAttributes(ref IntPtr phEnum, mdToken tk, mdToken tkType, mdCustomAttribute[] rCustomAttributes, int cMax, out int pcCustomAttributes)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetCustomAttributeProps(mdCustomAttribute cv, out mdToken ptkObj, out mdToken ptkType, out IntPtr ppBlob, out int pcbSize)
		{
			throw new NotImplementedException();
		}

		public HRESULT FindTypeRef(mdToken tkResolutionScope, string szName, out mdTypeRef ptr)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetMemberProps(mdToken mb, out mdTypeDef pClass, char[] szMember, int cchMember, out int pchMember, out int pdwAttr, out IntPtr ppvSigBlob, out int pcbSigBlob, out int pulCodeRVA, out int pdwImplFlags, out CorElementType pdwCPlusTypeFlag, out IntPtr ppValue, out int pcchValue)
		{
			throw new NotImplementedException();
		}

		public unsafe HRESULT GetFieldProps(mdFieldDef mb, out mdTypeDef pClass, char[] szField, int cchField, out int pchField, out CorFieldAttr pdwAttr, out IntPtr ppvSigBlob, out int pcbSigBlob, out CorElementType pdwCPlusTypeFlag, out IntPtr ppValue, out int pcchValue)
		{
			FieldDefinition fieldDef = _metadataReader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(mb.Rid));
			string name = _metadataReader.GetString(fieldDef.Name);
			BlobReader signatureReader = _metadataReader.GetBlobReader(fieldDef.Signature);

			pClass = default;
			pchField = name.Length + 1;
			pdwAttr = (CorFieldAttr)fieldDef.Attributes;
			ppvSigBlob = (IntPtr)signatureReader.StartPointer;
			pcbSigBlob = signatureReader.Length;
			pdwCPlusTypeFlag = default;
			ConstantHandle defValHandle = fieldDef.GetDefaultValue();
			if (defValHandle.IsNil)
			{
				ppValue = nint.Zero;
				pcchValue = 0;
			}
			else
			{
				Constant cons = _metadataReader.GetConstant(defValHandle);
				BlobReader consReader = _metadataReader.GetBlobReader(cons.Value);
				ppValue = (IntPtr)consReader.StartPointer;
				pcchValue = cons.TypeCode == ConstantTypeCode.String ? consReader.Length / 2 : 0;
			}

			if (szField != null)
			{
				name.ToCharArray().CopyTo(szField);
				szField[cchField - 1] = '\0';
			}
			return HRESULT.S_OK;
		}

		public unsafe HRESULT GetPropertyProps(mdProperty prop, out mdTypeDef pClass, char[] szProperty, int cchProperty, out int pchProperty, out CorPropertyAttr pdwPropFlags, out IntPtr ppvSig, out int pbSig, out CorElementType pdwCPlusTypeFlag, out IntPtr ppDefaultValue, out int pcchDefaultValue, out mdMethodDef pmdSetter, out mdMethodDef pmdGetter, mdMethodDef[] rmdOtherMethod, int cMax, out int pcOtherMethod)
		{
			PropertyDefinition propDef = _metadataReader.GetPropertyDefinition(MetadataTokens.PropertyDefinitionHandle(prop.Rid));
			string name = _metadataReader.GetString(propDef.Name);
			BlobReader signatureReader = _metadataReader.GetBlobReader(propDef.Signature);

			pClass = default;
			pchProperty = name.Length + 1;
			pdwPropFlags = (CorPropertyAttr)propDef.Attributes;
			ppvSig = (IntPtr)signatureReader.StartPointer;
			pbSig = signatureReader.Length;
			pdwCPlusTypeFlag = default;
			ConstantHandle defValHandle = propDef.GetDefaultValue();
			if (defValHandle.IsNil)
			{
				ppDefaultValue = nint.Zero;
				pcchDefaultValue = 0;
			}
			else
			{
				Constant cons = _metadataReader.GetConstant(defValHandle);
				BlobReader consReader = _metadataReader.GetBlobReader(cons.Value);
				ppDefaultValue = (IntPtr)consReader.StartPointer;
				pcchDefaultValue = cons.TypeCode == ConstantTypeCode.String ? consReader.Length / 2 : 0;
			}

			PropertyAccessors propAccessors = propDef.GetAccessors();
			pmdSetter = propAccessors.Setter.IsNil ? 0 : MetadataTokens.GetToken(propAccessors.Setter);
			pmdGetter = propAccessors.Getter.IsNil ? 0 : MetadataTokens.GetToken(propAccessors.Getter);
			int otherInd = 0;
			foreach (MethodDefinitionHandle otherDefHandle in propAccessors.Others.Take(cMax))
				rmdOtherMethod[otherInd++] = otherDefHandle.IsNil ? 0 : MetadataTokens.GetToken(otherDefHandle);
			pcOtherMethod = otherInd;

			if (szProperty != null)
			{
				name.ToCharArray().CopyTo(szProperty);
				szProperty[cchProperty - 1] = '\0';
			}
			return HRESULT.S_OK;
		}

		public unsafe HRESULT GetParamProps(mdParamDef tk, out mdMethodDef pmd, out int pulSequence, char[] szName, int cchName, out int pchName, out CorParamAttr pdwAttr, out CorElementType pdwCPlusTypeFlag, out IntPtr ppValue, out int pcchValue)
		{
			Parameter paramDef = _metadataReader.GetParameter(MetadataTokens.ParameterHandle(tk.Rid));
			string name = _metadataReader.GetString(paramDef.Name);

			pmd = default;
			pulSequence = paramDef.SequenceNumber;
			pchName = name.Length + 1;
			pdwAttr = (CorParamAttr)paramDef.Attributes;
			pdwCPlusTypeFlag = default;
			ConstantHandle defValHandle = paramDef.GetDefaultValue();
			if (defValHandle.IsNil)
			{
				ppValue = nint.Zero;
				pcchValue = 0;
			}
			else
			{
				Constant cons = _metadataReader.GetConstant(defValHandle);
				BlobReader consReader = _metadataReader.GetBlobReader(cons.Value);
				ppValue = (IntPtr)consReader.StartPointer;
				pcchValue = cons.TypeCode == ConstantTypeCode.String ? consReader.Length / 2 : 0;
			}

			if (szName != null)
			{
				name.ToCharArray().CopyTo(szName);
				szName[cchName - 1] = '\0';
			}
			return HRESULT.S_OK;
		}

		public unsafe HRESULT GetCustomAttributeByName(mdToken tkObj, string szName, out IntPtr ppData, out int pcbData)
		{
			(CustomAttributeHandle Handle, CustomAttribute Attr) attr = _metadataReader.CustomAttributes.Select(x => (Handle: x, Attr: _metadataReader.GetCustomAttribute(x)))
				.FirstOrDefault(x =>
				{
					CustomAttribute attr = x.Attr;

					if (MetadataTokens.GetToken(attr.Parent) != tkObj)
						return false;
					if (attr.Constructor.Kind == HandleKind.MemberReference)
					{
						MemberReference mr = _metadataReader.GetMemberReference((MemberReferenceHandle)attr.Constructor);
						if (mr.Parent.Kind == HandleKind.TypeReference)
						{
							TypeReference tr = _metadataReader.GetTypeReference((TypeReferenceHandle)mr.Parent);
							string ns = _metadataReader.GetString(tr.Namespace);
							string name = _metadataReader.GetString(tr.Name);
							if (string.Equals(Fullname(ns, name), szName, StringComparison.Ordinal))
								return true;
						}

						return false;
					}
					if (attr.Constructor.Kind == HandleKind.MethodDefinition)
					{
						MethodDefinition md = _metadataReader.GetMethodDefinition((MethodDefinitionHandle)attr.Constructor);
						TypeDefinition td = _metadataReader.GetTypeDefinition(md.GetDeclaringType());
						string ns = _metadataReader.GetString(td.Namespace);
						string name = _metadataReader.GetString(td.Name);
						return string.Equals(Fullname(ns, name), szName, StringComparison.Ordinal);
					}

					return false;
				});

			if (attr.Handle.IsNil)
			{
				ppData = IntPtr.Zero;
				pcbData = 0;
				return HRESULT.S_FALSE;
			}

			BlobReader dataReader = _metadataReader.GetBlobReader(attr.Attr.Value);
			ppData = (IntPtr)dataReader.StartPointer;
			pcbData = dataReader.Length;
			return HRESULT.S_OK;
		}

		public bool IsValidToken(mdToken tk)
		{
			int rid = tk.Rid;
			if (rid < 1)
				return false;
			int count = _metadataReader.GetTableRowCount((TableIndex)(((int)tk.Type) >> 24));
			return rid <= count;
		}

		public HRESULT GetNestedClassProps(mdTypeDef tdNestedClass, out mdTypeDef ptdEnclosingClass)
		{
			TypeDefinition td = _metadataReader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(tdNestedClass));
			ptdEnclosingClass = MetadataTokens.GetToken(td.GetDeclaringType());
			return HRESULT.S_OK;
		}

		public HRESULT GetNativeCallConvFromSig(IntPtr pvSig, int cbSig, out int pCallConv)
		{
			throw new NotImplementedException();
		}

		public HRESULT IsGlobal(mdToken pd, out bool pbGlobal)
		{
			throw new NotImplementedException();
		}
		#endregion IMetaDataImport

		#region IMetaDataImport2
		public HRESULT EnumGenericParams(ref IntPtr phEnum, mdToken tk, mdGenericParam[] rGenericParams, int cMax, out int pcGenericParams)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetGenericParamProps(mdGenericParam gp, out int pulParamSeq, out CorGenericParamAttr pdwParamFlags, out mdToken ptOwner, out int reserved, char[] wzname, int cchName, out int pchName)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetMethodSpecProps(mdMethodSpec mi, out mdToken tkParent, out IntPtr ppvSigBlob, out int pcbSigBlob)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumGenericParamConstraints(ref IntPtr phEnum, mdGenericParam tk, mdGenericParamConstraint[] rGenericParamConstraints, int cMax, out int pcGenericParamConstraints)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetGenericParamConstraintProps(mdGenericParamConstraint gpc, out mdGenericParam ptGenericParam, out mdToken ptkConstraintType)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetPEKind(out CorPEKind pdwPEKind, out IMAGE_FILE_MACHINE pdwMachine)
		{
			throw new NotImplementedException();
		}

		public HRESULT GetVersionString(char[] pwzBuf, int ccBufSize, out int pccBufSize)
		{
			throw new NotImplementedException();
		}

		public HRESULT EnumMethodSpecs(ref IntPtr phEnum, mdToken tk, mdMethodSpec[] rMethodSpecs, int cMax, out int pcMethodSpecs)
		{
			throw new NotImplementedException();
		}
		#endregion IMetaDataImport2

		static string Fullname(string ns, string name)
			=> string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
	}
}
