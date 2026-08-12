using UnityEngine;

namespace Shouyou.Data
{
    /// <summary>
    /// 主线关卡进度管理器。
    /// 
    /// 当前 Demo 阶段先用 PlayerPrefs 做本地持久化：
    /// 1. 运行时不依赖场景物体，任何 UI 脚本都能直接读取。
    /// 2. 完成战斗后记录最高已通关关卡。
    /// 3. 下一关自动变成可挑战状态。
    /// 
    /// 后续如果接入真正数据库或后端，只需要把 Load/Save 的实现替换掉，
    /// UI 层不用跟着大改。
    /// </summary>
    public sealed class LevelProgressManager
    {
        /// <summary>
        /// 关卡总数。第一版先固定第一章 6 个小关。
        /// </summary>
        public const int MaxMainlineStageId = 6;

        /// <summary>
        /// 新玩家默认只开放第 1 关，后续关卡须由胜利结算逐个解锁。
        /// </summary>
        private const int DemoInitialUnlockedStageId = 1;

        /// <summary>
        /// 本地保存键名。以后要迁移到 JSON 或服务器时，可以保留这个键做兼容。
        /// </summary>
        private const string HighestClearedStageKey = "Shouyou.Mainline.HighestClearedStageId";

        /// <summary>
        /// 剧情阅读记录的键前缀。阅读记录与通关记录分开保存：
        /// 玩家可以先阅读剧情、再战斗，也可以跳过后回看。
        /// </summary>
        private const string StoryReadKeyPrefix = "Shouyou.Mainline.StoryRead.";

        private static LevelProgressManager instance;

        /// <summary>
        /// 单例入口。这个类不是 MonoBehaviour，所以不需要挂到 Unity 场景物体上。
        /// </summary>
        public static LevelProgressManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LevelProgressManager();
                }

                return instance;
            }
        }

        /// <summary>
        /// 当前本地记录的最高已通关关卡。
        /// </summary>
        private int highestClearedStageId;

        private LevelProgressManager()
        {
            Load();
        }

        /// <summary>
        /// 判断关卡是否已经通关。
        /// </summary>
        public bool IsStageCleared(int stageId)
        {
            return NormalizeStageId(stageId) <= highestClearedStageId;
        }

        /// <summary>
        /// 判断关卡是否可挑战。
        ///
        /// 规则：
        /// - 已通关关卡可以重复挑战。
        /// - 最高已通关关卡的下一关会自动解锁。
        /// - 新档默认只开放第一关，后续关卡由胜利结算逐个解锁。
        /// </summary>
        public bool IsStageUnlocked(int stageId)
        {
            int safeStageId = NormalizeStageId(stageId);
            int highestUnlockedStageId = Mathf.Max(DemoInitialUnlockedStageId, highestClearedStageId + 1);
            return safeStageId <= Mathf.Clamp(highestUnlockedStageId, 1, MaxMainlineStageId);
        }

        /// <summary>
        /// 通关指定关卡，并把进度写入本地。
        /// 返回 true 代表本次确实推进了新进度。
        /// </summary>
        public bool CompleteStage(int stageId)
        {
            int safeStageId = NormalizeStageId(stageId);
            if (safeStageId <= highestClearedStageId)
            {
                return false;
            }

            // 进度管理器本身也要拦截越关调用，不能只依赖 UI 按钮是否置灰。
            // 这样未来增加其它入口时，也不会把第三关直接记成已通关。
            if (!IsStageUnlocked(safeStageId))
            {
                return false;
            }

            highestClearedStageId = safeStageId;
            Save();
            return true;
        }

        /// <summary>
        /// 用后端返回的最高通关进度同步本地缓存。
        /// 
        /// 注意：这里不直接相信 UI 传入的关卡标题，只接受数字进度。
        /// 后端如果返回异常值，会被限制在 0 到 MaxMainlineStageId 之间。
        /// </summary>
        public void SyncHighestClearedStage(int stageId)
        {
            int safeStageId = Mathf.Clamp(stageId, 0, MaxMainlineStageId);
            if (safeStageId == highestClearedStageId)
            {
                return;
            }

            highestClearedStageId = safeStageId;
            Save();
        }

        /// <summary>
        /// 获取指定关卡后面的下一关。
        /// 如果已经是最后一关，就仍然返回最后一关。
        /// </summary>
        public int GetNextStageId(int stageId)
        {
            return Mathf.Clamp(NormalizeStageId(stageId) + 1, 1, MaxMainlineStageId);
        }

        /// <summary>
        /// 生成给玩家看的关卡状态文案。
        /// </summary>
        public string GetStageStateLabel(int stageId)
        {
            if (IsStageCleared(stageId))
            {
                return "已通关";
            }

            return IsStageUnlocked(stageId) ? "可挑战" : "暂未解锁";
        }

        /// <summary>
        /// 获取当前最高已通关关卡。
        /// 主要给开发调试面板读取，避免 UI 直接访问内部字段。
        /// </summary>
        public int GetHighestClearedStageId()
        {
            return highestClearedStageId;
        }

        /// <summary>
        /// 判断指定关卡的剧情是否已经阅读或跳过。
        /// </summary>
        public bool IsStoryRead(int stageId)
        {
            return PlayerPrefs.GetInt(BuildStoryReadKey(stageId), 0) == 1;
        }

        /// <summary>
        /// 记录剧情已读。这个记录不推进关卡，也不直接发放奖励。
        /// </summary>
        public void MarkStoryRead(int stageId)
        {
            PlayerPrefs.SetInt(BuildStoryReadKey(stageId), 1);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            highestClearedStageId = Mathf.Clamp(PlayerPrefs.GetInt(HighestClearedStageKey, 0), 0, MaxMainlineStageId);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(HighestClearedStageKey, highestClearedStageId);
            PlayerPrefs.Save();
        }

        private static int NormalizeStageId(int stageId)
        {
            return Mathf.Clamp(stageId, 1, MaxMainlineStageId);
        }

        private static string BuildStoryReadKey(int stageId)
        {
            return StoryReadKeyPrefix + NormalizeStageId(stageId);
        }
    }
}
