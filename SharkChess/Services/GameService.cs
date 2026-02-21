using SharkChess.Models;
using System.Diagnostics;
using System.Text;

namespace SharkChess.Services
{
    /// <summary>
    /// 游戏服务实现
    /// </summary>
    public class GameService : IGameService
    {
        private readonly ChessPiece[] _board;
        private readonly List<Move> _moveHistory;
        private PieceColor _sideToMove;
        private GameState _gameState;
        private Square? _selectedSquare;
        private List<Square> _legalMoves;
        private bool _drawOfferPending; // 是否有待处理的和棋申请
        private PieceColor? _drawOfferSide; // 申请和棋的一方

        // 计时相关字段
        private double _redTimeElapsed;
        private double _blackTimeElapsed;
        private bool _isRedThinking;
        private DateTime _gameStartTime;
        private DateTime _lastMoveTime;

        // 60 回合自然限着追踪
        private int _halfMovesSinceLastCaptureOrPawnMove; // 自上次吃子或兵卒移动以来的半回合数

        // 三次重复局面检测
        private readonly List<string> _positionHistory; // 局面历史记录（用于检测重复）

        // 长将/长捉检测
        private readonly List<(PieceColor Color, bool IsCheck)> _checkHistory; // 将军历史
        private const int MaxCheckRepetition = 3; // 连续将军次数上限（长将判定）

        // 复盘功能
        private int _currentViewIndex; // 当前查看的走棋索引（用于复盘）
        private List<ChessPiece[]>? _boardHistory; // 棋盘历史记录
        private List<PieceColor>? _sideToMoveHistory; // 走棋方历史记录

        // 阵亡棋子追踪
        private readonly List<ChessPiece> _capturedRedPieces; // 红方被吃棋子
        private readonly List<ChessPiece> _capturedBlackPieces; // 黑方被吃棋子

        // 将/帅位置缓存（性能优化：避免每次都遍历棋盘查找）
        private Square? _redKingSquare;
        private Square? _blackKingSquare;

        /// <summary>
        /// 日志回调，用于记录错误和警告信息
        /// </summary>
        public Action<string>? LogCallback { get; set; }

        public GameService()
        {
            _board = new ChessPiece[ChessConstants.TotalSquares];
            _moveHistory = new List<Move>();
            _sideToMove = PieceColor.Red;
            _gameState = GameState.Ongoing;
            _legalMoves = new List<Square>();
            _drawOfferPending = false;
            _drawOfferSide = null;
            _redTimeElapsed = 0;
            _blackTimeElapsed = 0;
            _isRedThinking = false;
            
            // 初始化新字段
            _halfMovesSinceLastCaptureOrPawnMove = 0;
            _positionHistory = new List<string>();
            _checkHistory = new List<(PieceColor, bool)>();
            _currentViewIndex = -1;
            
            // 初始化阵亡棋子追踪
            _capturedRedPieces = new List<ChessPiece>();
            _capturedBlackPieces = new List<ChessPiece>();
            
            NewGame();
        }

        public ChessPiece[] Board => _board;
        public PieceColor SideToMove => _sideToMove;
        public GameState GameState => _gameState;
        public IReadOnlyList<Move> MoveHistory => _moveHistory.AsReadOnly();
        public IReadOnlyList<Square> LegalMoves => _legalMoves.AsReadOnly();
        public IReadOnlyList<ChessPiece> CapturedRedPieces => _capturedRedPieces.AsReadOnly();
        public IReadOnlyList<ChessPiece> CapturedBlackPieces => _capturedBlackPieces.AsReadOnly();

        public Square? SelectedSquare
        {
            get => _selectedSquare;
            set
            {
                _selectedSquare = value;
                UpdateLegalMoves();
            }
        }

        public BoardTheme CurrentTheme { get; set; } = BoardTheme.Standard;

        public event EventHandler<BoardChangedEventArgs>? BoardChanged;
        public event EventHandler<GameStateChangedEventArgs>? GameStateChanged;
        public event EventHandler<CheckStateChangedEventArgs>? CheckStateChanged;
        public event EventHandler<DrawOfferEventArgs>? DrawOffered;
        public event EventHandler<GameEndedEventArgs>? GameEnded;

        public void NewGame()
        {
            Array.Fill(_board, new ChessPiece());
            _moveHistory.Clear();
            _sideToMove = PieceColor.Red;
            _gameState = GameState.Ongoing;
            _selectedSquare = null;
            _legalMoves.Clear();
            _drawOfferPending = false;
            _drawOfferSide = null;
            _redTimeElapsed = 0;
            _blackTimeElapsed = 0;
            _isRedThinking = false;
            _gameStartTime = DateTime.Now;
            _lastMoveTime = _gameStartTime;

            // 重置新增的字段
            _halfMovesSinceLastCaptureOrPawnMove = 0;
            _positionHistory.Clear();
            _checkHistory.Clear();
            _currentViewIndex = -1;
            
            // 重置阵亡棋子追踪
            _capturedRedPieces.Clear();
            _capturedBlackPieces.Clear();
            
            // 重置将/帅位置缓存
            _redKingSquare = null;
            _blackKingSquare = null;

            SetupInitialPosition();
            
            // 初始化将/帅位置缓存
            InitializeKingPositionCache();
            
            // 初始化棋盘历史记录（用于复盘）
            InitializeBoardHistory();
            
            OnBoardChanged(null);
        }

        private void SetupInitialPosition()
        {
            // 黑方棋子（上方，rank 1-5）
            SetPiece(1, 1, PieceType.Rook, PieceColor.Black);
            SetPiece(9, 1, PieceType.Rook, PieceColor.Black);
            SetPiece(2, 1, PieceType.Knight, PieceColor.Black);
            SetPiece(8, 1, PieceType.Knight, PieceColor.Black);
            SetPiece(3, 1, PieceType.Bishop, PieceColor.Black);
            SetPiece(7, 1, PieceType.Bishop, PieceColor.Black);
            SetPiece(4, 1, PieceType.Advisor, PieceColor.Black);
            SetPiece(6, 1, PieceType.Advisor, PieceColor.Black);
            SetPiece(5, 1, PieceType.King, PieceColor.Black);
            SetPiece(2, 3, PieceType.Cannon, PieceColor.Black);
            SetPiece(8, 3, PieceType.Cannon, PieceColor.Black);

            for (int f = 1; f <= 9; f += 2)
            {
                SetPiece(f, 4, PieceType.Pawn, PieceColor.Black);
            }

            // 红方棋子（下方，rank 6-10）
            SetPiece(1, 10, PieceType.Rook, PieceColor.Red);
            SetPiece(9, 10, PieceType.Rook, PieceColor.Red);
            SetPiece(2, 10, PieceType.Knight, PieceColor.Red);
            SetPiece(8, 10, PieceType.Knight, PieceColor.Red);
            SetPiece(3, 10, PieceType.Bishop, PieceColor.Red);
            SetPiece(7, 10, PieceType.Bishop, PieceColor.Red);
            SetPiece(4, 10, PieceType.Advisor, PieceColor.Red);
            SetPiece(6, 10, PieceType.Advisor, PieceColor.Red);
            SetPiece(5, 10, PieceType.King, PieceColor.Red);
            SetPiece(2, 8, PieceType.Cannon, PieceColor.Red);
            SetPiece(8, 8, PieceType.Cannon, PieceColor.Red);

            for (int f = 1; f <= 9; f += 2)
            {
                SetPiece(f, 7, PieceType.Pawn, PieceColor.Red);
            }
        }

        private void SetPiece(int file, int rank, PieceType type, PieceColor color)
        {
            var square = new Square(file, rank);
            _board[square.ToIndex()] = new ChessPiece { Type = type, Color = color };
        }

        public ChessPiece GetPieceAt(Square square)
        {
            if (!square.IsValid) return new ChessPiece();
            return _board[square.ToIndex()];
        }

