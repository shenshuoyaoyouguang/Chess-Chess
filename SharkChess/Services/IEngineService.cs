namespace SharkChess.Services
{
    /// <summary>
    /// 引擎服务接口
    /// </summary>
    public interface IEngineService : IDisposable
    {
        /// <summary>
        /// 引擎是否已加载
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// 引擎是否正在思考
        /// </summary>
        bool IsThinking { get; }

        /// <summary>
        /// 引擎路径
        /// </summary>
        string? EnginePath { get; set; }

        /// <summary>
        /// 搜索深度
        /// </summary>
        int Depth { get; set; }

        /// <summary>
        /// 时间限制（毫秒）
        /// </summary>
        int TimeLimit { get; set; }

        /// <summary>
        /// 难度等级
        /// </summary>
        Models.EngineDifficulty Difficulty { get; set; }

        /// <summary>
        /// 引擎输出事件
        /// </summary>
        event EventHandler<EngineOutputEventArgs>? EngineOutput;

        /// <summary>
        /// 最佳走法事件
        /// </summary>
        event EventHandler<BestMoveEventArgs>? BestMove;

        /// <summary>
        /// 局面评估事件（用于AI判断是否同意和棋）
        /// </summary>
        event EventHandler<EvaluationEventArgs>? EvaluationReceived;

        /// <summary>
        /// 加载引擎
        /// </summary>
        bool LoadEngine(string path);

        /// <summary>
        /// 卸载引擎
        /// </summary>
        void UnloadEngine();

        /// <summary>
        /// 发送 UCI 命令
        /// </summary>
        void SendCommand(string command);

        /// <summary>
        /// 设置局面
        /// </summary>
        void SetPosition(string fen, IEnumerable<string>? moves = null);

        /// <summary>
        /// 开始思考
        /// </summary>
        void StartThinking(int? depth = null, int? timeMs = null);

        /// <summary>
        /// 停止思考
        /// </summary>
        void StopThinking();

        /// <summary>
        /// 发送新游戏命令
        /// </summary>
        void NewGame();

        /// <summary>
        /// 设置 UCI 选项
        /// </summary>
        void SetOption(string name, string value);

        /// <summary>
        /// 评估当前局面（不执行走棋）
        /// </summary>
        /// <param name="fen">FEN 字符串</param>
        /// <param name="depth">搜索深度</param>
        /// <returns>评估分数</returns>
        Task<int> EvaluatePositionAsync(string fen, int depth = 6);
    }

    /// <summary>
    /// 引擎输出事件参数
    /// </summary>
    public class EngineOutputEventArgs : EventArgs
    {
        public string Output { get; init; } = "";
        public bool IsError { get; init; }
    }

    /// <summary>
    /// 最佳走法事件参数
    /// </summary>
    public class BestMoveEventArgs : EventArgs
    {
        public string BestMove { get; init; } = "";
        public string? Ponder { get; init; }
        public int Score { get; init; }
        public int Depth { get; init; }
        public long Nodes { get; init; }
        public long TimeMs { get; init; }
    }

    /// <summary>
    /// 局面评估事件参数
    /// </summary>
    public class EvaluationEventArgs : EventArgs
    {
        /// <summary>
        /// 评估分数（以引擎方视角，单位：厘兵值）
        /// </summary>
        public int Score { get; init; }

        /// <summary>
        /// 搜索深度
        /// </summary>
        public int Depth { get; init; }
    }
}
