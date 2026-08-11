using UnityEngine;
using Shouyou.Network;
using System.Collections.Generic;

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
        /// 判断资源是否足够支付指定数量。
        /// 这是养成、体力和商店入口统一使用的只读检查；
        /// 非法资源 id 或非正数数量都视为不可支付，避免调用方把“0 消耗”误当作成功。
        /// </summary>
        public bool CanAfford(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0)
            {
                return false;
            }

            return GetCount(id) >= amount;
        }

        /// <summary>
        /// 尝试从本地资源钱包扣除数量。
        /// 只有余额充足时才会写入并保存；失败时不改变任何存档数据。
        /// 后续接入后端账本时，可在此方法内部替换为服务端确认，调用方无需改动。
        /// </summary>
        public bool TrySpend(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0)
            {
                return false;
            }

            if (!CanAfford(id, amount))
            {
                return false;
            }

            int current = GetCount(id);
            PlayerPrefs.SetInt(BuildKey(id), current - amount);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>
        /// 尝试一次性扣除多种资源。
        /// 养成升级通常同时消耗货币和材料，因此先汇总重复资源并检查全部余额；
        /// 任一条目无效或余额不足就整体失败，绝不留下“只扣除一部分”的本地存档。
        /// </summary>
        public bool TrySpend(RewardItem[] costs)
        {
            if (costs == null || costs.Length == 0)
            {
                return false;
            }

            Dictionary<string, int> totalCosts = new Dictionary<string, int>();
            for (int i = 0; i < costs.Length; i++)
            {
                RewardItem cost = costs[i];
                if (cost == null || string.IsNullOrEmpty(cost.id) || cost.amount <= 0)
                {
                    return false;
                }

                int currentTotal;
                totalCosts.TryGetValue(cost.id, out currentTotal);
                totalCosts[cost.id] = currentTotal + cost.amount;
            }

            foreach (KeyValuePair<string, int> pair in totalCosts)
            {
                if (!CanAfford(pair.Key, pair.Value))
                {
                    return false;
                }
            }

            foreach (KeyValuePair<string, int> pair in totalCosts)
            {
                PlayerPrefs.SetInt(BuildKey(pair.Key), GetCount(pair.Key) - pair.Value);
            }

            PlayerPrefs.Save();
            return true;
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
