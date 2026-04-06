using Mono.Debugging.Evaluation;
using System.Reflection;
using Mono.Debugging.Client;
using System.Diagnostics.SymbolStore;
using System.Diagnostics;
using System.Text;
using System.Runtime.CompilerServices;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.CorMetadata;
using CorApi2.Metadata.Microsoft.Samples.Debugging.CorMetadata;
using ClrDebug;
using Microsoft.Samples.Debugging.Extensions;

namespace Mono.Debugging.ClrDebug
{
	public class CorObjectAdaptor : ObjectValueAdaptor
	{
		private readonly Dictionary<string, CorType> nameToTypeCache = new Dictionary<string, CorType>();

		private readonly Dictionary<CorType, string> typeToNameCache = new Dictionary<CorType, string>();

		private readonly HashSet<string> unresolvedNames = new HashSet<string>();

		public override bool IsPrimitive(EvaluationContext ctx, object val)
		{
			object realObject = GetRealObject(ctx, val);
			if (!(realObject is CorGenericValue))
			{
				return realObject is CorStringValue;
			}
			return true;
		}

		public override bool IsPointer(EvaluationContext ctx, object val)
		{
			CorType targetType = (CorType)GetValueType(ctx, val);
			return IsPointer(targetType);
		}

		public override bool IsEnum(EvaluationContext ctx, object val)
		{
			if (!(val is CorValRef))
			{
				return false;
			}
			CorType targetType = (CorType)GetValueType(ctx, val);
			return IsEnum(ctx, targetType);
		}

		public override bool IsArray(EvaluationContext ctx, object val)
		{
			return GetRealObject(ctx, val) is CorArrayValue;
		}

		public override bool IsString(EvaluationContext ctx, object val)
		{
			return GetRealObject(ctx, val) is CorStringValue;
		}

		public override bool IsNull(EvaluationContext ctx, object gval)
		{
			if (gval == null)
			{
				return true;
			}
			if (gval is not CorValRef corValRef)
			{
				return true;
			}
			if (corValRef.Val == null || (corValRef.Val is CorReferenceValue && ((CorReferenceValue)corValRef.Val).IsNull))
			{
				return true;
			}
			CorValue realObject = GetRealObject(ctx, corValRef);
			if (realObject is CorReferenceValue)
			{
				return ((CorReferenceValue)realObject).IsNull;
			}
			return false;
		}

		public override bool IsValueType(object type)
		{
			return ((CorType)type).Type == CorElementType.ValueType;
		}

		public override bool IsClass(EvaluationContext ctx, object type)
		{
			CorType corType = (CorType)type;
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			if (corType.Type == CorElementType.String || corType.Type == CorElementType.Array || corType.Type == CorElementType.SZArray)
			{
				return true;
			}
			if (MetadataHelperFunctionsExtensions.CoreTypes.TryGetValue(corType.Type, out var _))
			{
				return false;
			}
			if (IsIEnumerable(corType, corEvaluationContext.Session))
			{
				return false;
			}
			if (corType.Type != CorElementType.Class || !(corType.Class != null))
			{
				return IsValueType(corType);
			}
			return true;
		}

		public override bool IsGenericType(EvaluationContext ctx, object type)
		{
			if (((CorType)type).Type != CorElementType.GenericInst)
			{
				return base.IsGenericType(ctx, type);
			}
			return true;
		}

		public override bool NullableHasValue(EvaluationContext ctx, object type, object obj)
		{
			return (bool)GetMember(ctx, type, obj, "hasValue").ObjectValue;
		}

		public override ValueReference NullableGetValue(EvaluationContext ctx, object type, object obj)
		{
			return GetMember(ctx, type, obj, "value");
		}

		public override string GetTypeName(EvaluationContext ctx, object gtype)
		{
			CorType corType = (CorType)gtype;
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			if (MetadataHelperFunctionsExtensions.CoreTypes.TryGetValue(corType.Type, out var value))
			{
				return value.FullName;
			}
			try
			{
				if (corType.Type == CorElementType.Array || corType.Type == CorElementType.SZArray)
				{
					return GetTypeName(ctx, corType.FirstTypeParameter) + "[" + new string(',', corType.Rank - 1) + "]";
				}
				if (corType.Type == CorElementType.ByRef)
				{
					return GetTypeName(ctx, corType.FirstTypeParameter) + "&";
				}
				if (corType.Type == CorElementType.Ptr)
				{
					return GetTypeName(ctx, corType.FirstTypeParameter) + "*";
				}
				return corType.GetTypeInfo(corEvaluationContext.Session).FullName;
			}
			catch (Exception ex)
			{
				DebuggerLoggingService.LogError("Exception in GetTypeName()", ex);
				return "[Unknown type]";
			}
		}

		public override object GetValueType(EvaluationContext ctx, object val)
		{
			if (val == null)
			{
				return GetType(ctx, "System.Object");
			}
			CorValue realObject = GetRealObject(ctx, val);
			if (realObject == null)
			{
				return GetType(ctx, "System.Object");
			}
			return realObject.ExactType;
		}

		public override object GetBaseType(EvaluationContext ctx, object type)
		{
			return ((CorType)type).Base;
		}

		protected override object GetBaseTypeWithAttribute(EvaluationContext ctx, object type, object attrType)
		{
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			Type typeInfo = ((CorType)attrType).GetTypeInfo(corEvaluationContext.Session);
			CorType corType = type as CorType;
			while (corType != null)
			{
				if (corType.GetTypeInfo(corEvaluationContext.Session).GetCustomAttributes(typeInfo, inherit: false).Any())
				{
					return corType;
				}
				corType = corType.Base;
			}
			return null;
		}

		public override object[] GetTypeArgs(EvaluationContext ctx, object type)
		{
			return CastArray<object>(((CorType)type).TypeParameters);
		}

		private static IEnumerable<Type> GetAllTypes(EvaluationContext gctx)
		{
			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
			foreach (CorModule allModule in ctx.Session.GetAllModules())
			{
				CorMetadataImport metadataForModule = ctx.Session.GetMetadataForModule(allModule);
				if (metadataForModule == null)
				{
					continue;
				}
				foreach (Type definedType in metadataForModule.DefinedTypes)
				{
					yield return definedType;
				}
			}
		}

		private string GetCacheName(string name, CorType[] typeArgs)
		{
			if (typeArgs == null || typeArgs.Length == 0)
			{
				return name;
			}
			StringBuilder stringBuilder = new StringBuilder(name + "<");
			for (int i = 0; i < typeArgs.Length; i++)
			{
				if (!typeToNameCache.TryGetValue(typeArgs[i], out var value))
				{
					DebuggerLoggingService.LogMessage("Can't get cached name for generic type {0} because it's substitution type isn't found in cache", name);
					return null;
				}
				stringBuilder.Append(value);
				if (i < typeArgs.Length - 1)
				{
					stringBuilder.Append(",");
				}
			}
			stringBuilder.Append(">");
			return stringBuilder.ToString();
		}

