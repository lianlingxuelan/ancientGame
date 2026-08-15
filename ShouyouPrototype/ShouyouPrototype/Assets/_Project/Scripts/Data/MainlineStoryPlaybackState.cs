namespace Shouyou.Data
{
    /// <summary>
    /// 主线剧情逐句播放的运行时状态。
    /// 这个对象不依赖页面或场景，可同时给首次阅读、剧情回看和未来演出页复用。
    /// </summary>
    public sealed class MainlineStoryPlaybackState
    {
        /// <summary>
        /// 玩家进入剧情后，跳过按钮的最短解锁时间。
        /// 让玩家先看到开场信息，同时保留快速阅读的选择。
        /// </summary>
        public const float SKIP_UNLOCK_SECONDS = 3f;

        private MainlineStorySequence _currentSequence;
        private int _currentLineIndex;
        private float _elapsedSeconds;
        private bool _isCompleted;

        /// <summary>
        /// 当前正在阅读的关卡编号。未成功开始时为 0。
        /// </summary>
        public int CurrentStageId
        {
            get { return _currentSequence == null ? 0 : _currentSequence.stageId; }
        }

        /// <summary>
        /// 当前显示的台词索引。未开始时为 -1。
        /// </summary>
        public int CurrentLineIndex
        {
            get { return _currentSequence == null ? -1 : _currentLineIndex; }
        }

        /// <summary>
        /// 当前关卡的剧情总行数。未开始或不存在剧情时为 0。
        /// </summary>
        public int LineCount
        {
            get { return _currentSequence == null ? 0 : _currentSequence.LineCount; }
        }

        /// <summary>
        /// 当前显示的剧情文本。完成或未开始时返回空文本，交由页面决定显示结束提示。
        /// </summary>
        public string CurrentLine
        {
            get
            {
                if (_currentSequence == null || _isCompleted)
                {
                    return string.Empty;
                }

                return _currentSequence.GetLine(_currentLineIndex);
            }
        }

        /// <summary>
        /// 是否已经成功载入某一关剧情。
        /// </summary>
        public bool IsStarted
        {
            get { return _currentSequence != null; }
        }

        /// <summary>
        /// 是否已经读到结尾或主动跳过。
        /// </summary>
        public bool IsCompleted
        {
            get { return _isCompleted; }
        }

        /// <summary>
        /// 页面可据此显示或置灰跳过按钮。
        /// </summary>
        public bool IsSkipAvailable
        {
            get { return IsStarted && !_isCompleted && _elapsedSeconds >= SKIP_UNLOCK_SECONDS; }
        }

        /// <summary>
        /// 安全开始指定关卡的剧情。不存在的编号会清空当前状态并返回 false。
        /// </summary>
        public bool TryStart(int stageId)
        {
            MainlineStorySequence sequence;
            if (!MainlineStoryCatalog.TryGet(stageId, out sequence) || sequence == null || sequence.LineCount <= 0)
            {
                Reset();
                return false;
            }

            _currentSequence = sequence;
            _currentLineIndex = 0;
            _elapsedSeconds = 0f;
            _isCompleted = false;
            return true;
        }

        /// <summary>
        /// 由页面每帧或定时调用，累计本次阅读时长。
        /// 负时间和已完成状态不会影响跳过时间。
        /// </summary>
        public void AdvanceTime(float deltaSeconds)
        {
            if (!IsStarted || _isCompleted || deltaSeconds <= 0f)
            {
                return;
            }

            _elapsedSeconds += deltaSeconds;
        }

        /// <summary>
        /// 前进到下一句。当前句已经是最后一句时，结束剧情并记录已读。
        /// 返回 true 表示成功切换到下一句；返回 false 表示未开始或本次调用完成了剧情。
        /// </summary>
        public bool TryAdvance()
        {
            if (!IsStarted || _isCompleted)
            {
                return false;
            }

            if (_currentLineIndex + 1 < LineCount)
            {
                _currentLineIndex++;
                return true;
            }

            CompletePlayback();
            return false;
        }

        /// <summary>
        /// 跳过剩余剧情。只有到达 3 秒跳过门槛后才会生效。
        /// </summary>
        public bool TrySkip()
        {
            if (!IsSkipAvailable)
            {
                return false;
            }

            CompletePlayback();
            return true;
        }

        /// <summary>
        /// 清空当前临时状态。不会修改已读记录，适用于关闭页面或开始新关卡前。
        /// </summary>
        public void Reset()
        {
            _currentSequence = null;
            _currentLineIndex = -1;
            _elapsedSeconds = 0f;
            _isCompleted = false;
        }

        /// <summary>
        /// 统一收口正常读完和跳过两条路径，避免二者写出不同的已读结果。
        /// </summary>
        private void CompletePlayback()
        {
            if (_isCompleted || _currentSequence == null)
            {
                return;
            }

            _isCompleted = true;
            LevelProgressManager.Instance.MarkStoryRead(_currentSequence.stageId);
        }
    }
}
