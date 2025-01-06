using System.Collections.Concurrent;
using Zelenay_MTCG.Models.Usermodel;

namespace Zelenay_MTCG.Server.Battle
{
    public class BattleManager
    {
        private static readonly Lazy<BattleManager> _instance =
            new Lazy<BattleManager>(() => new BattleManager());

        public static BattleManager Instance => _instance.Value;

        private readonly ConcurrentQueue<User> _waitingList;
        private readonly ConcurrentDictionary<string, string> _battleLogs;

        private BattleManager()
        {
            _waitingList = new ConcurrentQueue<User>();
            _battleLogs = new ConcurrentDictionary<string, string>();
        }

        public bool EnqueuePlayer(User user)
        {
            _waitingList.Enqueue(user);
            return true;
        }

        public User? DequeuePlayer()
        {
            _waitingList.TryDequeue(out var user);
            return user;
        }

        public void AddBattleLog(string username, string log)
        {
            _battleLogs[username] = log;
        }

        public string? GetBattleLogForPlayer(string username)
        {
            _battleLogs.TryGetValue(username, out var log);
            return log;
        }
    }
}