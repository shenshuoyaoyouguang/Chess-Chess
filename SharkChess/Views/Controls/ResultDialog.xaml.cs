using System.Windows;
using Microsoft.Win32;
using SharkChess.Models;
using SharkChess.Services;
using System.IO;
using System.Text;

namespace SharkChess.Views.Controls
{
    /// <summary>
    /// 胜负结果弹窗
    /// </summary>
    public partial class ResultDialog : Window
    {
        private readonly GameEndedEventArgs _gameResult;
        private readonly IGameService _gameService;

        public ResultDialog(Window owner, GameEndedEventArgs gameResult, IGameService gameService)
        {
            InitializeComponent();
            
            Owner = owner;
            _gameResult = gameResult;
            _gameService = gameService;
            
            // 初始化弹窗内容
            InitializeContent();
        }

        private void InitializeContent()
        {
            // 设置结果文本和颜色
            ResultText.Text = _gameResult.ResultDescription;
            ReasonText.Text = _gameResult.ReasonDescription;

            // 根据结果设置颜色
            ResultText.Foreground = _gameResult.Result switch
            {
                GameState.RedWin => (System.Windows.Media.Brush)FindResource("RedWinColor"),
                GameState.BlackWin => (System.Windows.Media.Brush)FindResource("BlackWinColor"),
                GameState.Draw => (System.Windows.Media.Brush)FindResource("DrawColor"),
                _ => System.Windows.Media.Brushes.White
            };

            // 设置时间信息
            RedTimeText.Text = _gameResult.RedTimeFormatted;
            BlackTimeText.Text = _gameResult.BlackTimeFormatted;

            // 设置步数信息
            MovesText.Text = $"共 {_gameResult.TotalMoves} 步";
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 保存棋谱按钮点击事件
        /// </summary>
        private void SaveGame_Click(object sender, RoutedEventArgs e)
        {
            SaveGameAsPgn();
        }

        /// <summary>
        /// 再来一局按钮点击事件
        /// </summary>
        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Tag = "NewGame";
            Close();
        }

        /// <summary>
        /// 返回大厅按钮点击事件
        /// </summary>
        private void ReturnToLobby_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Tag = "ReturnToLobby";
            Close();
        }

        /// <summary>
        /// 保存棋谱为 PGN 格式
        /// </summary>
        private void SaveGameAsPgn()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PGN 棋谱文件|*.pgn|所有文件|*.*",
                Title = "保存棋谱",
                DefaultExt = "pgn",
                FileName = $"SharkChess_{DateTime.Now:yyyyMMdd_HHmmss}.pgn"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var pgnContent = GeneratePgnContent();
                    File.WriteAllText(dialog.FileName, pgnContent, Encoding.UTF8);
                    MessageBox.Show($"棋谱已保存到：\n{dialog.FileName}", "保存成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败：{ex.Message}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 生成 PGN 格式内容
        /// </summary>
        private string GeneratePgnContent()
        {
            var sb = new StringBuilder();

            // PGN 标签
            sb.AppendLine($"[Event \"SharkChess Game\"]");
            sb.AppendLine($"[Site \"Local\"]");
            sb.AppendLine($"[Date \"{DateTime.Now:yyyy.MM.dd}\"]");
            sb.AppendLine($"[Round \"1\"]");
            sb.AppendLine($"[White \"红方\"]");
            sb.AppendLine($"[Black \"黑方\"]");
            
            // 结果
            string result = _gameResult.Result switch
            {
                GameState.RedWin => "1-0",
                GameState.BlackWin => "0-1",
                GameState.Draw => "1/2-1/2",
                _ => "*"
            };
            sb.AppendLine($"[Result \"{result}\"]");
            
            // 对局时长
            sb.AppendLine($"[WhiteTime \"{_gameResult.RedTimeFormatted}\"]");
            sb.AppendLine($"[BlackTime \"{_gameResult.BlackTimeFormatted}\"]");
            
            // 获胜原因
            sb.AppendLine($"[Termination \"{_gameResult.ReasonDescription}\"]");
            
            sb.AppendLine();

            // 走棋记录（简化版，使用 UCI 格式）
            var history = _gameService.MoveHistory;
            for (int i = 0; i < history.Count; i += 2)
            {
                int moveNumber = (i / 2) + 1;
                sb.Append($"{moveNumber}. {history[i].ToUCI()}");
                
                if (i + 1 < history.Count)
                {
                    sb.Append($" {history[i + 1].ToUCI()}");
                }
                
                sb.AppendLine();
            }

            sb.AppendLine(result);

            return sb.ToString();
        }
    }
}
