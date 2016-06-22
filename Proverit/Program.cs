using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Proverit
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Run with first argument as path to .sln file");
                return;
            }
            var slnFilename = Path.GetFullPath(args[0]);
            
            if (!File.Exists(slnFilename))
            {
                Console.WriteLine("File not found: {0}", slnFilename);
            }

            var projectFilenames = GetProjectFilesFromSolution(slnFilename);
            var testProjectInfo = GetTestProjectInfo(projectFilenames).ToList();

            //var uniqueOutputPaths = testProjectInfo.SelectMany(it => it.OutputPaths).Distinct().ToList();
            //Console.WriteLine("Output paths:");
            //foreach (var uniqueOutputPath in uniqueOutputPaths)
            //{
            //    Console.WriteLine("    {0}", uniqueOutputPath);
            //}
            //Console.WriteLine();

            foreach (var testDll in GetTestDlLs(testProjectInfo))
            {
                Console.WriteLine(testDll);
            }
        }

        private static IEnumerable<string> GetTestDlLs(List<ProjectInfo> testProjectInfo)
        {
            var testDlls = testProjectInfo
                .Select(v => new {v, dir = Path.GetDirectoryName(v.ProjectFile)})
                .SelectMany(o => o.v.OutputPaths
                    .Select(path => string.Format("{0}{1}.dll", Path.Combine(o.dir, path), o.v.AssemblyName)));

            return testDlls;
        }

        private class ProjectInfo
        {
            public string ProjectFile;
            public IList<string> OutputPaths;
            public string AssemblyName;
        }

        private static IEnumerable<ProjectInfo> GetTestProjectInfo(IEnumerable<string> projectFilenames)
        {
            return projectFilenames
                .Select(name => new {name, lines = File.ReadAllLines(name)})
                .Where(it => it.lines.Any(line => line.Contains(@"3AC096D0-A1C2-E12C-1390-A8335801FDAB")))
                .Select(it =>
                {
                    var outputPaths = GetTokens(it.lines, "<OutputPath>");
                    var assemblyName = GetTokens(it.lines, "<AssemblyName>").FirstOrDefault() ?? string.Empty;

                    return new ProjectInfo
                    {
                        ProjectFile = it.name,
                        OutputPaths = outputPaths.ToList(),
                        AssemblyName = assemblyName
                    };
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
            var sln = File.ReadAllLines(slnFilename);
            // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "StandaloneClient", "..\GUI\GUIFramework\StandaloneClient\StandaloneClient.csproj", "{E73558A8-5EB2-467A-9422-F64309752E26}"
            var x = sln
                .Where(it => it.StartsWith(@"Project("))
                .Select(it => it.Split(',')[1].Substring(1).Split('"')[1])
                .Where(it => it.EndsWith(".csproj"))
                .Select(it => Path.GetFullPath(Path.Combine(subdir, it)))
                .Where(File.Exists);
            return x;
        }
    }
}
