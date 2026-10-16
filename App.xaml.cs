using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace CargoPlanApp
{
    public partial class App : Application
    {
        // Нейтральные «мост»-ключи → ближайшие по цвету ресурсы палитры WPF-UI
        // (подобраны под прежние кисти MaterialDesign, см. отчёт этапа 3).
        private static readonly (string Key, string SourceKey)[] BridgeBrushes =
        {
            ("App.Background", "SolidBackgroundFillColorQuarternaryBrush"),
            ("App.Foreground", "TextFillColorPrimaryBrush"),
            ("App.TextAreaBackground", "ControlFillColorDefaultBrush"),
            ("App.ControlBorder", "ControlStrokeColorTertiaryBrush"),
            ("App.SelectionBackground", "AccentFillColorDefaultBrush"),
            ("App.DarkBackground", "SolidBackgroundFillColorQuarternaryBrush"),
        };

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Во время миграции фиксируем тёмную тему независимо от темы ОС.
            ApplicationThemeManager.Apply(ApplicationTheme.Dark);

            RefreshBridgeBrushes();
            ApplicationThemeManager.Changed += OnApplicationThemeChanged;
        }

        private void OnApplicationThemeChanged(ApplicationTheme currentApplicationTheme, Color systemAccent)
        {
            RefreshBridgeBrushes();
        }

        // SolidColorBrush с DynamicResource-цветом в словаре ресурсов разрешается один раз
        // и не следует за сменой темы, поэтому перематериализуем «мост»-кисти из WPF-UI-ресурсов.
        private void RefreshBridgeBrushes()
        {
            foreach (var (key, sourceKey) in BridgeBrushes)
            {
                if (TryFindResource(sourceKey) is Brush brush)
                {
                    Resources[key] = brush;
                }
            }
        }
    }
}
