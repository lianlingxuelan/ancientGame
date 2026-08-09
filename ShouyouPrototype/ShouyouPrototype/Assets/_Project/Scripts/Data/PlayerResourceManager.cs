using UnityEngine;
using Shouyou.Network;

namespace Shouyou.Data
{
    /// <summary>
    /// 玩家本地资源钱包。
    ///
    /// 第一章主流程闭环：通关结算时把奖励真实入账，而不是只展示文字。
    /// 当前 Demo 阶段先用 PlayerPrefs 做本地持久化，与关卡进度解耦保存：
    /// 1. 运行时不依赖场景物体，任何 UI 脚本都能直接读取。
    /// 2. 奖励按 RewardItem.id 累计，覆盖货币（铜钱/玉）与材料/收集品。
    /// 3. 后续接入真正数据库或后端时，只需要替换读写实现，UI 层不用跟着大改。
    /// </summary>
    public sealed class PlayerResourceManager
    {
        /// <summary>
        /// 本地保存键前缀。按奖励 id 追加：Shouyou.Player.Resource.{id}。
        /// </summary>
        private const string ResourceKeyPrefix = "Shouyou.Player.Resource.";

        private static PlayerResourceManager instance;

        /// <summary>
        /// 单例入口。这个类不是 MonoBehaviour，所以不需要挂到 Unity 场景物体上。
        /// </summary>
        public static PlayerResourceManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PlayerResourceManager();
                }

                return instance;
            }
        }

        private PlayerResourceManager()
        {
        }

        /// <summary>
        /// 读取指定资源的当前持有数量。未记录过的资源返回 0。
        /// </summary>
        public int GetCount(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return 0;
            }

            return PlayerPrefs.GetInt(BuildKey(id), 0);
        }

        /// <summary>
        /// 把一批结算奖励实际入账。
        ///
        /// 只按 RewardItem.id 累计数量；id 为空或数量不大于 0 的无效条目直接跳过。
        /// 入账后立即写入本地，避免中途退出或重复结算导致丢失。
        /// </summary>
        public void GrantRewards(RewardItem[] rewards)
        {
            if (rewards == null || rewards.Length == 0)
            {
                return;
            }

            bool changed = false;
            for (int i = 0; i < rewards.Length; i++)
            {
                RewardItem reward = rewards[i];
                if (reward == null || string.IsNullOrEmpty(reward.id) || reward.amount <= 0)
                {
                    continue;
                }

                int current = GetCount(reward.id);
                PlayerPrefs.SetInt(BuildKey(reward.id), current + reward.amount);
                changed = true;
            }

            if (changed)
            {
                PlayerPrefs.Save();
            }
        }

        private static string BuildKey(string id)
        {
            return ResourceKeyPrefix + id;
        }
    }
}
