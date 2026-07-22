namespace Shouyou.Data
{
    // 主线关卡数据。
    // 第一版先用普通 C# 类集中保存；后续可以升级为 JSON、ScriptableObject 或后端接口返回。
    public sealed class MainlineStageInfo
    {
        // 关卡序号，例如 1 表示第一关。
        public int id;

        // 关卡显示名，例如“1-1 明水入汴京”。
        public string title;

        // 推荐等级，用于提示玩家是否适合挑战。
        public int recommendLevel;

        // 推荐战力，用于强化数值追求感。
        public int recommendPower;

        // 关卡目标，告诉玩家这一关要做什么。
        public string objective;

        // 奖励预览，正式版可以拆成奖励数组。
        public string rewardPreview;

        // 是否已解锁。
        public bool unlocked;

        public MainlineStageInfo(int id, string title, int recommendLevel, int recommendPower, string objective, string rewardPreview, bool unlocked)
        {
            this.id = id;
            this.title = title;
            this.recommendLevel = recommendLevel;
            this.recommendPower = recommendPower;
            this.objective = objective;
            this.rewardPreview = rewardPreview;
            this.unlocked = unlocked;
        }
    }
}
