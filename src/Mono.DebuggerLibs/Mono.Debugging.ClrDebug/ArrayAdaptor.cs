using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Evaluation;
using System.Collections;

namespace Mono.Debugging.ClrDebug
{
	internal class ArrayAdaptor : ICollectionAdaptor
	{
		private readonly CorEvaluationContext ctx;

		private readonly CorValRef<CorArrayValue> valRef;

		public object ElementType => valRef.Val.ExactType.FirstTypeParameter;

		public ArrayAdaptor(EvaluationContext ctx, CorValRef<CorArrayValue> valRef)
		{
			this.ctx = (CorEvaluationContext)ctx;
			this.valRef = valRef;
		}

		public int[] GetLowerBounds()
		{
			CorArrayValue val = valRef.Val;
			if (val != null && val.HasBaseIndicies)
			{
				return val.GetBaseIndicies();
			}
			return new int[GetDimensions().Length];
		}

		public int[] GetDimensions()
		{
			CorArrayValue val = valRef.Val;
			if (!(val != null))
			{
				return new int[0];
			}
			return val.GetDimensions();
		}

		public object GetElement(int[] indices)
			=> new CorValRef(() => valRef.Val?.GetElement(indices));

		public Array GetElements(int[] indices, int count)
		{
			ArrayList arrayList = new ArrayList();
			int[] array = new int[indices.Length];
			for (int i = 0; i < indices.Length; i++)
			{
				array[i] = indices[i];
			}
			for (int j = 0; j < count; j++)
			{
				arrayList.Add(GetElement((int[])array.Clone()));
				array[^1]++;
			}
			return arrayList.ToArray();
		}

		public void SetElement(int[] indices, object val)
		{
			CorValRef thisVal = (CorValRef)GetElement(indices);
			valRef.Invalidate();
			thisVal.SetValue(ctx, (CorValRef)val);
		}
	}
}
