using System;

namespace Shouyou.Data
{
    // 一章剧情的数据结构。
    // chapterId 对应 Part_One、Part_Two、Part_Three 等资料文件夹。
    [Serializable]
    public sealed class StoryChapterDefinition
    {
        public string chapterId;
        public string title;
        public string summary;
        public int sceneCount;
        public bool unlocked;
    }

    // 一个剧情场景的数据结构。
    // 场景可以同时关联文字、角色、CG、战斗和场景背景。
    [Serializable]
    public sealed class StorySceneDefinition
    {
        public string sceneId;
        public string title;
        public string summary;
        public string[] characters;
        public string backgroundId;
        public string cgId;
        public string battleId;
        public bool hasChoice;
    }
}
