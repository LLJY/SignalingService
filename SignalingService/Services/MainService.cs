using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using SignalingService.Protos;

namespace SignalingService
{
    public class ResponseRef
    {
        public string UserId { get;}
        public IServerStreamWriter<Signal> ResponseStream { get; }

        public ResponseRef(string userId, IServerStreamWriter<Signal> responseStream)
        {
            UserId = userId;
            ResponseStream = responseStream;
        }
    }
    public class MainService : Signaling.SignalingBase
    {
        private readonly List<ResponseRef> _responseRefs = new List<ResponseRef>();
        public override async Task CallStream(IAsyncStreamReader<Signal> requestStream,
            IServerStreamWriter<Signal> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var current = requestStream.Current;
                // starting an initial stream, store it in a list of response references
                if (current.SenderInfo.IsInit)
                {
                    _responseRefs.Add(new ResponseRef(current.SenderInfo.Userid, responseStream));
                }
                // redirect the signal / offer / reply / ice request / ice response to the intended recipient. The frontend should handle the logic of converting between both, we are just a signaling service
                else
                {
                    _responseRefs.FirstOrDefault(x => x.UserId == current.RecieverId)?.ResponseStream.WriteAsync(current);
                }
            }
        }
    }
}