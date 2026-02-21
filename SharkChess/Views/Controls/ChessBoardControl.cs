using SharkChess.Models;
using SharkChess.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

// 使用别名避免与 ChessPiece 命名空间冲突
using ChessPieceModel = SharkChess.Models.ChessPiece;

namespace SharkChess.Views.Controls
{
    /// <summary>
    /// 中国象棋棋盘控件
    /// 支持 9 路 10 线制式，严格遵循 9:10 宽高比
    /// </summary>
    public class ChessBoardControl : FrameworkElement, IDisposable
    {
        private IGameService? _gameService;
        private IThemeService? _themeService;
        private ThemeConfig _themeConfig;
        private bool _isDisposed;

        // 渲染器
        private ChessBoardRenderer? _boardRenderer;
        private ChessPieceRenderer? _pieceRenderer;

        // 绘图资源
        private readonly Typeface _typeface;
        private readonly Typeface _chineseTypeface;

        #region 命令依赖属性

        /// <summary>
        /// 选择格子命令
        /// </summary>
        public static readonly DependencyProperty SelectSquareCommandProperty =
            DependencyProperty.Register(nameof(SelectSquareCommand), typeof(ICommand),
                typeof(ChessBoardControl), new PropertyMetadata(null));

        public ICommand? SelectSquareCommand
        {
            get => (ICommand?)GetValue(SelectSquareCommandProperty);
            set => SetValue(SelectSquareCommandProperty, value);
        }

        /// <summary>
        /// 走棋命令
        /// </summary>
        public static readonly DependencyProperty MovePieceCommandProperty =
            DependencyProperty.Register(nameof(MovePieceCommand), typeof(ICommand),
                typeof(ChessBoardControl), new PropertyMetadata(null));

        public ICommand? MovePieceCommand
        {
            get => (ICommand?)GetValue(MovePieceCommandProperty);
            set => SetValue(MovePieceCommandProperty, value);
        }

        #endregion

        // 棋盘尺寸
        private const int FILES = 9;      // 纵线数
        private const int RANKS = 10;     // 横线数
        private const double RATIO = 10.0 / 9.0; // 宽高比 9:10

        // 计算尺寸
        private double _cellSize;
        private double _boardWidth;
        private double _boardHeight;
        private double _margin;

        // 动画常量
        private const double AnimationDurationMs = 150; // 动画时长（毫秒）
        public const double ShadowOffsetFactor = 4.0;  // 阴影偏移系数（公开供渲染器使用）
        public const double ShadowRadiusFactor = 2.0;  // 阴影半径系数（公开供渲染器使用）
        public const double EdgeRadiusFactor = 1.5;    // 边缘半径系数（公开供渲染器使用）
        private const double DragOpacity = 0.75;        // 拖拽透明度
        private const double DragGhostOpacity = 0.3;    // 拖拽残影透明度
        private const int AnimationFps = 60;            // 动画帧率

        // 动画状态
        private System.Windows.Threading.DispatcherTimer? _animationTimer;
        private readonly Stopwatch _animationStopwatch = new();
        private Square? _animatedSquare;          // 正在动画的棋子位置
        private double _animationProgress;        // 动画进度 0-1
        private double _easedProgress;            // 缓动后的进度（避免重复计算）
        private bool _isAnimating;

        // 将军闪烁动画状态
        private System.Windows.Threading.DispatcherTimer? _checkFlashTimer;
        private bool _isInCheck;                  // 是否处于将军状态
        private Square? _checkKingSquare;         // 被将军的将/帅位置
        private double _checkFlashAlpha = 1.0;    // 闪烁透明度
        private const double CheckFlashFrequency = 1.0; // 闪烁频率（Hz）
        private const int CheckFlashFps = 30;     // 闪烁动画帧率

        // 拖拽状态
        private bool _isDragging;
        private ChessPieceModel _dragPiece = new ChessPieceModel(); // 初始化为空的棋子
        private Square? _dragSourceSquare;
        private Point _dragPosition;              // 当前拖拽位置（屏幕坐标）
        private Square? _dragHoverSquare;         // 鼠标悬停的目标位置
        private Point _dragStartPosition;         // 拖拽起始位置

