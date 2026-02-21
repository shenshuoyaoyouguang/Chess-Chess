namespace SharkChess.Models
{
    /// <summary>
    /// 棋子类型枚举
    /// </summary>
    public enum PieceType
    {
        None = 0,
        King = 1,     // 将/帅
        Advisor = 2,  // 士/仕
        Bishop = 3,   // 象/相
        Knight = 4,   // 马
        Rook = 5,     // 车
        Cannon = 6,   // 炮
        Pawn = 7      // 兵/卒
    }

    /// <summary>
    /// 棋子颜色
    /// </summary>
    public enum PieceColor
    {
        /// <summary>
        /// 无颜色（表示空位或无效状态）
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 红方
        /// </summary>
        Red = 1,
        
        /// <summary>
        /// 黑方
        /// </summary>
        Black = 2
    }

    /// <summary>
    /// 棋子模型
    /// </summary>
    public class ChessPiece
    {
        public PieceType Type { get; set; } = PieceType.None;
        public PieceColor Color { get; set; } = PieceColor.None;

        public bool IsEmpty => Type == PieceType.None;

        /// <summary>
        /// 获取棋子中文名称
        /// </summary>
        public string ChineseName => GetChineseName();

        private string GetChineseName()
        {
            return Color switch
            {
                PieceColor.Red => Type switch
                {
                    PieceType.King => "帅",
                    PieceType.Advisor => "仕",
                    PieceType.Bishop => "相",
                    PieceType.Knight => "马",
                    PieceType.Rook => "车",
                    PieceType.Cannon => "炮",
                    PieceType.Pawn => "兵",
                    _ => ""
                },
                PieceColor.Black => Type switch
                {
                    PieceType.King => "将",
                    PieceType.Advisor => "士",
                    PieceType.Bishop => "象",
                    PieceType.Knight => "马",
                    PieceType.Rook => "车",
                    PieceType.Cannon => "炮",
                    PieceType.Pawn => "卒",
                    _ => ""
                },
                _ => ""
            };
        }

        public override string ToString() => $"{Color}{ChineseName}";
    }

    /// <summary>
    /// 棋盘常量定义
    /// </summary>
    public static class ChessConstants
    {
        /// <summary>
        /// 棋盘总行数（纵线，从黑方到红方）
        /// </summary>
        public const int TotalRanks = 10;

        /// <summary>
        /// 棋盘总列数（横线，从右到左）
        /// </summary>
        public const int TotalFiles = 9;

        /// <summary>
        /// 棋盘总格子数
        /// </summary>
        public const int TotalSquares = TotalRanks * TotalFiles;
    }

    /// <summary>
    /// 棋盘坐标 (文件=纵线 1-9, 等级=横线 1-10)
    /// </summary>
    public readonly struct Square : IEquatable<Square>
    {
        public int File { get; }   // 1-9 (红方右下角为 1 路)
        public int Rank { get; }   // 1-10 (黑方底线为 1，红方底线为 10)

        public Square(int file, int rank)
        {
            File = file;
            Rank = rank;
        }

        public bool IsValid => File >= 1 && File <= ChessConstants.TotalFiles && Rank >= 1 && Rank <= ChessConstants.TotalRanks;

        /// <summary>
        /// 转换为数组索引 (0-89)
        /// </summary>
        public int ToIndex() => (Rank - 1) * 9 + (File - 1);

        /// <summary>
        /// 从索引创建
        /// </summary>
        public static Square FromIndex(int index) => new(index % 9 + 1, index / 9 + 1);

        public bool Equals(Square other) => File == other.File && Rank == other.Rank;
        public override bool Equals(object? obj) => obj is Square other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(File, Rank);
        public override string ToString() => $"({File},{Rank})";

        public static bool operator ==(Square left, Square right) => left.Equals(right);
        public static bool operator !=(Square left, Square right) => !left.Equals(right);
    }

    /// <summary>
    /// 走法
    /// </summary>
    public readonly struct Move : IEquatable<Move>
    {
        public Square From { get; }
        public Square To { get; }
        public ChessPiece? Captured { get; }

        public Move(Square from, Square to, ChessPiece? captured = null)
        {
            From = from;
            To = to;
            Captured = captured;
        }

        public bool IsValid => From.IsValid && To.IsValid;

        /// <summary>
        /// 转换为 UCI 格式字符串 (如 "a0a1")
        /// </summary>
        public string ToUCI()
        {
            char fromFile = (char)('a' + From.File - 1);
            char fromRank = (char)('0' + (10 - From.Rank));
            char toFile = (char)('a' + To.File - 1);
            char toRank = (char)('0' + (10 - To.Rank));
            return $"{fromFile}{fromRank}{toFile}{toRank}";
        }

        /// <summary>
        /// 从 UCI 格式解析
        /// </summary>
        public static Move FromUCI(string uci)
        {
            if (uci.Length < 4) return default;

            int fromFile = uci[0] - 'a' + 1;
            int fromRank = 10 - (uci[1] - '0');
            int toFile = uci[2] - 'a' + 1;
            int toRank = 10 - (uci[3] - '0');

            return new Move(new Square(fromFile, fromRank), new Square(toFile, toRank));
        }

        /// <summary>
        /// 转换为中文描述 (如 "炮二平五")
        /// </summary>
        public string ToChinese(ChessPiece piece)
        {
            if (!IsValid || piece.IsEmpty) return "";

            // 红方用中文数字，黑方用阿拉伯数字
            var fileNamesRed = new[] { "九", "八", "七", "六", "五", "四", "三", "二", "一" };
            var fileNamesBlack = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            var distanceNamesRed = new[] { "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            var distanceNamesBlack = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

            string pieceName = piece.ChineseName;
            bool isRed = piece.Color == PieceColor.Red;
            
            // 起始路线（红方从右到左为 1-9，黑方从左到右为 1-9）
            string fromFileStr = isRed 
                ? fileNamesRed[From.File - 1] 
                : fileNamesBlack[From.File - 1];

            int df = To.File - From.File;  // 纵向移动（正=向右）
            int dr = To.Rank - From.Rank;  // 横向移动（正=向下，即向黑方）

            string direction;
            string distance;

            // 根据棋子类型确定记谱方式
            switch (piece.Type)
            {
                case PieceType.Knight:
                    // 马走日：记"进"或"退"加上终点路线
                    direction = isRed 
                        ? (dr < 0 ? "进" : "退")  // 红方向上（rank 减小）为进
                        : (dr > 0 ? "进" : "退"); // 黑方向下（rank 增大）为进
                    distance = isRed 
                        ? fileNamesRed[To.File - 1] 
                        : fileNamesBlack[To.File - 1];
                    break;

                case PieceType.Bishop:
                case PieceType.Advisor:
                    // 象、士走斜线：记"进"或"退"加上终点路线
                    direction = isRed 
                        ? (dr < 0 ? "进" : "退")
                        : (dr > 0 ? "进" : "退");
                    distance = isRed 
                        ? fileNamesRed[To.File - 1] 
                        : fileNamesBlack[To.File - 1];
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
                            ? fileNamesRed[To.File - 1] 
                            : fileNamesBlack[To.File - 1];
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
                            ? fileNamesRed[To.File - 1] 
                            : fileNamesBlack[To.File - 1];
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
                            ? fileNamesRed[To.File - 1] 
                            : fileNamesBlack[To.File - 1];
                    }
                    break;

                default:
                    return "";
            }

            return $"{pieceName}{fromFileStr}{direction}{distance}";
        }

        /// <summary>
        /// 转换为中文描述（包含同路多子区分）
        /// </summary>
        public string ToChinese(ChessPiece piece, int? sameFileIndex = null, bool? isBackward = null)
        {
            // 基础记谱
            string baseNotation = ToChinese(piece);
            
            // 如果有同路多子，添加前缀区分
            if (sameFileIndex.HasValue && sameFileIndex.Value > 0)
            {
                var prefixes = new[] { "前", "后", "三", "四", "五" };
                if (sameFileIndex.Value <= prefixes.Length)
                {
                    // 用前缀替换起始路线
                    // 例如："炮二平五" -> "前炮平五"
                    // 这需要更复杂的逻辑，暂时使用简化版本
                    return $"{prefixes[sameFileIndex.Value - 1]}{baseNotation}";
                }
            }
            
            return baseNotation;
        }

        public bool Equals(Move other) => From.Equals(other.From) && To.Equals(other.To);
        public override bool Equals(object? obj) => obj is Move other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(From, To);
        public override string ToString() => $"{From}->{To}";
    }

    /// <summary>
    /// 游戏状态
    /// </summary>
    public enum GameState
    {
        Ongoing = 0,
        RedWin = 1,
        BlackWin = 2,
        Draw = 3
    }

    /// <summary>
    /// 棋盘主题类型
    /// </summary>
    public enum BoardTheme
    {
        Standard,    // 标准规范版
        Teaching,    // 教学专用版
        ChineseStyle // 国风 UI 设计版
    }

    /// <summary>
    /// 引擎难度等级
    /// </summary>
    public enum EngineDifficulty
    {
        /// <summary>
        /// 入门级（搜索深度 2）
        /// </summary>
        Beginner = 0,

        /// <summary>
        /// 新手级（搜索深度 4）
        /// </summary>
        Novice = 1,

        /// <summary>
        /// 业余级（搜索深度 6）
        /// </summary>
        Amateur = 2,

        /// <summary>
        /// 专业级（搜索深度 8）
        /// </summary>
        Professional = 3,

        /// <summary>
        /// 大师级（搜索深度 10）
        /// </summary>
        Master = 4,

        /// <summary>
        /// 顶级 AI（搜索深度 12+）
        /// </summary>
        Grandmaster = 5
    }

    /// <summary>
    /// 胜利原因
    /// </summary>
    public enum WinReason
    {
        /// <summary>
        /// 无（游戏未结束）
        /// </summary>
        None = 0,

        /// <summary>
        /// 将死
        /// </summary>
        Checkmate = 1,

        /// <summary>
        /// 困毙
        /// </summary>
        Stalemate = 2,

        /// <summary>
        /// 超时
        /// </summary>
        Timeout = 3,

        /// <summary>
        /// 认输
        /// </summary>
        Resignation = 4,

        /// <summary>
        /// 三次重复局面
        /// </summary>
        ThreefoldRepetition = 5,

        /// <summary>
        /// 60 回合自然限着
        /// </summary>
        SixtyMoveRule = 6,

        /// <summary>
        /// 长将
        /// </summary>
        LongCheck = 7,

        /// <summary>
        /// 长捉
        /// </summary>
        LongCapture = 8
    }

    /// <summary>
    /// 游戏结束事件参数
    /// </summary>
    public class GameEndedEventArgs : EventArgs
    {
        /// <summary>
        /// 游戏结果
        /// </summary>
        public GameState Result { get; init; }

        /// <summary>
        /// 获胜原因
        /// </summary>
        public WinReason Reason { get; init; }

        /// <summary>
        /// 红方用时（秒）
        /// </summary>
        public double RedTimeSeconds { get; init; }

        /// <summary>
        /// 黑方用时（秒）
        /// </summary>
        public double BlackTimeSeconds { get; init; }

        /// <summary>
        /// 总步数
        /// </summary>
        public int TotalMoves { get; init; }

        /// <summary>
        /// 获胜方颜色（和棋时为 None）
        /// </summary>
        public PieceColor WinnerColor { get; init; }

        /// <summary>
        /// 获取结果描述文本
        /// </summary>
        public string ResultDescription => Result switch
        {
            GameState.RedWin => "红方胜",
            GameState.BlackWin => "黑方胜",
            GameState.Draw => "和棋",
            _ => "进行中"
        };

        /// <summary>
        /// 获取获胜原因描述文本
        /// </summary>
        public string ReasonDescription => Reason switch
        {
            WinReason.Checkmate => "将死",
            WinReason.Stalemate => "困毙",
            WinReason.Timeout => "超时",
            WinReason.Resignation => "认输",
            WinReason.ThreefoldRepetition => "三次重复",
            WinReason.SixtyMoveRule => "60 回合限着",
            WinReason.LongCheck => "长将",
            WinReason.LongCapture => "长捉",
            _ => ""
        };

        /// <summary>
        /// 获取红方用时格式化字符串
        /// </summary>
        public string RedTimeFormatted => FormatTime(RedTimeSeconds);

        /// <summary>
        /// 获取黑方用时格式化字符串
        /// </summary>
        public string BlackTimeFormatted => FormatTime(BlackTimeSeconds);

        private static string FormatTime(double seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            return timeSpan.Hours > 0 
                ? timeSpan.ToString(@"h\:mm\:ss") 
                : timeSpan.ToString(@"mm\:ss");
        }
    }
}