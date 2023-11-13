using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lively.Grpc.Common.Proto.Commands;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.RPC
{
    internal class WallpaperPlaylistServer : PlaylistService.PlaylistServiceBase
    {
        public WallpaperPlaylistServer()
        {
            
        }

        public override Task<Empty> Start(Empty _, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> Stop(Empty _, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }
    }
}
