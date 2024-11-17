using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using OnairConv1.Helpers;
using OnairConv1.Models;
using Scriban;
using Scriban.Functions;
using Scriban.Runtime;
using Scriban.Syntax;
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
                scriptObject["link_converter"] = new LinkConverter();
                scriptObject["html"] = new HtmlFunctions();
                scriptObject["string"] = new StringFunctions();
                scriptObject["stroke_hr"] = new StrokeHrConverter();
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
            public int RequiredParameterCount => 1;
            public int ParameterCount => 1;
            public ScriptVarParamKind VarParamKind => ScriptVarParamKind.Direct;
            public Type ReturnType => typeof(string);

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
                        return $"`{input.Substring(1, center - 1)} <{input.Substring(center + 2, input.Length - 1 - (center + 2))}>`_";
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

            public async ValueTask<object> InvokeAsync(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
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

                return string.Concat(Enumerable.Repeat(symbol, count));
            }

            public async ValueTask<object> InvokeAsync(TemplateContext context, ScriptNode callerContext, ScriptArray arguments, ScriptBlockStatement blockStatement)
            {
                throw new NotImplementedException();
            }
        }
    }
}
