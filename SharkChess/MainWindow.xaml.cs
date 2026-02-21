using SharkChess.Models;
using SharkChess.Services;
using SharkChess.Views.Controls;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Media;

namespace SharkChess
{
    /// <summary>
    /// 主窗口
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IGameService _gameService;
        private readonly IEngineService _engineService;
        private readonly IThemeService _themeService;
        private bool _isEnginePlayMode; // 是否启用人机对战模式
        private bool _lastCheckState;   // 上一次的将军状态（用于判断是否需要播放音效）
        private SoundPlayer? _checkSoundPlayer; // 将军音效播放器

        public MainWindow(
            IGameService gameService,
            IEngineService engineService,
            IThemeService themeService)
        {
            InitializeComponent();

            _gameService = gameService;
            _engineService = engineService;
            _themeService = themeService;
            _isEnginePlayMode = false;
            _lastCheckState = false;

            // 订阅事件
            _gameService.BoardChanged += OnBoardChanged;
            _gameService.GameStateChanged += OnGameStateChanged;
            _gameService.CheckStateChanged += OnCheckStateChanged;
            _gameService.GameEnded += OnGameEnded;
            _gameService.DrawOffered += OnDrawOffered;
            
            _themeService.ThemeChanged += OnThemeChanged;
            
            _engineService.EngineOutput += OnEngineOutput;
            _engineService.BestMove += OnBestMove;

            // 初始化
            ChessBoard.SetGameService(_gameService);
            UpdateSideToMove();
            
            // 初始化将军音效
            InitializeCheckSound();
        }

