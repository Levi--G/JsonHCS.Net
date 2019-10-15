using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRHub.Hubs
{
    public class Hub : Microsoft.AspNetCore.SignalR.Hub
    {
        public async Task Broadcast(string message)
        {
            await Clients.All.SendAsync("Receive", message);
        }
    }
}
