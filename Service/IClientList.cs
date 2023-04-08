using Grpc.Core;
using Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Service
{
    /// <summary>
    /// Interface for <see cref="ClientList"/> for unit testing
    /// </summary>
    internal interface IClientList : IDisposable
    {
        /// <summary>
        /// Registers a new user by a unique identifier and adds their channel to the dictionary
        /// </summary>
        /// <param name="userIdentifier">Guid user identifier</param>
        /// <param name="clientServicer">Client servicer thread being added</param>
        /// <param name="channel">The channel used to communicate with said client servicer</param>
        Task Register(Guid userIdentifier, IDisposable clientServicer, ChannelWriter<Protos.Response> channel);

        /// <summary>
        /// Unregisters a user (log out)
        /// </summary>
        /// <param name="userIdentifier">Unique identifier for this user</param>
        Task Unregister(Guid userIdentifier);

        /// <summary>
        /// Change a user's name by their channel identifier
        /// </summary>
        /// <param name="userIdentifier">User identifier</param>
        /// <param name="name">Name we're changing this user to</param>
        Task ChangeName(Guid userIdentifier, string name);

        /// <summary>
        /// Get a list of all stream writers to send the global message to
        /// </summary>
        Task<IReadOnlyCollection<IAsyncStreamWriter<Response>>> GetGlobalMessageList();

        /// <summary>
        /// Get a list of stream writers to send the message to for this name
        /// </summary>
        /// <param name="name">Name to send the message to</param>
        Task<IReadOnlyCollection<IAsyncStreamWriter<Response>>> GetDirectMessageList(string name);
    }
}
