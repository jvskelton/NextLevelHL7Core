using System;
using System.Threading;
using System.Threading.Tasks;

namespace NextLevelHL7
{
    public interface IEHRInterface
    {
        /// <summary>
        /// Gets or sets a unique identifier which represents this interface instance.
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the interface.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Starts the interface.
        /// </summary>
        /// <param name="quiet">If true, starts an interface without logging a change in status</param>
        /// <returns></returns>
        bool Start(bool quiet = false);

        /// <summary>
        /// Stops an interface.
        /// </summary>
        /// <param name="quiet">If true, stops an interface without logging a change in status</param>
        /// <returns></returns>
        bool Stop(bool quiet = false);

        /// <summary>
        /// Starts the interface.
        /// </summary>
        /// <param name="quiet">If true, starts an interface without logging a change in status</param>
        /// <returns></returns>
        Task<bool> StartAsync(CancellationToken cancellationToken, bool quiet = false);

        /// <summary>
        /// Stops an interface.
        /// </summary>
        /// <param name="quiet">If true, stops an interface without logging a change in status</param>
        /// <returns></returns>
        Task<bool> StopAsync(CancellationToken cancellationToken, bool quiet = false);

        /// <summary>
        /// Occurs when an interface changes status.
        /// </summary>
        event EventHandler<InterfaceStatusEvent> StatusEvent;

        /// <summary>
        /// Gets interface statistics which track up-time, delivery statistics, and message failures.
        /// </summary>
        InterfaceStatistics Statistics { get; }
    }
}
