using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using OnairConv1.Helpers;
using OnairConv1.Models;
using Scriban;
using Scriban.Functions;
using Scriban.Runtime;
using Scriban.Syntax;
using System.Globalization;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace OnairConv1
{
    internal class Program
    {
        [Verb("convert")]
        private class ConvertOpt
        {
            [Value(0, HelpText = "XmlFileIn", Required = true)]
            public string XmlFileIn { get; set; } = null!;

            [Value(1, HelpText = "RstFileOut", Required = true)]
            public string RstFileOut { get; set; } = null!;

            [Option("template", Default = "kaf_onair")]
            public string TemplateIn { get; set; } = null!;

            [Option("no-timestamp", HelpText = "No twitter timestamp extraction")]
            public bool NoTimestamp { get; set; }
        }

        [Verb("dummy")]
        private class DummyOpt
        {

        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<ConvertOpt, DummyOpt>(args)
                .MapResult<ConvertOpt, DummyOpt, int>(
                    DoConvert,
                    DoDummy,
                    ex => 1
                );
        }

        private static int DoDummy(DummyOpt opt)
        {
            throw new NotImplementedException();
        }

        private static int DoConvert(ConvertOpt opt)
        {
            using (var container = new ServiceCollection()
                .AddOnairConv1()
                .BuildServiceProvider()
            )
            using (var xmlFileIn = File.OpenRead(opt.XmlFileIn))
            {
                var root = (Onairconv1Model)(new XmlSerializer(typeof(Onairconv1Model))
                    .Deserialize(
                        xmlFileIn
                    ) ?? throw new NullReferenceException());

                var template = Template.Parse(
                    File.ReadAllText(
                        Path.Combine(AppContext.BaseDirectory, $"RstTemplates/{opt.TemplateIn}.txt")
                    )
                );
                var scriptObject = new ScriptObject();
                scriptObject["root"] = root;
                scriptObject["single_markdown_link_to_rst"] = new LinkConverter(opt.NoTimestamp);
                scriptObject["html"] = new HtmlFunctions();
                scriptObject["string"] = new StringFunctions();
                scriptObject["with_heading"] = new StrokeHrConverter();
                scriptObject["strip_duplicated_whitespaces"] = new StripDupWs();
                scriptObject["sort_links_by_timestamp_desc"] = new SortLinksByTimestampDesc();
                scriptObject["object"] = new ObjectFunctions();
                scriptObject["attach_weekday"] = new AttachWeekday();
                var templateContext = new TemplateContext(
                    scriptObject
                );
                File.WriteAllText(
                    opt.RstFileOut,
                    template.Render(
                        templateContext
                    )
                );

                return 0;
            }
        }

        /// <summary>
        /// Convert markdown link to reStructuredText link
        /// </summary>
        private class LinkConverter : IScriptCustomFunction
        {
            private readonly bool _noTimestamp;

            public int RequiredParameterCount => 1;
            public int ParameterCount => 1;
            public ScriptVarParamKind VarParamKind => ScriptVarParamKind.Direct;
            public Type ReturnType => typeof(string);

            public LinkConverter(bool noTimestamp)
            {
                _noTimestamp = noTimestamp;
            }

            public ScriptParameterInfo GetParameterInfo(int index)
            {
                if (index == 0)
                {
                    return new ScriptParameterInfo(typeof(string), "rst");
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            public object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
            {
                var input = Regex.Replace(arguments[0] + "", "\\s+", " ");
                if (input.StartsWith("[") && input.EndsWith(")"))
                {
                    int center = input.IndexOf("](");
                    if (0 <= center)
                    {
                        var when = Helper.TryToExtractTwitterUrlTimestamp(input)?.ToLocalTime();

                        return $"`{input.Substring(1, center - 1)} <{input.Substring(center + 2, input.Length - 1 - (center + 2))}>`_{((!_noTimestamp && when.HasValue) ? $" :subscript:`({when:yyyy/M/d HH:mm})`" : "")}";
                    }
                    else
                    {
                        return input;
                    }
                }
                else
                {
                    return input;
                }
            }

#pragma warning disable CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
            public async ValueTask<object> InvokeAsync(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
#pragma warning restore CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
            {
                throw new NotImplementedException();
            }
        }

        private class StrokeHrConverter : IScriptCustomFunction
        {
            public int RequiredParameterCount => 2;
            public int ParameterCount => 2;
            public ScriptVarParamKind VarParamKind => ScriptVarParamKind.Direct;
            public Type ReturnType => typeof(string);

            public ScriptParameterInfo GetParameterInfo(int index)
            {
                if (index == 0)
                {
                    return new ScriptParameterInfo(typeof(string), "title");
                }
                else if (index == 1)
                {
                    return new ScriptParameterInfo(typeof(string), "symbol");
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            public object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
            {
                var title = Regex.Replace(arguments[0] + "", "\\s+", " ");
                var symbol = Regex.Replace(arguments[1] + "", "\\s+", " ");

                var count = title.Length + title.Count(it => 0x100 <= it || it == ' ');

                return title + "\n" + string.Concat(Enumerable.Repeat(symbol, count));
            }

#pragma warning disable CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
            public async ValueTask<object> InvokeAsync(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
#pragma warning restore CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
            {
                throw new NotImplementedException();
            }
        }

        private class StripDupWs : IScriptCustomFunction
        {
            public int RequiredParameterCount => 1;
            public int ParameterCount => 1;
            public ScriptVarParamKind VarParamKind => ScriptVarParamKind.Direct;
            public Type ReturnType => typeof(string);

            public ScriptParameterInfo GetParameterInfo(int index)
            {
                if (index == 0)
                {
                    return new ScriptParameterInfo(typeof(string), "text");
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            public object Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
            {
                var input = Regex.Replace(arguments[0] + "", "\\s+", " ");
                return Regex.Replace(input, "\\s+", " ");
            }

#pragma warning disable CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
            public async ValueTask<object> InvokeAsync(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
#pragma warning restore CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
            {
                throw new NotImplementedException();
            }
        }

        private class SortLinksByTimestampDesc : IScriptCustomFunction
        {
            public int RequiredParameterCount => 1;
            public int ParameterCount => 1;
            public ScriptVarParamKind VarParamKind => ScriptVarParamKind.Direct;

            public Type ReturnType => typeof(string[]);

            public ScriptParameterInfo GetParameterInfo(int index)
            {
                if (index == 0)
                {
                    return new ScriptParameterInfo(typeof(string[]), "array");
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            public object? Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
            {
                var array = (IEnumerable<string>)arguments[0];
                if (array != null)
                {
                    return array
                        .OrderByDescending(
                            one =>
                            {
                                // https://x.com/XXX/status/1111111111111111111
                                var match = Regex.Match(one, "https://x\\.com/\\w+/status/(?<ts>\\d+)");
                                if (match.Success)
                                {
                                    var ts = long.Parse(match.Groups["ts"].Value);
                                    return ts >> 22;
                                }
                                else
                                {
                                    return 0;
                                }
                            }
                        )
                        .ToArray();
                }
                else
                {
                    return null;
                }
            }

            public ValueTask<object> InvokeAsync(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
            {
                throw new NotImplementedException();
            }
        }

        private class AttachWeekday : IScriptCustomFunction
        {
            public int RequiredParameterCount => 1;
            public int ParameterCount => 1;
            public ScriptVarParamKind VarParamKind => ScriptVarParamKind.Direct;

            public Type ReturnType => typeof(string);

            public ScriptParameterInfo GetParameterInfo(int index)
            {
                if (index == 0)
                {
                    return new ScriptParameterInfo(typeof(string), "text");
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            public object? Invoke(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
            {
                return Regex.Replace(
                    arguments[0] + "",
                    "\\d{4}-\\d{2}-\\d{2}",
                    match =>
                    {
                        return $"{match.ValueSpan}({DateTime.Parse(match.Value).ToString("ddd", CultureInfo.GetCultureInfo("ja-jp"))})";
                    }
                );
            }

            public ValueTask<object> InvokeAsync(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
            {
                throw new NotImplementedException();
            }
        }

        private class Helper
        {
            public static DateTime? TryToExtractTwitterUrlTimestamp(string url)
            {
                // https://x.com/XXX/status/1111111111111111111
                var match = Regex.Match(url, "https://x\\.com/\\w+/status/(?<ts>\\d+)");
                if (match.Success)
                {
                    var ts = long.Parse(match.Groups["ts"].Value);
                    return new DateTime(2010, 11, 4, 1, 42, 54, 657, DateTimeKind.Utc).AddSeconds((ts >> 22) / 1000);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
