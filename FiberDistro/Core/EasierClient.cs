using System.Net;
using Microsoft.Extensions.Logging;
using SuperSocket.Client;
using SuperSocket.Connection;
using SuperSocket.ProtoBase;

namespace FiberDistro.Core;

public class EasierClient : EasyClient<Packet>
{
    public EasierClient(IPipelineFilter<Packet> pipelineFilter) : base(pipelineFilter)
    {
    }

    public EasierClient(IPipelineFilter<Packet> pipelineFilter, ILogger logger) : base(pipelineFilter, logger)
    {
    }

    public EasierClient(IPipelineFilter<Packet> pipelineFilter, ConnectionOptions options) : base(pipelineFilter, options)
    {
    }

    public EndPoint GetLocalEndPoint()
    {
        return Connection.LocalEndPoint;
    }
}