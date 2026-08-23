using ClrDebug;
using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;
using System.Reflection;

namespace Mono.Debugging.ClrDebug
{
	public class FieldReference : ValueReference
	{
		private readonly CorType type;

		private readonly FieldInfo @field;

		private readonly CorValRef thisobj;

		private readonly CorValRef<CorValue>.ValueLoader loader;

		private readonly ObjectValueFlags flags;

		private readonly string vname;

		public override object Type => ((CorValRef)Value).Val.ExactType;

		public override object DeclaringType => type;

		public override object ObjectValue
		{
			get
			{
				if (@field.IsLiteral && @field.IsStatic)
				{
					return @field.GetValue(null);
				}
				return base.ObjectValue;
			}
		}

		public override object Value
		{
			get
			{
				CorEvaluationContext corEvaluationContext = (CorEvaluationContext)base.Context;
				if (thisobj != null && !@field.IsStatic)
				{
					CorValue realObject = CorObjectAdaptor.GetRealObject(corEvaluationContext, thisobj);
					if (realObject is CorObjectValue)
					{
						realObject = ((CorObjectValue)realObject).GetFieldValue(type.Class, @field.MetadataToken);
						return new CorValRef(realObject, loader);
					}
					if (realObject is CorReferenceValue)
					{
						return new CorValRef((CorReferenceValue)realObject, loader);
					}
				}
				if (@field.IsLiteral && @field.IsStatic)
				{
					object value = @field.GetValue(null);
					CorObjectAdaptor adapter = corEvaluationContext.Adapter;
					if (adapter.IsEnum(corEvaluationContext, type))
					{
						return adapter.CreateEnum(corEvaluationContext, type, base.Context.Adapter.CreateValue(corEvaluationContext, value));
					}
					return base.Context.Adapter.CreateValue(corEvaluationContext, value);
				}
				try
				{
					CorValue realObject2 = type.GetStaticFieldValue(@field.MetadataToken, corEvaluationContext.Frame);
					return new CorValRef(realObject2, loader);
				}
				catch (DebugException ex)
				{
					if (ex.HResult == HRESULT.CORDBG_E_STATIC_VAR_NOT_AVAILABLE)
					{
						throw new EvaluatorException("A static variable is not available because it has not been initialized yet");
					}
					throw;
				}
			}
			set
			{
				((CorValRef)Value).SetValue(base.Context, (CorValRef)value);
				if (thisobj != null)
				{
					CorObjectValue corObjectValue = CorObjectAdaptor.GetRealObject(base.Context, thisobj) as CorObjectValue;
					if (corObjectValue != null && corObjectValue.IsValueClass)
					{
						thisobj.Invalidate();
					}
				}
			}
		}

		public override string Name => vname ?? @field.Name;

		public override ObjectValueFlags Flags => flags;

		public FieldReference(EvaluationContext ctx, CorValRef thisobj, CorType type, FieldInfo field, string vname, ObjectValueFlags vflags)
			: base(ctx)
		{
			this.thisobj = thisobj;
			this.type = type;
			this.@field = field;
			this.vname = vname;
			if (@field.IsStatic)
			{
				this.thisobj = null;
			}
			flags = vflags | GetFlags(@field);
			loader = () => ((CorValRef)Value).Val;
		}

		public FieldReference(EvaluationContext ctx, CorValRef thisobj, CorType type, FieldInfo field)
			: this(ctx, thisobj, type, field, null, ObjectValueFlags.Field)
		{
		}

		internal static ObjectValueFlags GetFlags(FieldInfo field)
		{
			ObjectValueFlags objectValueFlags = ObjectValueFlags.Field;
			if (field.IsStatic)
			{
				objectValueFlags |= ObjectValueFlags.Global;
			}
			objectValueFlags = (field.IsFamilyOrAssembly ? (objectValueFlags | ObjectValueFlags.InternalProtected) : (field.IsFamilyAndAssembly ? (objectValueFlags | ObjectValueFlags.Internal) : (field.IsFamily ? (objectValueFlags | ObjectValueFlags.Protected) : ((!field.IsPublic) ? (objectValueFlags | ObjectValueFlags.Private) : (objectValueFlags | ObjectValueFlags.Public)))));
			if (field.IsLiteral)
			{
				objectValueFlags |= ObjectValueFlags.ReadOnly;
			}
			return objectValueFlags;
		}
	}
}
