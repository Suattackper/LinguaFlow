using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace LinguaFlow.Web.Services;

/// <summary>Renders the small LCF Markdown subset as Blazor-owned, encoded DOM nodes.</summary>
public static class SafeMarkdownRenderer
{
    public static RenderFragment Render(string? source) => builder =>
    {
        if (string.IsNullOrEmpty(source)) return;
        var lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var sequence = 0;
        for (var i = 0; i < lines.Length;)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) { i++; continue; }
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                i++;
                var code = new StringBuilder();
                while (i < lines.Length && !lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal))
                { if (code.Length > 0) code.Append('\n'); code.Append(lines[i++]); }
                if (i < lines.Length) i++;
                builder.OpenElement(sequence++, "pre"); builder.OpenElement(sequence++, "code"); builder.AddContent(sequence++, code.ToString()); builder.CloseElement(); builder.CloseElement();
                continue;
            }
            var hashes = trimmed.TakeWhile(c => c == '#').Count();
            if (hashes is >= 1 and <= 6 && trimmed.Length > hashes && trimmed[hashes] == ' ')
            {
                builder.OpenElement(sequence++, $"h{hashes}"); RenderInline(builder, trimmed[(hashes + 1)..], ref sequence); builder.CloseElement(); i++; continue;
            }
            if (ListItem(trimmed, out var ordered, out _))
            {
                builder.OpenElement(sequence++, ordered ? "ol" : "ul");
                while (i < lines.Length && ListItem(lines[i].TrimStart(), out var nextOrdered, out var item) && nextOrdered == ordered)
                { builder.OpenElement(sequence++, "li"); RenderInline(builder, item, ref sequence); builder.CloseElement(); i++; }
                builder.CloseElement(); continue;
            }
            builder.OpenElement(sequence++, "p");
            var first = true;
            var hardBreak = false;
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
            {
                var current = lines[i].Trim();
                var headingMarks = current.TakeWhile(c => c == '#').Count();
                if (!first && (current.StartsWith("```", StringComparison.Ordinal) || ListItem(current, out _, out _) || headingMarks is >= 1 and <= 6 && current.Length > headingMarks && current[headingMarks] == ' ')) break;
                if (!first) { if (hardBreak) { builder.OpenElement(sequence++, "br"); builder.CloseElement(); } else builder.AddContent(sequence++, " "); }
                RenderInline(builder, current.TrimEnd('\\'), ref sequence);
                hardBreak = lines[i].EndsWith("  ", StringComparison.Ordinal) || lines[i].EndsWith('\\');
                first = false; i++;
            }
            builder.CloseElement();
        }
    };

    private static bool ListItem(string line, out bool ordered, out string item)
    {
        ordered = false; item = "";
        if (line.Length > 2 && (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))) { item = line[2..]; return true; }
        var digits = line.TakeWhile(char.IsDigit).Count();
        if (digits is > 0 and < 4 && line.Length > digits + 1 && line[digits] is '.' or ')' && line[digits + 1] == ' ')
        { ordered = true; item = line[(digits + 2)..]; return true; }
        return false;
    }

    private static void RenderInline(RenderTreeBuilder builder, string text, ref int sequence)
    {
        var plain = new StringBuilder();
        for (var i = 0; i < text.Length;)
        {
            if (text[i] == '\\' && i + 1 < text.Length && "\\`*_[]()".Contains(text[i + 1])) { plain.Append(text[i + 1]); i += 2; continue; }
            if (text[i] == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i + 1) { Flush(builder, plain, ref sequence); builder.OpenElement(sequence++, "code"); builder.AddContent(sequence++, text[(i + 1)..end]); builder.CloseElement(); i = end + 1; continue; }
            }
            if (text[i] == '[')
            {
                var close = text.IndexOf("](", i + 1, StringComparison.Ordinal);
                var end = close < 0 ? -1 : text.IndexOf(')', close + 2);
                if (end > close && SafeLink(text[(close + 2)..end]) is { } href)
                { Flush(builder, plain, ref sequence); builder.OpenElement(sequence++, "a"); builder.AddAttribute(sequence++, "href", href); if (href.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) builder.AddAttribute(sequence++, "rel", "noopener noreferrer"); RenderInline(builder, text[(i + 1)..close], ref sequence); builder.CloseElement(); i = end + 1; continue; }
            }
            var marker = text.AsSpan(i).StartsWith("**", StringComparison.Ordinal) ? "**" : text.AsSpan(i).StartsWith("__", StringComparison.Ordinal) ? "__" : text[i] is '*' or '_' ? text[i].ToString() : null;
            if (marker is not null)
            {
                var end = text.IndexOf(marker, i + marker.Length, StringComparison.Ordinal);
                if (marker.Length == 2 && end >= 0 && end + 2 < text.Length && text[end + 2] == marker[0]) end++;
                if (end > i + marker.Length)
                { Flush(builder, plain, ref sequence); builder.OpenElement(sequence++, marker.Length == 2 ? "strong" : "em"); RenderInline(builder, text[(i + marker.Length)..end], ref sequence); builder.CloseElement(); i = end + marker.Length; continue; }
            }
            plain.Append(text[i++]);
        }
        Flush(builder, plain, ref sequence);
    }

    private static void Flush(RenderTreeBuilder builder, StringBuilder plain, ref int sequence)
    { if (plain.Length > 0) { builder.AddContent(sequence++, plain.ToString()); plain.Clear(); } }

    private static string? SafeLink(string value)
    {
        if (value.StartsWith('#') && value.Length > 1 && !value.Any(char.IsControl)) return value;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" or "mailto" ? uri.ToString() : null;
    }
}
