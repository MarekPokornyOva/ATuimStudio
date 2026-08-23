using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

namespace Mono.Debugging.ClrDebug
{
	public class VariableReference : ValueReference
	{
		private readonly CorValRef var;

		private readonly ObjectValueFlags flags;

		private readonly string name;

		public override object Value
		{
			get
			{
				return var;
			}
			set
			{
				var.SetValue(base.Context, (CorValRef)value);
			}
		}

		public override string Name => name;

		public override object Type => var.Val.ExactType;

		public override ObjectValueFlags Flags => flags;

		public VariableReference(EvaluationContext ctx, CorValRef var, string name, ObjectValueFlags flags)
			: base(ctx)
		{
			this.flags = flags;
			this.var = var;
			this.name = name;
		}
	}
}
