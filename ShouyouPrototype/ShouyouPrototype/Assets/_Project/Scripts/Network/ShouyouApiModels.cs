using System;

namespace Shouyou.Network
{
    // 这里的类专门对应本地 Node 后台返回的 JSON。
    // Unity 自带的 JsonUtility 需要字段名和 JSON 字段名保持一致，所以这里使用后端字段名。
    [Serializable]
    public sealed class PlayerProfileResponse
    {
        public string id;
        public string name;
        public int level;
        public int coins;
        public int jade;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    public sealed class CharacterListResponse
    {
        public string playerId;
        public CharacterDto[] characters;
    }

    [Serializable]
    public sealed class CharacterDto
    {
        public string id;
        public string name;
        public string rarity;
        public string role;
        public string wordIntent;
        public string description;
        public int level;
        public int bondLevel;
        public bool unlocked;
    }

    [Serializable]
    public sealed class ChapterListResponse
    {
        public ChapterDto[] chapters;
    }

    [Serializable]
    public sealed class ChapterDto
    {
        public string id;
        public string title;
        public string subtitle;
        public int sortOrder;
        public StageDto[] stages;
    }

    [Serializable]
    public sealed class StageDto
    {
        public string id;
        public string title;
        public int recommendedLevel;
        public int energyCost;
        public bool defaultUnlocked;
        public int sortOrder;
    }

    [Serializable]
    public sealed class FormationResponse
    {
        public string playerId;
        public FormationSlotDto[] slots;
    }

    [Serializable]
    public sealed class FormationSlotDto
    {
        public int slotIndex;
        public string characterId;
        public string characterName;
        public string wordIntent;
    }

    [Serializable]
    public sealed class SaveProgressResponse
    {
        public string playerId;
        public string currentChapterId;
        public string currentStageId;
        public string[] completedStageIds;
        public string updatedAt;
    }

    [Serializable]
    public class StageProgressResponse
    {
        public string playerId;
        public string chapterId;
        public int highestClearedStageId;
        public StageProgressDto[] stages;
    }

    [Serializable]
    public sealed class StageCompleteResponse : StageProgressResponse
    {
        public string stageId;
        public bool progressAdvanced;
    }

    [Serializable]
    public sealed class StageProgressDto
    {
        public string id;
        public string title;
        public bool cleared;
        public string clearedAt;
        public bool unlocked;
    }


    [Serializable]
    public sealed class BattleDemoConfigResponse
    {
        public string stageId;
        public string title;
        public int maxActionPoint;
        public BattleUnitDto[] allies;
        public BattleUnitDto[] enemies;
        public BattleSkillDto[] skills;
    }

    [Serializable]
    public sealed class BattleUnitDto
    {
        public string id;
        public string name;
        public int slot;
        public int hp;
        public int attack;
        public string portraitIconKey;
    }

    [Serializable]
    public sealed class BattleSkillDto
    {
        public string id;
        public string label;
        public string iconKey;
        public string target;
        public float multiplier;
        public int cooldown;
    }

    [Serializable]
    public sealed class BattleSkillAssetListResponse
    {
        public string category;
        public int version;
        public BattleSkillAssetDto[] icons;
    }

    [Serializable]
    public sealed class BattleSkillAssetDto
    {
        public string iconKey;
        public string displayName;
        public string url;
        public string category;
        public int width;
        public int height;
        public bool _placeholder;
    }

    [Serializable]
    public sealed class HealthResponse
    {
        public bool ok;
        public string service;
        public string time;
    }
}
