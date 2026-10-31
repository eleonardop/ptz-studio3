using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PtzStudio.App.Ui;

internal static class Ux
{
    private static SolidColorBrush Frozen(byte r, byte g, byte b)
    {
        var br = new SolidColorBrush(Color.FromRgb(r, g, b));
        br.Freeze();
        return br;
    }

    // Paleta (mesmos valores de Themes/Dark.xaml): grafite e preto com detalhes em azul.
    public static readonly Brush Bg = Frozen(0x0E, 0x10, 0x14);
    public static readonly Brush PanelBg = Frozen(0x14, 0x17, 0x1D);
    public static readonly Brush CardBg = Frozen(0x1A, 0x1E, 0x26);
    public static readonly Brush Raised = Frozen(0x25, 0x2A, 0x35);
    public static readonly Brush Edge = Frozen(0x2A, 0x30, 0x3B);
    public static readonly Brush BorderStrong = Frozen(0x3B, 0x43, 0x52);
    public static readonly Brush TextBrush = Frozen(0xE8, 0xEB, 0xF1);
    public static readonly Brush Muted = Frozen(0x8E, 0x97, 0xA7);
    public static readonly Brush Accent = Frozen(0x3B, 0x82, 0xF6);
    public static readonly Brush AccentLight = Frozen(0x7D, 0xB1, 0xFF);
    public static readonly Brush Danger = Frozen(0xEF, 0x44, 0x44);
    public static readonly Brush DangerDim = Frozen(0x4A, 0x16, 0x1A);
    public static readonly Brush Ok = Frozen(0x22, 0xC5, 0x5E);
    public static readonly Brush Warn = Frozen(0xF5, 0x9E, 0x0B);

    public const string IconFont = "Segoe MDL2 Assets";

    public static TextBlock Text(string t, double size = 15, bool bold = false, Brush? fg = null) => new()
    {
        Text = t, FontSize = size, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
        Margin = new Thickness(0, 4, 0, 4), TextWrapping = TextWrapping.Wrap, Foreground = fg ?? TextBrush
    };