        /// <summary>
        /// 初始化将军音效
        /// </summary>
        private void InitializeCheckSound()
        {
            try
            {
                // 尝试从 Resources 目录加载将军音效
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var soundPath = System.IO.Path.Combine(appDir, "Resources", "check_sound.wav");
                
                if (System.IO.File.Exists(soundPath))
                {
                    _checkSoundPlayer = new SoundPlayer(soundPath);
                    _checkSoundPlayer.LoadAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] 初始化将军音效失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 播放将军音效
        /// </summary>
        private void PlayCheckSound()
        {
            try
            {
                if (_checkSoundPlayer != null)
                {
                    // 使用异步播放避免阻塞 UI
                    _checkSoundPlayer.Play();
                }
                else
                {
                    // 如果没有音效文件，使用系统提示音
                    SystemSounds.Exclamation.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] 播放将军音效失败：{ex.Message}");
            }
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            _gameService.NewGame();
            _engineService.NewGame();
            MoveHistoryList.Items.Clear();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_gameService.Undo())
            {
                UpdateMoveHistory();
            }
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            _gameService.NewGame();
            MoveHistoryList.Items.Clear();
        }

        private void DrawOffer_Click(object sender, RoutedEventArgs e)
        {
            // 使用 async Task 模式处理异常
            _ = HandleDrawOfferAsync();
        }

        /// <summary>
        /// 处理和棋申请
        /// </summary>
        private async Task HandleDrawOfferAsync()
        {
            try
            {
                // 检查游戏是否正在进行
                if (_gameService.GameState != GameState.Ongoing)
                {
                    MessageBox.Show("游戏已结束，无法申请和棋", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 检查是否已有待处理的和棋申请
                if (_gameService.IsDrawOfferPending)
                {
                    MessageBox.Show("已有待处理的和棋申请，请等待回应", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 在人机对战模式下，需要判断是谁在申请和棋
                if (_isEnginePlayMode)
                {
                    var humanColor = GetEngineColor() == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
                    
                    // 如果是人类玩家申请和棋
                    if (_gameService.SideToMove == humanColor)
                    {
                        // 先确认人类玩家的决定
                        var confirmResult = MessageBox.Show("您确定要申请和棋吗？", "和棋申请", 
                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                        
                        if (confirmResult != MessageBoxResult.Yes)
                            return;

                        // 发起和棋申请
                        if (_gameService.OfferDraw())
                        {
                            // AI 自动判断是否同意
                            await HandleEngineDrawResponseAsync();
                        }
                    }
                    else
                    {
                        // AI 不能主动申请和棋（简化处理）
                        MessageBox.Show("人机对战模式下，AI 不能主动申请和棋", "提示", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // 人人对战模式
                    if (_gameService.OfferDraw())
                    {
                        var result = MessageBox.Show(
                            $"{(_gameService.SideToMove == PieceColor.Red ? "红" : "黑")}方申请和棋，是否同意？", 
                            "和棋申请", 
                            MessageBoxButton.YesNo, 
                            MessageBoxImage.Question);
                        
                        _gameService.RespondToDrawOffer(
                            result == MessageBoxResult.Yes ? DrawResponse.Accept : DrawResponse.Decline);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"和棋申请处理失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// AI 自动判断是否同意和棋申请
        /// </summary>
        private async Task HandleEngineDrawResponseAsync()
        {
            if (!_isEnginePlayMode || !_engineService.IsLoaded)
            {
                // 引擎未加载，默认拒绝
                _gameService.RespondToDrawOffer(DrawResponse.Decline);
                return;
            }

            try
            {
                // 获取当前局面 FEN
                var fen = _gameService.ToFEN();
                
                // 评估当前局面
                int score = await _engineService.EvaluatePositionAsync(fen, 6);
                
                // AI 执黑方，分数为正表示黑方优势，为负表示黑方劣势
                // 根据分数决定是否同意和棋
                bool acceptDraw;
                
                if (score < -500)
                {
                    // 劣势时（分数 < -500）倾向于同意和棋
                    acceptDraw = true;
                }
                else if (score > 500)
                {
                    // 优势时（分数 > 500）倾向于拒绝和棋
                    acceptDraw = false;
                }
                else
                {
                    // 均势时随机决定（50% 概率同意）
                    acceptDraw = new Random().NextDouble() < 0.5;
                }

                // 应用 AI 的决定
                _gameService.RespondToDrawOffer(acceptDraw ? DrawResponse.Accept : DrawResponse.Decline);
                
                // 显示提示信息
                string scoreText = score > 0 ? $"+{score}" : score.ToString();
                string decisionText = acceptDraw ? "同意" : "拒绝";
                MessageBox.Show($"AI {decisionText}和棋申请（局面评估：{scoreText}）", "和棋结果", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI 判断和棋失败：{ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _gameService.RespondToDrawOffer(DrawResponse.Decline);
            }
        }

        private void LoadEngine_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "可执行文件|*.exe|所有文件|*.*",
                Title = "选择象棋引擎"
            };

            if (dialog.ShowDialog() == true)
            {
                var enginePath = dialog.FileName;
                var engineName = System.IO.Path.GetFileNameWithoutExtension(enginePath);
                
                if (_engineService.LoadEngine(enginePath))
                {
                    EngineStatusText.Text = "已加载";
                    EngineNameText.Text = engineName;
                }
                else
                {
                    EngineStatusText.Text = "加载失败";
                    EngineNameText.Text = "-";
                }
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var theme = selectedItem.Content.ToString() switch
                {
                    "标准规范版" => Models.BoardTheme.Standard,
                    "教学专用版" => Models.BoardTheme.Teaching,
                    "国风设计版" => Models.BoardTheme.ChineseStyle,
                    _ => Models.BoardTheme.Standard
                };
                _themeService.SetTheme(theme);
                _themeService.SaveCurrentTheme(); // 自动保存主题
            }
        }

        private void ToggleRightPanel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                if (menuItem.IsChecked)
                {
                    RightColumn.Width = new GridLength(380);
                    RightPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    RightColumn.Width = new GridLength(0);
                    RightPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            MoveHistoryList.Items.Clear();
        }

        private void SavePGN_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否有走棋记录
            if (_gameService.MoveHistory.Count == 0)
            {
                MessageBox.Show("当前没有走棋记录可保存。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 弹出保存对话框
            var dialog = new SaveFileDialog
            {
                Filter = "PGN 棋谱文件|*.pgn|所有文件|*.*",
                Title = "保存棋谱",
                FileName = $"棋谱_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".pgn"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // 获取对局信息（可以扩展为让用户输入）
                    string whitePlayer = "红方";
                    string blackPlayer = "黑方";
                    string eventName = "鲨鱼象棋对局";
                    string site = "本地";

                    // 如果启用了引擎对战，更新玩家名称
                    if (_isEnginePlayMode && _engineService.IsLoaded)
                    {
                        blackPlayer = EngineNameText.Text;
                    }

                    // 导出 PGN
                    string pgnContent = _gameService.ExportToPGN(whitePlayer, blackPlayer, eventName, site);

                    // 写入文件
                    System.IO.File.WriteAllText(dialog.FileName, pgnContent, System.Text.Encoding.UTF8);

                    MessageBox.Show($"棋谱已保存到：{dialog.FileName}", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnBoardChanged(object? sender, BoardChangedEventArgs e)
        {
            // 使用异步调用避免阻塞调用线程
            Dispatcher.BeginInvoke(() =>
            {
                ChessBoard.InvalidateVisual();
                UpdateSideToMove();
                UpdateMoveHistory();
                UpdateCapturedPieces();

                // 如果启用人机对战模式且轮到引擎走棋，触发引擎思考
                if (_isEnginePlayMode && _gameService.GameState == GameState.Ongoing)
                {
                    if (_gameService.SideToMove == GetEngineColor())
                    {
                        // 延迟一小段时间再触发思考，避免阻塞UI
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay(100);
                            await Dispatcher.InvokeAsync(ThinkAsync);
                        });
                    }
                }
            });
        }

        /// <summary>
        /// 更新阵亡棋子显示
        /// </summary>
        private void UpdateCapturedPieces()
        {
            CapturedRedPiecesControl.ItemsSource = _gameService.CapturedRedPieces;
            CapturedBlackPiecesControl.ItemsSource = _gameService.CapturedBlackPieces;
        }

        private void OnGameStateChanged(object? sender, GameStateChangedEventArgs e)
        {
            // 使用异步调用避免阻塞调用线程
            Dispatcher.BeginInvoke(() =>
            {
                UpdateSideToMove();
            });
        }

        /// <summary>
        /// 处理将军状态变更事件
        /// </summary>
        private void OnCheckStateChanged(object? sender, CheckStateChangedEventArgs e)
        {
            // 使用异步调用避免阻塞调用线程
            Dispatcher.BeginInvoke(() =>
            {
                UpdateCheckStatus(e);
            });
        }

        /// <summary>
        /// 更新将军状态显示
        /// </summary>
        private void UpdateCheckStatus(CheckStateChangedEventArgs e)
        {
            if (e.IsInCheck)
            {
                // 显示将军提示
                CheckStatusPanel.Visibility = Visibility.Visible;
                
                // 更新提示文字
                string checkedSide = e.CheckedColor == PieceColor.Red ? "红方" : "黑方";
                CheckStatusText.Text = $"{checkedSide}被将军！";
                
                // 仅在将军发生时播放一次音效（不是每次状态更新都播放）
                if (!_lastCheckState)
                {
                    PlayCheckSound();
                }
                
                _lastCheckState = true;
            }
            else
            {
                // 隐藏将军提示
                CheckStatusPanel.Visibility = Visibility.Collapsed;
                _lastCheckState = false;
            }
        }

        private void OnGameEnded(object? sender, GameEndedEventArgs e)
        {
            // 使用异步调用避免阻塞调用线程
            Dispatcher.BeginInvoke(() =>
            {
                // 显示胜负结果弹窗
                var dialog = new ResultDialog(this, e, _gameService);
                dialog.ShowDialog();

                // 处理用户选择
                if (dialog.DialogResult == true)
                {
                    string action = dialog.Tag as string;
                    
                    if (action == "NewGame")
                    {
                        // 再来一局
                        _gameService.NewGame();
                        _engineService.NewGame();
                        MoveHistoryList.Items.Clear();
                    }
                    else if (action == "ReturnToLobby")
                    {
                        // 返回大厅（目前关闭游戏，后续可扩展大厅功能）
                        // 暂时关闭弹窗即可
                    }
                }
            });
        }

        private void OnDrawOffered(object? sender, DrawOfferEventArgs e)
        {
            // 使用异步调用避免阻塞调用线程
            Dispatcher.BeginInvoke(() =>
            {
                // 在人机对战模式下，这个事件会触发 AI 自动判断
                // 人人对战模式下，这里可以显示通知
                if (!_isEnginePlayMode && e.RequiresResponse)
                {
                    var result = MessageBox.Show(
                        $"{(e.OfferingSide == PieceColor.Red ? "红" : "黑")}方申请和棋，是否同意？", 
                        "和棋申请", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);
                    
                    _gameService.RespondToDrawOffer(
                        result == MessageBoxResult.Yes ? DrawResponse.Accept : DrawResponse.Decline);
                }
            });
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            // 使用异步调用避免阻塞调用线程
            Dispatcher.BeginInvoke(() =>
            {
                ChessBoard.InvalidateVisual();
            });
        }

        private void OnEngineOutput(object? sender, EngineOutputEventArgs e)
        {
            // 使用异步调用避免阻塞调用线程
            Dispatcher.BeginInvoke(() =>
            {
                EngineOutputText.Text += e.Output + "\n";
                EngineOutputScroll.ScrollToEnd();
            });
        }

        private void OnBestMove(object? sender, BestMoveEventArgs e)
        {
            // 使用异步调用避免阻塞调用线程
            Dispatcher.BeginInvoke(() =>
            {
                // 引擎返回最佳走法
                if (!string.IsNullOrEmpty(e.BestMove))
                {
                    EngineOutputText.Text += $"最佳走法：{e.BestMove} (分数：{e.Score}, 深度：{e.Depth})\n";
                    EngineOutputScroll.ScrollToEnd();
                    
                    // 更新状态栏
                    StatusBarDepth.Text = e.Depth.ToString();
                    StatusBarScore.Text = e.Score.ToString();

                    // 如果启用人机对战模式，自动执行引擎走法
                    if (_isEnginePlayMode && _gameService.GameState == GameState.Ongoing)
                    {
                        // 解析 UCI 走法并执行
                        var move = Move.FromUCI(e.BestMove);
                        if (move.IsValid)
                        {
                            // 检查是否是引擎该走棋
                            var engineColor = GetEngineColor();
                            if (_gameService.SideToMove == engineColor)
                            {
                                _gameService.TryMove(move.From, move.To);
                                UpdateMoveHistory();
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 获取引擎执棋的颜色（默认黑方）
        /// </summary>
        private PieceColor GetEngineColor()
        {
            // 可以根据需要扩展为选择引擎颜色
            return PieceColor.Black;
        }

        private void EnginePlayMode_Checked(object sender, RoutedEventArgs e)
        {
            _isEnginePlayMode = true;
            
            // 如果当前轮到引擎走棋，立即触发思考
            if (_gameService.GameState == GameState.Ongoing && 
                _gameService.SideToMove == GetEngineColor())
            {
                ThinkAsync();
            }
        }

        private void EnginePlayMode_Unchecked(object sender, RoutedEventArgs e)
        {
            _isEnginePlayMode = false;
        }

        private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DifficultyComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var difficulty = selectedItem.Tag.ToString() switch
                {
                    "Beginner" => Models.EngineDifficulty.Beginner,
                    "Novice" => Models.EngineDifficulty.Novice,
                    "Amateur" => Models.EngineDifficulty.Amateur,
                    "Professional" => Models.EngineDifficulty.Professional,
                    "Master" => Models.EngineDifficulty.Master,
                    "Grandmaster" => Models.EngineDifficulty.Grandmaster,
                    _ => Models.EngineDifficulty.Amateur
                };
                
                _engineService.Difficulty = difficulty;
            }
        }

        #region 复盘控制

        private void GoToStart_Click(object sender, RoutedEventArgs e)
        {
            _gameService.GoToStart();
            UpdateSideToMove();
            UpdateMoveHistory();
        }

        private void GoToEnd_Click(object sender, RoutedEventArgs e)
        {
            _gameService.GoToEnd();
            UpdateSideToMove();
            UpdateMoveHistory();
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            _gameService.GoBack();
            UpdateSideToMove();
            UpdateMoveHistory();
        }

        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
            _gameService.GoForward();
            UpdateSideToMove();
            UpdateMoveHistory();
        }

        #endregion

        /// <summary>
        /// 异步触发引擎思考
        /// </summary>
        private void ThinkAsync()
        {
            if (_engineService != null && _engineService.IsLoaded && _gameService.GameState == GameState.Ongoing)
            {
                var fen = _gameService.ToFEN();
                _engineService.SetPosition(fen);
                _engineService.StartThinking();
            }
        }

        private void UpdateSideToMove()
        {
            bool isRed = _gameService.SideToMove == PieceColor.Red;
            
            // 更新右侧面板
            SideToMoveText.Text = isRed ? "红方" : "黑方";
            SideToMoveText.Foreground = isRed
                ? System.Windows.Media.Brushes.Crimson
                : System.Windows.Media.Brushes.DimGray;
            
            // 更新状态栏
            StatusBarSideToMove.Text = isRed ? "红方走棋" : "黑方走棋";
            StatusBarSideToMove.Foreground = isRed
                ? System.Windows.Media.Brushes.Crimson
                : System.Windows.Media.Brushes.DimGray;
        }

        private void UpdateMoveHistory()
        {
            MoveHistoryList.Items.Clear();
            var history = _gameService.MoveHistory;

            for (int i = 0; i < history.Count; i += 2)
            {
                string redMove = GetMoveDescription(history[i]);
                string blackMove = (i + 1 < history.Count) ? GetMoveDescription(history[i + 1]) : "";
                MoveHistoryList.Items.Add($"{(i / 2 + 1)}. {redMove}  {blackMove}");
            }

            if (MoveHistoryList.Items.Count > 0)
            {
                MoveHistoryList.ScrollIntoView(MoveHistoryList.Items[MoveHistoryList.Items.Count - 1]);
            }
        }

        private string GetMoveDescription(Move move)
        {
            // 获取起始位置的棋子（注意：此时棋子已经移动到目标位置）
            // 我们需要从移动历史中获取棋子信息，或者使用一种简化的方式
            // 这里使用一个简化的方法：根据 UCI 格式返回中文记谱
            // 注意：在走棋后，From 位置已经为空，所以需要特殊处理
            
            // 获取移动后的棋子（在 To 位置）
            var piece = _gameService.GetPieceAt(move.To);
            
            if (piece.IsEmpty)
            {
                // 如果目标位置也没有棋子（可能是悔棋后），返回 UCI 格式
                return move.ToUCI();
            }

            // 使用中文记谱（注意：此时棋子已经在目标位置，需要反向推算）
            return move.ToChinese(piece);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            
            // 取消所有事件订阅，防止内存泄漏
            _gameService.BoardChanged -= OnBoardChanged;
            _gameService.GameStateChanged -= OnGameStateChanged;
            _gameService.CheckStateChanged -= OnCheckStateChanged;
            _gameService.GameEnded -= OnGameEnded;
            _gameService.DrawOffered -= OnDrawOffered;
            
            _themeService.ThemeChanged -= OnThemeChanged;
            
            _engineService.EngineOutput -= OnEngineOutput;
            _engineService.BestMove -= OnBestMove;
            
            // 释放引擎资源
            _engineService.Dispose();
            
            // 释放音效播放器资源
            _checkSoundPlayer?.Dispose();
        }
    }
}