        public bool TryMove(Square from, Square to)
        {
            if (!IsLegalMove(from, to)) return false;

            var piece = GetPieceAt(from);
            var captured = GetPieceAt(to);
            var move = new Move(from, to, captured);

            // 更新走棋方用时
            UpdateTimeElapsed();

            // 更新 60 回合自然限着计数器
            UpdateHalfMoveCounter(piece, captured);

            // 记录被吃的棋子
            if (!captured.IsEmpty)
            {
                if (captured.Color == PieceColor.Red)
                {
                    _capturedRedPieces.Add(captured);
                }
                else
                {
                    _capturedBlackPieces.Add(captured);
                }
            }

            // 执行移动
            _board[to.ToIndex()] = piece;
            _board[from.ToIndex()] = new ChessPiece();
            
            // 更新将/帅位置缓存
            if (piece.Type == PieceType.King)
            {
                if (piece.Color == PieceColor.Red)
                {
                    _redKingSquare = to;
                }
                else
                {
                    _blackKingSquare = to;
                }
            }

            _moveHistory.Add(move);
            _sideToMove = _sideToMove == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            _selectedSquare = null;
            _legalMoves.Clear();

            // 更新最后走棋时间
            _lastMoveTime = DateTime.Now;
            _isRedThinking = _sideToMove == PieceColor.Red;

            // 记录当前局面到历史（用于三次重复检测）
            RecordPosition();
            
            // 记录棋盘状态（用于复盘）
            RecordBoardState();

            // 检查游戏状态
            CheckGameState();

            OnBoardChanged(move);
            return true;
        }

        public bool Undo()
        {
            if (_moveHistory.Count == 0) return false;

            var lastMove = _moveHistory[^1];
            _moveHistory.RemoveAt(_moveHistory.Count - 1);

            var piece = GetPieceAt(lastMove.To);
            _board[lastMove.From.ToIndex()] = piece;
            _board[lastMove.To.ToIndex()] = lastMove.Captured ?? new ChessPiece();
            
            // 恢复将/帅位置缓存
            if (piece.Type == PieceType.King)
            {
                if (piece.Color == PieceColor.Red)
                {
                    _redKingSquare = lastMove.From;
                }
                else
                {
                    _blackKingSquare = lastMove.From;
                }
            }

            _sideToMove = _sideToMove == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            _gameState = GameState.Ongoing;
            _selectedSquare = null;
            _legalMoves.Clear();
            _lastMoveTime = DateTime.Now;
            _isRedThinking = _sideToMove == PieceColor.Red;

            // 回滚追踪字段
            if (_positionHistory.Count > 0)
            {
                _positionHistory.RemoveAt(_positionHistory.Count - 1);
            }
            if (_checkHistory.Count > 0)
            {
                _checkHistory.RemoveAt(_checkHistory.Count - 1);
            }

            // 重新计算半回合计数器（简化处理：减 1 或重置为 0）
            if (_halfMovesSinceLastCaptureOrPawnMove > 0)
            {
                _halfMovesSinceLastCaptureOrPawnMove--;
            }

            OnBoardChanged(lastMove);
            return true;
        }

        public void SelectSquare(Square square)
        {
            if (!square.IsValid)
            {
                _selectedSquare = null;
                _legalMoves.Clear();
                return;
            }

            var piece = GetPieceAt(square);

            if (_selectedSquare.HasValue && _selectedSquare == square)
            {
                // 取消选择
                _selectedSquare = null;
                _legalMoves.Clear();
            }
            else if (piece.Color == _sideToMove)
            {
                // 选择己方棋子
                _selectedSquare = square;
                UpdateLegalMoves();
            }
            else if (_selectedSquare.HasValue)
            {
                // 尝试走棋
                if (TryMove(_selectedSquare.Value, square))
                {
                    return;
                }
            }

            OnBoardChanged(null);
        }

        private void UpdateLegalMoves()
        {
            _legalMoves.Clear();

            if (!_selectedSquare.HasValue) return;

            var piece = GetPieceAt(_selectedSquare.Value);
            if (piece.Color != _sideToMove) return;

            // 生成所有可能的目标位置
            for (int rank = 1; rank <= ChessConstants.TotalRanks; rank++)
            {
                for (int file = 1; file <= ChessConstants.TotalFiles; file++)
                {
                    var to = new Square(file, rank);
                    if (IsLegalMove(_selectedSquare.Value, to))
                    {
                        _legalMoves.Add(to);
                    }
                }
            }
        }

        public bool IsLegalMove(Square from, Square to)
        {
            if (!from.IsValid || !to.IsValid) return false;

            var piece = GetPieceAt(from);
            var target = GetPieceAt(to);

            // 不能吃自己的棋
            if (target.Color == piece.Color) return false;

            // 检查基础走法规则
            bool isBasicLegal = piece.Type switch
            {
                PieceType.Pawn => IsPawnMoveLegal(from, to, piece.Color),
                PieceType.Knight => IsKnightMoveLegal(from, to),
                PieceType.Bishop => IsBishopMoveLegal(from, to, piece.Color),
                PieceType.Rook => IsRookMoveLegal(from, to),
                PieceType.Cannon => IsCannonMoveLegal(from, to),
                PieceType.Advisor => IsAdvisorMoveLegal(from, to, piece.Color),
                PieceType.King => IsKingMoveLegal(from, to, piece.Color),
                _ => false
            };

            if (!isBasicLegal) return false;

            // 送将检测：走棋后不能导致己方被将军
            if (WouldBeInCheck(from, to, piece.Color)) return false;

            // 对脸将检测：走棋后不能导致两将对面
            if (WouldCauseFacingKings(from, to, piece.Color)) return false;

            return true;
        }

        #region 走棋规则验证

        /// <summary>
        /// 检查兵的走法是否合法（调用 CanPawnAttack 避免重复代码）
        /// </summary>
        private bool IsPawnMoveLegal(Square from, Square to, PieceColor color)
        {
            return CanPawnAttack(from, to, color);
        }

        private bool IsKnightMoveLegal(Square from, Square to)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            // 马走日字
            if (Math.Abs(df) + Math.Abs(dr) != 3) return false;
            if (Math.Abs(df) == 0 || Math.Abs(dr) == 0) return false;

            // 检查马腿
            int legF = from.File + (Math.Abs(df) == 2 ? df / 2 : 0);
            int legR = from.Rank + (Math.Abs(dr) == 2 ? dr / 2 : 0);

            return GetPieceAt(new Square(legF, legR)).IsEmpty;
        }

        private bool IsBishopMoveLegal(Square from, Square to, PieceColor color)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            // 象走田字
            if (Math.Abs(df) != 2 || Math.Abs(dr) != 2) return false;

            // 象不能过河
            if (color == PieceColor.Red && to.Rank <= 5) return false;
            if (color == PieceColor.Black && to.Rank >= 6) return false;

            // 检查象眼
            int eyeF = from.File + df / 2;
            int eyeR = from.Rank + dr / 2;

