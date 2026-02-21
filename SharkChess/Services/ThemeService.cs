using System.Diagnostics;
using System.IO;
using System.Text.Json;
using SharkChess.Models;

namespace SharkChess.Services
{
    /// <summary>
    /// 主题服务实现
    /// </summary>
    public class ThemeService : IThemeService
    {
        private const string ThemeConfigFile = "theme.config.json";
        private BoardTheme _currentTheme;
        private BoardTheme _savedTheme;

        public ThemeService()
        {
            _currentTheme = BoardTheme.Standard;
            _savedTheme = BoardTheme.Standard;
        }

        public BoardTheme CurrentTheme => _currentTheme;

        public BoardTheme SavedTheme => _savedTheme;

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public void SetTheme(BoardTheme theme)
        {
            if (_currentTheme == theme) return;

            _currentTheme = theme;
            var config = GetThemeConfig(theme);

            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
            {
                NewTheme = theme,
                Config = config
            });
        }

        public ThemeConfig GetThemeConfig(BoardTheme theme)
        {
            return theme switch
            {
                BoardTheme.Standard => GetStandardTheme(),
                BoardTheme.Teaching => GetTeachingTheme(),
                BoardTheme.ChineseStyle => GetChineseStyleTheme(),
                _ => GetStandardTheme()
            };
        }

        public ThemeConfig GetCurrentConfig()
        {
            return GetThemeConfig(_currentTheme);
        }

        /// <summary>
        /// 标准规范版主题
        /// </summary>
        private ThemeConfig GetStandardTheme()
        {
            return new ThemeConfig
            {
                Name = "标准规范版",
                BoardBackground = "#DEB887",        // 木质色
                LineColor = "#5C4033",              // 深棕色
                LineThickness = 1.5,
                RiverTextColor = "#5C4033",
                RiverTextSize = 24,
                CoordinateColor = "#5C4033",
                ShowCoordinates = true,
                PalaceLineColor = "#5C4033",
                SelectedHighlight = "#FF6B6B80",    // 半透明红色
                LegalMoveHint = "#90EE9080",        // 半透明绿色
                RedPieceBackground = "#FFF5EE",
                RedPieceBorder = "#DC143C",
                RedPieceText = "#DC143C",
                BlackPieceBackground = "#FFF5EE",
                BlackPieceBorder = "#1a1a1a",
                BlackPieceText = "#1a1a1a",
                PieceFontSize = 28,
                HighlightSpecialAreas = false,
                IntersectionMarkColor = "#888888",
                
                // 标准版配置
                ShowEdgeCoordinates = false,        // 仅单侧路数标注
                HighlightRiverArea = false,         // 不高亮楚河汉界
                HighlightPalaceArea = false,        // 不高亮九宫格
                RiverAreaHighlightColor = "#87CEEB40",
                PalaceAreaHighlightColor = "#FFE4B580",
                RiverFontFamily = "KaiTi",          // 默认楷体
                ShowPaperTexture = false,           // 无纹理
                ShowIntersectionDots = false,       // 无落子点圆点
                
                // 棋子装饰配置
                DecorationColor = "#B8860B",        // 金色装饰纹
                OuterRingThickness = 3.0,
                InnerRingThickness = 1.0,
                ShadowDepth = 4.0,
                HighlightIntensity = 0.3,
                PieceFontFamily = "KaiTi",
                SelectionGlowColor = "#FFD700",
                EnablePieceShadow = true,
                EnablePieceHighlight = true,
                EnablePieceDecoration = true
            };
        }

        /// <summary>
        /// 教学专用版主题
        /// </summary>
        private ThemeConfig GetTeachingTheme()
        {
            return new ThemeConfig
            {
                Name = "教学专用版",
                BoardBackground = "#F5DEB3",        // 小麦色（高对比度）
                LineColor = "#2F4F4F",              // 深岩灰色（高对比度）
                LineThickness = 2.0,
                RiverTextColor = "#2F4F4F",
                RiverTextSize = 28,
                CoordinateColor = "#2F4F4F",
                ShowCoordinates = true,
                PalaceLineColor = "#8B0000",        // 深红色高亮九宫格
                SelectedHighlight = "#FF149380",    // 深粉色高亮
                LegalMoveHint = "#32CD3280",        // 亮绿色提示
                RedPieceBackground = "#FFFFFF",
                RedPieceBorder = "#FF0000",
                RedPieceText = "#FF0000",
                BlackPieceBackground = "#FFFFFF",
                BlackPieceBorder = "#000000",
                BlackPieceText = "#000000",
                PieceFontSize = 32,
                HighlightSpecialAreas = true,       // 高亮特殊区域
                IntersectionMarkColor = "#4a4a4a",
                
                // 教学版特殊配置
                ShowEdgeCoordinates = true,         // 双侧路数/线数标注
                HighlightRiverArea = true,          // 楚河汉界色块高亮
                HighlightPalaceArea = true,         // 九宫格色块高亮
                RiverAreaHighlightColor = "#87CEEB40",  // 天蓝色半透明
                PalaceAreaHighlightColor = "#FFE4B580", // 薄雾玫瑰色半透明
                RiverFontFamily = "KaiTi",
                ShowPaperTexture = false,
                ShowIntersectionDots = false,
                IntersectionDotColor = "#CCCCCC",
                
                // 棋子装饰配置（教学版使用简约风格）
                DecorationColor = "#666666",        // 灰色装饰纹
                OuterRingThickness = 2.5,
                InnerRingThickness = 0.8,
                ShadowDepth = 2.0,
                HighlightIntensity = 0.2,
                PieceFontFamily = "SimHei",         // 黑体（教学用清晰字体）
                SelectionGlowColor = "#FF1493",
                EnablePieceShadow = false,          // 教学版无阴影
                EnablePieceHighlight = true,
                EnablePieceDecoration = false       // 教学版无装饰纹
            };
        }

