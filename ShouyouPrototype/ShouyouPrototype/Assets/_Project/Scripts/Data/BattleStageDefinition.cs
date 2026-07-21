using System;

namespace Shouyou.Data
{
    // PVE 关卡的基础数据结构。
    // 先用简单字段支撑第一章 Demo，后续再接敌人、奖励和剧情条件。
    [Serializable]
    public sealed class BattleStageDefinition
    {
        // 关卡唯一编号和显示名称。
        public string id;
        public string displayName;

        // 推荐等级、体力消耗和是否已经完成。
        public int recommendedLevel = 1;
        public int staminaCost = 6;
        public bool completed;

        // 关卡进入前可以展示的词意提示。
        public string ciYiHint;
    }
}
