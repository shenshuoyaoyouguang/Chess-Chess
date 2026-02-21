using SharkChess.Models;
using SharkChess.Services;
using System.Windows;
using System.Windows.Media;

// 使用别名避免与 ChessPiece 命名空间冲突
using ChessPieceModel = SharkChess.Models.ChessPiece;

namespace SharkChess.Views.Controls
{
    /// <summary>
    /// 棋子渲染器 - 负责棋子的绘制逻辑
    /// </summary>
    internal class ChessPieceRenderer
    {
        private ThemeConfig _themeConfig;
        private readonly Typeface _chineseTypeface;
        
        // 渐变画刷缓存
        private readonly Dictionary<Color, RadialGradientBrush> _gradientBrushCache = new();
        
        // 动画画刷缓存
        private readonly Dictionary<Color, RadialGradientBrush> _animationGradientCache = new();

        // 预创建的常用画刷（性能优化：避免每次绘制时创建）
        private SolidColorBrush _redBorderBrush = null!;
        private SolidColorBrush _blackBorderBrush = null!;
        private SolidColorBrush _shadowBrush = null!;
        private SolidColorBrush _softShadowBrush = null!;
        
        // 预创建的常用 Pen（性能优化）
        private Pen? _redBorderPen;
        private Pen? _blackBorderPen;

        /// <summary>
        /// 初始化棋子渲染器
        /// </summary>
        /// <param name="themeConfig">主题配置</param>
        /// <param name="chineseTypeface">中文字体</param>
        public ChessPieceRenderer(ThemeConfig themeConfig, Typeface chineseTypeface)
        {
            _themeConfig = themeConfig;
            _chineseTypeface = chineseTypeface;
            
            // 预创建常用画刷
            InitializeCommonBrushes();
        }

        /// <summary>
        /// 初始化常用画刷（性能优化：避免每次绘制时创建）
        /// </summary>
        private void InitializeCommonBrushes()
        {
            // 阴影画刷（固定颜色）
            _shadowBrush = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0));
            _softShadowBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
            