        /// <summary>
        /// 国风 UI 设计版主题
        /// </summary>
        private ThemeConfig GetChineseStyleTheme()
        {
            return new ThemeConfig
            {
                Name = "国风设计版",
                BoardBackground = "#F0E6D2",        // 宣纸纹理色
                LineColor = "#654321",              // 深棕色（实木质感）
                LineThickness = 2.0,
                RiverTextColor = "#8B4513",         // 马鞍棕色（行书书法）
                RiverTextSize = 32,
                CoordinateColor = "#8B7355",
                ShowCoordinates = true,
                PalaceLineColor = "#DAA520",        // 浅金色细线勾勒九宫格
                SelectedHighlight = "#CD5C5C60",    // 印度红半透明
                LegalMoveHint = "#228B2260",        // 森林绿半透明
                RedPieceBackground = "#FAF0E6",     // 亚麻色
                RedPieceBorder = "#B22222",         // 火砖红色
                RedPieceText = "#8B0000",           // 深红色
                BlackPieceBackground = "#FAF0E6",
                BlackPieceBorder = "#2F4F4F",       // 深岩灰色
                BlackPieceText = "#2F4F4F",
                PieceFontSize = 30,
                HighlightSpecialAreas = false,
                IntersectionMarkColor = "#A0A0A0",
                
                // 国风版特殊配置
                ShowEdgeCoordinates = false,        // 仅单侧路数标注
                HighlightRiverArea = false,         // 不高亮楚河汉界
                HighlightPalaceArea = false,        // 不高亮九宫格
                RiverAreaHighlightColor = "#87CEEB40",
                PalaceAreaHighlightColor = "#FFE4B580",
                RiverFontFamily = "STXingkai",      // 华文行楷
                ShowPaperTexture = true,            // 启用宣纸纹理
                ShowIntersectionDots = true,        // 显示落子点圆点
                IntersectionDotColor = "#CCCCCC",   // 浅灰色圆点
                
                // 棋子装饰配置（国风版使用精美装饰）
                DecorationColor = "#DAA520",        // 金色装饰纹
                OuterRingThickness = 3.5,
                InnerRingThickness = 1.2,
                ShadowDepth = 5.0,
                HighlightIntensity = 0.4,
                PieceFontFamily = "STXingkai, KaiTi",  // 华文行楷优先
                SelectionGlowColor = "#FFD700",     // 金色光晕
                EnablePieceShadow = true,
                EnablePieceHighlight = true,
                EnablePieceDecoration = true
            };
        }

        #region 主题持久化

        /// <summary>
        /// 保存当前主题到配置文件
        /// </summary>
        public void SaveCurrentTheme()
        {
            try
            {
                var config = new ThemeConfigDto
                {
                    Theme = _currentTheme,
                    SavedTime = DateTime.Now
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var configPath = GetConfigPath();
                File.WriteAllText(configPath, json);

                _savedTheme = _currentTheme;
                Debug.WriteLine($"[ThemeService] 主题已保存：{_currentTheme}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThemeService] 保存主题失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 从配置文件加载保存的主题
        /// </summary>
        public void LoadSavedTheme()
        {
            try
            {
                var configPath = GetConfigPath();
                if (!File.Exists(configPath))
                {
                    _savedTheme = BoardTheme.Standard;
                    Debug.WriteLine("[ThemeService] 配置文件不存在，使用默认主题");
                    return;
                }

                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<ThemeConfigDto>(json);

                if (config != null && Enum.IsDefined(typeof(BoardTheme), config.Theme))
                {
                    _savedTheme = config.Theme;
                    SetTheme(config.Theme);
                    Debug.WriteLine($"[ThemeService] 已加载保存的主题：{config.Theme}");
                }
                else
                {
                    _savedTheme = BoardTheme.Standard;
                    Debug.WriteLine("[ThemeService] 配置无效，使用默认主题");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ThemeService] 加载主题失败：{ex.Message}");
                _savedTheme = BoardTheme.Standard;
            }
        }

        /// <summary>
        /// 获取配置文件路径（存储在 %APPDATA%\SharkChess 目录）
        /// </summary>
        private static string GetConfigPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "SharkChess");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, ThemeConfigFile);
        }

        /// <summary>
        /// 主题配置数据传输对象
        /// </summary>
        private class ThemeConfigDto
        {
            /// <summary>
            /// 主题类型
            /// </summary>
            public BoardTheme Theme { get; set; }

            /// <summary>
            /// 保存时间
            /// </summary>
            public DateTime SavedTime { get; set; }
        }

        #endregion
    }
}
