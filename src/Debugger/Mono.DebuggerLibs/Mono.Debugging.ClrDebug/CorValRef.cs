using ClrDebug;
using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Client;

namespace Mono.Debugging.ClrDebug
{
	public class CorValRef<TValue> where TValue : CorValue
	{
		public delegate TValue ValueLoader();

		private TValue val;

		private readonly ValueLoader loader;

		private bool needToReload;

		public bool IsValid
		{
			get
			{
				if (needToReload)
				{
					return false;
				}
				return IsAlive();
			}
		}

		public TValue Val
		{
			get
			{
				if (!IsValid)
				{
					Reload();
				}
				return val;
			}
		}

		public CorValRef(TValue val)
		{
			this.val = val;
		}

		public CorValRef(TValue val, ValueLoader loader)
		{
			this.val = val;
			this.loader = loader;
		}

		public CorValRef(ValueLoader loader)
		{
			val = loader();
			this.loader = loader;
		}

		private bool IsAlive()
		{
			if (val == null)
			{
				return true;
			}
			try
			{
				_ = val.ExactType;
			}
			catch (DebugException ex)
			{
				if (ex.HResult == HRESULT.CORDBG_E_OBJECT_NEUTERED)
				{
					DebuggerLoggingService.LogMessage($"Value is out of date: {ex.Message}");
					return false;
				}
				throw;
			}
			return true;
		}

		public void Invalidate()
		{
			needToReload = true;
		}

		public void Reload()
		{
			if (loader != null)
			{
				TValue val = loader();
				if (val != null)
				{
					this.val = val;
					needToReload = false;
				}
			}
		}
	}

	public class CorValRef : CorValRef<CorValue>
	{
		public CorValRef(CorValue val)
			: base(val)
		{
		}

		public CorValRef(CorValue val, ValueLoader loader)
			: base(val, loader)
		{
		}

		public CorValRef(ValueLoader loader)
			: base(loader)
		{
		}
	}
}
