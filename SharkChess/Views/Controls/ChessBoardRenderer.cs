using SharkChess.Models;
using SharkChess.Services;
using System.Windows;
using System.Windows.Media;

namespace SharkChess.Views.Controls
{
    /// <summary>
    /// 棋盘渲染器 - 负责棋盘背景、网格、文字等绘制
    /// </summary>
    internal class ChessBoardRenderer
    {
        private ThemeConfig _themeConfig;
        private DrawingGroup? _paperTextureCache;
        private double _cachedBoardWidth;
        private double _cachedBoardHeight;

        // 棋盘尺寸常量
        private const int FILES = 9;      // 纵线数
        private const int RANKS = 10;     // 横线数

        /// <summary>
        /// 初始化棋盘渲染器
        /// </summary>
        /// <param name="themeConfig">主题配置</param>
        public ChessBoardRenderer(ThemeConfig themeConfig)
        {
            _themeConfig = themeConfig;
        }

        /// <summary>
        /// 更新主题配置
        /// </summary>
        /// <param name="config">新的主题配置</param>
        public void UpdateTheme(ThemeConfig config)
        {
            _themeConfig = config;
            _paperTextureCache = null; // 清空纹理缓存
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void ClearCache()
        {
            _paperTextureCache = null;
        }

        /// <summary>
        /// 绘制棋盘背景
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="boardWidth">棋盘宽度</param>
        /// <param name="boardHeight">棋盘高度</param>
        public void DrawBoardBackground(DrawingContext dc, double boardWidth, double boardHeight)
        {
            var bgColor = SafeParseColor(_themeConfig.BoardBackground, Colors.Wheat);
            dc.DrawRectangle(new SolidColorBrush(bgColor), null, new Rect(0, 0, boardWidth, boardHeight));
        }

        /// <summary>
        /// 绘制宣纸纹理效果（国风版）
        /// 程序化生成细微噪点纹理
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="boardWidth">棋盘宽度</param>
        /// <param name="boardHeight">棋盘高度</param>
        public void DrawPaperTexture(DrawingContext dc, double boardWidth, double boardHeight)
        {
            // 检查是否需要重建缓存
            if (_paperTextureCache == null || 
                Math.Abs(_cachedBoardWidth - boardWidth) > 0.1 || 
                Math.Abs(_cachedBoardHeight - boardHeight) > 0.1)
            {
                _paperTextureCache = CreatePaperTexture(boardWidth, boardHeight);
                _cachedBoardWidth = boardWidth;
                _cachedBoardHeight = boardHeight;
            }
            
            dc.DrawDrawing(_paperTextureCache);
        }

        /// <summary>
        /// 创建宣纸纹理
        /// </summary>
        private DrawingGroup CreatePaperTexture(double boardWidth, double boardHeight)
        {
            var random = new Random(42); // 固定种子确保一致性
            var textureGroup = new DrawingGroup();
            
            using (var textureContext = textureGroup.Open())
            {
                // 绘制细微噪点
                int dotCount = 200; // 噪点数量
                for (int i = 0; i < dotCount; i++)
                {
                    double x = random.NextDouble() * boardWidth;
                    double y = random.NextDouble() * boardHeight;
                    
                    // 随机灰度值（浅灰色系）
                    byte gray = (byte)(200 + random.Next(40));
                    double opacity = 0.03 + random.NextDouble() * 0.05; // 3%-8% 透明度
                    
                    var dotColor = Color.FromArgb(
                        (byte)(opacity * 255),
                        gray, gray, gray
                    );
                    
                    var dotBrush = new SolidColorBrush(dotColor);
                    double dotSize = 1 + random.NextDouble() * 2;
                    
                    textureContext.DrawEllipse(dotBrush, null, new Point(x, y), dotSize, dotSize);
                }
                
                // 添加细微的纤维线条
                for (int i = 0; i < 15; i++)
                {
                    double startX = random.NextDouble() * boardWidth;
                    double startY = random.NextDouble() * boardHeight;
                    double length = 20 + random.NextDouble() * 40;
                    double angle = random.NextDouble() * Math.PI;
                    
                    double endX = startX + Math.Cos(angle) * length;
                    double endY = startY + Math.Sin(angle) * length;
                    
                    var fiberColor = Color.FromArgb(15, 180, 170, 150);
                    var fiberPen = new Pen(new SolidColorBrush(fiberColor), 0.5);
                    
                    textureContext.DrawLine(fiberPen, new Point(startX, startY), new Point(endX, endY));
                }
            }

            return textureGroup;
        }

        /// <summary>
        /// 绘制楚河汉界区域高亮（教学版）
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="margin">边距</param>
        /// <param name="cellSize">格子大小</param>
        public void DrawRiverHighlight(DrawingContext dc, double margin, double cellSize)
        {
            var highlightColor = SafeParseColor(_themeConfig.RiverAreaHighlightColor, Color.FromArgb(30, 135, 206, 235));
            var highlightBrush = new SolidColorBrush(highlightColor);
            
            // 楚河汉界区域：Rank 5-6 之间（横线 4 和 5 之间）
            double y1 = margin + 4 * cellSize; // 第5条横线
            double y2 = margin + 5 * cellSize; // 第6条横线
            
            double boardWidth = margin * 2 + 8 * cellSize;
            var riverRect = new Rect(margin, y1, boardWidth - 2 * margin, y2 - y1);
            dc.DrawRectangle(highlightBrush, null, riverRect);
        }

        /// <summary>
        /// 绘制九宫格区域高亮（教学版）
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="margin">边距</param>
        /// <param name="cellSize">格子大小</param>
        public void DrawPalaceHighlight(DrawingContext dc, double margin, double cellSize)
        {
            var highlightColor = SafeParseColor(_themeConfig.PalaceAreaHighlightColor, Color.FromArgb(30, 255, 215, 0));
            var highlightBrush = new SolidColorBrush(highlightColor);
            
            double palaceWidth = 2 * cellSize;
            double palaceHeight = 2 * cellSize;
            
            // 上方九宫格（黑方）：横线 0-2，纵线 3-5
            double topX = margin + 3 * cellSize;
            double topY = margin;
            
            var topPalaceRect = new Rect(topX, topY, palaceWidth, palaceHeight);
            dc.DrawRectangle(highlightBrush, null, topPalaceRect);
            
            // 下方九宫格（红方）：横线 7-9，纵线 3-5
            double bottomX = margin + 3 * cellSize;
            double bottomY = margin + 7 * cellSize;
            
            var bottomPalaceRect = new Rect(bottomX, bottomY, palaceWidth, palaceHeight);
            dc.DrawRectangle(highlightBrush, null, bottomPalaceRect);
        }

        /// <summary>
        /// 绘制所有落子点圆点标记（国风版）
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="margin">边距</param>
        /// <param name="cellSize">格子大小</param>
        public void DrawIntersectionDots(DrawingContext dc, double margin, double cellSize)
        {
            var dotColor = SafeParseColor(_themeConfig.IntersectionDotColor, Colors.Gray);
            var dotBrush = new SolidColorBrush(dotColor);
            double dotRadius = 2;
            
            // 遍历所有交叉点
            for (int rank = 0; rank < RANKS; rank++)
            {
                for (int file = 0; file < FILES; file++)
                {
                    // 跳过楚河汉界中间区域的点（无交叉线）
                    if (rank >= 4 && rank <= 5 && file > 0 && file < FILES - 1)
                    {
                        continue; // 中间竖线在楚河汉界处断开，无落子点
                    }
                    
                    double x = margin + file * cellSize;
                    double y = margin + rank * cellSize;
                    
                    dc.DrawEllipse(dotBrush, null, new Point(x, y), dotRadius, dotRadius);
                }
            }
        }

        /// <summary>
        /// 绘制棋盘格子
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="margin">边距</param>
        /// <param name="cellSize">格子大小</param>
        /// <param name="boardWidth">棋盘宽度</param>
        /// <param name="boardHeight">棋盘高度</param>
        public void DrawBoardGrid(DrawingContext dc, double margin, double cellSize, double boardWidth, double boardHeight)
        {
            var lineColor = SafeParseColor(_themeConfig.LineColor, Colors.SaddleBrown);
            var lineBrush = new SolidColorBrush(lineColor);
            var linePen = new Pen(lineBrush, _themeConfig.LineThickness);

            // 绘制横线
            for (int rank = 0; rank < RANKS; rank++)
            {
                double y = margin + rank * cellSize;
                dc.DrawLine(linePen, new Point(margin, y), new Point(boardWidth - margin, y));
            }

            // 绘制竖线（注意楚河汉界处中间断开）
            for (int file = 0; file < FILES; file++)
            {
                double x = margin + file * cellSize;

                if (file == 0 || file == FILES - 1)
                {
                    // 边线不断开
                    dc.DrawLine(linePen, new Point(x, margin), new Point(x, boardHeight - margin));
                }
                else
                {
                    // 中间的线在楚河汉界处断开（rank 4-5 之间）
                    double y1 = margin + 4 * cellSize; // 第5条横线
                    double y2 = margin + 5 * cellSize; // 第6条横线

                    // 上半部分
                    dc.DrawLine(linePen, new Point(x, margin), new Point(x, y1));
                    // 下半部分
                    dc.DrawLine(linePen, new Point(x, y2), new Point(x, boardHeight - margin));
                }
            }

            // 绘制落子点标记
            DrawIntersectionMarks(dc, margin, cellSize);
        }

        /// <summary>
        /// 绘制落子点标记
        /// </summary>
        private void DrawIntersectionMarks(DrawingContext dc, double margin, double cellSize)
        {
            var markColor = SafeParseColor(_themeConfig.IntersectionMarkColor, Colors.SaddleBrown);
            var markBrush = new SolidColorBrush(markColor);
            double markSize = 3;

            // 关键位置：炮位、兵卒位
            int[][] positions = new int[][]
            {
                // 炮位
                new[] { 1, 2 }, new[] { 7, 2 },
                new[] { 1, 7 }, new[] { 7, 7 },
                // 兵卒位
                new[] { 0, 3 }, new[] { 2, 3 }, new[] { 4, 3 }, new[] { 6, 3 }, new[] { 8, 3 },
                new[] { 0, 6 }, new[] { 2, 6 }, new[] { 4, 6 }, new[] { 6, 6 }, new[] { 8, 6 }
            };

            foreach (var pos in positions)
            {
                double x = margin + pos[0] * cellSize;
                double y = margin + pos[1] * cellSize;

                // 绘制小标记
                DrawCornerMark(dc, x, y, markBrush, markSize, pos[0]);
            }
        }

        /// <summary>
        /// 绘制角标记
        /// </summary>
        private void DrawCornerMark(DrawingContext dc, double x, double y, Brush brush, double size, int file)
        {
            double offset = 4;
            double gap = 2;

            var pen = new Pen(brush, 1);

            // 左上角
            if (file > 0)
            {
                dc.DrawLine(pen, new Point(x - offset - gap, y - gap), new Point(x - gap, y - gap));
                dc.DrawLine(pen, new Point(x - gap, y - gap), new Point(x - gap, y - offset - gap));
            }

            // 右上角
            if (file < FILES - 1)
            {
                dc.DrawLine(pen, new Point(x + gap, y - gap), new Point(x + offset + gap, y - gap));
                dc.DrawLine(pen, new Point(x + gap, y - gap), new Point(x + gap, y - offset - gap));
            }

            // 左下角
            if (file > 0)
            {
                dc.DrawLine(pen, new Point(x - offset - gap, y + gap), new Point(x - gap, y + gap));
                dc.DrawLine(pen, new Point(x - gap, y + gap), new Point(x - gap, y + offset + gap));
            }

            // 右下角
            if (file < FILES - 1)
            {
                dc.DrawLine(pen, new Point(x + gap, y + gap), new Point(x + offset + gap, y + gap));
                dc.DrawLine(pen, new Point(x + gap, y + gap), new Point(x + gap, y + offset + gap));
            }
        }

        /// <summary>
        /// 绘制九宫格
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="margin">边距</param>
        /// <param name="cellSize">格子大小</param>
        public void DrawPalace(DrawingContext dc, double margin, double cellSize)
        {
            var palaceColor = SafeParseColor(_themeConfig.PalaceLineColor, Colors.SaddleBrown);
            var palacePen = new Pen(new SolidColorBrush(palaceColor), _themeConfig.LineThickness);

            // 上方九宫格（黑方）
            DrawPalaceLines(dc, margin, cellSize, 3, 0, palacePen);

            // 下方九宫格（红方）
            DrawPalaceLines(dc, margin, cellSize, 3, 7, palacePen);
        }

        /// <summary>
        /// 绘制九宫格斜线
        /// </summary>
        private void DrawPalaceLines(DrawingContext dc, double margin, double cellSize, int startFile, int startRank, Pen pen)
        {
            // 九宫格斜线
            double x1 = margin + startFile * cellSize;
            double y1 = margin + startRank * cellSize;
            double x2 = margin + (startFile + 2) * cellSize;
            double y2 = margin + (startRank + 2) * cellSize;

            dc.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
            dc.DrawLine(pen, new Point(x2, y1), new Point(x1, y2));
        }

        /// <summary>
        /// 绘制楚河汉界文字
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="margin">边距</param>
        /// <param name="cellSize">格子大小</param>
        /// <param name="boardWidth">棋盘宽度</param>
        public void DrawRiver(DrawingContext dc, double margin, double cellSize, double boardWidth)
        {
            var textColor = SafeParseColor(_themeConfig.RiverTextColor, Colors.SaddleBrown);
            var textBrush = new SolidColorBrush(textColor);

            // 计算文字位置
            double y = margin + 4.5 * cellSize;
            double fontSize = _themeConfig.RiverTextSize;

            // 使用配置的字体（支持书法字体如华文行楷）
            var riverTypeface = new Typeface(_themeConfig.RiverFontFamily);

            var formattedTextLeft = new FormattedText(
                "楚河",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                riverTypeface,
                fontSize,
                textBrush,
                1.0);

            var formattedTextRight = new FormattedText(
                "汉界",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                riverTypeface,
                fontSize,
                textBrush,
                1.0);

            // 楚河（左侧）
            dc.DrawText(formattedTextLeft, new Point(margin + cellSize * 0.5, y - fontSize / 2));

            // 汉界（右侧）
            dc.DrawText(formattedTextRight, new Point(boardWidth - margin - cellSize * 1.5, y - fontSize / 2));
        }

        /// <summary>
        /// 绘制坐标标注
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="margin">边距</param>
        /// <param name="cellSize">格子大小</param>
        /// <param name="boardHeight">棋盘高度</param>
        /// <param name="typeface">字体</param>
        public void DrawCoordinates(DrawingContext dc, double margin, double cellSize, double boardHeight, Typeface typeface)
        {
            if (!_themeConfig.ShowCoordinates) return;

            var coordColor = SafeParseColor(_themeConfig.CoordinateColor, Colors.SaddleBrown);
            var coordBrush = new SolidColorBrush(coordColor);
            double fontSize = 12;

            // 红方坐标（下方，一至九路）
            string[] redFiles = { "九", "八", "七", "六", "五", "四", "三", "二", "一" };
            for (int i = 0; i < 9; i++)
            {
                double x = margin + i * cellSize;
                double y = boardHeight - margin + 5;

                var text = new FormattedText(
                    redFiles[i],
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    coordBrush,
                    1.0);

                dc.DrawText(text, new Point(x - text.Width / 2, y));
            }

            // 黑方坐标（上方，1 至 9 路）
            string[] blackFiles = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            for (int i = 0; i < 9; i++)
            {
                double x = margin + i * cellSize;
                double y = margin - fontSize - 5;

                var text = new FormattedText(
                    blackFiles[i],
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    coordBrush,
                    1.0);

                dc.DrawText(text, new Point(x - text.Width / 2, y));
            }

            // 教学版：添加左右两侧的线数标注（1-10）
            if (_themeConfig.ShowEdgeCoordinates)
            {
                DrawEdgeRankNumbers(dc, margin, cellSize, coordBrush, fontSize, typeface);
            }
        }

        /// <summary>
        /// 绘制边缘线数标注（教学版）
        /// 左右两侧标注 1-10 线数
        /// </summary>
        private void DrawEdgeRankNumbers(DrawingContext dc, double margin, double cellSize, 
                                         SolidColorBrush brush, double fontSize, Typeface typeface)
        {
            // 左侧标注（从下到上：1-10）
            for (int rank = 0; rank < RANKS; rank++)
            {
                double y = margin + rank * cellSize;
                double x = margin - fontSize - 8;

                // 线数从黑方底线开始为 1
                int rankNumber = RANKS - rank;

                var text = new FormattedText(
                    rankNumber.ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    brush,
                    1.0);

                dc.DrawText(text, new Point(x - text.Width / 2, y - text.Height / 2));
            }

            // 右侧标注（从下到上：1-10）
            for (int rank = 0; rank < RANKS; rank++)
            {
                double y = margin + rank * cellSize;
                double x = margin + 8 * cellSize + margin + 8; // boardWidth = margin * 2 + 8 * cellSize

                // 线数从黑方底线开始为 1
                int rankNumber = RANKS - rank;

                var text = new FormattedText(
                    rankNumber.ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    brush,
                    1.0);

                dc.DrawText(text, new Point(x - text.Width / 2, y - text.Height / 2));
            }
        }

        /// <summary>
        /// 绘制选中状态和合法走法提示
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="margin">边距</param>
        /// <param name="cellSize">格子大小</param>
        /// <param name="gameService">游戏服务</param>
        public void DrawSelection(DrawingContext dc, double margin, double cellSize, IGameService? gameService)
        {
            if (gameService == null) return;

            // 绘制合法走法提示
            var highlightColor = SafeParseColor(_themeConfig.LegalMoveHint, Color.FromArgb(150, 50, 205, 50));
            var highlightBrush = new SolidColorBrush(highlightColor);

            foreach (var target in gameService.LegalMoves)
            {
                double x = margin + (target.File - 1) * cellSize;
                double y = margin + (target.Rank - 1) * cellSize;

                // 绘制提示点
                dc.DrawEllipse(highlightBrush, null, new Point(x, y), 8, 8);
            }

            // 绘制选中高亮
            if (gameService.SelectedSquare.HasValue)
            {
                var selected = gameService.SelectedSquare.Value;
                double x = margin + (selected.File - 1) * cellSize;
                double y = margin + (selected.Rank - 1) * cellSize;

                var selectedColor = SafeParseColor(_themeConfig.SelectedHighlight, Color.FromArgb(80, 255, 255, 0));
                var selectedBrush = new SolidColorBrush(selectedColor);

                // 绘制选中框
                double halfSize = cellSize * 0.45;
                var rect = new Rect(x - halfSize, y - halfSize, halfSize * 2, halfSize * 2);
                dc.DrawRectangle(selectedBrush, null, rect);
            }
        }

        /// <summary>
        /// 安全解析颜色（带异常处理）
        /// </summary>
        /// <param name="colorString">颜色字符串</param>
        /// <param name="fallback">回退颜色</param>
        /// <returns>解析后的颜色或回退颜色</returns>
        private static Color SafeParseColor(string? colorString, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(colorString))
            {
                System.Diagnostics.Debug.WriteLine($"[ChessBoardRenderer] 颜色字符串为空，使用默认值：{fallback}");
                return fallback;
            }

            try
            {
                return (Color)ColorConverter.ConvertFromString(colorString);
            }
            catch (FormatException)
            {
                System.Diagnostics.Debug.WriteLine($"[ChessBoardRenderer] 颜色格式错误：{colorString}，使用默认值：{fallback}");
                return fallback;
            }
            catch (ArgumentException)
            {
                System.Diagnostics.Debug.WriteLine($"[ChessBoardRenderer] 颜色参数错误：{colorString}，使用默认值：{fallback}");
                return fallback;
            }
        }
    }
}
