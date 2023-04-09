using Grpc.Core;
using Protos;

namespace Service
{
    internal class ClientList : IClientList
    {
        #region State

        private Dictionary<Guid, IServerStreamWriter<Response>> channelDict = new ();

        private Dictionary<string, List<Guid>> guidLookupDict = new (StringComparer.InvariantCultureIgnoreCase);

        private Dictionary<Guid, string> nameLookupDict = new ();

        private readonly Microsoft.VisualStudio.Threading.AsyncReaderWriterLock mainLock = new (joinableTaskContext: null);

        #endregion

        /// <inheritdoc/>
        public async Task Register(Guid userIdentifier, IClientServicer clientServicer, IServerStreamWriter<Response> writer)
        {
            await using (await this.mainLock.WriteLockAsync())
            {
                if (!this.channelDict.TryAdd(userIdentifier, writer))
                {
                    throw new NotImplementedException($"User Identifier {userIdentifier} already exists.");
                }
            }
        }

        /// <inheritdoc/>
        public async Task Unregister(Guid userIdentifier)
        {
            await using (await this.mainLock.WriteLockAsync())
            {
                // remove name references
                this.RemoveName(userIdentifier);

                // remove the channel from the dictionary
                if (!this.channelDict.Remove(userIdentifier))
                {
                    throw new NotImplementedException($"User Identifier {userIdentifier} was not already registered");
                }
            }
        }

        /// <inheritdoc/>
        public async Task ChangeName(Guid userIdentifier, string name)
        {
            await using (await this.mainLock.WriteLockAsync())
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
                    if (!this.guidLookupDict.TryAdd(name, nameList))
                    {
                        throw new NotImplementedException("Should be impossible, is there a race condition?");
                    }
                }

                nameList.Add(userIdentifier);
            }
        }

        public async Task<IReadOnlyCollection<IServerStreamWriter<Response>>> GetGlobalMessageList(Guid selfIdentifier)
        {
            await using (await this.mainLock.ReadLockAsync())
            {
                // needs to copy the list to avoid concurrency issue
                return this.channelDict
                    .Where(x => x.Key != selfIdentifier)
                    .Select(x => x.Value)
                    .ToArray();
            }
        }

        public async Task<IReadOnlyCollection<IServerStreamWriter<Response>>> GetDirectMessageList(string name, Guid selfIdentifier)
        {
            await using (await this.mainLock.ReadLockAsync())
            {
                if (!this.guidLookupDict.TryGetValue(name, out List<Guid>? list) || !list.Any())
                {
                    // nobody to DM to, just return none
                    return Array.Empty<IServerStreamWriter<Response>>();
                }

                // have to copy it into a new array to avoid concurrency issues
                return list
                    .Where(x => x != selfIdentifier)
                    .Select(guid =>
                    {
                        if (!this.channelDict.TryGetValue(guid, out var writier))
                        {
                            throw new NotImplementedException("Should never happen. All guids in the guidLookupDict should have a channel");
                        }

                        return writier;
                    }).ToArray();
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
    }
}