        public ChessBoardControl()
        {
            _typeface = new Typeface("Microsoft YaHei");
            
            // 字体回退链：优先使用楷体，依次回退到其他支持中文的字体
            // KaiTi: Windows 标配楷体
            // STXingkai: 华文行楷
            // FangSong: 仿宋
            // SimSun: 宋体（Windows 必备）
            // Microsoft YaHei: 微软雅黑（Windows Vista+ 必备）
            _chineseTypeface = new Typeface("KaiTi, STXingkai, FangSong, SimSun, Microsoft YaHei, Global User Interface");

            _themeConfig = new ThemeConfig();

            // 设置默认尺寸
            Width = 540;
            Height = 600;

            // 注册卸载事件，确保资源清理
            Unloaded += OnControlUnloaded;
        }

        /// <summary>
        /// 控件卸载时清理资源
        /// </summary>
        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        /// <summary>
        /// 设置游戏服务
        /// </summary>
        public void SetGameService(IGameService gameService)
        {
            // 先取消旧服务的事件订阅
            if (_gameService != null)
            {
                _gameService.BoardChanged -= OnBoardChanged;
                _gameService.GameStateChanged -= OnGameStateChanged;
                _gameService.CheckStateChanged -= OnCheckStateChanged;
            }

            _gameService = gameService;
            _gameService.BoardChanged += OnBoardChanged;
            _gameService.GameStateChanged += OnGameStateChanged;
            _gameService.CheckStateChanged += OnCheckStateChanged;
        }

        /// <summary>
        /// 设置主题服务
        /// </summary>
        public void SetThemeService(IThemeService themeService)
        {
            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            }

            _themeService = themeService;
            _themeService.ThemeChanged += OnThemeChanged;
            _themeConfig = _themeService.GetCurrentConfig();
            
            // 初始化渲染器
            InitializeRenderers();
        }

        /// <summary>
        /// 初始化渲染器
        /// </summary>
        private void InitializeRenderers()
        {
            _boardRenderer = new ChessBoardRenderer(_themeConfig);
            _pieceRenderer = new ChessPieceRenderer(_themeConfig, _chineseTypeface);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            // 棋盘有 9 条纵线（8 个间隔）和 10 条横线（9 个间隔）
            // 需要 9:10 宽高比，且格子必须为正方形
            double width = sizeInfo.NewSize.Width;
            double height = sizeInfo.NewSize.Height;

            // 先基于宽度计算格子大小
            double cellSizeByWidth = width / 9; // 9 个格子宽度（8 个间隔 + 2 个半格边距）
            
            // 再基于高度计算格子大小
            double cellSizeByHeight = height / 10; // 10 个格子高度（9 个间隔 + 2 个半格边距）

            // 选择较小的格子大小，确保棋盘能完整显示
            _cellSize = Math.Min(cellSizeByWidth, cellSizeByHeight);

            // 根据格子大小计算实际棋盘尺寸（确保对称）
            // 棋盘宽度 = 8 个间隔 + 2 个半格边距 = 9 个格子
            // 棋盘高度 = 9 个间隔 + 2 个半格边距 = 10 个格子
            _boardWidth = _cellSize * 9;
            _boardHeight = _cellSize * 10;

            // 边距为半个格子（确保第一条和最后一条线到边缘距离相等）
            _margin = _cellSize / 2;

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (_gameService == null) return;

            // 更新主题配置
            if (_themeService != null)
            {
                _themeConfig = _themeService.GetCurrentConfig();
            }

            // 确保渲染器已初始化
            if (_boardRenderer == null || _pieceRenderer == null)
            {
                InitializeRenderers();
            }

            // 计算中心偏移
            double offsetX = (ActualWidth - _boardWidth) / 2;
            double offsetY = (ActualHeight - _boardHeight) / 2;

            dc.PushTransform(new TranslateTransform(offsetX, offsetY));

            // 绘制棋盘（按层次顺序）- 使用棋盘渲染器
            _boardRenderer!.DrawBoardBackground(dc, _boardWidth, _boardHeight);
            
            if (_themeConfig.ShowPaperTexture)
            {
                _boardRenderer.DrawPaperTexture(dc, _boardWidth, _boardHeight);
            }
            
            if (_themeConfig.HighlightRiverArea)
            {
                _boardRenderer.DrawRiverHighlight(dc, _margin, _cellSize);
            }
            
            if (_themeConfig.HighlightPalaceArea)
            {
                _boardRenderer.DrawPalaceHighlight(dc, _margin, _cellSize);
            }
            
            _boardRenderer.DrawBoardGrid(dc, _margin, _cellSize, _boardWidth, _boardHeight);
            
            if (_themeConfig.ShowIntersectionDots)
            {
                _boardRenderer.DrawIntersectionDots(dc, _margin, _cellSize);
            }
            
            _boardRenderer.DrawPalace(dc, _margin, _cellSize);
            _boardRenderer.DrawRiver(dc, _margin, _cellSize, _boardWidth);
            _boardRenderer.DrawCoordinates(dc, _margin, _cellSize, _boardHeight, _typeface);
            
            // 绘制棋子 - 使用棋子渲染器
            DrawPieces(dc);
            
            // 绘制选中状态
            _boardRenderer.DrawSelection(dc, _margin, _cellSize, _gameService);
            
            // 绘制拖拽中的棋子（最上层）
            if (_isDragging)
            {
                DrawDraggingPiece(dc);
            }
            
            // 绘制动画中的棋子（最上层）
            if (_isAnimating && _animatedSquare.HasValue)
            {
                DrawAnimatedPiece(dc);
            }

            dc.Pop();
        }

