using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using SignalingService.Protos;

namespace SignalingService
{
    public class MainService : Signaling.SignalingBase
    {
        private static readonly ConcurrentDictionary<string, IServerStreamWriter<Signal>> _responseRefs = new();
        /**
         * The task is simple, this server simply acts as a middleman for communication between two clients.
         */
        public override async Task CallStream(IAsyncStreamReader<Signal> requestStream,
            IServerStreamWriter<Signal> responseStream, ServerCallContext context)
        {
            Action removeResponseStream = () =>
            {
                var rRef = _responseRefs.FirstOrDefault(x => x.Value == responseStream);
                _responseRefs.TryRemove(rRef);
            };
            try
            {
                
                while (await requestStream.MoveNext())
                {
                    var current = requestStream.Current;
                    // starting an initial stream, store it in a list of response references
                    if (current.SenderInfo.IsInit)
                    {
                        Console.WriteLine($"call has been initialized {current.SenderInfo}");
                        _responseRefs.TryAdd(current.SenderInfo.Userid, responseStream);
                    }
                    // redirect the signal / offer / reply / ice request / ice response to the intended recipient. The frontend should handle the logic of converting between both, we are just a signaling service
                    else
                    {
                        Console.WriteLine("Message Received!");
                        var response = _responseRefs.FirstOrDefault(x => x.Key == current.RecieverId);
                        await response.Value.WriteAsync(current);
                    }
                }
                // remove any references to the stream once it has ended
                removeResponseStream();
            }
            catch (Exception e)
            {
                // remove any references to the stream once it has ended
                removeResponseStream();
            }
        }
    }
}