            return GetPieceAt(new Square(eyeF, eyeR)).IsEmpty;
        }

        private bool IsRookMoveLegal(Square from, Square to)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            // 车走直线
            if (df != 0 && dr != 0) return false;

            // 检查路径
            int stepF = Math.Sign(df);
            int stepR = Math.Sign(dr);

            int f = from.File + stepF;
            int r = from.Rank + stepR;

            while (f != to.File || r != to.Rank)
            {
                if (!GetPieceAt(new Square(f, r)).IsEmpty) return false;
                f += stepF;
                r += stepR;
            }

            return true;
        }

        private bool IsCannonMoveLegal(Square from, Square to)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            // 炮走直线
            if (df != 0 && dr != 0) return false;

            // 计算中间棋子数
            int stepF = Math.Sign(df);
            int stepR = Math.Sign(dr);

            int f = from.File + stepF;
            int r = from.Rank + stepR;
            int count = 0;

            while (f != to.File || r != to.Rank)
            {
                if (!GetPieceAt(new Square(f, r)).IsEmpty) count++;
                f += stepF;
                r += stepR;
            }

            var target = GetPieceAt(to);
            if (target.IsEmpty)
            {
                // 不吃子：中间不能有棋子
                return count == 0;
            }
            else
            {
                // 吃子：中间必须有一个棋子（翻山）
                return count == 1;
            }
        }

        private bool IsAdvisorMoveLegal(Square from, Square to, PieceColor color)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            // 士走斜线一格
            if (Math.Abs(df) != 1 || Math.Abs(dr) != 1) return false;

            // 必须在九宫格内
            return IsInPalace(to, color);
        }

        private bool IsKingMoveLegal(Square from, Square to, PieceColor color)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            // 将帅走直线一格
            if (Math.Abs(df) + Math.Abs(dr) != 1) return false;

            // 必须在九宫格内
            return IsInPalace(to, color);
        }

        private bool IsInPalace(Square square, PieceColor color)
        {
            if (square.File < 4 || square.File > 6) return false;
            if (color == PieceColor.Red)
            {
                return square.Rank >= 8 && square.Rank <= 10;
            }
            else
            {
                return square.Rank >= 1 && square.Rank <= 3;
            }
        }

        #endregion

        #region 将军与将死检测

        /// <summary>
        /// 检查指定方是否被将军
        /// </summary>
        public bool IsInCheck(PieceColor color)
        {
            // 找到该方的将/帅位置
            var kingSquare = FindKing(color);
            if (!kingSquare.HasValue) return false;

            // 检查对方所有棋子是否能攻击到将/帅
            var opponentColor = color == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            return CanAttackSquare(kingSquare.Value, opponentColor);
        }

        /// <summary>
        /// 查找指定方的将/帅位置（使用缓存优化）
        /// </summary>
        private Square? FindKing(PieceColor color)
        {
            // 使用缓存值，避免每次遍历棋盘
            if (color == PieceColor.Red)
            {
                return _redKingSquare;
            }
            else
            {
                return _blackKingSquare;
            }
        }

        /// <summary>
        /// 初始化将/帅位置缓存（在设置棋盘后调用）
        /// </summary>
        private void InitializeKingPositionCache()
        {
            _redKingSquare = FindKingByScan(PieceColor.Red);
            _blackKingSquare = FindKingByScan(PieceColor.Black);
        }

        /// <summary>
        /// 通过遍历棋盘查找将/帅位置（仅在缓存失效时使用）
        /// </summary>
        private Square? FindKingByScan(PieceColor color)
        {
            for (int r = 1; r <= ChessConstants.TotalRanks; r++)
            {
                for (int f = 1; f <= ChessConstants.TotalFiles; f++)
                {
                    var square = new Square(f, r);
                    var piece = GetPieceAt(square);
                    if (piece.Type == PieceType.King && piece.Color == color)
                    {
                        return square;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 检查指定方是否有棋子能攻击到目标位置
        /// </summary>
        private bool CanAttackSquare(Square target, PieceColor attackerColor)
        {
            for (int r = 1; r <= ChessConstants.TotalRanks; r++)
            {
                for (int f = 1; f <= ChessConstants.TotalFiles; f++)
                {
                    var from = new Square(f, r);
                    var piece = GetPieceAt(from);

                    if (piece.Color != attackerColor) continue;

                    // 使用基础走法规则（不考虑送将）检查是否能攻击目标
                    if (CanPieceAttack(from, target, piece))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 检查棋子是否能攻击到目标位置（基础走法，不考虑送将）
        /// </summary>
        private bool CanPieceAttack(Square from, Square to, ChessPiece piece)
        {
            return piece.Type switch
            {
                PieceType.Pawn => CanPawnAttack(from, to, piece.Color),
                PieceType.Knight => CanKnightAttack(from, to),
                PieceType.Bishop => CanBishopAttack(from, to, piece.Color),
                PieceType.Rook => CanRookAttack(from, to),
                PieceType.Cannon => CanCannonAttack(from, to),
                PieceType.Advisor => CanAdvisorAttack(from, to, piece.Color),
                PieceType.King => CanKingAttack(from, to, piece.Color),
                _ => false
            };
        }

        private bool CanPawnAttack(Square from, Square to, PieceColor color)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            if (color == PieceColor.Red)
            {
                if (dr < -1 || dr > 0) return false;
                if (from.Rank <= 5)
                {
                    return (dr == 0 && Math.Abs(df) == 1) || (dr == -1 && df == 0);
                }
                return dr == -1 && df == 0;
            }
            else
            {
                if (dr > 1 || dr < 0) return false;
                if (from.Rank >= 6)
                {
                    return (dr == 0 && Math.Abs(df) == 1) || (dr == 1 && df == 0);
                }
                return dr == 1 && df == 0;
            }
        }

        private bool CanKnightAttack(Square from, Square to)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            if (Math.Abs(df) + Math.Abs(dr) != 3) return false;
            if (Math.Abs(df) == 0 || Math.Abs(dr) == 0) return false;

            int legF = from.File + (Math.Abs(df) == 2 ? df / 2 : 0);
            int legR = from.Rank + (Math.Abs(dr) == 2 ? dr / 2 : 0);
            var legSquare = new Square(legF, legR);

            return legSquare.IsValid && GetPieceAt(legSquare).IsEmpty;
        }

        private bool CanBishopAttack(Square from, Square to, PieceColor color)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            if (Math.Abs(df) != 2 || Math.Abs(dr) != 2) return false;

            if (color == PieceColor.Red && to.Rank <= 5) return false;
            if (color == PieceColor.Black && to.Rank >= 6) return false;

            int eyeF = from.File + df / 2;
            int eyeR = from.Rank + dr / 2;
            var eyeSquare = new Square(eyeF, eyeR);

            return eyeSquare.IsValid && GetPieceAt(eyeSquare).IsEmpty;
        }

        private bool CanRookAttack(Square from, Square to)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            if (df != 0 && dr != 0) return false;

            int stepF = Math.Sign(df);
            int stepR = Math.Sign(dr);

            int f = from.File + stepF;
            int r = from.Rank + stepR;

            while (f != to.File || r != to.Rank)
            {
                var square = new Square(f, r);
                if (!square.IsValid || !GetPieceAt(square).IsEmpty) return false;
                f += stepF;
                r += stepR;
            }

            return true;
        }

        private bool CanCannonAttack(Square from, Square to)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            if (df != 0 && dr != 0) return false;

            int stepF = Math.Sign(df);
            int stepR = Math.Sign(dr);

            int f = from.File + stepF;
            int r = from.Rank + stepR;
            int count = 0;

            while (f != to.File || r != to.Rank)
            {
                var square = new Square(f, r);
                if (!square.IsValid)
                {
                    return false;
                }
                if (!GetPieceAt(square).IsEmpty) count++;
                f += stepF;
                r += stepR;
            }

            // 炮吃子需要翻山
            return count == 1;
        }

        private bool CanAdvisorAttack(Square from, Square to, PieceColor color)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            if (Math.Abs(df) != 1 || Math.Abs(dr) != 1) return false;

            return IsInPalace(to, color);
        }

        private bool CanKingAttack(Square from, Square to, PieceColor color)
        {
            int df = to.File - from.File;
            int dr = to.Rank - from.Rank;

            if (Math.Abs(df) + Math.Abs(dr) != 1) return false;

            return IsInPalace(to, color);
        }

        /// <summary>
        /// 检查是否被将死
        /// </summary>
        public bool IsCheckmate(PieceColor color)
        {
            // 如果没被将军，则不是将死
            if (!IsInCheck(color)) return false;

            // 检查是否有任何合法走法可以解除将军
            return !HasLegalMove(color);
        }

        /// <summary>
        /// 检查是否被困毙（无子可走但未被将军）
        /// </summary>
        public bool IsStalemate(PieceColor color)
        {
            // 如果被将军，则不是困毙
            if (IsInCheck(color)) return false;

            // 检查是否有任何合法走法
            return !HasLegalMove(color);
        }

        /// <summary>
        /// 检查指定方是否有合法走法（不送将的走法）
        /// </summary>
        private bool HasLegalMove(PieceColor color)
        {
            for (int r = 1; r <= ChessConstants.TotalRanks; r++)
            {
                for (int f = 1; f <= ChessConstants.TotalFiles; f++)
                {
                    var from = new Square(f, r);
                    var piece = GetPieceAt(from);

                    if (piece.Color != color) continue;

                    // 检查该棋子的所有可能目标位置
                    for (int tr = 1; tr <= ChessConstants.TotalRanks; tr++)
                    {
                        for (int tf = 1; tf <= ChessConstants.TotalFiles; tf++)
                        {
                            var to = new Square(tf, tr);
                            if (IsBasicLegalMove(from, to) && !WouldBeInCheck(from, to, color))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 基础走法规则检测（不含送将检测）
        /// </summary>
        private bool IsBasicLegalMove(Square from, Square to)
        {
            if (!from.IsValid || !to.IsValid) return false;

            var piece = GetPieceAt(from);
            var target = GetPieceAt(to);

            if (target.Color == piece.Color) return false;

            return piece.Type switch
            {
                PieceType.Pawn => IsPawnMoveLegal(from, to, piece.Color),
                PieceType.Knight => IsKnightMoveLegal(from, to),
                PieceType.Bishop => IsBishopMoveLegal(from, to, piece.Color),
                PieceType.Rook => IsRookMoveLegal(from, to),
                PieceType.Cannon => IsCannonMoveLegal(from, to),
                PieceType.Advisor => IsAdvisorMoveLegal(from, to, piece.Color),
                PieceType.King => IsKingMoveLegal(from, to, piece.Color),
                _ => false
            };
        }

        /// <summary>
        /// 检查走棋后是否导致己方被将军（送将检测）
        /// </summary>
        private bool WouldBeInCheck(Square from, Square to, PieceColor color)
        {
            // 模拟走棋
            var piece = GetPieceAt(from);
            var captured = GetPieceAt(to);

            _board[to.ToIndex()] = piece;
            _board[from.ToIndex()] = new ChessPiece();

            // 检查是否被将军
            bool inCheck = IsInCheck(color);

            // 恢复棋盘
            _board[from.ToIndex()] = piece;
            _board[to.ToIndex()] = captured;

            return inCheck;
        }

        /// <summary>
        /// 检查走棋后是否会导致两将对面（对脸将）
        /// </summary>
        private bool WouldCauseFacingKings(Square from, Square to, PieceColor color)
        {
            // 模拟走棋
            var piece = GetPieceAt(from);
            var captured = GetPieceAt(to);

            _board[to.ToIndex()] = piece;
            _board[from.ToIndex()] = new ChessPiece();

            // 检查是否对脸
            bool facing = IsFacingKings();

            // 恢复棋盘
            _board[from.ToIndex()] = piece;
            _board[to.ToIndex()] = captured;

            return facing;
        }

        /// <summary>
        /// 检查是否对脸将军（两个将/帅在同一列上且中间无棋子）
        /// </summary>
        private bool IsFacingKings()
        {
            var redKing = FindKing(PieceColor.Red);
            var blackKing = FindKing(PieceColor.Black);

            if (!redKing.HasValue || !blackKing.HasValue) return false;

            // 必须在同一列
            if (redKing.Value.File != blackKing.Value.File) return false;

            // 检查中间是否有棋子
            int file = redKing.Value.File;
            int minRank = Math.Min(redKing.Value.Rank, blackKing.Value.Rank);
            int maxRank = Math.Max(redKing.Value.Rank, blackKing.Value.Rank);

            for (int r = minRank + 1; r < maxRank; r++)
            {
                if (!GetPieceAt(new Square(file, r)).IsEmpty)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        private void CheckGameState()
        {
            WinReason winReason = WinReason.None;
            
            // 检查当前走棋方是否被将军，触发将军事件
            bool isInCheck = IsInCheck(_sideToMove);
            var kingSquare = FindKing(_sideToMove);
            OnCheckStateChanged(isInCheck, _sideToMove, kingSquare);

            // 记录将军历史（用于长将检测）
            RecordCheckHistory(isInCheck);

            // 检查 60 回合自然限着
            if (IsSixtyMoveRule())
            {
                _gameState = GameState.Draw;
                winReason = WinReason.SixtyMoveRule;
            }
            // 检查三次重复局面
            else if (IsThreefoldRepetition())
            {
                _gameState = GameState.Draw;
                winReason = WinReason.ThreefoldRepetition;
            }
            // 检查长将（连续将军超过上限）
            else if (IsLongCheck())
            {
                _gameState = GameState.Draw;
                winReason = WinReason.LongCheck;
            }
            // 检查当前走棋方是否被将死
            else if (IsCheckmate(_sideToMove))
            {
                _gameState = _sideToMove == PieceColor.Red ? GameState.BlackWin : GameState.RedWin;
                winReason = WinReason.Checkmate;
            }
            // 检查是否困毙
            else if (IsStalemate(_sideToMove))
            {
                _gameState = GameState.Draw;
                winReason = WinReason.Stalemate;
            }
            else
            {
                _gameState = GameState.Ongoing;
            }

            OnGameStateChanged();

            // 触发游戏结束事件
            if (_gameState != GameState.Ongoing)
            {
                UpdateTimeElapsed();
                OnGameEnded(winReason);
            }
        }

        /// <summary>
        /// 更新走棋方用时
        /// </summary>
        private void UpdateTimeElapsed()
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastMoveTime).TotalSeconds;
            
            if (_isRedThinking)
            {
                _redTimeElapsed += elapsed;
            }
            else
            {
                _blackTimeElapsed += elapsed;
            }
        }

        #region 60 回合自然限着检测

        /// <summary>
        /// 更新半回合计数器（用于 60 回合自然限着检测）
        /// </summary>
        private void UpdateHalfMoveCounter(ChessPiece movedPiece, ChessPiece captured)
        {
            // 如果发生吃子或兵/卒移动，重置计数器
            if (!captured.IsEmpty || movedPiece.Type == PieceType.Pawn)
            {
                _halfMovesSinceLastCaptureOrPawnMove = 0;
            }
            else
            {
                _halfMovesSinceLastCaptureOrPawnMove++;
            }
        }

        /// <summary>
        /// 检查是否达到 60 回合自然限着（120 个半回合）
        /// </summary>
        private bool IsSixtyMoveRule()
        {
            // 根据中国象棋规则，60 回合（120 个半回合）无吃子、无兵卒移动判和
            return _halfMovesSinceLastCaptureOrPawnMove >= 120;
        }

        /// <summary>
        /// 获取当前半回合计数（用于 UI 显示）
        /// </summary>
        public int GetHalfMoveCount() => _halfMovesSinceLastCaptureOrPawnMove;

        #endregion

        #region 三次重复局面检测

        /// <summary>
        /// 记录当前局面到历史
        /// </summary>
        private void RecordPosition()
        {
            // 使用 FEN 字符串作为局面标识（不包含走棋方信息，因为走棋方变化不算重复）
            var position = GetPositionKey();
            _positionHistory.Add(position);
        }

        /// <summary>
        /// 获取当前局面的唯一标识（简化版 FEN）
        /// </summary>
        private string GetPositionKey()
        {
            var sb = new StringBuilder();
            
            // 记录棋盘状态
            for (int r = 1; r <= ChessConstants.TotalRanks; r++)
            {
                for (int f = 1; f <= ChessConstants.TotalFiles; f++)
                {
                    var piece = GetPieceAt(new Square(f, r));
                    if (piece.IsEmpty)
                    {
                        sb.Append('.');
                    }
                    else
                    {
                        char c = piece.Type switch
                        {
                            PieceType.King => piece.Color == PieceColor.Red ? 'K' : 'k',
                            PieceType.Advisor => piece.Color == PieceColor.Red ? 'A' : 'a',
                            PieceType.Bishop => piece.Color == PieceColor.Red ? 'B' : 'b',
                            PieceType.Knight => piece.Color == PieceColor.Red ? 'N' : 'n',
                            PieceType.Rook => piece.Color == PieceColor.Red ? 'R' : 'r',
                            PieceType.Cannon => piece.Color == PieceColor.Red ? 'C' : 'c',
                            PieceType.Pawn => piece.Color == PieceColor.Red ? 'P' : 'p',
                            _ => '.'
                        };
                        sb.Append(c);
                    }
                }
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// 检查是否出现三次重复局面
        /// </summary>
        private bool IsThreefoldRepetition()
        {
            if (_positionHistory.Count < 6) return false;

            var currentPosition = _positionHistory[^1];
            int count = _positionHistory.Count(p => p == currentPosition);
            
            return count >= 3;
        }

        /// <summary>
        /// 获取当前局面的重复次数
        /// </summary>
        public int GetPositionRepetitionCount()
        {
            if (_positionHistory.Count == 0) return 0;
            
            var currentPosition = _positionHistory[^1];
            return _positionHistory.Count(p => p == currentPosition);
        }

        #endregion

        #region 长将检测

        /// <summary>
        /// 记录将军历史
        /// </summary>
        private void RecordCheckHistory(bool isInCheck)
        {
            // 记录当前走棋方的将军状态
            _checkHistory.Add((_sideToMove, isInCheck));
            
            // 限制历史记录长度，避免内存无限增长
            if (_checkHistory.Count > 100)
            {
                _checkHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 检查是否出现长将（一方连续将军超过限制次数）
        /// </summary>
        private bool IsLongCheck()
        {
            // 检查最近的历史中是否存在长将情况
            // 长将的定义：一方连续将军，且对方每次只能应将，形成循环
            if (_checkHistory.Count < MaxCheckRepetition * 2) return false;

            int consecutiveChecks = 0;
            
            // 从最近的历史往回检查
            for (int i = _checkHistory.Count - 1; i >= 0; i--)
            {
                var (color, isCheck) = _checkHistory[i];
                
                if (isCheck)
                {
                    consecutiveChecks++;
                    
                    // 如果连续将军次数超过限制，判定为长将
                    if (consecutiveChecks >= MaxCheckRepetition * 2) // 每个完整回合包含两个半回合
                    {
                        return true;
                    }
                }
                else
                {
                    // 如果将军中断，重置计数
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查当前是否处于连续将军状态
        /// </summary>
        public bool IsInConsecutiveCheck()
        {
            if (_checkHistory.Count < 2) return false;
            
            // 检查最近的两个状态是否都是将军
            return _checkHistory[^1].IsCheck && _checkHistory[^2].IsCheck;
        }

        #endregion

        /// <summary>
        /// 获取当前红方用时（秒）
        /// </summary>
        public double GetRedTimeElapsed()
        {
            var elapsed = _redTimeElapsed;
            if (_isRedThinking && _gameState == GameState.Ongoing)
            {
                elapsed += (DateTime.Now - _lastMoveTime).TotalSeconds;
            }
            return elapsed;
        }

        /// <summary>
        /// 获取当前黑方用时（秒）
        /// </summary>
        public double GetBlackTimeElapsed()
        {
            var elapsed = _blackTimeElapsed;
            if (!_isRedThinking && _gameState == GameState.Ongoing)
            {
                elapsed += (DateTime.Now - _lastMoveTime).TotalSeconds;
            }
            return elapsed;
        }

        /// <summary>
        /// 触发游戏结束事件
        /// </summary>
        private void OnGameEnded(WinReason reason)
        {
            GameEnded?.Invoke(this, new GameEndedEventArgs
            {
                Result = _gameState,
                Reason = reason,
                RedTimeSeconds = (int)_redTimeElapsed,
                BlackTimeSeconds = (int)_blackTimeElapsed,
                TotalMoves = _moveHistory.Count,
                WinnerColor = _gameState switch
                {
                    GameState.RedWin => PieceColor.Red,
                    GameState.BlackWin => PieceColor.Black,
                    _ => PieceColor.None
                }
            });
        }

        public bool LoadFromFEN(string fen)
        {
            // 先验证 FEN 格式
            if (!IsValidFEN(fen))
            {
                LogCallback?.Invoke($"[LoadFromFEN] FEN 格式验证失败：{fen}");
                return false;
            }

            try
            {
                // 清空棋盘
                Array.Fill(_board, new ChessPiece());
                _moveHistory.Clear();
                _selectedSquare = null;
                _legalMoves.Clear();

                // 解析 FEN 字符串
                // 格式: 棋盘布局 走棋方
                // 例如: rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w
                var parts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // 解析棋盘布局
                string boardPart = parts[0];
                int rank = 10; // 从红方底线开始
                int file = 1;

                foreach (char c in boardPart)
                {
                    if (c == '/')
                    {
                        rank--;
                        file = 1;
                    }
                    else if (char.IsDigit(c))
                    {
                        int emptyCount = c - '0';
                        // 边界检查：确保 file 不会超出范围
                        if (file + emptyCount > 10)
                        {
                            LogCallback?.Invoke($"[LoadFromFEN] 边界溢出：file={file}, emptyCount={emptyCount}");
                            return false;
                        }
                        file += emptyCount;
                    }
                    else
                    {
                        var piece = ParsePieceFromFEN(c);
                        
                        // 边界检查：确保 file 和 rank 在有效范围内
                        if (file < 1 || file > 9)
                        {
                            LogCallback?.Invoke($"[LoadFromFEN] file 越界：file={file}");
                            return false;
                        }
                        if (rank < 1 || rank > 10)
                        {
                            LogCallback?.Invoke($"[LoadFromFEN] rank 越界：rank={rank}");
                            return false;
                        }

                        if (!piece.IsEmpty)
                        {
                            int index = new Square(file, rank).ToIndex();
                            if (index < 0 || index >= 90)
                            {
                                LogCallback?.Invoke($"[LoadFromFEN] 索引越界：file={file}, rank={rank}, index={index}");
                                return false;
                            }
                            _board[index] = piece;
                        }
                        file++;
                    }
                }

                // 解析走棋方
                if (parts.Length > 1)
                {
                    _sideToMove = parts[1].ToLower() == "w" ? PieceColor.Red : PieceColor.Black;
                }
                else
                {
                    _sideToMove = PieceColor.Red;
                }

                // 初始化将/帅位置缓存
                _redKingSquare = null;
                _blackKingSquare = null;
                InitializeKingPositionCache();

                _gameState = GameState.Ongoing;
                OnBoardChanged(null);
                LogCallback?.Invoke($"[LoadFromFEN] 成功加载 FEN，走棋方：{_sideToMove}");
                return true;
            }
            catch (Exception ex)
            {
                // 记录异常信息，而不是静默吞掉
                LogCallback?.Invoke($"[LoadFromFEN] 解析异常：{ex.Message}");
                Debug.WriteLine($"[GameService.LoadFromFEN] 异常：{ex}");
                return false;
            }
        }

        /// <summary>
        /// 验证 FEN 字符串格式是否合法
        /// </summary>
        /// <param name="fen">要验证的 FEN 字符串</param>
        /// <returns>如果 FEN 格式合法返回 true，否则返回 false</returns>
        private bool IsValidFEN(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen))
            {
                LogCallback?.Invoke("[FEN 验证失败] FEN 字符串为空");
                return false;
            }

            var parts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
            {
                LogCallback?.Invoke("[FEN 验证失败] FEN 缺少棋盘布局部分");
                return false;
            }

            string boardPart = parts[0];
            
            // 检查是否包含合法的棋子字符或数字
            string validChars = "rnbaKCPrnpakcb123456789/";
            foreach (char c in boardPart)
            {
                if (!validChars.Contains(c))
                {
                    LogCallback?.Invoke($"[FEN 验证失败] 发现非法字符：'{c}'");
                    return false;
                }
            }

            // 检查行数
            string[] ranks = boardPart.Split('/');
            if (ranks.Length != ChessConstants.TotalRanks)
            {
                LogCallback?.Invoke($"[FEN 验证失败] 行数不正确，应为 {ChessConstants.TotalRanks} 行，实际为{ranks.Length}行");
                return false;
            }

            // 检查每行的列数是否为 TotalFiles
            for (int i = 0; i < ranks.Length; i++)
            {
                int fileCount = 0;
                string rank = ranks[i];
                
                foreach (char c in rank)
                {
                    if (char.IsDigit(c))
                    {
                        int empty = c - '0';
                        if (empty < 1 || empty > ChessConstants.TotalFiles)
                        {
                            LogCallback?.Invoke($"[FEN 验证失败] 第{i + 1}行数字{empty}超出范围 (1-{ChessConstants.TotalFiles})");
                            return false;
                        }
                        fileCount += empty;
                    }
                    else
                    {
                        fileCount++;
                    }
                }

                if (fileCount != ChessConstants.TotalFiles)
                {
                    LogCallback?.Invoke($"[FEN 验证失败] 第{i + 1}行位置数不正确，应为 {ChessConstants.TotalFiles}，实际为{fileCount}");
                    return false;
                }
            }

            // 验证走棋方（如果存在）
            if (parts.Length > 1)
            {
                string sidePart = parts[1].ToLower();
                if (sidePart != "w" && sidePart != "b")
                {
                    LogCallback?.Invoke($"[FEN 验证失败] 走棋方标识非法，应为'w'或'b'，实际为'{parts[1]}'");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 从 FEN 字符解析棋子
        /// </summary>
        private ChessPiece ParsePieceFromFEN(char c)
        {
            var piece = new ChessPiece();

            // 大写字母为红方，小写为黑方
            piece.Color = char.IsUpper(c) ? PieceColor.Red : PieceColor.Black;

            char lower = char.ToLower(c);
            piece.Type = lower switch
            {
                'k' => PieceType.King,
                'a' => PieceType.Advisor,
                'b' => PieceType.Bishop,
                'n' => PieceType.Knight,
                'r' => PieceType.Rook,
                'c' => PieceType.Cannon,
                'p' => PieceType.Pawn,
                _ => PieceType.None
            };

            return piece;
        }

        public string ToFEN()
        {
            var sb = new StringBuilder();

            for (int r = 1; r <= ChessConstants.TotalRanks; r++)
            {
                int empty = 0;
                for (int f = 1; f <= ChessConstants.TotalFiles; f++)
                {
                    var piece = GetPieceAt(new Square(f, r));
                    if (piece.IsEmpty)
                    {
                        empty++;
                    }
                    else
                    {
                        if (empty > 0)
                        {
                            sb.Append(empty);
                            empty = 0;
                        }

                        char c = piece.Type switch
                        {
                            PieceType.King => piece.Color == PieceColor.Red ? 'K' : 'k',
                            PieceType.Advisor => piece.Color == PieceColor.Red ? 'A' : 'a',
                            PieceType.Bishop => piece.Color == PieceColor.Red ? 'B' : 'b',
                            PieceType.Knight => piece.Color == PieceColor.Red ? 'N' : 'n',
                            PieceType.Rook => piece.Color == PieceColor.Red ? 'R' : 'r',
                            PieceType.Cannon => piece.Color == PieceColor.Red ? 'C' : 'c',
                            PieceType.Pawn => piece.Color == PieceColor.Red ? 'P' : 'p',
                            _ => ' '
                        };

                        sb.Append(c);
                    }
                }
                if (empty > 0) sb.Append(empty);
                if (r < ChessConstants.TotalRanks) sb.Append('/');
            }

            sb.Append(' ');
            sb.Append(_sideToMove == PieceColor.Red ? 'w' : 'b');

            return sb.ToString();
        }

        public IReadOnlyList<Move> GetLegalMoves()
        {
            var moves = new List<Move>();

            for (int r = 1; r <= ChessConstants.TotalRanks; r++)
            {
                for (int f = 1; f <= ChessConstants.TotalFiles; f++)
                {
                    var from = new Square(f, r);
                    var piece = GetPieceAt(from);

                    if (piece.Color != _sideToMove) continue;

                    for (int tr = 1; tr <= ChessConstants.TotalRanks; tr++)
                    {
                        for (int tf = 1; tf <= ChessConstants.TotalFiles; tf++)
                        {
                            var to = new Square(tf, tr);
                            if (IsLegalMove(from, to))
                            {
                                moves.Add(new Move(from, to));
                            }
                        }
                    }
                }
            }

            return moves.AsReadOnly();
        }

        #region 和棋申请功能

        /// <summary>
        /// 是否有待处理的和棋申请
        /// </summary>
        public bool IsDrawOfferPending => _drawOfferPending;

        /// <summary>
        /// 申请和棋的一方
        /// </summary>
        public PieceColor? DrawOfferSide => _drawOfferSide;

        /// <summary>
        /// 申请和棋
        /// </summary>
        /// <returns>和棋申请是否成功发起</returns>
        public bool OfferDraw()
        {
            // 游戏已结束，无法申请和棋
            if (_gameState != GameState.Ongoing)
            {
                LogCallback?.Invoke("[OfferDraw] 游戏已结束，无法申请和棋");
                return false;
            }

            // 已有待处理的和棋申请
            if (_drawOfferPending)
            {
                LogCallback?.Invoke("[OfferDraw] 已有待处理的和棋申请");
                return false;
            }

            // 设置和棋申请状态
            _drawOfferPending = true;
            _drawOfferSide = _sideToMove;

            // 触发和棋申请事件
            OnDrawOffered();

            LogCallback?.Invoke($"[OfferDraw] {_drawOfferSide}方申请和棋");
            return true;
        }

        /// <summary>
        /// 回应和棋申请
        /// </summary>
        /// <param name="response">回应结果</param>
        public void RespondToDrawOffer(DrawResponse response)
        {
            // 没有待处理的和棋申请
            if (!_drawOfferPending || _drawOfferSide == null)
            {
                LogCallback?.Invoke("[RespondToDrawOffer] 没有待处理的和棋申请");
                return;
            }

            if (response == DrawResponse.Accept)
            {
                // 同意和棋，设置游戏状态为和棋
                _gameState = GameState.Draw;
                LogCallback?.Invoke("[RespondToDrawOffer] 双方同意和棋");
                OnGameStateChanged();
                
                // 触发游戏结束事件
                UpdateTimeElapsed();
                OnGameEnded(WinReason.Resignation); // 使用和棋原因
            }
            else
            {
                // 拒绝和棋，继续游戏
                LogCallback?.Invoke($"[RespondToDrawOffer] {(_drawOfferSide == PieceColor.Red ? "黑" : "红")}方拒绝和棋申请");
            }

            // 重置和棋申请状态
            _drawOfferPending = false;
            _drawOfferSide = null;
        }

        protected virtual void OnDrawOffered()
        {
            DrawOffered?.Invoke(this, new DrawOfferEventArgs
            {
                OfferingSide = _drawOfferSide!.Value,
                RequiresResponse = true
            });
        }

        #endregion

        protected virtual void OnBoardChanged(Move? lastMove)
        {
            BoardChanged?.Invoke(this, new BoardChangedEventArgs
            {
                LastMove = lastMove,
                Board = (ChessPiece[])_board.Clone()
            });
        }

        protected virtual void OnGameStateChanged()
        {
            GameStateChanged?.Invoke(this, new GameStateChangedEventArgs
            {
                NewState = _gameState,
                SideToMove = _sideToMove
            });
        }

        /// <summary>
        /// 触发将军状态变更事件
        /// </summary>
        /// <param name="isInCheck">是否被将军</param>
        /// <param name="checkedColor">被将军的一方</param>
        /// <param name="kingSquare">将/帅位置</param>
        protected virtual void OnCheckStateChanged(bool isInCheck, PieceColor checkedColor, Square? kingSquare)
        {
            CheckStateChanged?.Invoke(this, new CheckStateChangedEventArgs
            {
                IsInCheck = isInCheck,
                CheckedColor = checkedColor,
                KingSquare = kingSquare
            });
        }

        #region PGN 导出

        /// <summary>
        /// 导出棋谱为 PGN 格式
        /// </summary>
        /// <param name="whitePlayer">红方玩家名称</param>
        /// <param name="blackPlayer">黑方玩家名称</param>
        /// <param name="eventName">事件名称</param>
        /// <param name="site">地点</param>
        /// <returns>PGN 格式字符串</returns>
        public string ExportToPGN(string whitePlayer = "红方", string blackPlayer = "黑方", string eventName = "鲨鱼象棋对局", string site = "本地")
        {
            var sb = new StringBuilder();

            // PGN 头信息
            sb.AppendLine($"[Event \"{eventName}\"]");
            sb.AppendLine($"[Site \"{site}\"]");
            sb.AppendLine($"[Date \"{DateTime.Now:yyyy.MM.dd}\"]");
            sb.AppendLine($"[Round \"1\"]");
            sb.AppendLine($"[White \"{whitePlayer}\"]");
            sb.AppendLine($"[Black \"{blackPlayer}\"]");
            sb.AppendLine($"[Result \"{GetResultString()}\"]");
            sb.AppendLine($"[Game \"Chinese Chess\"]");
            sb.AppendLine($"[FEN \"rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w\"]");
            sb.AppendLine();

            // 走棋记录
            sb.Append(GenerateMoveNotation());
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// 获取结果字符串
        /// </summary>
        private string GetResultString()
        {
            return _gameState switch
            {
                GameState.RedWin => "1-0",
                GameState.BlackWin => "0-1",
                GameState.Draw => "1/2-1/2",
                _ => "*"
            };
        }

        /// <summary>
        /// 生成走棋记录（中文记谱）
        /// </summary>
        private string GenerateMoveNotation()
        {
            if (_moveHistory.Count == 0) return "";

            var sb = new StringBuilder();
            
            // 为了正确生成中文记谱，需要模拟棋盘状态
            // 先创建一个临时棋盘来追踪棋子位置
            var tempBoard = new ChessPiece[ChessConstants.TotalSquares];
            SetupInitialPositionOnBoard(tempBoard);

            for (int i = 0; i < _moveHistory.Count; i++)
            {
                var move = _moveHistory[i];
                var piece = tempBoard[move.From.ToIndex()];
                
                // 每两个走法为一回合
                if (i % 2 == 0)
                {
                    sb.Append($"{(i / 2 + 1)}. ");
                }

                // 生成中文记谱
                string notation = GenerateChineseNotation(move, piece, tempBoard);
                sb.Append(notation);

                // 执行移动（更新临时棋盘）
                tempBoard[move.To.ToIndex()] = piece;
                tempBoard[move.From.ToIndex()] = new ChessPiece();

                // 添加空格分隔
                if (i % 2 == 0)
                {
                    sb.Append(" ");
                }
                else
                {
                    sb.Append("  ");
                }
            }

            // 添加结果
            sb.Append(GetResultString());

            return sb.ToString();
        }

        /// <summary>
        /// 在指定棋盘上设置初始位置
        /// </summary>
        private void SetupInitialPositionOnBoard(ChessPiece[] board)
        {
            Array.Fill(board, new ChessPiece());

            // 黑方棋子（上方，rank 1-5）
            SetPieceOnBoard(board, 1, 1, PieceType.Rook, PieceColor.Black);
            SetPieceOnBoard(board, 9, 1, PieceType.Rook, PieceColor.Black);
            SetPieceOnBoard(board, 2, 1, PieceType.Knight, PieceColor.Black);
            SetPieceOnBoard(board, 8, 1, PieceType.Knight, PieceColor.Black);
            SetPieceOnBoard(board, 3, 1, PieceType.Bishop, PieceColor.Black);
            SetPieceOnBoard(board, 7, 1, PieceType.Bishop, PieceColor.Black);
            SetPieceOnBoard(board, 4, 1, PieceType.Advisor, PieceColor.Black);
            SetPieceOnBoard(board, 6, 1, PieceType.Advisor, PieceColor.Black);
            SetPieceOnBoard(board, 5, 1, PieceType.King, PieceColor.Black);
            SetPieceOnBoard(board, 2, 3, PieceType.Cannon, PieceColor.Black);
            SetPieceOnBoard(board, 8, 3, PieceType.Cannon, PieceColor.Black);

            for (int f = 1; f <= 9; f += 2)
            {
                SetPieceOnBoard(board, f, 4, PieceType.Pawn, PieceColor.Black);
            }

            // 红方棋子（下方，rank 6-10）
            SetPieceOnBoard(board, 1, 10, PieceType.Rook, PieceColor.Red);
            SetPieceOnBoard(board, 9, 10, PieceType.Rook, PieceColor.Red);
            SetPieceOnBoard(board, 2, 10, PieceType.Knight, PieceColor.Red);
            SetPieceOnBoard(board, 8, 10, PieceType.Knight, PieceColor.Red);
            SetPieceOnBoard(board, 3, 10, PieceType.Bishop, PieceColor.Red);
            SetPieceOnBoard(board, 7, 10, PieceType.Bishop, PieceColor.Red);
            SetPieceOnBoard(board, 4, 10, PieceType.Advisor, PieceColor.Red);
            SetPieceOnBoard(board, 6, 10, PieceType.Advisor, PieceColor.Red);
            SetPieceOnBoard(board, 5, 10, PieceType.King, PieceColor.Red);
            SetPieceOnBoard(board, 2, 8, PieceType.Cannon, PieceColor.Red);
            SetPieceOnBoard(board, 8, 8, PieceType.Cannon, PieceColor.Red);

            for (int f = 1; f <= 9; f += 2)
            {
                SetPieceOnBoard(board, f, 7, PieceType.Pawn, PieceColor.Red);
            }
        }

        /// <summary>
        /// 在指定棋盘上设置棋子
        /// </summary>
        private void SetPieceOnBoard(ChessPiece[] board, int file, int rank, PieceType type, PieceColor color)
        {
            var square = new Square(file, rank);
            board[square.ToIndex()] = new ChessPiece { Type = type, Color = color };
        }

        /// <summary>
        /// 生成中文记谱符号
        /// </summary>
        private string GenerateChineseNotation(Move move, ChessPiece piece, ChessPiece[] board)
        {
            if (!move.IsValid || piece.IsEmpty) return move.ToUCI();

            // 红方用中文数字，黑方用阿拉伯数字
            var fileNamesRed = new[] { "九", "八", "七", "六", "五", "四", "三", "二", "一" };
            var fileNamesBlack = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            var distanceNamesRed = new[] { "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            var distanceNamesBlack = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

            string pieceName = piece.ChineseName;
            bool isRed = piece.Color == PieceColor.Red;

            // 检查同一列是否有同名棋子，需要添加"前/后"前缀
            string prefix = GetSameFilePrefix(move, piece, board);

            // 起始路线
            string fromFileStr = isRed
                ? fileNamesRed[move.From.File - 1]
                : fileNamesBlack[move.From.File - 1];

            int df = move.To.File - move.From.File;
            int dr = move.To.Rank - move.From.Rank;

            string direction;
            string distance;

            // 根据棋子类型确定记谱方式
            switch (piece.Type)
            {
                case PieceType.Knight:
                    // 马走日：记"进"或"退"加上终点路线
                    direction = isRed
                        ? (dr < 0 ? "进" : "退")
                        : (dr > 0 ? "进" : "退");
                    distance = isRed
                        ? fileNamesRed[move.To.File - 1]
                        : fileNamesBlack[move.To.File - 1];
                    break;

                case PieceType.Bishop:
                case PieceType.Advisor:
                    // 象、士走斜线：记"进"或"退"加上终点路线
                    direction = isRed
                        ? (dr < 0 ? "进" : "退")
                        : (dr > 0 ? "进" : "退");
                    distance = isRed
                        ? fileNamesRed[move.To.File - 1]
                        : fileNamesBlack[move.To.File - 1];
                    break;

                case PieceType.Rook:
                case PieceType.Cannon:
                    // 车、炮直线移动
                    if (df == 0)
                    {
                        // 直进或直退
                        direction = isRed
                            ? (dr < 0 ? "进" : "退")
                            : (dr > 0 ? "进" : "退");
                        int dist = Math.Abs(dr);
                        distance = isRed
                            ? distanceNamesRed[dist - 1]
                            : distanceNamesBlack[dist - 1];
                    }
                    else
                    {
                        // 平移
                        direction = "平";
                        distance = isRed
                            ? fileNamesRed[move.To.File - 1]
                            : fileNamesBlack[move.To.File - 1];
                    }
                    break;

                case PieceType.Pawn:
                    // 兵/卒
                    if (df == 0)
                    {
                        // 直进
                        direction = "进";
                        int dist = Math.Abs(dr);
                        distance = isRed
                            ? distanceNamesRed[dist - 1]
                            : distanceNamesBlack[dist - 1];
                    }
                    else
                    {
                        // 平移（已过河）
                        direction = "平";
                        distance = isRed
                            ? fileNamesRed[move.To.File - 1]
                            : fileNamesBlack[move.To.File - 1];
                    }
                    break;

                case PieceType.King:
                    // 将/帅
                    if (df == 0)
                    {
                        // 直进或直退
                        direction = isRed
                            ? (dr < 0 ? "进" : "退")
                            : (dr > 0 ? "进" : "退");
                        int dist = Math.Abs(dr);
                        distance = isRed
                            ? distanceNamesRed[dist - 1]
                            : distanceNamesBlack[dist - 1];
                    }
                    else
                    {
                        // 平移
                        direction = "平";
                        distance = isRed
                            ? fileNamesRed[move.To.File - 1]
                            : fileNamesBlack[move.To.File - 1];
                    }
                    break;

                default:
                    return $"{pieceName}{fromFileStr}??";
            }

            // 如果有前缀（前/后），替换起始路线
            if (!string.IsNullOrEmpty(prefix))
            {
                return $"{prefix}{pieceName}{direction}{distance}";
            }

            return $"{pieceName}{fromFileStr}{direction}{distance}";
        }

        /// <summary>
        /// 获取同名棋子在同一列的前/后前缀
        /// </summary>
        private string GetSameFilePrefix(Move move, ChessPiece piece, ChessPiece[] board)
        {
            // 只检查兵/卒和车、炮
            if (piece.Type != PieceType.Pawn && piece.Type != PieceType.Rook && piece.Type != PieceType.Cannon)
            {
                return "";
            }

            // 查找同一列上是否有其他同名棋子
            var samePieces = new List<(Square Square, ChessPiece Piece)>();

            for (int r = 1; r <= ChessConstants.TotalRanks; r++)
            {
                var square = new Square(move.From.File, r);
                var p = board[square.ToIndex()];
                if (p.Type == piece.Type && p.Color == piece.Color)
                {
                    samePieces.Add((square, p));
                }
            }

            // 如果有多个同名棋子在同一列
            if (samePieces.Count > 1)
            {
                // 按位置排序（红方：rank 大的在前；黑方：rank 小的在前）
                if (piece.Color == PieceColor.Red)
                {
                    samePieces.Sort((a, b) => b.Square.Rank.CompareTo(a.Square.Rank));
                }
                else
                {
                    samePieces.Sort((a, b) => a.Square.Rank.CompareTo(b.Square.Rank));
                }

                // 找到当前棋子的索引
                int index = samePieces.FindIndex(s => s.Square.Equals(move.From));

                if (index == 0)
                {
                    return "前";
                }
                else if (index == 1)
                {
                    return "后";
                }
                // 如果有更多，使用三、四等（较少见）
                else if (index == 2)
                {
                    return "三";
                }
                else if (index == 3)
                {
                    return "四";
                }
                else if (index == 4)
                {
                    return "五";
                }
            }

            return "";
        }

        #endregion

        #region 复盘功能

        /// <summary>
        /// 初始化棋盘历史记录（在 NewGame 后调用）
        /// </summary>
        private void InitializeBoardHistory()
        {
            _boardHistory = new List<ChessPiece[]>();
            _sideToMoveHistory = new List<PieceColor>();
            _currentViewIndex = -1;
            
            // 保存初始局面
            _boardHistory!.Add((ChessPiece[])_board.Clone());
            _sideToMoveHistory!.Add(_sideToMove);
        }

        /// <summary>
        /// 记录当前棋盘状态到历史（在每次走棋后调用）
        /// </summary>
        private void RecordBoardState()
        {
            if (_boardHistory == null || _sideToMoveHistory == null) return;
            
            _boardHistory.Add((ChessPiece[])_board.Clone());
            _sideToMoveHistory.Add(_sideToMove);
            _currentViewIndex = _boardHistory.Count - 1;
        }

        /// <summary>
        /// 跳转到指定步数
        /// </summary>
        /// <param name="moveIndex">走棋索引（0-based）</param>
        /// <returns>是否成功跳转</returns>
        public bool GoToMove(int moveIndex)
        {
            if (_boardHistory == null || _boardHistory.Count == 0) return false;
            if (moveIndex < 0 || moveIndex >= _boardHistory.Count) return false;

            // 恢复棋盘状态
            var boardState = _boardHistory[moveIndex];
            Array.Copy(boardState, _board, boardState.Length);
            _sideToMove = _sideToMoveHistory![moveIndex];
            _currentViewIndex = moveIndex;
            _gameState = GameState.Ongoing; // 复盘时不检查游戏状态
            _selectedSquare = null;
            _legalMoves.Clear();

            OnBoardChanged(null);
            return true;
        }

        /// <summary>
        /// 跳转到第一步
        /// </summary>
        /// <returns>是否成功跳转</returns>
        public bool GoToStart()
        {
            return GoToMove(0);
        }

        /// <summary>
        /// 跳转到最后一步
        /// </summary>
        /// <returns>是否成功跳转</returns>
        public bool GoToEnd()
        {
            if (_boardHistory == null || _boardHistory.Count == 0) return false;
            return GoToMove(_boardHistory.Count - 1);
        }

        /// <summary>
        /// 上一步
        /// </summary>
        /// <returns>是否成功跳转</returns>
        public bool GoBack()
        {
            if (_currentViewIndex <= 0) return false;
            return GoToMove(_currentViewIndex - 1);
        }

        /// <summary>
        /// 下一步
        /// </summary>
        /// <returns>是否成功跳转</returns>
        public bool GoForward()
        {
            if (_currentViewIndex < 0 || _currentViewIndex >= _boardHistory.Count - 1) return false;
            return GoToMove(_currentViewIndex + 1);
        }

        /// <summary>
        /// 获取当前复盘位置索引
        /// </summary>
        public int CurrentViewIndex => _currentViewIndex;

        /// <summary>
        /// 获取总步数
        /// </summary>
        public int TotalMoves => _boardHistory?.Count ?? 0;

        /// <summary>
        /// 退出复盘模式，恢复到实际对局状态
        /// </summary>
        public void ExitReviewMode()
        {
            if (_boardHistory == null || _boardHistory.Count == 0) return;
            
            // 恢复到最新局面
            GoToEnd();
            _gameState = GameState.Ongoing;
        }

        #endregion
    }
}
