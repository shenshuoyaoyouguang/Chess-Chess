using SharkChess.Models;

namespace SharkChess.Services
{
    /// <summary>
    /// 游戏服务接口
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// 当前棋盘状态
        /// </summary>
        ChessPiece[] Board { get; }

        /// <summary>
        /// 当前走棋方
        /// </summary>
        PieceColor SideToMove { get; }

        /// <summary>
        /// 游戏状态
        /// </summary>
        GameState GameState { get; }

        /// <summary>
        /// 选中的棋子位置
        /// </summary>
        Square? SelectedSquare { get; set; }

        /// <summary>
        /// 可走的合法位置
        /// </summary>
        IReadOnlyList<Square> LegalMoves { get; }

        /// <summary>
        /// 走棋历史
        /// </summary>
        IReadOnlyList<Move> MoveHistory { get; }

        /// <summary>
        /// 红方被吃棋子
        /// </summary>
        IReadOnlyList<ChessPiece> CapturedRedPieces { get; }

        /// <summary>
        /// 黑方被吃棋子
        /// </summary>
        IReadOnlyList<ChessPiece> CapturedBlackPieces { get; }

        /// <summary>
        /// 棋盘主题
        /// </summary>
        BoardTheme CurrentTheme { get; set; }

        /// <summary>
        /// 棋盘状态变更事件
        /// </summary>
        event EventHandler<BoardChangedEventArgs>? BoardChanged;

        /// <summary>
        /// 游戏状态变更事件
        /// </summary>
        event EventHandler<GameStateChangedEventArgs>? GameStateChanged;

        /// <summary>
        /// 将军状态变更事件
        /// </summary>
        event EventHandler<CheckStateChangedEventArgs>? CheckStateChanged;

        /// <summary>
        /// 游戏结束事件
        /// </summary>
        event EventHandler<GameEndedEventArgs>? GameEnded;

        /// <summary>
        /// 和棋申请事件
        /// </summary>
        event EventHandler<DrawOfferEventArgs>? DrawOffered;

        /// <summary>
        /// 初始化新游戏
        /// </summary>
        void NewGame();

        /// <summary>
        /// 从 FEN 字符串加载局面
        /// </summary>
        bool LoadFromFEN(string fen);

        /// <summary>
        /// 转换为 FEN 字符串
        /// </summary>
        string ToFEN();

        /// <summary>
        /// 尝试走棋
        /// </summary>
        bool TryMove(Square from, Square to);

        /// <summary>
        /// 悔棋
        /// </summary>
        bool Undo();

        /// <summary>
        /// 获取指定位置的棋子
        /// </summary>
        ChessPiece GetPieceAt(Square square);

        /// <summary>
        /// 选择棋子
        /// </summary>
        void SelectSquare(Square square);

        /// <summary>
        /// 获取合法走法
        /// </summary>
        IReadOnlyList<Move> GetLegalMoves();

        /// <summary>
        /// 判断是否为合法走法
        /// </summary>
        bool IsLegalMove(Square from, Square to);

        /// <summary>
        /// 是否有待处理的和棋申请
        /// </summary>
        bool IsDrawOfferPending { get; }

        /// <summary>
        /// 发起和棋申请
        /// </summary>
        /// <returns>如果申请成功发起返回 true</returns>
        bool OfferDraw();

        /// <summary>
        /// 回应和棋申请
        /// </summary>
        /// <param name="response">回应结果</param>
        void RespondToDrawOffer(DrawResponse response);

        /// <summary>
        /// 导出棋谱为 PGN 格式
        /// </summary>
        /// <param name="whitePlayer">红方玩家名称</param>
        /// <param name="blackPlayer">黑方玩家名称</param>
        /// <param name="eventName">事件名称</param>
        /// <param name="site">地点</param>
        /// <returns>PGN 格式字符串</returns>
        string ExportToPGN(string whitePlayer = "红方", string blackPlayer = "黑方", string eventName = "鲨鱼象棋对局", string site = "本地");

        #region 复盘功能

        /// <summary>
        /// 跳转到指定步数
        /// </summary>
        /// <param name="moveIndex">走棋索引（0-based）</param>
        /// <returns>是否成功跳转</returns>
        bool GoToMove(int moveIndex);

        /// <summary>
        /// 跳转到第一步
        /// </summary>
        /// <returns>是否成功跳转</returns>
        bool GoToStart();

        /// <summary>
        /// 跳转到最后一步
        /// </summary>
        /// <returns>是否成功跳转</returns>
        bool GoToEnd();

        /// <summary>
        /// 上一步
        /// </summary>
        /// <returns>是否成功跳转</returns>
        bool GoBack();

        /// <summary>
        /// 下一步
        /// </summary>
        /// <returns>是否成功跳转</returns>
        bool GoForward();

        /// <summary>
        /// 获取当前复盘位置索引
        /// </summary>
        int CurrentViewIndex { get; }

        /// <summary>
        /// 退出复盘模式，恢复到实际对局状态
        /// </summary>
        void ExitReviewMode();

        #endregion
    }

    /// <summary>
    /// 棋盘变更事件参数
    /// </summary>
    public class BoardChangedEventArgs : EventArgs
    {
        public Move? LastMove { get; init; }
        public ChessPiece[] Board { get; init; } = Array.Empty<ChessPiece>();
    }

    /// <summary>
    /// 游戏状态变更事件参数
    /// </summary>
    public class GameStateChangedEventArgs : EventArgs
    {
        public GameState NewState { get; init; }
        public PieceColor SideToMove { get; init; }
    }

    /// <summary>
    /// 将军状态变更事件参数
    /// </summary>
    public class CheckStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 是否处于将军状态
        /// </summary>
        public bool IsInCheck { get; init; }

        /// <summary>
        /// 被将军的一方
        /// </summary>
        public PieceColor CheckedColor { get; init; }

        /// <summary>
        /// 将/帅的位置
        /// </summary>
        public Square? KingSquare { get; init; }
    }

    /// <summary>
    /// 和棋申请事件参数
    /// </summary>
    public class DrawOfferEventArgs : EventArgs
    {
        /// <summary>
        /// 申请和棋的一方
        /// </summary>
        public PieceColor OfferingSide { get; init; }

        /// <summary>
        /// 是否需要对方回应（人机对战时为false，由AI自动决定）
        /// </summary>
        public bool RequiresResponse { get; init; }
    }

    /// <summary>
    /// 和棋回应结果
    /// </summary>
    public enum DrawResponse
    {
        /// <summary>
        /// 同意和棋
        /// </summary>
        Accept,

        /// <summary>
        /// 拒绝和棋
        /// </summary>
        Decline
    }
}