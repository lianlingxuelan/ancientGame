namespace Shouyou.Data
{
    // 主线关卡目录。
    // 这里先集中写死第一章 6 个关卡，等后端和配置表稳定后再迁移到数据库。
    public static class MainlineStageCatalog
    {
        private static readonly MainlineStageInfo[] Stages =
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

        // 根据关卡序号读取关卡数据。
        // 如果传入的 id 不合法，默认返回第一关，避免运行时报空。
        public static MainlineStageInfo Get(int id)
        {
            for (int i = 0; i < Stages.Length; i++)
            {
                if (Stages[i].id == id)
                {
                    return Stages[i];
                }
            }

            return Stages[0];
        }
    }
}
