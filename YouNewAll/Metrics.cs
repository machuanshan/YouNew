using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace YouNewAll
{
    public class Metrics
    {
        private readonly ILogger<Metrics> _logger;
        private int _totalConnectionCount = 0;
        private int _activeConnectionCount = 0;

        public Metrics(ILogger<Metrics> logger)
        {
            _logger = logger;
        }

        public void ConnectionCreated()
        {
            Interlocked.Increment(ref _totalConnectionCount);
            Interlocked.Increment(ref _activeConnectionCount);
            _logger.LogInformation($"Connection created: {_activeConnectionCount}/{_totalConnectionCount}"); ;
        }

        public void ConnectionClosed()
        {
            Interlocked.Decrement(ref _activeConnectionCount);
        }
    }
}
