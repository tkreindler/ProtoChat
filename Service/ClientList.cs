using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Service
{
    internal class ClientList : IClientList
    {
        #region State

        private Dictionary<Guid, (IDisposable clientServicer, ChannelWriter<Protos.Response> channel)> channelDict = new ();

        private Dictionary<string, List<Guid>> guidLookupDict = new ();

        private Dictionary<Guid, string> nameLookupDict = new ();

        private readonly ReaderWriterLockSlim mainLock = new ReaderWriterLockSlim();

        private readonly List<IDisposable> disposalQueue = new List<IDisposable>();

        #endregion

        /// <inheritdoc/>
        public void Register(Guid userIdentifier, IDisposable clientServicer, ChannelWriter<Protos.Response> channel)
        {
            this.mainLock.EnterWriteLock();
            try
            {
                if (!this.channelDict.TryAdd(userIdentifier, (clientServicer, channel)))
                {
                    throw new NotImplementedException($"User Identifier {userIdentifier} already exists.");
                }
            }
            finally
            {
                this.mainLock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public void Unregister(Guid userIdentifier)
        {
            this.mainLock.EnterWriteLock();
            try
            {
                // remove name references
                this.RemoveName(userIdentifier);

                if (!this.channelDict.TryGetValue(userIdentifier, out var tup))
                {
                    throw new NotImplementedException($"User Identifier {userIdentifier} was not already registered");
                }

                // complete the channel and add the client servicer to the disposal queue for later disposal
                this.disposalQueue.Add(tup.clientServicer);
                tup.channel.Complete();

                // remove the channel from the dictionary
                if (!this.channelDict.Remove(userIdentifier))
                {
                    throw new NotImplementedException("Should be impossible, are you sure the write lock is held?");
                }
            }
            finally
            {
                this.mainLock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public void ChangeName(Guid userIdentifier, string name)
        {
            this.mainLock.EnterWriteLock();
            try
            {
                // remove old name if it exists
                this.RemoveName(userIdentifier);

                if (!this.nameLookupDict.TryAdd(userIdentifier, name))
                {
                    throw new NotImplementedException($"User Identifier {userIdentifier} already exists.");
                }

                List<Guid> nameList;
                if (this.guidLookupDict.TryGetValue(name, out List<Guid>? temp))
                {
                    // if there's already a nameList for this name grab it
                    nameList = temp;
                }
                else
                {
                    // otherwise add a new nameList, usually will only need to hold one item
                    nameList = new List<Guid>(capacity: 1);
                }

                nameList.Add(userIdentifier);
            }
            finally
            {
                this.mainLock.ExitWriteLock();
            }
        }

        /// <inheritdoc/>
        public void SendGlobalMessage(Protos.Response message)
        {
            this.mainLock.EnterReadLock();
            try
            {
                foreach (ChannelWriter<Protos.Response> channel in this.channelDict.Values.Select(x => x.channel))
                {
                    if (!channel.TryWrite(message))
                    {
                        throw new NotImplementedException("Channel is full, should never happen for unbounded channel");
                    }
                }
            }
            finally
            {
                this.mainLock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public void SendDirectMessage(Protos.Response message, string name)
        {
            this.mainLock.EnterReadLock();
            try
            {
                if (!this.guidLookupDict.TryGetValue(name, out List<Guid>? list) || !list.Any())
                {
                    throw new NotImplementedException("No users with that name registered. TODO figure out what to do in that case.");
                }

                foreach (Guid guid in list)
                {
                    if (!this.channelDict.TryGetValue(guid, out var tuple))
                    {
                        throw new NotImplementedException("Should never happen. All guids in the guidLookupDict should have a channel");
                    }

                    if (!tuple.channel.TryWrite(message))
                    {
                        throw new NotImplementedException("Channel is full, should never happen for unbounded channel");
                    }
                }
            }
            finally
            {
                this.mainLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Removes the name registered for this Guid
        /// </summary>
        /// <remarks>
        /// REQUIRES this.mainLock.WriteLock held
        /// </remarks>
        /// <param name="userIdentifier">User identifier</param>
        private void RemoveName(Guid userIdentifier)
        {
            if (this.nameLookupDict.TryGetValue(userIdentifier, out string? oldName))
            {
                if (!this.guidLookupDict.TryGetValue(oldName, out List<Guid>? list))
                {
                    throw new NotImplementedException("Should never be in the nameLookupDict but not the guidLookupDict");
                }

                if (!list.Remove(userIdentifier))
                {
                    throw new NotImplementedException("Should never not be found in this list if in the nameLookupDict");
                }

                if (!this.nameLookupDict.Remove(userIdentifier))
                {
                    throw new NotImplementedException("Should be impossible, are you sure the write lock is held?");
                }
            }
        }

        #region IDisposable

        /// <summary>
        /// Implements disposability pattern
        /// </summary>
        /// <param name="disposing">True if disposing, false if finalizing</param>
        public virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose of managed disposable 
                this.mainLock.Dispose();

                foreach ((IDisposable servicer, var channel) in this.channelDict.Values)
                {
                    channel.Complete();
                    servicer.Dispose();
                }

                foreach (IDisposable servicer in this.disposalQueue)
                {
                    servicer.Dispose();
                }
            }

            // Set large objects to null to enable GC
            this.channelDict = null!;
            this.nameLookupDict = null!;
            this.guidLookupDict = null!;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
