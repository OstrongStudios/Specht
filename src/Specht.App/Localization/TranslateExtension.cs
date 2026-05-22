using Microsoft.UI.Xaml.Markup;
using Specht.App.Services;

namespace Specht.App.Localization;

/// <summary>
/// XAML markup extension that resolves a resw key via the <see cref="Strings"/>
/// helper. Works in unpackaged WinUI 3 where x:Uid ignores runtime language
/// overrides — this extension is invoked per page parse and consults a
/// ResourceContext that DOES honor the user's chosen language.
///
/// Usage: <code>Text="{loc:Translate Key=SettingsTitle.Text}"</code>
/// Shorthand:  <code>Text="{loc:Translate SettingsTitle.Text}"</code>
/// </summary>
[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed class TranslateExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public TranslateExtension() { }

    public TranslateExtension(string key) { Key = key; }

    protected override object ProvideValue() =>
        string.IsNullOrEmpty(Key) ? string.Empty : Strings.Get(Key);
}
