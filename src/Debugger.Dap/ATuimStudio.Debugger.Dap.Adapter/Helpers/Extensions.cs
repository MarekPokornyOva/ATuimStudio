using OmniSharp.Extensions.DebugAdapter.Protocol.Events;
using OmniSharp.Extensions.JsonRpc;

namespace ATuimStudio.Debugger.Dap.Adapter
{
	static class Extensions
	{
		extension(IResponseRouter responseRouter)
		{
			public void SendOutput(OutputEvent request)
				=> responseRouter.SendNotification(request);

			public void SendStopped(StoppedEvent request)
				=> responseRouter.SendNotification(request);
		}
	}
}
