using System.Collections.Generic;
using Shouyou.Network;

namespace Shouyou.Data
{
    // 主线关卡目录。
    // 这里先集中写死第一章 6 个关卡，等后端和配置表稳定后再迁移到数据库。
    public static class MainlineStageCatalog
    {
        // 本地默认表永远保留，作为离线运行和接口异常时的安全兜底。
        private static readonly MainlineStageInfo[] DefaultStages =
        {
            new MainlineStageInfo(
                1,
                "1-1 明水入汴京",
                1,
                800,
                "前往李府庭院，触发第一段剧情。",
                "铜钱 1200、词意经验 80",
                true),

            new MainlineStageInfo(
                2,
                "1-2 雅集赴会",
                1,
                980,
                "完成第一次词意试炼。",
                "铜钱 1200、名士信笺 1",
                true),

            new MainlineStageInfo(
                3,
                "1-3 词论初临",
                2,
                1160,
                "在雅集中回应前辈论词。",
                "铜钱 1500、突破材料 1",
                false),

            new MainlineStageInfo(
                4,
                "1-4 风雨前夜",
                3,
                1340,
                "完成雨夜前的准备。",
                "铜钱 1500、词意经验 120",
                false),

            new MainlineStageInfo(
                5,
                "1-5 故人入梦",
                4,
                1520,
                "进入李清照的梦境支线，触发神识波动。",
                "梦境碎片 1、铜钱 1800",
                false),

            new MainlineStageInfo(
                6,
                "1-6 潮声再起",
                5,
                1700,
                "完成第一卷收束战，出现梦域觉醒预告。",
                "玉 60、CG 解锁进度 1",
                false)
        };

        // MainlineStageInfo 位于本轮四文件范围之外，因此奖励由目录按关卡 ID 独立提供。
        private static readonly Dictionary<int, RewardItem[]> DefaultRewardsByStageId =
            new Dictionary<int, RewardItem[]>
            {
                { 1, new[] { CreateReward("coin", "货币", "铜钱", 1200, 1), CreateReward("poetry_exp", "材料", "词意经验", 80, 1) } },
                { 2, new[] { CreateReward("coin", "货币", "铜钱", 1200, 1), CreateReward("letter", "材料", "名士信笺", 1, 2) } },
                { 3, new[] { CreateReward("coin", "货币", "铜钱", 1500, 1), CreateReward("break_material", "材料", "突破材料", 1, 2) } },
                { 4, new[] { CreateReward("coin", "货币", "铜钱", 1500, 1), CreateReward("poetry_exp", "材料", "词意经验", 120, 1) } },
                { 5, new[] { CreateReward("dream_fragment", "材料", "梦境碎片", 1, 3), CreateReward("coin", "货币", "铜钱", 1800, 1) } },
                { 6, new[] { CreateReward("jade", "货币", "玉", 60, 3), CreateReward("cg_progress", "收集", "CG 解锁进度", 1, 3) } }
            };

        // 当前运行时正在使用的关卡表。
        // 后端数据加载成功后会替换为远程版本，但不会改写 DefaultStages。
        private static MainlineStageInfo[] activeStages = Clone(DefaultStages);

        // 是否已经成功套用后端章节配置，供调试面板和日志判断数据来源。
        public static bool IsUsingRemoteConfig { get; private set; }

        // 套用后端整理过的关卡表。
        // 输入为空时保持当前数据不变，避免接口异常时把主线页面清空。
        public static void ApplyRemoteStages(MainlineStageInfo[] remoteStages)
        {
            if (remoteStages == null || remoteStages.Length == 0)
            {
                return;
            }

            activeStages = Clone(remoteStages);
            IsUsingRemoteConfig = true;
        }

        // 根据关卡序号读取本地兜底数据。
        // 远程映射时会保留本地表里的战力、目标和奖励等暂未由后端维护的字段。
        public static MainlineStageInfo GetLocalFallback(int id)
        {
            return Find(DefaultStages, id);
        }

        // 根据关卡序号读取当前生效的关卡数据。
        // 如果传入的 id 不合法，默认返回第一关，避免运行时报空。
        public static MainlineStageInfo Get(int id)
        {
            return Find(activeStages, id);
        }

        /// <summary>
        /// 返回奖励副本，避免 UI 或后续系统修改目录内的本地兜底数据。
        /// </summary>
        public static RewardItem[] GetRewards(int stageId)
        {
            RewardItem[] rewards;
            if (!DefaultRewardsByStageId.TryGetValue(stageId, out rewards))
            {
                return new RewardItem[0];
            }

            return CloneRewards(rewards);
        }

        private static RewardItem CreateReward(string id, string category, string name, int amount, int quality)
        {
            return new RewardItem
            {
                id = id,
                category = category,
                name = name,
                amount = amount,
                quality = quality
            };
        }

        private static RewardItem[] CloneRewards(RewardItem[] source)
        {
            RewardItem[] copy = new RewardItem[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                RewardItem item = source[i];
                copy[i] = new RewardItem
                {
                    id = item.id,
                    category = item.category,
                    name = item.name,
                    amount = item.amount,
                    iconPath = item.iconPath,
                    quality = item.quality,
                    isBound = item.isBound,
                    expireTime = item.expireTime,
                    description = item.description
                };
            }

            return copy;
        }

        private static MainlineStageInfo Find(MainlineStageInfo[] stages, int id)
        {
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i].id == id)
                {
                    return stages[i];
                }
            }

            return stages[0];
        }

        // 复制数组和元素，避免调用方误改一份配置后污染本地兜底表。
        private static MainlineStageInfo[] Clone(MainlineStageInfo[] source)
        {
            MainlineStageInfo[] copy = new MainlineStageInfo[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                MainlineStageInfo stage = source[i];
                copy[i] = new MainlineStageInfo(
                    stage.id,
                    stage.title,
                    stage.recommendLevel,
                    stage.recommendPower,
                    stage.objective,
                    stage.rewardPreview,
                    stage.unlocked);
            }

            return copy;
        }
    }
}
