namespace Shouyou.Data
{
    /// <summary>
    /// 一段主线剧情的轻量配置。
    /// 第一版先使用本地数据，后续可由后端按 stageId 返回同样的结构。
    /// </summary>
    public sealed class MainlineStorySequence
    {
        public readonly int stageId;
        public readonly string title;
        public readonly string[] lines;

        /// <summary>
        /// 剧情总行数。供逐句播放和剧情回看安全判断边界。
        /// </summary>
        public int LineCount
        {
            get { return lines == null ? 0 : lines.Length; }
        }

        public MainlineStorySequence(int stageId, string title, params string[] lines)
        {
            this.stageId = stageId;
            this.title = title;
            this.lines = lines;
        }

        /// <summary>
        /// 安全取得指定剧情行；非法索引返回空文本，避免 UI 切换时发生越界异常。
        /// </summary>
        public string GetLine(int index)
        {
            if (lines == null || index < 0 || index >= lines.Length)
            {
                return string.Empty;
            }

            return lines[index] ?? string.Empty;
        }
    }

    /// <summary>
    /// 第一章主线剧情目录。
    /// 文案与关卡编号放在同一个入口，避免 UI 脚本按按钮名称硬编码内容。
    /// </summary>
    public static class MainlineStoryCatalog
    {
        private static readonly MainlineStorySequence[] Sequences =
        {
            new MainlineStorySequence(
                1,
                "明水入汴京",
                "【旁白】春日庭院的风穿过竹影，李清照收起未写完的词稿。",
                "【李清照】今日的汴京，或许会有我未曾见过的文章与人。",
                "【婉禾】雅集人多眼杂，我陪你一道去。",
                "【旁白】两人约定在暮色前归来，第一段行程就此展开。"),
            new MainlineStorySequence(
                2,
                "雅集赴会",
                "【旁白】园中已摆好长案，诸位来客正以春景为题填词。",
                "【婉禾】别紧张，你写下的句子，比他们的议论更有分量。",
                "【李清照】那便以词相见。",
                "【旁白】一场关于才情与勇气的试炼，悄然开始。"),
            new MainlineStorySequence(
                3,
                "词论初临",
                "【旁白】前辈将一阕旧词推到案前，等候李清照应答。",
                "【李清照】词不止是闺中风月，也可写山河与心志。",
                "【旁白】四下安静下来，第一次词论即将见分晓。"),
            new MainlineStorySequence(
                4,
                "风雨前夜",
                "【旁白】雨声打在檐角，雅集散后仍有人对今日的词论耿耿于怀。",
                "【婉禾】明日若有人为难，我会站在你身边。",
                "【李清照】不必替我挡风。我要亲自回答。"),
            new MainlineStorySequence(
                5,
                "故人入梦",
                "【旁白】夜深后，词稿上的墨迹化作微光，神识忽然被牵入陌生的梦境。",
                "【李清照】这不是寻常的梦……是谁在唤我？",
                "【旁白】一只梦蝶停在指尖，命运的裂隙由此出现。"),
            new MainlineStorySequence(
                6,
                "潮声再起",
                "【旁白】潮声越过梦境与现实的边界，旧日回响逐渐清晰。",
                "【李清照】若命运早有定数，我也要为自己写下另一种结局。",
                "【旁白】第一卷暂告一段，梦域的门仍在远处微微发亮。"
            )
        };

        /// <summary>
        /// 根据关卡编号取得剧情；非法编号回退到第一关，保证 Demo 不会空引用。
        /// </summary>
        public static MainlineStorySequence Get(int stageId)
        {
            MainlineStorySequence sequence;
            if (TryGet(stageId, out sequence))
            {
                return sequence;
            }

            return Sequences[0];
        }

        /// <summary>
        /// 尝试读取指定关卡的剧情。调用方需要区分不存在关卡时，应使用此方法而不是旧的回退接口。
        /// </summary>
        public static bool TryGet(int stageId, out MainlineStorySequence sequence)
        {
            for (int i = 0; i < Sequences.Length; i++)
            {
                if (Sequences[i].stageId == stageId)
                {
                    sequence = Sequences[i];
                    return true;
                }
            }

            sequence = null;
            return false;
        }

        /// <summary>
        /// 返回当前章节配置的关卡编号副本，供关卡列表和剧情回看入口枚举使用。
        /// </summary>
        public static int[] GetStageIds()
        {
            var stageIds = new int[Sequences.Length];
            for (int i = 0; i < Sequences.Length; i++)
            {
                stageIds[i] = Sequences[i].stageId;
            }

            return stageIds;
        }
    }
}
