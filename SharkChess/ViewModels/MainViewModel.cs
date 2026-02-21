using SharkChess.Models;
using SharkChess.Services;
using System.Windows.Input;

namespace SharkChess.ViewModels
{
    /// <summary>
    /// 主窗口 ViewModel
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IGameService _gameService;
        private readonly IEngineService _engineService;
        private readonly IThemeService _themeService;

        private PieceColor _sideToMove;
        private GameState _gameState;
        private BoardTheme _currentTheme;

        public MainViewModel(
            IGameService gameService,
            IEngineService engineService,
            IThemeService themeService)
        {
            _gameService = gameService;
            _engineService = engineService;
            _themeService = themeService;

            // 订阅事件
            _gameService.BoardChanged += OnBoardChanged;
            _gameService.GameStateChanged += OnGameStateChanged;
            _themeService.ThemeChanged += OnThemeChanged;

            // 初始化命令
            NewGameCommand = new RelayCommand(NewGame);
            UndoCommand = new RelayCommand(Undo, CanUndo);
            LoadEngineCommand = new RelayCommand<string>(LoadEngine);

            // 初始化 MVVM 命令
            SelectSquareCommand = new RelayCommand<Square?>(SelectSquare, CanSelectSquare);
            MovePieceCommand = new RelayCommand<Move?>(MovePiece, CanMovePiece);

            // 初始化状态
            _sideToMove = _gameService.SideToMove;
            _gameState = _gameService.GameState;
            _currentTheme = _themeService.CurrentTheme;
        }

        public PieceColor SideToMove
        {
            get => _sideToMove;
            private set => SetProperty(ref _sideToMove, value);
        }

        public GameState GameState
        {
            get => _gameState;
            private set => SetProperty(ref _gameState, value);
        }

        public BoardTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (SetProperty(ref _currentTheme, value))
                {
                    _themeService.SetTheme(value);
                }
            }
        }

        /// <summary>新游戏命令</summary>
        public ICommand NewGameCommand { get; }

        /// <summary>悔棋命令</summary>
        public ICommand UndoCommand { get; }

        /// <summary>加载引擎命令</summary>
        public ICommand LoadEngineCommand { get; }

        /// <summary>选择格子命令</summary>
        public ICommand SelectSquareCommand { get; }

        /// <summary>走棋命令</summary>
        public ICommand MovePieceCommand { get; }

        private void NewGame()
        {
            _gameService.NewGame();
            _engineService.NewGame();
        }

        private void Undo()
        {
            _gameService.Undo();
        }

        private bool CanUndo()
        {
            return _gameService.MoveHistory.Count > 0;
        }

        private void LoadEngine(string? path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                _engineService.LoadEngine(path);
            }
        }

        /// <summary>
        /// 选择格子
        /// </summary>
        private void SelectSquare(Square? square)
        {
            if (square.HasValue && square.Value.IsValid)
            {
                _gameService.SelectSquare(square.Value);
            }
        }

        /// <summary>
        /// 判断是否可以选择格子
        /// </summary>
        private bool CanSelectSquare(Square? square)
        {
            return square.HasValue && square.Value.IsValid;
        }

        /// <summary>
        /// 执行走棋
        /// </summary>
        private void MovePiece(Move? move)
        {
            if (move.HasValue && move.Value.IsValid)
            {
                // 先选中起始位置
                _gameService.SelectSquare(move.Value.From);
                // 再选中目标位置完成走棋
                _gameService.SelectSquare(move.Value.To);
            }
        }

        /// <summary>
        /// 判断是否可以走棋
        /// </summary>
        private bool CanMovePiece(Move? move)
        {
            if (!move.HasValue || !move.Value.IsValid)
                return false;

            // 检查是否为合法走法
            foreach (var legalMove in _gameService.LegalMoves)
            {
                if (legalMove == move.Value.To)
                    return true;
            }
            return false;
        }

        private void OnBoardChanged(object? sender, BoardChangedEventArgs e)
        {
            SideToMove = _gameService.SideToMove;
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnGameStateChanged(object? sender, GameStateChangedEventArgs e)
        {
            GameState = e.NewState;
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            _currentTheme = e.NewTheme;
            OnPropertyChanged(nameof(CurrentTheme));
        }
    }

    /// <summary>
    /// 简单的命令实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }

    /// <summary>
    /// 泛型命令实现
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;
            
            // 处理可空值类型
            if (parameter == null && default(T) == null)
                return _canExecute(default);
            
            if (parameter == null)
                return _canExecute(default(T));
            
            // 类型转换
            T? typedParam;
            try
            {
                typedParam = (T?)parameter;
            }
            catch
            {
                return false;
            }
            
            return _canExecute(typedParam);
        }

        public void Execute(object? parameter)
        {
            // 处理可空值类型
            if (parameter == null && default(T) == null)
            {
                _execute(default);
                return;
            }
            
            T? typedParam = (parameter == null) ? default(T) : (T?)parameter;
            _execute(typedParam);
        }
    }
}
