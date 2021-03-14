using System;

namespace Vostok.Metrics.System.Helpers
{
    internal class LinuxTeamingDriverConnector : IDisposable
    {
        public TeamingCollector Connect(string teamingInterfaceName)
        {
            return new TeamingCollector();
        }

        public void Dispose() { }
    }

    internal class TeamingCollector : IDisposable
    {
        public string GetCurrentState()
        {
            throw new NotImplementedException();
        }

        public string GetTeamingMode()
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
    }
}