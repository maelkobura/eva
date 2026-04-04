using System;

namespace Eva.Node.Network
{
    /// <summary>
    /// Interface for network management services.
    /// </summary>
    public interface INetworkManager : IDisposable
    {
        /// <summary>
        /// Starts the web server and initializes all routes and middleware.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the web server and releases all resources.
        /// </summary>
        void Stop();
    }
}