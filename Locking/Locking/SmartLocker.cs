using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Locking
{
    public class LockOptions 
    {
        public int ChannelRepositoryLockLeaseTime_msec;
        public int ChannelRepositoryLockMaxTryAcquireTime_msec;
        public int ChannelRepositoryLockStepTime_msec;
    }
    public interface IConcurrentDict
    {
        public bool StartExclusiveAccess(string key);
        public void EndExclusiveAccess(string key);

    }
    public class ConcurrentDict : IConcurrentDict
    {

        private ConcurrentDictionary<string, Tuple<DateTime, Guid>> cache = null;
        private readonly LockOptions _Options;

        public ConcurrentDict(LockOptions options)
        {
            _Options = options;
            cache = new ConcurrentDictionary<string, Tuple<DateTime, Guid>>();
        }

        public bool StartExclusiveAccess(string key)
        {
            var beginTime = DateTime.UtcNow;
            var guid = Guid.NewGuid();
            var tupple = new Tuple<DateTime, Guid>(beginTime, guid);
            Tuple<DateTime, Guid> cachedTupple = default;
            var rand = new Random();

            while (cachedTupple != tupple && (DateTime.UtcNow - beginTime).TotalMilliseconds < _Options.ChannelRepositoryLockLeaseTime_msec)
            {
                cachedTupple = cache.GetOrAdd(key, tupple);
                if (cachedTupple != tupple)
                {
                    if ((beginTime - cachedTupple.Item1).TotalMilliseconds <= _Options.ChannelRepositoryLockMaxTryAcquireTime_msec)
                    {
                        Task.Delay(rand.Next(_Options.ChannelRepositoryLockStepTime_msec));
                        continue;
                    }
                    cache.TryUpdate(key, newValue: tupple, comparisonValue: cachedTupple);
                    cachedTupple = cache.GetOrAdd(key, tupple);
                }
            }
            return (cachedTupple == tupple);
        }

        public void EndExclusiveAccess(string key)
        {
            if (key != default)
            {
                cache.TryRemove(key, out _);
            }
        }
    }
}
