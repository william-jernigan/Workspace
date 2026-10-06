using Contentful.Core.Models;
using OneExchangeOrderService.Domain.Constants;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OneExchangeOrderService.Application.Helpers
{
    public static class RichTextHtmlHelper
    {
        /// <summary>
        /// Convert CMS rich text to HTML.
        /// </summary>
        /// <param name="document">Rich text document.</param>
        /// <returns>HTML string.</returns>
        public static string RenderDocumentToHtml(Document document)
        {
            if (document == null || document.Content == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            foreach (var node in document.Content)
            {
                sb.Append(RenderNode(node));
            }

            return SanitizeHtml(sb.ToString());
        }

        private static string RenderNode(IContent node) => node switch
        {
            Paragraph paragraph => $"<p>{string.Concat(paragraph.Content.Select(RenderNode))}</p>",
            Hyperlink hyperlink => RenderHyperlink(hyperlink),
            Text textNode => RenderText(textNode),

            // Render unordered list (<ul>)
            List listNode when listNode.NodeType == "unordered-list" =>
                $"<ul>{string.Concat(listNode.Content.Select(RenderNode))}</ul>",

            // Render ordered list (<ol>)
            List listNode when listNode.NodeType == "ordered-list" =>
                $"<ol>{string.Concat(listNode.Content.Select(RenderNode))}</ol>",

            // Render list item (<li>)
            ListItem listItemNode =>
                $"<li>{string.Concat(listItemNode.Content.Select(RenderNode))}</li>",

            _ => string.Empty,
        };

        public static string SanitizeHtml(string html)
        {
            var unescapedHtml = Regex.Unescape(html);
            unescapedHtml = unescapedHtml.Trim();

            return unescapedHtml;
        }

        private static string RenderText(Text text)
        {
            var content = WebUtility.HtmlEncode(text.Value);

            if (text.Marks != null)
            {
                foreach (var mark in text.Marks)
                {
                    var type = ParseMarkupType(mark.Type);
                    if (type == null)
                    {
                        continue;
                    }

                    switch (type)
                    {
                        case MarkupType.Bold:
                            content = $"<strong>{content}</strong>";
                            break;
                        case MarkupType.Italic:
                            content = $"<em>{content}</em>";
                            break;
                        case MarkupType.Underline:
                            content = $"<u>{content}</u>";
                            break;
                        default:
                            break;
                    }
                }
            }

            return content;
        }


        private static string RenderHyperlink(Hyperlink hyperlink)
        {
            var rawUrl = hyperlink.Data?.Uri ?? "#";
            rawUrl = SanitizeUrl(rawUrl);
            var url = EnsureAbsoluteUrl(rawUrl);

            var innerContent = string.Concat(hyperlink.Content.Select(RenderNode)).Trim();

            // If innerContent exactly matches rawUrl, strip the scheme (http:// or https://) from innerContent for display
            if (string.Equals(innerContent, rawUrl, StringComparison.OrdinalIgnoreCase))
            {
                innerContent = RemoveUrlScheme(innerContent);
            }

            return $"<a href=\"{url}\" target=\"_blank\" rel=\"noopener noreferrer\">{innerContent}</a>";
        }

        private static string EnsureAbsoluteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "#";
            }

            if (!url.StartsWith(OneExchangeOrderServiceConstants.HTTPPREFIX, StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith(OneExchangeOrderServiceConstants.HTTPSPREFIX, StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith(OneExchangeOrderServiceConstants.MAILTOPREFIX, StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith(OneExchangeOrderServiceConstants.TELPREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return OneExchangeOrderServiceConstants.HTTPSPREFIX + url;
            }

            return url;
        }

        private static string SanitizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "#";
            }

            // Remove escaped characters like \"
            url = RemoveEscapedCharacters(url);

            // Strip leading/trailing quotes and smart quotes
            return url.Trim().Trim('"').Trim('“', '”');
        }

        private static string RemoveEscapedCharacters(string input) =>
            // Remove escaped double quotes
            input.Replace("\\\"", "\"");

        private static string RemoveUrlScheme(string url)
        {
            if (url.StartsWith(OneExchangeOrderServiceConstants.HTTPPREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return url[OneExchangeOrderServiceConstants.HTTPPREFIX.Length..];
            }
            if (url.StartsWith(OneExchangeOrderServiceConstants.HTTPSPREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return url[OneExchangeOrderServiceConstants.HTTPSPREFIX.Length..];
            }
            return url;
        }
        private static MarkupType? ParseMarkupType(string type) => type.ToLowerInvariant() switch
        {
            "bold" => MarkupType.Bold,
            "italic" => MarkupType.Italic,
            "underline" => MarkupType.Underline,
            _ => null
        };
        internal enum MarkupType
        {
            Bold,
            Italic,
            Underline
        }
    }
}