    /// <summary>Ícone vetorial da fonte do Windows (Segoe MDL2 Assets). Se a fonte faltar, o glifo some sem quebrar nada.</summary>
    public static TextBlock Icon(string glyph, double size = 16, Brush? fg = null) => new()
    {
        Text = glyph, FontFamily = new FontFamily(IconFont), FontSize = size, Foreground = fg ?? TextBrush,
        VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0)
    };

    /// <summary>Cabeçalho de aba/seção: título grande + subtítulo discreto.</summary>
    public static UIElement Header(string title, string? subtitle = null)
    {
        var p = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        p.Children.Add(new TextBlock { Text = title, FontSize = 22, FontWeight = FontWeights.SemiBold, Foreground = TextBrush });
        if (!string.IsNullOrEmpty(subtitle))
            p.Children.Add(new TextBlock { Text = subtitle, FontSize = 13, Foreground = Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
        return p;
    }

    /// <summary>Cartão arredondado com título opcional. Use para agrupar controles relacionados.</summary>
    public static Border Card(UIElement content, string? title = null, double pad = 16)
    {
        var inner = new StackPanel();
        if (title != null)
            inner.Children.Add(new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = AccentLight, Margin = new Thickness(0, 0, 0, 10) });
        inner.Children.Add(content);
        return new Border
        {
            Background = CardBg, BorderBrush = Edge, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(pad), Margin = new Thickness(0, 0, 12, 12), Child = inner
        };
    }

    /// <summary>Etiqueta de status (ponto colorido + texto) para o cabeçalho.</summary>
    public static Border Chip(string icon, TextBlock value, string label)
    {
        var l = new TextBlock { Text = label, FontSize = 11, Foreground = Muted };
        value.FontSize = 14; value.Margin = new Thickness(0); value.FontWeight = FontWeights.SemiBold;
        var col = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        col.Children.Add(l); col.Children.Add(value);
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(Icon(icon, 18, AccentLight)); row.Children.Add(col);
        return new Border
        {
            Background = CardBg, BorderBrush = Edge, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 7, 14, 7), Margin = new Thickness(0, 0, 8, 0), Child = row
        };
    }

    public static Button Btn(string text, Action click, double minWidth = 150, Brush? bg = null, Brush? fg = null)
    {
        var b = new Button
        {
            Content = text, MinWidth = minWidth, MinHeight = 44, Margin = new Thickness(4), Padding = new Thickness(16, 6, 16, 6),
            FontSize = 15
        };
        if (bg != null) { b.Background = bg; b.BorderBrush = bg; b.Foreground = fg ?? Brushes.White; b.FontWeight = FontWeights.SemiBold; }
        b.Click += (_, _) => click();
        return b;
    }

    public static StackPanel H(params UIElement[] c)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var e in c) p.Children.Add(e);
        return p;
    }

    public static StackPanel V(params UIElement[] c)
    {
        var p = new StackPanel { Orientation = Orientation.Vertical };
        foreach (var e in c) p.Children.Add(e);
        return p;
    }

    public static WrapPanel Wrap(params UIElement[] c)
    {
        var p = new WrapPanel();
        foreach (var e in c) p.Children.Add(e);
        return p;
    }

    public static GroupBox Group(string header, UIElement content) => new()
    {
        Header = header, Content = content, Margin = new Thickness(0, 4, 12, 12), Padding = new Thickness(16),
        FontSize = 14, FontWeight = FontWeights.Normal
    };

    public static ScrollViewer Scroll(UIElement c) => new()
    {
        Content = c, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        Padding = new Thickness(22, 18, 14, 18)
    };

    /// <summary>Insere o cabeçalho padrão (título + subtítulo) no topo de uma página criada com Scroll(V(...)).</summary>
    public static ScrollViewer WithHeader(ScrollViewer sv, string title, string? subtitle = null)
    {
        if (sv.Content is StackPanel sp) sp.Children.Insert(0, Header(title, subtitle));
        return sv;
    }

    public static UIElement SliderRow(string label, double min, double max, double value, Action<double> set, string fmt = "0.00")
    {
        var g = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 160 });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        var l = Text(label, 14); Grid.SetColumn(l, 0);
        var val = Text(value.ToString(fmt), 14); Grid.SetColumn(val, 2); val.HorizontalAlignment = HorizontalAlignment.Right;
        var s = new Slider { Minimum = min, Maximum = max, Value = Math.Clamp(value, min, max), VerticalAlignment = VerticalAlignment.Center, Height = 28 };
        Grid.SetColumn(s, 1);
        s.ValueChanged += (_, e) => { set(e.NewValue); val.Text = e.NewValue.ToString(fmt); };
        g.Children.Add(l); g.Children.Add(s); g.Children.Add(val);
        return g;
    }

    public static CheckBox Check(string text, bool value, Action<bool> set)
    {
        var c = new CheckBox { Content = text, IsChecked = value, FontSize = 15, Margin = new Thickness(0, 5, 18, 5) };
        c.Checked += (_, _) => set(true);
        c.Unchecked += (_, _) => set(false);
        return c;
    }

    public static ComboBox Combo(IList<string> items, int selected, Action<int> set, double width = 240)
    {
        var c = new ComboBox { Width = width, FontSize = 14, MinHeight = 38, Margin = new Thickness(4) };
        foreach (var i in items) c.Items.Add(i);
        c.SelectedIndex = Math.Clamp(selected, -1, items.Count - 1);
        c.SelectionChanged += (_, _) => { if (c.SelectedIndex >= 0) set(c.SelectedIndex); };
        return c;
    }

    public static TextBox Input(string text, Action<string> set, double width = 220)
    {
        var t = new TextBox { Text = text, Width = width, FontSize = 14, MinHeight = 38, Margin = new Thickness(4), VerticalContentAlignment = VerticalAlignment.Center };
        t.LostFocus += (_, _) => set(t.Text);
        return t;
    }

    public static UIElement Labeled(string label, UIElement control, double labelWidth = 230)
    {
        var l = Text(label, 14); l.Width = labelWidth; l.VerticalAlignment = VerticalAlignment.Center;
        return H(l, control);
    }

    public static string? Prompt(Window owner, string title, string label, string def = "")
    {
        var w = new Window
        {
            Title = title, Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner, SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize, MinWidth = 380, Background = Bg
        };
        System.Windows.Documents.TextElement.SetForeground(w, TextBrush);
        var tb = new TextBox { Text = def, FontSize = 14, MinHeight = 38, Margin = new Thickness(0, 6, 0, 12) };
        string? result = null;
        var ok = new Button { Content = "OK", MinWidth = 90, MinHeight = 36, IsDefault = true, Margin = new Thickness(4) };
        var cancel = new Button { Content = "Cancelar", MinWidth = 90, MinHeight = 36, IsCancel = true, Margin = new Thickness(4) };
        ok.Click += (_, _) => { result = tb.Text; w.DialogResult = true; };
        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock { Text = label, FontSize = 15 });
        panel.Children.Add(tb);
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        row.Children.Add(ok); row.Children.Add(cancel);
        panel.Children.Add(row);
        w.Content = panel;
        tb.Loaded += (_, _) => { tb.Focus(); tb.SelectAll(); };
        return w.ShowDialog() == true ? result : null;
    }
}

/// <summary>Base das abas: reconstrói o conteúdo quando perfil/configurações mudam e liga um timer apenas enquanto visível.</summary>
internal abstract class TabBase : UserControl
{
    protected readonly Engine.AppEngine E;
    private readonly System.Windows.Threading.DispatcherTimer _t = new() { Interval = TimeSpan.FromMilliseconds(80) };

    protected TabBase(Engine.AppEngine e)
    {
        E = e;
        _t.Tick += (_, _) => { if (IsActive) Refresh(); };
        Loaded += (_, _) => { _t.Start(); };
        Unloaded += (_, _) => _t.Stop();
    }

    /// <summary>Reconstrói a aba. Antes, desanexa a árvore antiga: vários elementos (TextBlocks, TextBoxes) são reaproveitados
    /// entre reconstruções e o WPF não permite que um elemento tenha dois pais.</summary>
    public void Rebuild()
    {
        var old = Content;
        Content = null;
        Detach(old);
        Build();
    }

    private static void Detach(object? o)
    {
        switch (o)
        {
            case Panel p:
                foreach (var c in p.Children.Cast<object>().ToList()) Detach(c);
                p.Children.Clear();
                break;
            case ContentControl cc:
                var inner = cc.Content; cc.Content = null; Detach(inner);
                break;
            case Decorator d:
                var ch = d.Child; d.Child = null; Detach(ch);
                break;
        }
    }
    /// <summary>Só atualiza a tela quando ela pode ser vista (janela escondida na bandeja ou minimizada = nenhum trabalho).</summary>
    private bool IsActive => IsVisible && Window.GetWindow(this)?.WindowState != WindowState.Minimized;

    protected abstract void Build();
    protected virtual void Refresh() { }
    protected void Init() => Build();
}
