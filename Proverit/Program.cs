using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Proverit
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(@"Arguments:");
                Console.WriteLine(@"  First parameter is .sln file");
                Console.WriteLine(@"  Followed by options:");
                Console.WriteLine(@"      -o filename    <-- output to file instead of console");
                Console.WriteLine(@"      -c contains    <-- indicate \release\ or \debug\");
                Console.WriteLine(@"          (does only a 'contains' filter on the path; may specify any substring in assembly path)");
                Console.WriteLine(@"      -d delimiter   <-- space if not specified, otherwise comma separated ascii in decimal");
                Console.WriteLine(@"          (eg, 13,10 means cr/lf; 9 means tab, etc.)");
                return;
            }
            var slnFilename = Path.GetFullPath(args[0]);
            if (!File.Exists(slnFilename))
            {
                Console.WriteLine("File not found: {0}", slnFilename);
                return;
            }
            var c = GetArgument(args, "-c") ?? string.Empty;
            var delim = GetDelimiter(GetArgument(args, "-d") ?? string.Empty);
            var outputFile = GetArgument(args, "-o");

            using (var outstream = outputFile == null
                ? Console.Out
                : new StreamWriter(new FileStream(outputFile, FileMode.Create)))
            {
                outstream.Write(GetTestDlLs(GetTestProjectInfo(GetProjectFilesFromSolution(slnFilename)))
                    .Aggregate(new StringBuilder(),
                        (a, s) => a.Append(s.ToLowerInvariant().Contains(c.ToLowerInvariant()) ? string.Format("{0}{1}", s, delim) : string.Empty),
                        a => a.ToString()));
            }
        }

        private static string GetArgument(IEnumerable<string> args, string option)
        {
            return args.SkipWhile(i => i != option).Skip(1).Take(1).FirstOrDefault();
        }

        private static string GetDelimiter(string d)
        {
            return string.IsNullOrEmpty(d) ? " " : new string(d.Split(',').Select(v => Convert.ToChar(int.Parse(v))).ToArray());
        }

        private static IEnumerable<string> GetTestDlLs(IEnumerable<ProjectInfo> testProjectInfo)
        {
            return testProjectInfo
                .Select(v => new {v, dir = Path.GetDirectoryName(v.ProjectFile)})
                .SelectMany(o => o.v.OutputPaths
                    .Select(path => string.Format("{0}{1}.dll", Path.Combine(o.dir, path), o.v.AssemblyName)));
        }

        private static IEnumerable<ProjectInfo> GetTestProjectInfo(IEnumerable<string> projectFilenames)
        {
            return projectFilenames
                .Select(name => new {name, lines = File.ReadAllLines(name)})
                .Where(it => it.lines.Any(line => line.Contains(@"3AC096D0-A1C2-E12C-1390-A8335801FDAB")))
                .Select(it => new ProjectInfo
                {
                    ProjectFile = it.name,
                    OutputPaths = GetTokens(it.lines, "<OutputPath>").ToList(),
                    AssemblyName = GetTokens(it.lines, "<AssemblyName>").FirstOrDefault() ?? string.Empty
                });
        }

        private static string[] GetTokens(string[] lines, string token)
        {
            return lines
                .Where(line => line.Contains(token))
                .Select(o => Regex.Match(o, token + "(.*)" + "</").Groups)
                .Where(g => g.Count > 1)
                .Select(g => g[1].Value)
                .ToArray();
        }

        private static IEnumerable<string> GetProjectFilesFromSolution(string slnFilename)
        {
            var subdir = Path.GetDirectoryName(slnFilename);
            if (subdir == null) throw new Exception();
            return File.ReadAllLines(slnFilename)
                .Where(it => it.StartsWith(@"Project("))
                .Select(it => it.Split(',')[1].Substring(1).Split('"')[1])
                .Where(it => it.EndsWith(".csproj"))
                .Select(it => Path.GetFullPath(Path.Combine(subdir, it)))
                .Where(File.Exists);
        }

        private class ProjectInfo
        {
            public string AssemblyName;
            public IList<string> OutputPaths;
            public string ProjectFile;
        }
    }
}