		public override object GetType(EvaluationContext gctx, string name, object[] gtypeArgs)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
			CorType[] array = CastArray<CorType>(gtypeArgs);
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)gctx;
			CorAppDomain appDomain = corEvaluationContext.Frame.Function.Class.Module.Assembly.AppDomain;
			string text = $"{appDomain.Id}:{name}";
			string cacheName = GetCacheName(text, array);
			if (!string.IsNullOrEmpty(cacheName) && nameToTypeCache.TryGetValue(cacheName, out var value))
			{
				return value;
			}
			if (unresolvedNames.Contains(cacheName ?? text))
			{
				return null;
			}
			foreach (CorModule module in corEvaluationContext.Session.GetModules(appDomain))
			{
				CorMetadataImport metadataForModule = corEvaluationContext.Session.GetMetadataForModule(module);
				if (metadataForModule == null)
				{
					continue;
				}
				int typeTokenFromName = metadataForModule.GetTypeTokenFromName(name);
				if (typeTokenFromName == -1)
				{
					continue;
				}
				Type type = metadataForModule.GetType(typeTokenFromName);
				CorType parameterizedType = module.GetClassFromToken(type.MetadataToken).GetParameterizedType(CorElementType.Class, array);
				if (parameterizedType != null)
				{
					if (!string.IsNullOrEmpty(cacheName))
					{
						nameToTypeCache[cacheName] = parameterizedType;
						typeToNameCache[parameterizedType] = cacheName;
					}
					return parameterizedType;
				}
			}
			unresolvedNames.Add(cacheName ?? text);
			return null;
		}

		private static T[] CastArray<T>(object[] array)
		{
			if (array == null)
			{
				return null;
			}
			T[] array2 = new T[array.Length];
			Array.Copy(array, array2, array.Length);
			return array2;
		}

		public override string CallToString(EvaluationContext ctx, object objr)
		{
			CorValue realObject = GetRealObject(ctx, objr);
			if (realObject is CorReferenceValue && ((CorReferenceValue)realObject).IsNull)
			{
				return string.Empty;
			}
			CorStringValue corStringValue = realObject as CorStringValue;
			if (corStringValue != null)
			{
				return corStringValue.String;
			}
			CorGenericValue corGenericValue = realObject as CorGenericValue;
			if (corGenericValue != null)
			{
				return corGenericValue.GetValue().ToString();
			}
			CorArrayValue corArrayValue = realObject as CorArrayValue;
			if (corArrayValue != null)
			{
				StringBuilder stringBuilder = new StringBuilder(GetDisplayTypeName(ctx, corArrayValue.ExactType.FirstTypeParameter));
				stringBuilder.Append("[");
				int[] dimensions = corArrayValue.GetDimensions();
				for (int i = 0; i < dimensions.Length; i++)
				{
					if (i > 0)
					{
						stringBuilder.Append(',');
					}
					stringBuilder.Append(dimensions[i]);
				}
				stringBuilder.Append("]");
				return stringBuilder.ToString();
			}
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			CorObjectValue corObjectValue = realObject as CorObjectValue;
			if (corObjectValue != null)
			{
				if (IsEnum(ctx, corObjectValue.ExactType))
				{
					MetadataType metadataType = corObjectValue.ExactType.GetTypeInfo(corEvaluationContext.Session) as MetadataType;
					bool flag = metadataType != null && metadataType.ReallyIsFlagsEnum;
					string typeName = GetTypeName(ctx, corObjectValue.ExactType);
					ulong num = (ulong)System.Convert.ChangeType(GetMember(ctx, null, objr, "value__").ObjectValue, typeof(ulong));
					ulong num2 = num;
					string text = null;
					foreach (ValueReference member in GetMembers(ctx, corObjectValue.ExactType, null, BindingFlags.Static | BindingFlags.Public))
					{
						ulong num3 = (ulong)System.Convert.ChangeType(member.ObjectValue, typeof(ulong));
						if (num == num3)
						{
							return member.Name;
						}
						if (flag && num3 != 0L && (num & num3) == num3)
						{
							text = ((text != null) ? (text + " | " + typeName + "." + member.Name) : (typeName + "." + member.Name));
							num2 &= ~num3;
						}
					}
					if (flag)
					{
						if (num2 == num)
						{
							return num.ToString();
						}
						if (num2 != 0L)
						{
							text = text + " | " + num2;
						}
						return text;
					}
					return num.ToString();
				}
				CorType corType = (CorType)GetValueType(ctx, objr);
				Tuple<MethodInfo, CorType> tuple = OverloadResolve(corEvaluationContext, "ToString", corType, new CorType[0], BindingFlags.Instance | BindingFlags.Public, throwIfNotFound: false);
				if (tuple != null && tuple.Item1.DeclaringType != null && tuple.Item1.DeclaringType.FullName != "System.Object")
				{
					object[] array = new object[0];
					object objr2 = RuntimeInvoke(ctx, corType, objr, "ToString", new object[0], array, array);
					CorStringValue corStringValue2 = GetRealObject(ctx, objr2) as CorStringValue;
					if (corStringValue2 != null)
					{
						return corStringValue2.String;
					}
				}
				return GetDisplayTypeName(ctx, corType);
			}
			return base.CallToString(ctx, realObject);
		}

		public override object CreateTypeObject(EvaluationContext ctx, object type)
		{
			CorType corType = (CorType)type;
			string value = GetTypeName(ctx, corType) + ", " + Path.GetFileNameWithoutExtension(corType.Class.Module.Assembly.Name);
			CorType targetType = (CorType)GetType(ctx, "System.Type");
			object[] argTypes = new object[1] { GetType(ctx, "System.String") };
			object[] argValues = new object[1] { CreateValue(ctx, value) };
			return RuntimeInvoke(ctx, targetType, null, "GetType", argTypes, argValues);
		}

		public CorValRef GetBoxedArg(CorEvaluationContext ctx, CorValRef val, Type argType)
		{
			if (argType == typeof(object) && IsValueType(ctx, val))
			{
				return Box(ctx, val);
			}
			return val;
		}

		private static bool IsValueType(CorEvaluationContext ctx, CorValRef val)
		{
			CorValue realObject = GetRealObject(ctx, val);
			if (realObject.Type == CorElementType.ValueType)
			{
				return true;
			}
			return realObject is CorGenericValue;
		}

		private CorValRef Box(CorEvaluationContext ctx, CorValRef val)
		{
			CorValRef arr = new CorValRef(() => ctx.Session.NewArray(ctx, (CorType)GetValueType(ctx, val), 1));
			new ArrayAdaptor(ctx, new CorValRef<CorArrayValue>(() => (CorArrayValue)GetRealObject(ctx, arr))).SetElement(new int[1], val);
			CorType targetType = (CorType)GetType(ctx, "System.Array");
			object[] argTypes = new object[1] { GetType(ctx, "System.Int32") };
			return (CorValRef)RuntimeInvoke(ctx, targetType, arr, "GetValue", argTypes, new object[1] { CreateValue(ctx, 0) });
		}

		public override bool HasMethod(EvaluationContext gctx, object gtargetType, string methodName, object[] ggenericArgTypes, object[] gargTypes, BindingFlags flags)
		{
			CorType type = (CorType)gtargetType;
			CorType[] argtypes = ((gargTypes != null) ? CastArray<CorType>(gargTypes) : null);
			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
			flags = flags | BindingFlags.Public | BindingFlags.NonPublic;
			return OverloadResolve(ctx, methodName, type, argtypes, flags, throwIfNotFound: false) != null;
		}

		public override object RuntimeInvoke(EvaluationContext gctx, object gtargetType, object gtarget, string methodName, object[] ggenericArgTypes, object[] gargTypes, object[] gargValues)
		{
			CorType type = (CorType)gtargetType;
			CorValRef target = (CorValRef)gtarget;
			CorType[] argtypes = CastArray<CorType>(gargTypes);
			CorValRef[] argValues = CastArray<CorValRef>(gargValues);
			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
			bindingFlags = ((target == null) ? (bindingFlags | BindingFlags.Static) : (bindingFlags | BindingFlags.Instance));
			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
			Tuple<MethodInfo, CorType> tuple = OverloadResolve(ctx, methodName, type, argtypes, bindingFlags, throwIfNotFound: true);
			if (tuple == null)
			{
				return null;
			}
			MethodInfo method = tuple.Item1;
			CorType methodOwner = tuple.Item2;
			ParameterInfo[] parameters = method.GetParameters();
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].ParameterType == typeof(object) && IsValueType(ctx, argValues[i]) && !IsEnum(ctx, argValues[i]))
				{
					argValues[i] = Box(ctx, argValues[i]);
				}
			}
			CorValRef corValRef = new CorValRef(delegate
			{
				CorModule corModule = null;
				corModule = ((methodOwner.Type != CorElementType.Array && methodOwner.Type != CorElementType.SZArray && !MetadataHelperFunctionsExtensions.CoreTypes.ContainsKey(methodOwner.Type)) ? methodOwner.Class.Module : ((CorType)ctx.Adapter.GetType(ctx, "System.Object")).Class.Module);
				CorFunction functionFromToken = corModule.GetFunctionFromToken(method.MetadataToken);
				CorValue[] array = new CorValue[argValues.Length];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = argValues[j].Val;
				}
				return (methodOwner.Type == CorElementType.Array || methodOwner.Type == CorElementType.SZArray || MetadataHelperFunctionsExtensions.CoreTypes.ContainsKey(methodOwner.Type)) ? ctx.RuntimeInvoke(functionFromToken, new CorType[0], (target != null) ? target.Val : null, array) : ctx.RuntimeInvoke(functionFromToken, methodOwner.TypeParameters, (target != null) ? target.Val : null, array);
			});
			if (!(corValRef.Val == null))
			{
				return corValRef;
			}
			return null;
		}

		private Tuple<MethodInfo, CorType> OverloadResolve(CorEvaluationContext ctx, string methodName, CorType type, CorType[] argtypes, BindingFlags flags, bool throwIfNotFound)
		{
			List<Tuple<MethodInfo, CorType>> list = new List<Tuple<MethodInfo, CorType>>();
			CorType corType = type;
			while (corType != null)
			{
				Type typeInfo = corType.GetTypeInfo(ctx.Session);
				MethodInfo[] methods = typeInfo.GetMethods(flags);
				foreach (MethodInfo methodInfo in methods)
				{
					if (methodInfo.Name == methodName || (!ctx.CaseSensitive && methodInfo.Name.Equals(methodName, StringComparison.CurrentCultureIgnoreCase)))
					{
						if (argtypes == null)
						{
							return Tuple.Create(methodInfo, corType);
						}
						if (methodInfo.GetParameters().Length == argtypes.Length)
						{
							list.Add(Tuple.Create(methodInfo, corType));
						}
					}
				}
				if ((argtypes == null && list.Count > 0) || methodName == ".ctor")
				{
					break;
				}
				if ((typeInfo.BaseType == null && typeInfo.FullName != "System.Object") || corType.Type == CorElementType.Array || corType.Type == CorElementType.SZArray || corType.Type == CorElementType.String)
				{
					corType = ctx.Adapter.GetType(ctx, "System.Object") as CorType;
					continue;
				}
				if (typeInfo.BaseType != null && typeInfo.BaseType.FullName == "System.ValueType")
				{
					corType = ctx.Adapter.GetType(ctx, "System.ValueType") as CorType;
					continue;
				}
				try
				{
					corType = corType.Base;
				}
				catch (Exception)
				{
					corType = null;
				}
			}
			return OverloadResolve(ctx, GetTypeName(ctx, type), methodName, argtypes, list, throwIfNotFound);
		}

		private bool IsApplicable(CorEvaluationContext ctx, MethodInfo method, CorType[] types, out string error, out int matchCount)
		{
			ParameterInfo[] parameters = method.GetParameters();
			matchCount = 0;
			for (int i = 0; i < types.Length; i++)
			{
				Type parameterType = parameters[i].ParameterType;
				if (parameterType.FullName == GetTypeName(ctx, types[i]))
				{
					matchCount++;
				}
				else if (!IsAssignableFrom(ctx, parameterType, types[i]))
				{
					error = $"Argument {i}: Cannot implicitly convert `{GetTypeName(ctx, types[i])}' to `{parameterType.FullName}'";
					return false;
				}
			}
			error = null;
			return true;
		}

		private Tuple<MethodInfo, CorType> OverloadResolve(CorEvaluationContext ctx, string typeName, string methodName, CorType[] argtypes, List<Tuple<MethodInfo, CorType>> candidates, bool throwIfNotFound)
		{
			if (candidates.Count == 1)
			{
				if (IsApplicable(ctx, candidates[0].Item1, argtypes, out var error, out var _))
				{
					return candidates[0];
				}
				if (throwIfNotFound)
				{
					throw new EvaluatorException("Invalid arguments for method `{0}': {1}", methodName, error);
				}
				return null;
			}
			if (candidates.Count == 0)
			{
				if (throwIfNotFound)
				{
					throw new EvaluatorException("Method `{0}' not found in type `{1}'.", methodName, typeName);
				}
				return null;
			}
			Tuple<MethodInfo, CorType> tuple = null;
			int num = -1;
			//bool flag = false;
			foreach (Tuple<MethodInfo, CorType> candidate in candidates)
			{
				if (IsApplicable(ctx, candidate.Item1, argtypes, out var _, out var matchCount2))
				{
					/*if (matchCount2 == num)
					{
						flag = true;
					}
					else */
					if (matchCount2 > num)
					{
						tuple = candidate;
						num = matchCount2;
						//flag = false;
					}
				}
			}
			if (tuple == null)
			{
				if (!throwIfNotFound)
				{
					return null;
				}
				if (methodName != null)
				{
					throw new EvaluatorException("Invalid arguments for method `{0}'.", methodName);
				}
				throw new EvaluatorException("Invalid arguments for indexer.");
			}
			return tuple;
		}

		public override string[] GetImportedNamespaces(EvaluationContext ctx)
		{
			HashSet<string> hashSet = new HashSet<string>();
			foreach (Type allType in GetAllTypes(ctx))
			{
				hashSet.Add(allType.Namespace);
			}
			string[] array = new string[hashSet.Count];
			hashSet.CopyTo(array);
			return array;
		}

		public override void GetNamespaceContents(EvaluationContext ctx, string namspace, out string[] childNamespaces, out string[] childTypes)
		{
			HashSet<string> hashSet = new HashSet<string>();
			HashSet<string> hashSet2 = new HashSet<string>();
			string value = ((namspace.Length > 0) ? (namspace + ".") : "");
			foreach (Type allType in GetAllTypes(ctx))
			{
				if (allType.Namespace == namspace || allType.Namespace.StartsWith(value, StringComparison.InvariantCulture))
				{
					hashSet.Add(allType.Namespace);
					hashSet2.Add(allType.FullName);
				}
			}
			childNamespaces = new string[hashSet.Count];
			hashSet.CopyTo(childNamespaces);
			childTypes = new string[hashSet2.Count];
			hashSet2.CopyTo(childTypes);
		}

		private bool IsAssignableFrom(CorEvaluationContext ctx, Type baseType, CorType ctype)
		{
			if (baseType is MethodGenericParameter)
			{
				return true;
			}
			string fullName = baseType.FullName;
			string typeName = GetTypeName(ctx, ctype);
			if (fullName == "System.Object")
			{
				return true;
			}
			if (fullName == typeName)
			{
				return true;
			}
			if (MetadataHelperFunctionsExtensions.CoreTypes.ContainsKey(ctype.Type))
			{
				return false;
			}
			CorElementType type = ctype.Type;
			if ((uint)(type - 15) <= 1u || type == CorElementType.Array || type == CorElementType.SZArray)
			{
				return false;
			}
			while (ctype != null)
			{
				if (GetTypeName(ctx, ctype) == fullName)
				{
					return true;
				}
				ctype = ctype.Base;
			}
			return false;
		}

		public override object TryCast(EvaluationContext ctx, object val, object type)
		{
			CorType corType = (CorType)GetValueType(ctx, val);
			CorValue realObject = GetRealObject(ctx, val);
			CorReferenceValue corReferenceValue = realObject.CastToReferenceValue();
			if (corReferenceValue != null && corReferenceValue.IsNull)
			{
				return val;
			}
			string typeName = GetTypeName(ctx, type);
			string valueTypeName = GetValueTypeName(ctx, val);
			if (typeName == "System.Object")
			{
				return val;
			}
			if (typeName == valueTypeName)
			{
				return val;
			}
			if (realObject is CorStringValue)
			{
				if (!(valueTypeName == typeName))
				{
					return null;
				}
				return val;
			}
			if (realObject is CorArrayValue)
			{
				if (!(valueTypeName == typeName) && !(valueTypeName == "System.Array"))
				{
					return null;
				}
				return val;
			}
			CorGenericValue corGenericValue = realObject as CorGenericValue;
			if (corGenericValue != null)
			{
				Type type2 = Type.GetType(typeName);
				try
				{
					if (type2 != null && type2.IsPrimitive && type2 != typeof(string))
					{
						object value = corGenericValue.GetValue();
						try
						{
							value = System.Convert.ChangeType(value, type2);
						}
						catch
						{
							return null;
						}
						return CreateValue(ctx, value);
					}
					if (IsEnum(ctx, (CorType)type))
					{
						return CreateEnum(ctx, (CorType)type, val);
					}
				}
				catch
				{
				}
			}
			if (realObject is CorObjectValue)
			{
				CorObjectValue corObjectValue = (CorObjectValue)realObject;
				if (IsEnum(ctx, corObjectValue.ExactType))
				{
					ValueReference member = GetMember(ctx, null, val, "value__");
					return TryCast(ctx, member.Value, type);
				}
				while (corType != null)
				{
					if (GetTypeName(ctx, corType) == typeName)
					{
						return val;
					}
					corType = corType.Base;
				}
				return null;
			}
			return null;
		}

		public bool IsPointer(CorType targetType)
		{
			return targetType.Type == CorElementType.Ptr;
		}

		public object CreateEnum(EvaluationContext ctx, CorType type, object val)
		{
			object type2 = GetType(ctx, "System.Enum");
			object obj = CreateTypeObject(ctx, type);
			object[] argTypes = new object[2]
			{
			GetValueType(ctx, obj),
			GetValueType(ctx, val)
			};
			object[] argValues = new object[2] { obj, val };
			return RuntimeInvoke(ctx, type2, null, "ToObject", argTypes, argValues);
		}

		public bool IsEnum(EvaluationContext ctx, CorType targetType)
		{
			if ((targetType.Type == CorElementType.ValueType || targetType.Type == CorElementType.Class) && targetType.Base != null)
			{
				return GetTypeName(ctx, targetType.Base) == "System.Enum";
			}
			return false;
		}

		public override object CreateValue(EvaluationContext gctx, object value)
		{
			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
			if (value is string)
			{
				return new CorValRef(() => ctx.Session.NewString(ctx, (string)value));
			}
			foreach (KeyValuePair<CorElementType, Type> coreType in MetadataHelperFunctionsExtensions.CoreTypes)
			{
				if (coreType.Value == value.GetType())
				{
					CorValue corValue = ctx.Eval.CreateValue(coreType.Key, null);
					corValue.CastToGenericValue().SetValue(value);
					return new CorValRef(corValue);
				}
			}
			ctx.WriteDebuggerError(new NotSupportedException($"Unable to create value for type: {value.GetType()}"));
			return null;
		}

		public override object CreateValue(EvaluationContext ctx, object type, params object[] gargs)
		{
			CorValRef[] args = CastArray<CorValRef>(gargs);
			return new CorValRef(() => CreateCorValue(ctx, (CorType)type, args));
		}

		public CorValue CreateCorValue(EvaluationContext ctx, CorType type, params CorValRef[] args)
		{
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			CorValue[] array = new CorValue[args.Length];
			CorType[] array2 = new CorType[args.Length];
			for (int i = 0; i < args.Length; i++)
			{
				array[i] = args[i].Val;
				array2[i] = array[i].ExactType;
			}
			MethodInfo methodInfo = null;
			Tuple<MethodInfo, CorType> tuple = OverloadResolve(corEvaluationContext, ".ctor", type, array2, BindingFlags.Instance | BindingFlags.Public, throwIfNotFound: false);
			if (tuple != null)
			{
				methodInfo = tuple.Item1;
			}
			if (methodInfo == null)
			{
				MethodInfo[] methods = type.GetTypeInfo(corEvaluationContext.Session).GetMethods();
				foreach (MethodInfo methodInfo2 in methods)
				{
					if (methodInfo2.IsSpecialName && methodInfo2.Name == ".ctor" && methodInfo2.GetParameters().Length == 1)
					{
						methodInfo = methodInfo2;
						break;
					}
				}
			}
			if (methodInfo == null)
			{
				return null;
			}
			CorFunction functionFromToken = type.Class.Module.GetFunctionFromToken(methodInfo.MetadataToken);
			return corEvaluationContext.RuntimeInvoke(functionFromToken, type.TypeParameters, null, array);
		}

		public override object CreateNullValue(EvaluationContext gctx, object type)
		{
			return new CorValRef(((CorEvaluationContext)gctx).Eval.CreateValueForType((CorType)type));
		}

		public override ICollectionAdaptor CreateArrayAdaptor(EvaluationContext ctx, object arr)
		{
			CorValue realObject = GetRealObject(ctx, arr);
			if (realObject is CorArrayValue)
			{
				return new ArrayAdaptor(ctx, new CorValRef<CorArrayValue>((CorArrayValue)realObject, () => (CorArrayValue)GetRealObject(ctx, arr)));
			}
			return null;
		}

		public override IStringAdaptor CreateStringAdaptor(EvaluationContext ctx, object str)
		{
			CorValue realObject = GetRealObject(ctx, str);
			if (realObject is CorStringValue)
			{
				return new StringAdaptor(ctx, (CorValRef)str, (CorStringValue)realObject);
			}
			return null;
		}

		public static CorValue GetRealObject(EvaluationContext cctx, object objr)
		{
			if (objr == null)
			{
				return null;
			}
			CorValue corValue = objr as CorValue;
			if (corValue != null)
			{
				return GetRealObject(cctx, corValue);
			}
			if (objr is CorValRef corValRef)
			{
				return GetRealObject(cctx, corValRef.Val);
			}
			return null;
		}

		public static CorValue GetRealObject(EvaluationContext ctx, CorValue obj)
		{
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			if (obj == null)
			{
				return null;
			}
			try
			{
				if (obj is CorStringValue)
				{
					return obj;
				}
				if (obj is CorGenericValue)
				{
					return obj;
				}
				CorArrayValue corArrayValue = obj.CastToArrayValue();
				if (corArrayValue != null)
				{
					return corArrayValue;
				}
				CorReferenceValue corReferenceValue = obj.CastToReferenceValue();
				if (corReferenceValue != null)
				{
					corEvaluationContext.Session.WaitUntilStopped();
					if (corReferenceValue.IsNull)
					{
						return corReferenceValue;
					}
					return GetRealObject(corEvaluationContext, corReferenceValue.Dereference());
				}
				corEvaluationContext.Session.WaitUntilStopped();
				CorBoxValue corBoxValue = obj.CastToBoxValue();
				if (corBoxValue != null)
				{
					return Unbox(ctx, corBoxValue);
				}
				if (obj.ExactType.Type == CorElementType.String)
				{
					return obj.CastToStringValue();
				}
				if (MetadataHelperFunctionsExtensions.CoreTypes.ContainsKey(obj.Type))
				{
					CorGenericValue corGenericValue = obj.CastToGenericValue();
					if (corGenericValue != null)
					{
						return corGenericValue;
					}
				}
				if (!(obj is CorObjectValue))
				{
					return obj.CastToObjectValue();
				}
				return obj;
			}
			catch
			{
				throw;
			}
		}

		private static CorValue Unbox(EvaluationContext ctx, CorBoxValue boxVal)
		{
			CorObjectValue corObjectValue = boxVal.GetObject();
			Type type = Type.GetType(ctx.Adapter.GetTypeName(ctx, corObjectValue.ExactType));
			if (type != null && type.IsPrimitive)
			{
				type = corObjectValue.ExactType.GetTypeInfo(((CorEvaluationContext)ctx).Session);
				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (FieldInfo fieldInfo in fields)
				{
					if (fieldInfo.Name == "m_value")
					{
						CorValue fieldValue = corObjectValue.GetFieldValue(corObjectValue.ExactType.Class, fieldInfo.MetadataToken);
						return GetRealObject(ctx, fieldValue);
					}
				}
			}
			return GetRealObject(ctx, corObjectValue);
		}

		public override object GetEnclosingType(EvaluationContext gctx)
		{
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)gctx;
			if (corEvaluationContext.Frame.FrameType != CorFrameType.ILFrame || corEvaluationContext.Frame.Function == null)
			{
				return null;
			}
			CorClass corClass = corEvaluationContext.Frame.Function.Class;
			List<CorType> list = new List<CorType>();
			foreach (CorType typeParameter in corEvaluationContext.Frame.TypeParameters)
			{
				list.Add(typeParameter);
			}
			return corClass.GetParameterizedType(CorElementType.Class, list.ToArray());
		}

		public override IEnumerable<EnumMember> GetEnumMembers(EvaluationContext ctx, object tt)
		{
			CorType type = (CorType)tt;
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			Type typeInfo = type.GetTypeInfo(corEvaluationContext.Session);
			FieldInfo[] fields = typeInfo.GetFields(BindingFlags.Static | BindingFlags.Public);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (fieldInfo.IsLiteral && fieldInfo.IsStatic)
				{
					object value = fieldInfo.GetValue(null);
					yield return new EnumMember
					{
						Value = (long)System.Convert.ChangeType(value, typeof(long)),
						Name = fieldInfo.Name
					};
				}
			}
		}

		public override ValueReference GetIndexerReference(EvaluationContext ctx, object target, object[] indices)
		{
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			CorType corType = GetValueType(ctx, target) as CorType;
			CorValRef[] array = new CorValRef[indices.Length];
			CorType[] array2 = new CorType[indices.Length];
			for (int i = 0; i < indices.Length; i++)
			{
				array2[i] = (CorType)GetValueType(ctx, indices[i]);
				array[i] = (CorValRef)indices[i];
			}
			List<Tuple<MethodInfo, CorType>> list = new List<Tuple<MethodInfo, CorType>>();
			List<PropertyInfo> list2 = new List<PropertyInfo>();
			List<CorType> list3 = new List<CorType>();
			CorType corType2 = corType;
			while (corType2 != null)
			{
				PropertyInfo[] properties = corType2.GetTypeInfo(corEvaluationContext.Session).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (PropertyInfo propertyInfo in properties)
				{
					MethodInfo methodInfo = null;
					try
					{
						methodInfo = (propertyInfo.CanRead ? propertyInfo.GetGetMethod(nonPublic: true) : null);
					}
					catch
					{
					}
					if (methodInfo != null && !methodInfo.IsStatic && methodInfo.GetParameters().Length != 0)
					{
						list.Add(Tuple.Create(methodInfo, corType2));
						list2.Add(propertyInfo);
						list3.Add(corType2);
					}
				}
				if (corEvaluationContext.Adapter.IsPrimitive(ctx, target))
				{
					break;
				}
				corType2 = corType2.Base;
			}
			Tuple<MethodInfo, CorType> item = OverloadResolve(corEvaluationContext, GetTypeName(ctx, corType), null, array2, list, throwIfNotFound: true);
			int index = list.IndexOf(item);
			if (list2[index].GetGetMethod(nonPublic: true) == null)
			{
				return null;
			}
			return new PropertyReference(ctx, list2[index], (CorValRef)target, list3[index], array);
		}

		public override bool HasMember(EvaluationContext ctx, object tt, string memberName, BindingFlags bindingFlags)
		{
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			CorType corType = (CorType)tt;
			while (corType != null)
			{
				Type typeInfo = corType.GetTypeInfo(corEvaluationContext.Session);
				if (typeInfo.GetField(memberName, bindingFlags) != null)
				{
					return true;
				}
				PropertyInfo property = typeInfo.GetProperty(memberName, bindingFlags);
				if (property != null && (property.CanRead ? property.GetGetMethod(bindingFlags.HasFlag(BindingFlags.NonPublic)) : null) != null)
				{
					return true;
				}
				if (bindingFlags.HasFlag(BindingFlags.DeclaredOnly))
				{
					break;
				}
				corType = corType.Base;
			}
			return false;
		}

		protected override IEnumerable<ValueReference> GetMembers(EvaluationContext ctx, object tt, object gval, BindingFlags bindingFlags)
		{
			Dictionary<string, PropertyInfo> subProps = new Dictionary<string, PropertyInfo>();
			CorType t = (CorType)tt;
			CorValRef val = (CorValRef)gval;
			CorType corType = null;
			if (gval != null && (bindingFlags & BindingFlags.Instance) != BindingFlags.Default)
			{
				corType = GetValueType(ctx, gval) as CorType;
			}
			if (t.Type == CorElementType.Class && t.Class == null)
			{
				yield break;
			}
			CorEvaluationContext cctx = (CorEvaluationContext)ctx;
			while (corType != null && corType != t)
			{
				PropertyInfo[] properties = corType.GetTypeInfo(cctx.Session).GetProperties(bindingFlags | BindingFlags.DeclaredOnly);
				foreach (PropertyInfo propertyInfo in properties)
				{
					MethodInfo getMethod = propertyInfo.GetGetMethod(nonPublic: true);
					if (!(getMethod == null) && getMethod.GetParameters().Length == 0 && !getMethod.IsAbstract && getMethod.IsVirtual && !getMethod.IsStatic && (!getMethod.IsPublic || (bindingFlags & BindingFlags.Public) != BindingFlags.Default) && (getMethod.IsPublic || (bindingFlags & BindingFlags.NonPublic) != BindingFlags.Default))
					{
						subProps[propertyInfo.Name] = propertyInfo;
					}
				}
				corType = corType.Base;
			}
			while (t != null)
			{
				Type type = t.GetTypeInfo(cctx.Session);
				FieldInfo[] fields = type.GetFields(bindingFlags);
				foreach (FieldInfo fieldInfo in fields)
				{
					if ((!fieldInfo.IsStatic || (bindingFlags & BindingFlags.Static) != BindingFlags.Default) && (fieldInfo.IsStatic || (bindingFlags & BindingFlags.Instance) != BindingFlags.Default) && (!fieldInfo.IsPublic || (bindingFlags & BindingFlags.Public) != BindingFlags.Default) && (fieldInfo.IsPublic || (bindingFlags & BindingFlags.NonPublic) != BindingFlags.Default))
					{
						yield return new FieldReference(ctx, val, t, fieldInfo);
					}
				}
				PropertyInfo[] properties2 = type.GetProperties(bindingFlags);
				foreach (PropertyInfo prop in properties2)
				{
					MethodInfo methodInfo = null;
					try
					{
						methodInfo = (prop.CanRead ? prop.GetGetMethod(nonPublic: true) : null);
					}
					catch
					{
					}
					if (methodInfo == null || methodInfo.GetParameters().Length != 0 || methodInfo.IsAbstract || (methodInfo.IsStatic && (bindingFlags & BindingFlags.Static) == 0) || (!methodInfo.IsStatic && (bindingFlags & BindingFlags.Instance) == 0) || (methodInfo.IsPublic && (bindingFlags & BindingFlags.Public) == 0) || (!methodInfo.IsPublic && (bindingFlags & BindingFlags.NonPublic) == 0))
					{
						continue;
					}
					if (methodInfo.IsVirtual && subProps.TryGetValue(prop.Name, out var value))
					{
						methodInfo = value.GetGetMethod(nonPublic: true);
						if (!(methodInfo == null))
						{
							CorType declaringType = GetType(ctx, value.DeclaringType.FullName) as CorType;
							yield return new PropertyReference(ctx, value, val, declaringType);
						}
					}
					else
					{
						yield return new PropertyReference(ctx, prop, val, t);
					}
				}
				if ((bindingFlags & BindingFlags.DeclaredOnly) == 0)
				{
					t = t.Base;
					continue;
				}
				break;
			}
		}

		private static T FindByName<T>(IEnumerable<T> elems, Func<T, string> getName, string name, bool caseSensitive)
		{
			T result = default(T);
			foreach (T elem in elems)
			{
				string text = getName(elem);
				if (text == name)
				{
					return elem;
				}
				if (!caseSensitive && text.Equals(name, StringComparison.CurrentCultureIgnoreCase))
				{
					result = elem;
				}
			}
			return result;
		}

		private static bool IsStatic(PropertyInfo prop)
		{
			return (prop.GetGetMethod(nonPublic: true) ?? prop.GetSetMethod(nonPublic: true)).IsStatic;
		}

		private static bool IsAnonymousType(Type type)
		{
			return type.Name.StartsWith("<>__AnonType", StringComparison.Ordinal);
		}

		private static bool IsCompilerGenerated(FieldInfo field)
		{
			return field.GetCustomAttributes(inherit: true).Any((object v) => v is DebuggerHiddenAttribute);
		}

		protected override ValueReference GetMember(EvaluationContext ctx, object t, object co, string name)
		{
			CorEvaluationContext corEvaluationContext = ctx as CorEvaluationContext;
			CorType corType = t as CorType;
			if (IsNullableType(ctx, t))
			{
				if (!(name == "Value"))
				{
					if (name == "HasValue")
					{
						name = "hasValue";
					}
				}
				else
				{
					name = "value";
				}
			}
			while (corType != null)
			{
				Type typeInfo = corType.GetTypeInfo(corEvaluationContext.Session);
				FieldInfo fieldInfo = FindByName(typeInfo.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), (FieldInfo f) => f.Name, name, ctx.CaseSensitive);
				if (fieldInfo != null && (fieldInfo.IsStatic || co != null))
				{
					return new FieldReference(ctx, co as CorValRef, corType, fieldInfo);
				}
				PropertyInfo propertyInfo = FindByName(typeInfo.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), (PropertyInfo p) => p.Name, name, ctx.CaseSensitive);
				if (propertyInfo != null && (IsStatic(propertyInfo) || co != null))
				{
					string name2 = string.Format("<{0}>{1}", propertyInfo.Name, IsAnonymousType(typeInfo) ? "" : "k__BackingField");
					if ((fieldInfo = FindByName(typeInfo.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic), (FieldInfo f) => f.Name, name2, caseSensitive: true)) != null && IsCompilerGenerated(fieldInfo))
					{
						return new FieldReference(ctx, co as CorValRef, corType, fieldInfo, propertyInfo.Name, ObjectValueFlags.Property);
					}
					if (propertyInfo.GetGetMethod(nonPublic: true) == null)
					{
						return null;
					}
					return new PropertyReference(ctx, propertyInfo, co as CorValRef, corType);
				}
				corType = corType.Base;
			}
			return null;
		}

		private static bool IsIEnumerable(Type type)
		{
			if (type.Namespace == "System.Collections" && type.Name == "IEnumerable")
			{
				return true;
			}
			if (type.Namespace == "System.Collections.Generic" && type.Name == "IEnumerable`1")
			{
				return true;
			}
			return false;
		}

		private static bool IsIEnumerable(CorType type, CorDebuggerSession session)
		{
			return IsIEnumerable(type.GetTypeInfo(session));
		}

		protected override CompletionData GetMemberCompletionData(EvaluationContext ctx, ValueReference vr)
		{
			HashSet<string> hashSet = new HashSet<string>();
			HashSet<string> hashSet2 = new HashSet<string>();
			HashSet<string> hashSet3 = new HashSet<string>();
			CompletionData completionData = new CompletionData();
			CorType corType = vr.Type as CorType;
			bool flag = false;
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			Type typeInfo;
			while (corType != null)
			{
				typeInfo = corType.GetTypeInfo(corEvaluationContext.Session);
				if (!flag && IsIEnumerable(typeInfo))
				{
					flag = true;
				}
				FieldInfo[] fields = typeInfo.GetFields();
				foreach (FieldInfo fieldInfo in fields)
				{
					if (!fieldInfo.IsStatic && !fieldInfo.IsSpecialName && fieldInfo.IsPublic && hashSet3.Add(fieldInfo.Name))
					{
						completionData.Items.Add(new CompletionItem(fieldInfo.Name, FieldReference.GetFlags(fieldInfo)));
					}
				}
				PropertyInfo[] properties = typeInfo.GetProperties();
				foreach (PropertyInfo propertyInfo in properties)
				{
					MethodInfo getMethod = propertyInfo.GetGetMethod(nonPublic: true);
					if (!(getMethod == null) && !getMethod.IsStatic && getMethod.IsPublic && hashSet.Add(propertyInfo.Name))
					{
						completionData.Items.Add(new CompletionItem(propertyInfo.Name, PropertyReference.GetFlags(propertyInfo)));
					}
				}
				MethodInfo[] methods = typeInfo.GetMethods();
				foreach (MethodInfo methodInfo in methods)
				{
					if (!methodInfo.IsStatic && !methodInfo.IsConstructor && !methodInfo.IsSpecialName && methodInfo.IsPublic && hashSet2.Add(methodInfo.Name))
					{
						completionData.Items.Add(new CompletionItem(methodInfo.Name, ObjectValueFlags.Method | ObjectValueFlags.Public));
					}
				}
				corType = ((!(typeInfo.BaseType == null) || !(typeInfo.FullName != "System.Object")) ? corType.Base : (ctx.Adapter.GetType(ctx, "System.Object") as CorType));
			}
			corType = vr.Type as CorType;
			typeInfo = corType.GetTypeInfo(corEvaluationContext.Session);
			Type[] interfaces = typeInfo.GetInterfaces();
			foreach (Type type in interfaces)
			{
				if (!flag && IsIEnumerable(type))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				CorType corType2 = ctx.Adapter.GetType(ctx, "System.Linq.Enumerable") as CorType;
				if (corType2 != null)
				{
					MethodInfo[] methods = corType2.GetTypeInfo(corEvaluationContext.Session).GetMethods();
					foreach (MethodInfo methodInfo2 in methods)
					{
						if (methodInfo2.IsStatic && !methodInfo2.IsConstructor && !methodInfo2.IsSpecialName && methodInfo2.IsPublic && hashSet2.Add(methodInfo2.Name))
						{
							completionData.Items.Add(new CompletionItem(methodInfo2.Name, ObjectValueFlags.Method | ObjectValueFlags.Public));
						}
					}
				}
			}
			completionData.ExpressionLength = 0;
			return completionData;
		}

		public override object TargetObjectToObject(EvaluationContext ctx, object objr)
		{
			CorValue realObject = GetRealObject(ctx, objr);
			if (realObject is CorReferenceValue && ((CorReferenceValue)realObject).IsNull)
			{
				return null;
			}
			CorStringValue corStringValue = realObject as CorStringValue;
			if (corStringValue != null)
			{
				string text;
				if (ctx.Options.EllipsizeStrings)
				{
					text = corStringValue.String;
					if (text.Length > ctx.Options.EllipsizedLength)
					{
						text = text.Substring(0, ctx.Options.EllipsizedLength) + "…";
					}
				}
				else
				{
					text = corStringValue.String;
				}
				return text;
			}
			if (realObject as CorArrayValue != null)
			{
				return base.TargetObjectToObject(ctx, objr);
			}
			if (realObject as CorObjectValue != null)
			{
				return base.TargetObjectToObject(ctx, objr);
			}
			CorGenericValue corGenericValue = realObject as CorGenericValue;
			if (corGenericValue != null)
			{
				return corGenericValue.GetValue();
			}
			return base.TargetObjectToObject(ctx, objr);
		}

		private static bool InGeneratedClosureOrIteratorType(CorEvaluationContext ctx)
		{
			MethodInfo methodInfo = ctx.Frame.Function.GetMethodInfo(ctx.Session);
			if (methodInfo == null || methodInfo.IsStatic)
			{
				return false;
			}
			return IsGeneratedType(methodInfo.DeclaringType);
		}

		internal static bool IsGeneratedType(string name)
		{
			if (name[0] == '<')
			{
				if (name.IndexOf(">c__", StringComparison.Ordinal) <= 0)
				{
					return name.IndexOf(">d__", StringComparison.Ordinal) > 0;
				}
				return true;
			}
			return false;
		}

		internal static bool IsGeneratedType(Type tm)
		{
			return IsGeneratedType(tm.Name);
		}

		private ValueReference GetHoistedThisReference(CorEvaluationContext cx)
		{
			CorValRef val = new CorValRef(() => cx.Frame.GetArgument(0));
			CorType type = (CorType)GetValueType(cx, val);
			return GetHoistedThisReference(cx, type, val);
		}

		private ValueReference GetHoistedThisReference(CorEvaluationContext cx, CorType type, object val)
		{
			Type typeInfo = type.GetTypeInfo(cx.Session);
			CorValRef thisobj = (CorValRef)val;
			FieldInfo[] fields = typeInfo.GetFields();
			foreach (FieldInfo field in fields)
			{
				if (IsHoistedThisReference(field))
				{
					return new FieldReference(cx, thisobj, type, field, "this", ObjectValueFlags.Literal);
				}
				if (IsClosureReferenceField(field))
				{
					FieldReference fieldReference = new FieldReference(cx, thisobj, type, field);
					CorType type2 = (CorType)GetValueType(cx, fieldReference.Value);
					ValueReference hoistedThisReference = GetHoistedThisReference(cx, type2, fieldReference.Value);
					if (hoistedThisReference != null)
					{
						return hoistedThisReference;
					}
				}
			}
			return null;
		}

		private static bool IsHoistedThisReference(FieldInfo field)
		{
			if (!(field.Name == "$this"))
			{
				if (field.Name.StartsWith("<>", StringComparison.Ordinal))
				{
					return field.Name.EndsWith("__this", StringComparison.Ordinal);
				}
				return false;
			}
			return true;
		}

		private static bool IsClosureReferenceField(FieldInfo field)
		{
			if (!field.Name.StartsWith("CS$<>", StringComparison.Ordinal) && !field.Name.StartsWith("<>f__ref", StringComparison.Ordinal))
			{
				return field.Name.StartsWith("<>8__", StringComparison.Ordinal);
			}
			return true;
		}

		private static bool IsClosureReferenceLocal(ISymbolVariable local)
		{
			if (local.Name == null)
			{
				return false;
			}
			if (local.Name.Length != 0 && local.Name[0] != '<' && !local.Name.StartsWith("$locvar", StringComparison.Ordinal))
			{
				return local.Name.StartsWith("CS$<>", StringComparison.Ordinal);
			}
			return true;
		}

		private static bool IsGeneratedTemporaryLocal(ISymbolVariable local)
		{
			if (local.Name != null)
			{
				if (!local.Name.StartsWith("CS$", StringComparison.Ordinal))
				{
					return local.Name.StartsWith("<>t__", StringComparison.Ordinal);
				}
				return true;
			}
			return false;
		}

		protected override ValueReference OnGetThisReference(EvaluationContext ctx)
		{
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			if (corEvaluationContext.Frame.FrameType != CorFrameType.ILFrame || corEvaluationContext.Frame.Function == null)
			{
				return null;
			}
			if (InGeneratedClosureOrIteratorType(corEvaluationContext))
			{
				return GetHoistedThisReference(corEvaluationContext);
			}
			return GetThisReference(corEvaluationContext);
		}

		private ValueReference GetThisReference(CorEvaluationContext ctx)
		{
			MethodInfo methodInfo = ctx.Frame.Function.GetMethodInfo(ctx.Session);
			if (methodInfo == null || methodInfo.IsStatic)
				return null;
			CorValRef var = new CorValRef(delegate
			{
				CorValue argument = ctx.Frame.GetArgument(0);
				return (argument.Type == CorElementType.ByRef) ? argument.CastToReferenceValue().Dereference() : argument;
			});
			return new VariableReference(ctx, var, "this", ObjectValueFlags.Variable | ObjectValueFlags.ReadOnly);
		}

		private static VariableReference CreateParameterReference(CorEvaluationContext ctx, int paramIndex, string paramName, ObjectValueFlags flags = ObjectValueFlags.Parameter)
		{
			return new VariableReference(var: new CorValRef(() => ctx.Frame.GetArgument(paramIndex)), ctx: ctx, name: paramName, flags: flags);
		}

		protected override IEnumerable<ValueReference> OnGetParameters(EvaluationContext gctx)
		{
			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
			if (ctx.Frame.FrameType == CorFrameType.ILFrame && ctx.Frame.Function != null)
			{
				MethodInfo met = ctx.Frame.Function.GetMethodInfo(ctx.Session);
				if (met != null)
				{
					ParameterInfo[] parameters = met.GetParameters();
					foreach (ParameterInfo pi in parameters)
					{
						int pos = pi.Position;
						if (met.IsStatic)
						{
							pos--;
						}
						VariableReference variableReference = CreateParameterReference(ctx, pos, pi.Name);
						if (variableReference != null)
						{
							yield return variableReference;
						}
					}
					yield break;
				}
			}
			int count = ctx.Frame.GetArgumentCount();
			for (int n = 0; n < count; n++)
			{
				int locn = n;
				VariableReference variableReference2 = CreateParameterReference(ctx, locn, "arg_" + (locn + 1));
				if (variableReference2 != null)
				{
					yield return variableReference2;
				}
			}
		}

		protected override IEnumerable<ValueReference> OnGetLocalVariables(EvaluationContext ctx)
		{
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			if (InGeneratedClosureOrIteratorType(corEvaluationContext))
			{
				ValueReference thisReference = GetThisReference(corEvaluationContext);
				return GetHoistedLocalVariables(corEvaluationContext, thisReference).Union(GetLocalVariables(corEvaluationContext));
			}
			return GetLocalVariables(corEvaluationContext);
		}

		private IEnumerable<ValueReference> GetHoistedLocalVariables(CorEvaluationContext cx, ValueReference vthis)
		{
			if (vthis == null)
			{
				return new ValueReference[0];
			}
			object value = vthis.Value;
			if (IsNull(cx, value))
			{
				return new ValueReference[0];
			}
			CorType type = (CorType)vthis.Type;
			Type typeInfo = type.GetTypeInfo(cx.Session);
			bool flag = IsGeneratedType(typeInfo);
			List<ValueReference> list = new List<ValueReference>();
			FieldInfo[] fields = typeInfo.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (IsHoistedThisReference(fieldInfo))
				{
					continue;
				}
				if (IsClosureReferenceField(fieldInfo))
				{
					list.AddRange(GetHoistedLocalVariables(cx, new FieldReference(cx, (CorValRef)value, type, fieldInfo)));
				}
				else if (fieldInfo.Name[0] == '<')
				{
					if (flag)
					{
						string hoistedIteratorLocalName = GetHoistedIteratorLocalName(fieldInfo);
						if (!string.IsNullOrEmpty(hoistedIteratorLocalName))
						{
							list.Add(new FieldReference(cx, (CorValRef)value, type, fieldInfo, hoistedIteratorLocalName, ObjectValueFlags.Variable));
						}
					}
				}
				else if (!fieldInfo.Name.Contains("$"))
				{
					list.Add(new FieldReference(cx, (CorValRef)value, type, fieldInfo, fieldInfo.Name, ObjectValueFlags.Variable));
				}
			}
			return list;
		}

		private static string GetHoistedIteratorLocalName(FieldInfo field)
		{
			if (field.Name.StartsWith("<$>", StringComparison.Ordinal))
			{
				return field.Name.Substring(3);
			}
			if (field.Name[0] == '<')
			{
				int num = field.Name.IndexOf('>');
				if (num > 1)
				{
					return field.Name.Substring(1, num - 1);
				}
			}
			return null;
		}

		private IEnumerable<ValueReference> GetLocalVariables(CorEvaluationContext cx)
		{
			cx.Frame.GetIP(out int offset, out CorDebugMappingResult mr);
			return GetLocals(cx, null, offset, showHidden: false);
		}

		public override ValueReference GetCurrentException(EvaluationContext ctx)
		{
			CorEvaluationContext wctx = (CorEvaluationContext)ctx;
			CorValue exception = wctx.Thread.CurrentException;
			if (exception == null)
			{
				return null;
			}
			CorValRef var = new CorValRef(() => wctx.Session.GetHandle(exception));
			return new VariableReference(ctx, var, ctx.Options.CurrentExceptionTag, ObjectValueFlags.Variable);
		}

		private static VariableReference CreateLocalVariableReference(CorEvaluationContext ctx, int varIndex, string varName, ObjectValueFlags flags = ObjectValueFlags.Variable)
		{
			return new VariableReference(var: new CorValRef(() => ctx.Frame.GetLocalVariable(varIndex)), ctx: ctx, name: varName, flags: flags);
		}

		private IEnumerable<ValueReference> GetLocals(CorEvaluationContext ctx, ISymbolScope scope, int offset, bool showHidden)
		{
			if (ctx.Frame.FrameType != CorFrameType.ILFrame)
			{
				yield break;
			}
			if (scope == null)
			{
				ISymbolMethod symbolMethod = ctx.Frame.Function.GetSymbolMethod(ctx.Session);
				if (symbolMethod == null)
				{
					int count = ctx.Frame.GetLocalVariablesCount();
					for (int n = 0; n < count; n++)
					{
						int locn = n;
						VariableReference variableReference = CreateLocalVariableReference(ctx, locn, "local_" + (locn + 1));
						if (variableReference != null)
						{
							yield return variableReference;
						}
					}
					yield break;
				}
				scope = symbolMethod.RootScope;
			}
			ISymbolVariable[] locals = scope.GetLocals();
			foreach (ISymbolVariable var in locals)
			{
				if (var.Name == "$site")
				{
					continue;
				}
				if (IsClosureReferenceLocal(var))
				{
					VariableReference variableReference2 = CreateLocalVariableReference(ctx, var.AddressField1, var.Name);
					if (variableReference2 == null)
					{
						continue;
					}
					foreach (ValueReference hoistedLocalVariable in GetHoistedLocalVariables(ctx, variableReference2))
					{
						yield return hoistedLocalVariable;
					}
				}
				else if (!IsGeneratedTemporaryLocal(var) || showHidden)
				{
					VariableReference variableReference3 = CreateLocalVariableReference(ctx, var.AddressField1, var.Name);
					if (variableReference3 != null)
					{
						yield return variableReference3;
					}
				}
			}
			ISymbolScope[] children = scope.GetChildren();
			foreach (ISymbolScope symbolScope in children)
			{
				if (symbolScope.StartOffset > offset || symbolScope.EndOffset < offset)
				{
					continue;
				}
				foreach (ValueReference local in GetLocals(ctx, symbolScope, offset, showHidden))
				{
					yield return local;
				}
			}
		}

		protected override TypeDisplayData OnGetTypeDisplayData(EvaluationContext ctx, object gtype)
		{
			CorType type = (CorType)gtype;
			CorEvaluationContext corEvaluationContext = (CorEvaluationContext)ctx;
			Type typeInfo = type.GetTypeInfo(corEvaluationContext.Session);
			if (typeInfo == null)
			{
				return null;
			}
			string proxyType = null;
			string nameDisplayString = null;
			string typeDisplayString = null;
			string valueDisplayString = null;
			Dictionary<string, DebuggerBrowsableState> dictionary = null;
			bool flag = false;
			bool isCompilerGenerated = false;
			try
			{
				object[] customAttributes = typeInfo.GetCustomAttributes(inherit: false);
				foreach (object obj in customAttributes)
				{
					if (obj is DebuggerTypeProxyAttribute debuggerTypeProxyAttribute)
					{
						proxyType = debuggerTypeProxyAttribute.ProxyTypeName;
						flag = true;
					}
					else if (obj is DebuggerDisplayAttribute debuggerDisplayAttribute)
					{
						flag = true;
						nameDisplayString = debuggerDisplayAttribute.Name;
						typeDisplayString = debuggerDisplayAttribute.Type;
						valueDisplayString = debuggerDisplayAttribute.Value;
					}
					else if (obj is CompilerGeneratedAttribute)
					{
						isCompilerGenerated = true;
					}
				}
				List<MemberInfo> arrayList = [];
				arrayList.AddRange(typeInfo.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
				arrayList.AddRange(typeInfo.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
				foreach (MemberInfo item in arrayList)
				{
					object[] customAttributes2 = item.GetCustomAttributes(typeof(DebuggerBrowsableAttribute), inherit: false);
					if (customAttributes2.Length != 0)
					{
						flag = true;
						if (dictionary == null)
						{
							dictionary = new Dictionary<string, DebuggerBrowsableState>();
						}
						dictionary[item.Name] = ((DebuggerBrowsableAttribute)customAttributes2[0]).State;
					}
				}
			}
			catch (Exception ex)
			{
				DebuggerLoggingService.LogError("Exception in OnGetTypeDisplayData()", ex);
			}
			if (flag)
			{
				return new TypeDisplayData(proxyType, valueDisplayString, typeDisplayString, nameDisplayString, isCompilerGenerated, dictionary);
			}
			return null;
		}

		public override IEnumerable<object> GetNestedTypes(EvaluationContext ctx, object type)
		{
			CorType corType = (CorType)type;
			CorEvaluationContext wctx = (CorEvaluationContext)ctx;
			CorModule mod = corType.Class.Module;
			int token = corType.Class.Token;
			CorMetadataImport metadataForModule = wctx.Session.GetMetadataForModule(mod);
			foreach (object definedType in metadataForModule.DefinedTypes)
			{
				if (((MetadataType)definedType).DeclaringType != null && ((MetadataType)definedType).DeclaringType.MetadataToken == token)
				{
					CorType parameterizedType = mod.GetClassFromToken(((MetadataType)definedType).MetadataToken).GetParameterizedType(CorElementType.Class, new CorType[0]);
					if (!IsGeneratedType(parameterizedType.GetTypeInfo(wctx.Session)))
					{
						yield return parameterizedType;
					}
				}
			}
		}

		public override IEnumerable<object> GetImplementedInterfaces(EvaluationContext ctx, object type)
		{
			Type typeInfo = ((CorType)type).GetTypeInfo(((CorEvaluationContext)ctx).Session);
			Type[] interfaces = typeInfo.GetInterfaces();
			foreach (Type type2 in interfaces)
			{
				if (!string.IsNullOrEmpty(type2.FullName))
				{
					yield return GetType(ctx, type2.FullName);
				}
			}
		}

		public override bool IsExternalType(EvaluationContext ctx, object type)
		{
			return base.IsExternalType(ctx, type);
		}

		public override bool IsTypeLoaded(EvaluationContext ctx, string typeName)
		{
			return ctx.Adapter.GetType(ctx, typeName) != null;
		}

		public override bool IsTypeLoaded(EvaluationContext ctx, object type)
		{
			return IsTypeLoaded(ctx, GetTypeName(ctx, type));
		}
	}
}