            // 边框画刷（会在主题更新时重新创建）
            UpdateBorderBrushes();
        }

        /// <summary>
        /// 更新边框画刷（主题变更时调用）
        /// </summary>
        private void UpdateBorderBrushes()
        {
            var redBorderColor = SafeParseColor(_themeConfig.RedPieceBorder, Colors.Brown);
            var blackBorderColor = SafeParseColor(_themeConfig.BlackPieceBorder, Colors.Black);
            
            _redBorderBrush = new SolidColorBrush(redBorderColor);
            _blackBorderBrush = new SolidColorBrush(blackBorderColor);
            
            // 重新创建边框 Pen
            _redBorderPen = new Pen(_redBorderBrush, _themeConfig.OuterRingThickness);
            _blackBorderPen = new Pen(_blackBorderBrush, _themeConfig.OuterRingThickness);
        }

        /// <summary>
        /// 更新主题配置
        /// </summary>
        /// <param name="config">新的主题配置</param>
        public void UpdateTheme(ThemeConfig config)
        {
            _themeConfig = config;
            ClearCache();
            UpdateBorderBrushes();
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void ClearCache()
        {
            _gradientBrushCache.Clear();
            _animationGradientCache.Clear();
        }

        /// <summary>
        /// 绘制棋子（精美立体风格：干净渐变 + 强立体感）
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="piece">棋子模型</param>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        public void DrawPiece(DrawingContext dc, ChessPieceModel piece, Point center, double radius)
        {
            // 确定颜色
            Color bgColor, borderColor, textColor;
            if (piece.Color == PieceColor.Red)
            {
                bgColor = SafeParseColor(_themeConfig.RedPieceBackground, Colors.IndianRed);
                borderColor = SafeParseColor(_themeConfig.RedPieceBorder, Colors.Brown);
                textColor = SafeParseColor(_themeConfig.RedPieceText, Colors.White);
            }
            else
            {
                bgColor = SafeParseColor(_themeConfig.BlackPieceBackground, Colors.SaddleBrown);
                borderColor = SafeParseColor(_themeConfig.BlackPieceBorder, Colors.Black);
                textColor = SafeParseColor(_themeConfig.BlackPieceText, Colors.White);
            }

            // ========== 第 1 层：阴影（强立体感） ==========
            if (_themeConfig.EnablePieceShadow)
            {
                // 主阴影（较大偏移）
                dc.DrawEllipse(_shadowBrush, null,
                    new Point(center.X + 3, center.Y + 4), radius * 1.02, radius * 1.02);
                
                // 柔和扩散阴影
                dc.DrawEllipse(_softShadowBrush, null,
                    new Point(center.X + 2, center.Y + 3), radius * 1.05, radius * 1.05);
            }

            // ========== 第 2 层：外圈边框（清晰锐利） ==========
            // 使用预创建的 Pen 或临时创建（当厚度变化时）
            Pen borderPen;
            if (piece.Color == PieceColor.Red)
            {
                if (_redBorderPen?.Thickness == _themeConfig.OuterRingThickness)
                {
                    borderPen = _redBorderPen;
                }
                else
                {
                    borderPen = new Pen(_redBorderBrush, _themeConfig.OuterRingThickness);
                }
            }
            else
            {
                if (_blackBorderPen?.Thickness == _themeConfig.OuterRingThickness)
                {
                    borderPen = _blackBorderPen;
                }
                else
                {
                    borderPen = new Pen(_blackBorderBrush, _themeConfig.OuterRingThickness);
                }
            }
            dc.DrawEllipse(null, borderPen, center, radius, radius);

            // ========== 第 3 层：径向渐变背景（纯净渐变，不发灰） ==========
            var gradientBrush = GetOrCreateGradientBrush(bgColor);
            dc.DrawEllipse(gradientBrush, null, center, radius - _themeConfig.OuterRingThickness / 2, radius - _themeConfig.OuterRingThickness / 2);

            // ========== 第 4 层：统一简约装饰（单环 + 四点） ==========
            if (_themeConfig.EnablePieceDecoration)
            {
                DrawSimpleDecoration(dc, center, radius, borderColor);
            }

            // ========== 第 5 层：汉字（清晰醒目） ==========
            DrawPieceText(dc, piece.ChineseName, center, textColor, radius);

            // ========== 第 6 层：高光效果（强立体感） ==========
            if (_themeConfig.EnablePieceHighlight)
            {
                DrawPieceHighlight(dc, center, radius);
            }
        }

        /// <summary>
        /// 绘制 3D 斜角效果棋子
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="piece">棋子模型</param>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <param name="progress3D">3D 进度（0=2D 平面，1=完全 3D）</param>
        /// <param name="opacity">透明度</param>
        public void DrawPiece3D(DrawingContext dc, ChessPieceModel piece, Point center, double radius, 
                                double progress3D, double opacity = 1.0)
        {
            // 确定颜色
            Color bgColor, borderColor, textColor;
            if (piece.Color == PieceColor.Red)
            {
                bgColor = SafeParseColor(_themeConfig.RedPieceBackground, Colors.IndianRed);
                borderColor = SafeParseColor(_themeConfig.RedPieceBorder, Colors.Brown);
                textColor = SafeParseColor(_themeConfig.RedPieceText, Colors.White);
            }
            else
            {
                bgColor = SafeParseColor(_themeConfig.BlackPieceBackground, Colors.SaddleBrown);
                borderColor = SafeParseColor(_themeConfig.BlackPieceBorder, Colors.Black);
                textColor = SafeParseColor(_themeConfig.BlackPieceText, Colors.White);
            }
            
            // 获取装饰颜色
            var decorationColor = SafeParseColor(_themeConfig.DecorationColor, Colors.Gray);

            // 应用透明度
            dc.PushOpacity(opacity);

            // 1. 绘制外层阴影（模拟立体高度）
            if (progress3D > 0)
            {
                double shadowOffset = ChessBoardControl.ShadowOffsetFactor * progress3D;
                double shadowRadius = radius + ChessBoardControl.ShadowRadiusFactor * progress3D;
                var shadowColor = Color.FromArgb((byte)(60 * progress3D), 0, 0, 0);
                dc.DrawEllipse(new SolidColorBrush(shadowColor), null,
                               new Point(center.X + shadowOffset, center.Y + shadowOffset),
                               shadowRadius, shadowRadius);
            }

            // 2. 绘制斜角边缘层（深色边缘）
            if (progress3D > 0.2)
            {
                double edgeRadius = radius + ChessBoardControl.EdgeRadiusFactor * progress3D;
                var edgeColor = Color.FromArgb((byte)(80 * progress3D),
                    (byte)Math.Max(0, bgColor.R - 40),
                    (byte)Math.Max(0, bgColor.G - 40),
                    (byte)Math.Max(0, bgColor.B - 40));
                dc.DrawEllipse(new SolidColorBrush(edgeColor), null, center, edgeRadius, edgeRadius);
            }

            // 3. 绘制棋子主体（使用径向渐变模拟立体效果）
            if (progress3D > 0.3)
            {
                var gradientBrush = GetOrCreateAnimationGradientBrush(bgColor, progress3D);
                dc.DrawEllipse(gradientBrush, 
                               new Pen(new SolidColorBrush(borderColor), 2 + progress3D), 
                               center, radius, radius);
            }
            else
            {
                // 2D 模式：普通绘制
                dc.DrawEllipse(new SolidColorBrush(bgColor), 
                               new Pen(new SolidColorBrush(borderColor), 2), 
                               center, radius, radius);
            }

            // 4. 绘制顶部高光（模拟光泽表面）
            if (progress3D > 0.5)
            {
                double highlightRadius = radius * 0.5;
                var highlightColor = Color.FromArgb((byte)(80 * progress3D), 255, 255, 255);
                dc.DrawEllipse(new SolidColorBrush(highlightColor), null,
                               new Point(center.X - radius * 0.25, center.Y - radius * 0.25),
                               highlightRadius, highlightRadius * 0.7);
            }

            // 4.5 绘制内圈装饰线和装饰纹样（如果启用）
            if (_themeConfig.EnablePieceDecoration)
            {
                double innerRingRadius = radius - _themeConfig.OuterRingThickness - 3;
                var innerPen = new Pen(new SolidColorBrush(borderColor), _themeConfig.InnerRingThickness);
                dc.DrawEllipse(null, innerPen, center, innerRingRadius, innerRingRadius);

                // 绘制简约装饰纹样
                double decorationRadius = innerRingRadius - 5;
                DrawSimpleDecoration(dc, center, decorationRadius, decorationColor);
            }

            // 5. 绘制棋子文字（轻微缩放效果）
            double fontSize = _themeConfig.PieceFontSize;
            if (progress3D > 0)
            {
                fontSize *= (1 + 0.05 * progress3D); // 3D 时文字略微放大
            }

            var text = new FormattedText(
                piece.ChineseName,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _chineseTypeface,
                fontSize,
                new SolidColorBrush(textColor),
                1.0);

            dc.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));

            dc.Pop();
        }

        /// <summary>
        /// 绘制统一简约装饰（单环 + 四点）
        /// </summary>
        private void DrawSimpleDecoration(DrawingContext dc, Point center, double radius, Color decorationColor)
        {
            double innerRingRadius = radius * 0.82;
            double dotRadius = 2.2;
            double dotOffset = radius * 0.82;
            
            // 使用带透明度的颜色创建画笔
            var ringColor = Color.FromArgb((byte)(decorationColor.A * 0.6), decorationColor.R, decorationColor.G, decorationColor.B);
            var decorationPen = new Pen(new SolidColorBrush(ringColor), 1.0);

            // 单层细线圆环
            dc.DrawEllipse(null, decorationPen, center, innerRingRadius, innerRingRadius);

            // 四个简约圆点点缀（上下左右）
            var dotColor = Color.FromArgb((byte)(decorationColor.A * 0.5), decorationColor.R, decorationColor.G, decorationColor.B);
            var dotBrush = new SolidColorBrush(dotColor);
            
            // 上
            dc.DrawEllipse(dotBrush, null, new Point(center.X, center.Y - dotOffset), dotRadius, dotRadius);
            // 下
            dc.DrawEllipse(dotBrush, null, new Point(center.X, center.Y + dotOffset), dotRadius, dotRadius);
            // 左
            dc.DrawEllipse(dotBrush, null, new Point(center.X - dotOffset, center.Y), dotRadius, dotRadius);
            // 右
            dc.DrawEllipse(dotBrush, null, new Point(center.X + dotOffset, center.Y), dotRadius, dotRadius);
        }

        /// <summary>
        /// 绘制棋子文字（带阴影）
        /// </summary>
        private void DrawPieceText(DrawingContext dc, string text, Point center, Color textColor, double radius)
        {
            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _chineseTypeface,
                _themeConfig.PieceFontSize,
                new SolidColorBrush(textColor),
                1.0);

            // 文字阴影（增强可读性）
            var textShadow = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _chineseTypeface,
                _themeConfig.PieceFontSize,
                new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
                1.0);
            
            dc.DrawText(textShadow, new Point(center.X - formattedText.Width / 2 + 1, center.Y - formattedText.Height / 2 + 1));
            dc.DrawText(formattedText, new Point(center.X - formattedText.Width / 2, center.Y - formattedText.Height / 2));
        }

        /// <summary>
        /// 绘制棋子高光效果
        /// </summary>
        private void DrawPieceHighlight(DrawingContext dc, Point center, double radius)
        {
            // 主高光（顶部）
            var highlightBrush = new RadialGradientBrush();
            highlightBrush.GradientStops = new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(70, 255, 255, 255), 0.0),
                new GradientStop(Color.FromArgb(20, 255, 255, 255), 0.5),
                new GradientStop(Colors.Transparent, 1.0)
            };
            highlightBrush.GradientOrigin = new Point(0.3, 0.2);
            highlightBrush.Center = center;
            highlightBrush.RadiusX = radius * 0.4;
            highlightBrush.RadiusY = radius * 0.35;

            dc.DrawEllipse(highlightBrush, null, center, radius, radius);
            
            // 边缘反光（底部）
            var rimLightBrush = new RadialGradientBrush();
            rimLightBrush.GradientStops = new GradientStopCollection
            {
                new GradientStop(Colors.Transparent, 0.6),
                new GradientStop(Color.FromArgb(25, 255, 255, 255), 0.85),
                new GradientStop(Colors.Transparent, 1.0)
            };
            rimLightBrush.GradientOrigin = new Point(0.5, 0.7);
            rimLightBrush.Center = center;
            rimLightBrush.RadiusX = radius;
            rimLightBrush.RadiusY = radius;

            dc.DrawEllipse(rimLightBrush, null, center, radius, radius);
        }

        /// <summary>
        /// 获取或创建渐变画刷（缓存）
        /// </summary>
        private RadialGradientBrush GetOrCreateGradientBrush(Color bgColor)
        {
            if (_gradientBrushCache.TryGetValue(bgColor, out var brush))
                return brush;

            // 优化：使用 HSV 调整，而非简单加减 RGB
            Color lightColor = AdjustColorPure(bgColor, 1.06, 0.98);   // 亮度 +6%，饱和度 -2%
            Color darkColor = AdjustColorPure(bgColor, 0.94, 1.02);    // 亮度 -6%，饱和度 +2%
            
            var gradientStops = new GradientStopCollection
            {
                new GradientStop(lightColor, 0.0),
                new GradientStop(bgColor, 0.45),
                new GradientStop(darkColor, 0.85),
                new GradientStop(darkColor, 1.0)
            };

            var newBrush = new RadialGradientBrush(gradientStops)
            {
                GradientOrigin = new Point(0.35, 0.35)
            };

            _gradientBrushCache[bgColor] = newBrush;
            return newBrush;
        }

        /// <summary>
        /// 获取或创建 3D 动画渐变画刷（缓存）
        /// </summary>
        private RadialGradientBrush GetOrCreateAnimationGradientBrush(Color bgColor, double progress3D)
        {
            // 使用颜色 + 进度作为缓存键
            var cacheKey = Color.FromArgb(
                bgColor.A,
                (byte)(bgColor.R * progress3D),
                (byte)(bgColor.G * progress3D),
                (byte)(bgColor.B * progress3D)
            );

            if (_animationGradientCache.TryGetValue(cacheKey, out var brush))
                return brush;

            // 创建径向渐变（从左上到右下，模拟光照）
            var lightColor = Color.FromArgb(255,
                (byte)Math.Min(255, bgColor.R + 30),
                (byte)Math.Min(255, bgColor.G + 30),
                (byte)Math.Min(255, bgColor.B + 30));
            var darkColor = Color.FromArgb(255,
                (byte)Math.Max(0, bgColor.R - 20),
                (byte)Math.Max(0, bgColor.G - 20),
                (byte)Math.Max(0, bgColor.B - 20));

            var gradientStops = new GradientStopCollection
            {
                new GradientStop(lightColor, 0.0),
                new GradientStop(bgColor, 0.4),
                new GradientStop(darkColor, 1.0)
            };

            var newBrush = new RadialGradientBrush(gradientStops)
            {
                GradientOrigin = new Point(0.35, 0.35) // 光源在左上方
            };

            _animationGradientCache[cacheKey] = newBrush;
            return newBrush;
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
                System.Diagnostics.Debug.WriteLine($"[ChessPieceRenderer] 颜色字符串为空，使用默认值：{fallback}");
                return fallback;
            }

            try
            {
                return (Color)ColorConverter.ConvertFromString(colorString);
            }
            catch (FormatException)
            {
                System.Diagnostics.Debug.WriteLine($"[ChessPieceRenderer] 颜色格式错误：{colorString}，使用默认值：{fallback}");
                return fallback;
            }
            catch (ArgumentException)
            {
                System.Diagnostics.Debug.WriteLine($"[ChessPieceRenderer] 颜色参数错误：{colorString}，使用默认值：{fallback}");
                return fallback;
            }
        }

        /// <summary>
        /// 纯净颜色调整（保持饱和度，避免发灰）
        /// </summary>
        private static Color AdjustColorPure(Color color, double brightnessFactor, double saturationFactor)
        {
            // 转换为 HSV
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            double s = max == 0 ? 0 : delta / max;
            double v = max;

            // 调整亮度和饱和度
            v = Math.Clamp(v * brightnessFactor, 0, 1);
            s = Math.Clamp(s * saturationFactor, 0, 1);

            // 转回 RGB
            double c = v * s;
            double x = c * (1 - Math.Abs((h * 6) % 2 - 1));
            double m = v - c;

            double r1, g1, b1;
            if (s == 0)
            {
                r1 = g1 = b1 = v;
            }
            else if (delta == 0)
            {
                r1 = g1 = b1 = v;
            }
            else
            {
                // 保持原色相
                if (max == r)
                {
                    if (min == g) { r1 = c; g1 = 0; b1 = x; }
                    else { r1 = x; g1 = 0; b1 = c; }
                }
                else if (max == g)
                {
                    if (min == b) { r1 = x; g1 = c; b1 = 0; }
                    else { r1 = 0; g1 = c; b1 = x; }
                }
                else
                {
                    if (min == r) { r1 = 0; g1 = x; b1 = c; }
                    else { r1 = x; g1 = c; b1 = 0; }
                }
            }

            return Color.FromArgb(
                color.A,
                (byte)Math.Round((r1 + m) * 255),
                (byte)Math.Round((g1 + m) * 255),
                (byte)Math.Round((b1 + m) * 255)
            );
        }

        /// <summary>
        /// 绘制带将军闪烁效果的棋子
        /// </summary>
        /// <param name="dc">绘图上下文</param>
        /// <param name="piece">棋子模型</param>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <param name="flashAlpha">闪烁透明度（0-1，0=完全透明，1=完全不透明）</param>
        public void DrawPieceWithCheckFlash(DrawingContext dc, ChessPieceModel piece, Point center, double radius, double flashAlpha)
        {
            // 确定颜色
            Color bgColor, borderColor, textColor;
            if (piece.Color == PieceColor.Red)
            {
                bgColor = SafeParseColor(_themeConfig.RedPieceBackground, Colors.IndianRed);
                borderColor = SafeParseColor(_themeConfig.RedPieceBorder, Colors.Brown);
                textColor = SafeParseColor(_themeConfig.RedPieceText, Colors.White);
            }
            else
            {
                bgColor = SafeParseColor(_themeConfig.BlackPieceBackground, Colors.SaddleBrown);
                borderColor = SafeParseColor(_themeConfig.BlackPieceBorder, Colors.Black);
                textColor = SafeParseColor(_themeConfig.BlackPieceText, Colors.White);
            }

            // 应用闪烁透明度
            dc.PushOpacity(flashAlpha);

            // ========== 第 1 层：阴影（强立体感） ==========
            if (_themeConfig.EnablePieceShadow)
            {
                // 主阴影（较大偏移）
                dc.DrawEllipse(_shadowBrush, null,
                    new Point(center.X + 3, center.Y + 4), radius * 1.02, radius * 1.02);
                
                // 柔和扩散阴影
                dc.DrawEllipse(_softShadowBrush, null,
                    new Point(center.X + 2, center.Y + 3), radius * 1.05, radius * 1.05);
            }

            // ========== 第 2 层：外圈边框（清晰锐利，红色闪烁） ==========
            double outerRingThickness = _themeConfig.OuterRingThickness;
            // 将军状态下边框变为红色闪烁
            var flashBorderColor = Color.FromArgb(
                (byte)(borderColor.A * flashAlpha),
                (byte)Math.Min(255, borderColor.R + (byte)(100 * (1 - flashAlpha))),
                borderColor.G,
                borderColor.B
            );
            var borderPen = new Pen(new SolidColorBrush(flashBorderColor), outerRingThickness);
            dc.DrawEllipse(null, borderPen, center, radius, radius);

            // ========== 第 3 层：径向渐变背景（纯净渐变，不发灰） ==========
            var gradientBrush = GetOrCreateGradientBrush(bgColor);
            dc.DrawEllipse(gradientBrush, null, center, radius - outerRingThickness / 2, radius - outerRingThickness / 2);

            // ========== 第 4 层：统一简约装饰（单环 + 四点） ==========
            if (_themeConfig.EnablePieceDecoration)
            {
                DrawSimpleDecoration(dc, center, radius, borderColor);
            }

            // ========== 第 5 层：汉字（清晰醒目） ==========
            DrawPieceText(dc, piece.ChineseName, center, textColor, radius);

            // ========== 第 6 层：高光效果（强立体感） ==========
            if (_themeConfig.EnablePieceHighlight)
            {
                DrawPieceHighlight(dc, center, radius);
            }

            // ========== 第 7 层：红色光晕（将军特效） ==========
            if (flashAlpha > 0.3)
            {
                DrawCheckGlow(dc, center, radius, flashAlpha);
            }

            dc.Pop();
        }

        /// <summary>
        /// 绘制将军红色光晕效果
        /// </summary>
        private void DrawCheckGlow(DrawingContext dc, Point center, double radius, double intensity)
        {
            // 外层红色光晕
            var glowBrush = new RadialGradientBrush();
            var glowColor = Color.FromArgb(
                (byte)(100 * intensity),
                255,
                0,
                0
            );
            var transparentColor = Colors.Transparent;
            
            glowBrush.GradientStops = new GradientStopCollection
            {
                new GradientStop(glowColor, 0.0),
                new GradientStop(Color.FromArgb((byte)(50 * intensity), 255, 0, 0), 0.5),
                new GradientStop(transparentColor, 1.0)
            };
            glowBrush.GradientOrigin = new Point(0.5, 0.5);
            glowBrush.Center = center;
            glowBrush.RadiusX = radius * 1.4;
            glowBrush.RadiusY = radius * 1.4;

            dc.DrawEllipse(glowBrush, null, center, radius * 1.4, radius * 1.4);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            // 注意：WPF SolidColorBrush 不需要显式释放
            // 预创建的画刷会由 WPF 框架管理生命周期
            
            // 清空缓存
            ClearCache();
        }
    }
}
