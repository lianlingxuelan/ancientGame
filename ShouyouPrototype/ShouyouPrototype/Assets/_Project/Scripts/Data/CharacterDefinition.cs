using System;

namespace Shouyou.Data
{
    // 角色的基础数据结构。
    // 当前先作为 Demo 数据契约，后续可以替换成 ScriptableObject 或 JSON 数据。
    [Serializable]
    public sealed class CharacterDefinition
    {
        // 角色唯一编号，正式项目中用于存档和抽卡结果识别。
        public string id;

        // 角色在界面中显示的姓名。
        public string displayName;

        // 当前等级和等级上限。
        public int level = 1;
        public int maxLevel = 60;

        // 词意名称，例如“如梦令”“破阵子”等。
        public string ciYi;

        // 稀有度和战斗定位，例如 SSR、辅助、输出。
        public string rarity;
        public string role;
    }
}
