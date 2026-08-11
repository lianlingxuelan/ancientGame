using UnityEngine;
using Shouyou.Network;

namespace Shouyou.Data
{
    /// <summary>
    /// 角色养成的数据入口。
    ///
    /// 当前第一版只开放李清照的等级成长：统一提供等级、派生属性、下一级材料预览和升级结果。
    /// UI 与战斗都只读取快照，不自行计算等级或直接操作 PlayerPrefs；后续接入服务端角色档案时，
    /// 可以替换本类内部的读取和写入实现，不改变页面层调用方式。
    /// </summary>
    public sealed class CharacterDevelopmentManager
    {
        /// <summary>
        /// 第一位可养成角色的稳定 id。后续角色沿用英文 id，避免中文名变更影响存档。
        /// </summary>
        public const string LiQingzhaoId = "li_qingzhao";

        private const string LevelKeyPrefix = "Shouyou.Player.Character.Level.";
        private const int LiQingzhaoMaxLevel = 60;
        private const int LiQingzhaoBaseHealth = 1200;
        private const int LiQingzhaoBaseAttack = 180;
        private const int LiQingzhaoBaseDefense = 95;
        private const int HealthPerLevel = 90;
        private const int AttackPerLevel = 16;
        private const int DefensePerLevel = 9;

        private static CharacterDevelopmentManager instance;

        /// <summary>
        /// 不依赖场景对象的单例入口，方便角色页、编队页和后续战斗入口读取同一份角色数据。
        /// </summary>
        public static CharacterDevelopmentManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CharacterDevelopmentManager();
                }

                return instance;
            }
        }

        private CharacterDevelopmentManager()
        {
        }

        /// <summary>
        /// 返回角色当前等级和由等级推导出的基础属性快照。
        /// 未有存档记录时从 Lv.1 开始；未知角色返回 null，调用方必须自行提示。
        /// </summary>
        public CharacterDevelopmentSnapshot GetSnapshot(string characterId)
        {
            if (!IsSupportedCharacter(characterId))
            {
                return null;
            }

            int level = Mathf.Clamp(PlayerPrefs.GetInt(BuildLevelKey(characterId), 1), 1, LiQingzhaoMaxLevel);
            int levelOffset = level - 1;
            return new CharacterDevelopmentSnapshot(
                characterId,
                "李清照",
                level,
                LiQingzhaoMaxLevel,
                LiQingzhaoBaseHealth + levelOffset * HealthPerLevel,
                LiQingzhaoBaseAttack + levelOffset * AttackPerLevel,
                LiQingzhaoBaseDefense + levelOffset * DefensePerLevel);
        }

        /// <summary>
        /// 返回升到下一等级需要的材料副本。
        /// 成本只在本类定义，页面只负责展示，避免不同入口显示出不同价格。
        /// 已满级或未知角色返回空数组。
        /// </summary>
        public RewardItem[] GetNextLevelCosts(string characterId)
        {
            CharacterDevelopmentSnapshot snapshot = GetSnapshot(characterId);
            if (snapshot == null || snapshot.level >= snapshot.maxLevel)
            {
                return new RewardItem[0];
            }

            int levelOffset = snapshot.level - 1;
            return new[]
            {
                CreateCost("coin", "铜钱", 300 + levelOffset * 150),
                CreateCost("poetry_exp", "词意经验", 40 + levelOffset * 20)
            };
        }

        /// <summary>
        /// 尝试升级指定角色。
        /// 先校验角色和等级上限，再通过资源钱包原子扣除全部材料；只有材料全部扣除成功后才写入新等级。
        /// </summary>
        public CharacterLevelUpResult TryLevelUp(string characterId)
        {
            CharacterDevelopmentSnapshot snapshot = GetSnapshot(characterId);
            if (snapshot == null)
            {
                return CharacterLevelUpResult.Failed("当前角色尚未开放养成。");
            }

            if (snapshot.level >= snapshot.maxLevel)
            {
                return CharacterLevelUpResult.Failed(snapshot.characterName + "已达到等级上限。", snapshot);
            }

            RewardItem[] costs = GetNextLevelCosts(characterId);
            if (!PlayerResourceManager.Instance.TrySpend(costs))
            {
                return CharacterLevelUpResult.Failed("材料不足，暂时无法升级。", snapshot, costs);
            }

            int nextLevel = snapshot.level + 1;
            PlayerPrefs.SetInt(BuildLevelKey(characterId), nextLevel);
            PlayerPrefs.Save();

            CharacterDevelopmentSnapshot nextSnapshot = GetSnapshot(characterId);
            return CharacterLevelUpResult.Succeeded(
                snapshot,
                nextSnapshot,
                costs,
                snapshot.characterName + "已升至 Lv." + nextLevel + "。"
            );
        }

        private static RewardItem CreateCost(string id, string name, int amount)
        {
            return new RewardItem
            {
                id = id,
                category = "材料",
                name = name,
                amount = amount,
                quality = 1
            };
        }

        private static bool IsSupportedCharacter(string characterId)
        {
            return characterId == LiQingzhaoId;
        }

        private static string BuildLevelKey(string characterId)
        {
            return LevelKeyPrefix + characterId;
        }
    }

    /// <summary>
    /// 供 UI、编队和后续战斗读取的角色状态快照。
    /// 快照是一次读取的结果，不暴露写入入口，避免调用方绕开养成管理器改等级。
    /// </summary>
    public sealed class CharacterDevelopmentSnapshot
    {
        public readonly string characterId;
        public readonly string characterName;
        public readonly int level;
        public readonly int maxLevel;
        public readonly int health;
        public readonly int attack;
        public readonly int defense;

        public CharacterDevelopmentSnapshot(
            string characterId,
            string characterName,
            int level,
            int maxLevel,
            int health,
            int attack,
            int defense)
        {
            this.characterId = characterId;
            this.characterName = characterName;
            this.level = level;
            this.maxLevel = maxLevel;
            this.health = health;
            this.attack = attack;
            this.defense = defense;
        }
    }

    /// <summary>
    /// 升级操作返回值。失败时保留升级前快照，UI 可以据此展示材料不足或满级原因。
    /// </summary>
    public sealed class CharacterLevelUpResult
    {
        public readonly bool succeeded;
        public readonly string message;
        public readonly CharacterDevelopmentSnapshot previousSnapshot;
        public readonly CharacterDevelopmentSnapshot currentSnapshot;
        public readonly RewardItem[] spentCosts;

        private CharacterLevelUpResult(
            bool succeeded,
            string message,
            CharacterDevelopmentSnapshot previousSnapshot,
            CharacterDevelopmentSnapshot currentSnapshot,
            RewardItem[] spentCosts)
        {
            this.succeeded = succeeded;
            this.message = message;
            this.previousSnapshot = previousSnapshot;
            this.currentSnapshot = currentSnapshot;
            this.spentCosts = spentCosts ?? new RewardItem[0];
        }

        public static CharacterLevelUpResult Succeeded(
            CharacterDevelopmentSnapshot previousSnapshot,
            CharacterDevelopmentSnapshot currentSnapshot,
            RewardItem[] spentCosts,
            string message)
        {
            return new CharacterLevelUpResult(true, message, previousSnapshot, currentSnapshot, spentCosts);
        }

        public static CharacterLevelUpResult Failed(
            string message,
            CharacterDevelopmentSnapshot snapshot = null,
            RewardItem[] costs = null)
        {
            return new CharacterLevelUpResult(false, message, snapshot, snapshot, costs);
        }
    }
}
