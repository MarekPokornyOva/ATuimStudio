using Mono.Debugging.Backend;

namespace Mono.Debugging.ClrDebug
{
	class MtaObjectValueUpdater : IObjectValueUpdater, IDebuggerBackendObject
	{
		private readonly IObjectValueUpdater source;

		public MtaObjectValueUpdater(IObjectValueUpdater s)
		{
			source = s;
		}

		public void RegisterUpdateCallbacks(UpdateCallback[] callbacks)
		{
			MtaThread.Run(delegate
			{
				source.RegisterUpdateCallbacks(callbacks);
			});
		}
	}
}
