using ClrDebug;
using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;
using System.Reflection;
using System.Text;

namespace Mono.Debugging.ClrDebug
{
	internal class PropertyReference : ValueReference
	{
		private readonly PropertyInfo prop;

		private readonly CorValRef thisobj;

		private readonly CorValRef[] index;

		private readonly CorModule module;

		private readonly CorType declaringType;

		private readonly CorValRef<CorValue>.ValueLoader loader;

		private readonly ObjectValueFlags flags;

		private CorValRef cachedValue;

		public override object Type
		{
			get
			{
				if (!prop.CanRead)
				{
					return null;
				}
				return ((CorValRef)Value).Val.ExactType;
			}
		}

		public override object DeclaringType => declaringType;

		public override object Value
		{
			get
			{
				if (cachedValue != null && cachedValue.IsValid)
				{
					return cachedValue;
				}
				if (!prop.CanRead)
				{
					return null;
				}
				CorEvaluationContext corEvaluationContext = (CorEvaluationContext)base.Context;
				CorValue[] array;
				if (index != null)
				{
					array = new CorValue[index.Length];
					ParameterInfo[] parameters = prop.GetGetMethod(nonPublic: true).GetParameters();
					for (int i = 0; i < index.Length; i++)
					{
						array[i] = corEvaluationContext.Adapter.GetBoxedArg(corEvaluationContext, index[i], parameters[i].ParameterType).Val;
					}
				}
				else
				{
					array = new CorValue[0];
				}
				MethodInfo getMethod = prop.GetGetMethod(nonPublic: true);
				CorFunction functionFromToken = module.GetFunctionFromToken(getMethod.MetadataToken);
				CorValue corValue = null;
				corValue = ((declaringType.Type != CorElementType.Array && declaringType.Type != CorElementType.SZArray) ? corEvaluationContext.RuntimeInvoke(functionFromToken, declaringType.TypeParameters, (thisobj != null) ? thisobj.Val : null, array) : corEvaluationContext.RuntimeInvoke(functionFromToken, new CorType[0], (thisobj != null) ? thisobj.Val : null, array));
				return cachedValue = new CorValRef(corValue, loader);
			}
			set
			{
				CorEvaluationContext corEvaluationContext = (CorEvaluationContext)base.Context;
				CorFunction functionFromToken = module.GetFunctionFromToken(prop.GetSetMethod(nonPublic: true).MetadataToken);
				CorValRef val = (CorValRef)value;
				ParameterInfo[] parameters = prop.GetSetMethod(nonPublic: true).GetParameters();
				CorValue[] array;
				if (index == null)
				{
					array = new CorValue[1];
				}
				else
				{
					array = new CorValue[index.Length + 1];
					for (int i = 0; i < index.Length; i++)
					{
						array[i] = corEvaluationContext.Adapter.GetBoxedArg(corEvaluationContext, index[i], parameters[i].ParameterType).Val;
					}
				}
				array[^1] = corEvaluationContext.Adapter.GetBoxedArg(corEvaluationContext, val, parameters[^1].ParameterType).Val;
				corEvaluationContext.RuntimeInvoke(functionFromToken, declaringType.TypeParameters, (thisobj != null) ? thisobj.Val : null, array);
			}
		}

		public override string Name
		{
			get
			{
				if (index != null)
				{
					StringBuilder stringBuilder = new StringBuilder("[");
					CorValRef[] array = index;
					foreach (CorValRef obj in array)
					{
						if (stringBuilder.Length > 1)
						{
							stringBuilder.Append(",");
						}
						stringBuilder.Append(base.Context.Evaluator.TargetObjectToExpression(base.Context, obj));
					}
					stringBuilder.Append("]");
					return stringBuilder.ToString();
				}
				return prop.Name;
			}
		}

		public override ObjectValueFlags Flags => flags;

		public PropertyReference(EvaluationContext ctx, PropertyInfo prop, CorValRef thisobj, CorType declaringType)
			: this(ctx, prop, thisobj, declaringType, null)
		{
		}

		public PropertyReference(EvaluationContext ctx, PropertyInfo prop, CorValRef thisobj, CorType declaringType, CorValRef[] index)
			: base(ctx)
		{
			this.prop = prop;
			this.declaringType = declaringType;
			if (declaringType.Type == CorElementType.Array || declaringType.Type == CorElementType.SZArray)
			{
				module = ((CorType)((CorEvaluationContext)ctx).Adapter.GetType(ctx, "System.Object")).Class.Module;
			}
			else
			{
				module = declaringType.Class.Module;
			}
			this.index = index;
			if (!prop.GetGetMethod(nonPublic: true).IsStatic)
			{
				this.thisobj = thisobj;
			}
			flags = GetFlags(prop);
			loader = () => ((CorValRef)Value).Val;
		}

		internal static ObjectValueFlags GetFlags(PropertyInfo prop)
		{
			ObjectValueFlags objectValueFlags = ObjectValueFlags.Property;
			MethodInfo methodInfo = prop.GetGetMethod(nonPublic: true) ?? prop.GetSetMethod(nonPublic: true);
			if (prop.GetSetMethod(nonPublic: true) == null)
			{
				objectValueFlags |= ObjectValueFlags.ReadOnly;
			}
			if (methodInfo.IsStatic)
			{
				objectValueFlags |= ObjectValueFlags.Global;
			}
			objectValueFlags = (methodInfo.IsFamilyAndAssembly ? (objectValueFlags | ObjectValueFlags.Internal) : (methodInfo.IsFamilyOrAssembly ? (objectValueFlags | ObjectValueFlags.InternalProtected) : (methodInfo.IsFamily ? (objectValueFlags | ObjectValueFlags.Protected) : ((!methodInfo.IsPublic) ? (objectValueFlags | ObjectValueFlags.Private) : (objectValueFlags | ObjectValueFlags.Public)))));
			if (!prop.CanWrite)
			{
				objectValueFlags |= ObjectValueFlags.ReadOnly;
			}
			return objectValueFlags;
		}
	}
}