        /// <summary>
        /// 绘制棋子
        /// </summary>
        private void DrawPieces(DrawingContext dc)
        {
            if (_gameService == null || _pieceRenderer == null) return;

            double pieceSize = _cellSize * 0.85;
            double pieceRadius = pieceSize / 2;

            for (int rank = 1; rank <= RANKS; rank++)
            {
                for (int file = 1; file <= FILES; file++)
                {
                    var square = new Square(file, rank);
                    var piece = _gameService.GetPieceAt(square);

                    if (!piece.IsEmpty)
                    {
                        double x = _margin + (file - 1) * _cellSize;
                        double y = _margin + (rank - 1) * _cellSize;
                        var center = new Point(x, y);

                        // 如果是正在拖拽的棋子，绘制半透明残影
                        if (_isDragging && _dragSourceSquare.HasValue && _dragSourceSquare.Value == square)
                        {
                            // 绘制半透明残影
                            dc.PushOpacity(DragGhostOpacity);
                            _pieceRenderer.DrawPiece(dc, piece, center, pieceRadius);
                            dc.Pop();
                            continue;
                        }

                        // 如果是正在动画的棋子，跳过（由 DrawAnimatedPiece 绘制）
                        if (_isAnimating && _animatedSquare.HasValue && _animatedSquare.Value == square)
                        {
                            continue;
                        }

                        // 如果是将/帅棋子且处于将军状态，绘制闪烁效果
                        if (_isInCheck && piece.Type == PieceType.King && 
                            _checkKingSquare.HasValue && _checkKingSquare.Value == square)
                        {
                            _pieceRenderer.DrawPieceWithCheckFlash(dc, piece, center, pieceRadius, _checkFlashAlpha);
                        }
                        else
                        {
                            _pieceRenderer.DrawPiece(dc, piece, center, pieceRadius);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 绘制拖拽中的棋子（半透明，跟随鼠标）
        /// </summary>
        private void DrawDraggingPiece(DrawingContext dc)
        {
            if (_pieceRenderer == null) return;

            double pieceSize = _cellSize * 0.85;
            double pieceRadius = pieceSize / 2;

            // 在外层应用透明度，避免双重透明度
            dc.PushOpacity(DragOpacity);

            // 在鼠标位置绘制棋子（使用 3D 效果）
            var center = new Point(_dragPosition.X, _dragPosition.Y);
            _pieceRenderer.DrawPiece3D(dc, _dragPiece, center, pieceRadius, 1.0, 1.0);

            dc.Pop();

            // 如果悬停在有效位置，绘制目标位置提示
            if (_dragHoverSquare.HasValue)
            {
                double x = _margin + (_dragHoverSquare.Value.File - 1) * _cellSize;
                double y = _margin + (_dragHoverSquare.Value.Rank - 1) * _cellSize;

                // 绘制半透明绿色提示圈
                var hintColor = Color.FromArgb(120, 50, 205, 50); // 半透明绿色
                dc.DrawEllipse(new SolidColorBrush(hintColor), null,
                               new Point(x, y), _cellSize * 0.35, _cellSize * 0.35);

                // 绘制外圈提示
                var outerColor = Color.FromArgb(80, 50, 205, 50);
                var outerPen = new Pen(new SolidColorBrush(outerColor), 2);
                dc.DrawEllipse(null, outerPen, new Point(x, y), _cellSize * 0.45, _cellSize * 0.45);
            }
        }

        /// <summary>
        /// 绘制动画中的棋子
        /// </summary>
        private void DrawAnimatedPiece(DrawingContext dc)
        {
            if (_animatedSquare == null || _gameService == null || _pieceRenderer == null) return;

            var piece = _gameService.GetPieceAt(_animatedSquare.Value);
            if (piece.IsEmpty) return;

            double x = _margin + (_animatedSquare.Value.File - 1) * _cellSize;
            double y = _margin + (_animatedSquare.Value.Rank - 1) * _cellSize;
            double pieceSize = _cellSize * 0.85;
            double pieceRadius = pieceSize / 2;

            // 使用缓动进度字段（已由 UpdateAnimation 计算）
            // 添加轻微缩放效果
            double scale = 1.0 + 0.1 * _easedProgress * (1 - _easedProgress); // 最大放大 10%
            
            var center = new Point(x, y);
            _pieceRenderer.DrawPiece3D(dc, piece, center, pieceRadius * scale, _easedProgress);
        }

        /// <summary>
        /// 鼠标点击处理
        /// </summary>
        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (_gameService == null) return;

            var position = e.GetPosition(this);

            // 计算偏移
            double offsetX = (ActualWidth - _boardWidth) / 2;
            double offsetY = (ActualHeight - _boardHeight) / 2;

            // 转换为棋盘坐标
            double boardX = position.X - offsetX - _margin;
            double boardY = position.Y - offsetY - _margin;

            // 计算交叉点
            int file = (int)Math.Round(boardX / _cellSize) + 1;
            int rank = (int)Math.Round(boardY / _cellSize) + 1;

            var square = new Square(file, rank);

            if (square.IsValid)
            {
                var piece = _gameService.GetPieceAt(square);

                // 如果点击的是己方棋子，开始拖拽
                if (!piece.IsEmpty && piece.Color == _gameService.SideToMove)
                {
                    _isDragging = true;
                    _dragPiece = piece;
                    _dragSourceSquare = square;
                    _dragStartPosition = position;
                    _dragPosition = position;
                    CaptureMouse(); // 捕获鼠标
                }
                else
                {
                    // 点击其他位置，触发 3D 动画
                    if (!piece.IsEmpty)
                    {
                        Start3DAnimation(square);
                    }
                    // 使用命令模式（优先）或服务（兼容模式）
                    if (SelectSquareCommand?.CanExecute(square) == true)
                    {
                        SelectSquareCommand.Execute(square);
                    }
                    else
                    {
                        _gameService.SelectSquare(square);
                    }
                }
            }
        }

        /// <summary>
        /// 启动 3D 动画
        /// </summary>
        private void Start3DAnimation(Square square)
        {
            _animatedSquare = square;
            _animationStopwatch.Restart(); // 使用高精度计时器
            _isAnimating = true;
            _animationProgress = 0;
            _easedProgress = 0;

            // 创建定时器（60FPS）
            if (_animationTimer == null)
            {
                _animationTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(1000.0 / AnimationFps) // ~60fps
                };
                _animationTimer.Tick += (s, e) => UpdateAnimation();
            }
            _animationTimer.Start();
        }

        /// <summary>
        /// 更新动画状态
        /// </summary>
        private void UpdateAnimation()
        {
            var elapsed = _animationStopwatch.Elapsed.TotalMilliseconds;
            _animationProgress = Math.Min(1.0, elapsed / AnimationDurationMs);

            // 使用缓动函数（Ease-Out Cubic）并存储结果
            _easedProgress = 1 - Math.Pow(1 - _animationProgress, 3);

            if (_animationProgress >= 1.0)
            {
                _animationTimer?.Stop();
                _isAnimating = false;
                _animatedSquare = null;
                _animationProgress = 0;
                _easedProgress = 0;
            }

            InvalidateVisual();
        }

        /// <summary>
        /// 获取鼠标位置对应的棋盘坐标
        /// </summary>
        private Square GetSquareFromPosition(Point position)
        {
            // 计算偏移
            double offsetX = (ActualWidth - _boardWidth) / 2;
            double offsetY = (ActualHeight - _boardHeight) / 2;

            // 转换为棋盘坐标
            double boardX = position.X - offsetX - _margin;
            double boardY = position.Y - offsetY - _margin;

            // 计算交叉点
            int file = (int)Math.Round(boardX / _cellSize) + 1;
            int rank = (int)Math.Round(boardY / _cellSize) + 1;

            return new Square(file, rank);
        }

        /// <summary>
        /// 鼠标移动处理
        /// </summary>
        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging)
            {
                _dragPosition = e.GetPosition(this);

                // 计算悬停的目标位置
                var hoverSquare = GetSquareFromPosition(_dragPosition);
                if (hoverSquare.IsValid)
                {
                    _dragHoverSquare = hoverSquare;
                }
                else
                {
                    _dragHoverSquare = null;
                }

                InvalidateVisual();
            }
        }

        /// <summary>
        /// 鼠标释放处理
        /// </summary>
        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isDragging && _dragSourceSquare.HasValue)
            {
                // 计算最终位置
                var targetSquare = GetSquareFromPosition(e.GetPosition(this));

                if (targetSquare.IsValid && targetSquare != _dragSourceSquare.Value)
                {
                    // 尝试走棋
                    bool moved = TryMovePiece(_dragSourceSquare.Value, targetSquare);
                    if (moved)
                    {
                        // 走棋成功，在目标位置触发动画
                        Start3DAnimation(targetSquare);
                    }
                }
                else
                {
                    // 返回原位，触发动画
                    Start3DAnimation(_dragSourceSquare.Value);
                }

                // 清除拖拽状态
                ClearDragState();
            }
        }

        /// <summary>
        /// 鼠标离开处理
        /// </summary>
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (_isDragging)
            {
                // 取消拖拽
                ClearDragState();
            }
        }

        /// <summary>
        /// 清除拖拽状态
        /// </summary>
        private void ClearDragState()
        {
            _isDragging = false;
            _dragPiece = new ChessPieceModel();
            _dragSourceSquare = null;
            _dragHoverSquare = null;
            ReleaseMouseCapture();
            InvalidateVisual();
        }

        /// <summary>
        /// 尝试走棋
        /// </summary>
        private bool TryMovePiece(Square from, Square to)
        {
            if (_gameService == null) return false;

            // 先选中起始位置（使用命令或服务）
            if (SelectSquareCommand?.CanExecute(from) == true)
            {
                SelectSquareCommand.Execute(from);
            }
            else
            {
                _gameService.SelectSquare(from);
            }
            
            // 检查目标位置是否在合法走法列表中
            foreach (var legalMove in _gameService.LegalMoves)
            {
                if (legalMove == to)
                {
                    // 执行走棋命令（优先）或服务（兼容模式）
                    var move = new Move(from, to);
                    if (MovePieceCommand?.CanExecute(move) == true)
                    {
                        MovePieceCommand.Execute(move);
                    }
                    else
                    {
                        // 兼容模式：使用服务
                        _gameService.SelectSquare(to);
                    }
                    return true;
                }
            }

            return false;
        }

        private void OnBoardChanged(object? sender, BoardChangedEventArgs e)
        {
            // 如果棋盘状态改变，取消正在进行的动画（避免显示过期数据）
            if (_isAnimating)
            {
                _animationTimer?.Stop();
                _isAnimating = false;
                _animatedSquare = null;
                _animationProgress = 0;
                _easedProgress = 0;
            }

            Dispatcher.Invoke(() => InvalidateVisual());
        }

        private void OnGameStateChanged(object? sender, GameStateChangedEventArgs e)
        {
            // 状态改变时重绘
            Dispatcher.Invoke(() => InvalidateVisual());
        }

        /// <summary>
        /// 处理将军状态变更事件
        /// </summary>
        private void OnCheckStateChanged(object? sender, CheckStateChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _isInCheck = e.IsInCheck;
                _checkKingSquare = e.KingSquare;

                if (e.IsInCheck)
                {
                    // 启动闪烁动画
                    StartCheckFlashAnimation();
                }
                else
                {
                    // 停止闪烁动画
                    StopCheckFlashAnimation();
                }

                InvalidateVisual();
            });
        }

        /// <summary>
        /// 启动将军闪烁动画
        /// </summary>
        private void StartCheckFlashAnimation()
        {
            if (_checkFlashTimer == null)
            {
                _checkFlashTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(1000.0 / CheckFlashFps)
                };
                _checkFlashTimer.Tick += OnCheckFlashTimerTick;
            }

            _checkFlashAlpha = 1.0;
            _checkFlashTimer.Start();
        }

        /// <summary>
        /// 停止将军闪烁动画
        /// </summary>
        private void StopCheckFlashAnimation()
        {
            _checkFlashTimer?.Stop();
            _isInCheck = false;
            _checkKingSquare = null;
            _checkFlashAlpha = 1.0;
        }

        /// <summary>
        /// 将军闪烁动画定时器回调
        /// </summary>
        private void OnCheckFlashTimerTick(object? sender, EventArgs e)
        {
            if (!_isInCheck)
            {
                _checkFlashTimer?.Stop();
                return;
            }

            // 计算闪烁透明度（正弦波动画）
            // 频率 1Hz 意味着每秒完成一次明暗循环
            double flashCycle = 2 * Math.PI * CheckFlashFrequency;
            double elapsedSeconds = DateTime.Now.TimeOfDay.TotalSeconds;
            _checkFlashAlpha = 0.3 + 0.7 * (0.5 + 0.5 * Math.Sin(flashCycle * elapsedSeconds));

            InvalidateVisual();
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            _themeConfig = e.Config;
            
            // 更新渲染器的主题配置
            _boardRenderer?.UpdateTheme(_themeConfig);
            _pieceRenderer?.UpdateTheme(_themeConfig);
            
            Dispatcher.Invoke(() => InvalidateVisual());
        }

        /// <summary>
        /// 取消所有事件订阅
        /// </summary>
        private void UnsubscribeEvents()
        {
            // 取消游戏服务事件订阅
            if (_gameService != null)
            {
                _gameService.BoardChanged -= OnBoardChanged;
                _gameService.GameStateChanged -= OnGameStateChanged;
                _gameService.CheckStateChanged -= OnCheckStateChanged;
            }

            // 取消主题服务事件订阅
            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            }

            // 取消控件事件订阅
            Unloaded -= OnControlUnloaded;
        }

        /// <summary>
        /// 终结器 - 双重保障资源释放
        /// 注意：终结器中不应执行实际清理操作（可能访问已释放资源），
        /// 仅记录警告提醒开发者正确调用 Dispose()
        /// </summary>
        ~ChessBoardControl()
        {
            if (!_isDisposed)
            {
                System.Diagnostics.Debug.WriteLine("[ChessBoardControl] 警告：未正确调用 Dispose()，资源可能泄漏。请在使用完毕后调用 Dispose()。");
            }
        }

        /// <summary>
        /// 释放资源，取消所有事件订阅
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            // 停止动画定时器
            if (_animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer = null;
            }

            // 停止将军闪烁定时器
            if (_checkFlashTimer != null)
            {
                _checkFlashTimer.Stop();
                _checkFlashTimer = null;
            }

            // 清理渲染器缓存
            _boardRenderer?.ClearCache();
            _pieceRenderer?.ClearCache();

            // 取消所有事件订阅
            UnsubscribeEvents();

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}