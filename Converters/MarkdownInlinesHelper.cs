using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace EcoUtils.Converters;

public static class MarkdownInlinesHelper
{
    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.RegisterAttached(
            "MarkdownText",
            typeof(string),
            typeof(MarkdownInlinesHelper),
            new PropertyMetadata(null, OnMarkdownTextChanged));

    public static string GetMarkdownText(DependencyObject obj) =>
        (string)obj.GetValue(MarkdownTextProperty);

    public static void SetMarkdownText(DependencyObject obj, string value) =>
        obj.SetValue(MarkdownTextProperty, value);

    private static void OnMarkdownTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb) return;

        tb.Inlines.Clear();

        if (e.NewValue is not string text || string.IsNullOrEmpty(text))
            return;

        // Regex.Split com grupo de captura: os índices ímpares são o conteúdo bold
        var parts = Regex.Split(text, @"\*\*(.+?)\*\*");

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;

            tb.Inlines.Add(i % 2 == 1
                ? new Run(parts[i]) { FontWeight = FontWeights.Bold }
                : new Run(parts[i]));
        }
    }
}
