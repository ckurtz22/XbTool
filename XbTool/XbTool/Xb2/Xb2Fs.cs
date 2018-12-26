﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNet.Globbing;
using XbTool.Common;

namespace XbTool.Xb2
{
    public class Xb2Fs : IDisposable, IFileReader
    {
        public List<string> Directories { get; } = new List<string>();
        private Dictionary<string, FsFile> Files { get; } = new Dictionary<string, FsFile>(StringComparer.OrdinalIgnoreCase);
        public FileArchive Archive { get; }

        public Xb2Fs(string directory)
        {
            var baseDir = Path.Combine(directory, "base");
            var dlcDir = Path.Combine(directory, "dlc");
            if (!Directory.Exists(baseDir)) throw new DirectoryNotFoundException($"Could not find {baseDir}");

            var dirs = new List<string>();

            if (Directory.Exists(dlcDir))
            {
                var dlcDirs = Directory.GetDirectories(dlcDir);
                dirs.AddRange(dlcDirs);
            }
            dirs.Add(baseDir);

            for (int i = 0; i < dirs.Count; i++)
            {
                foreach (var file in Directory.GetFiles(dirs[i], "*", SearchOption.AllDirectories))
                {
                    var path = Helpers.GetRelativePath(file, dirs[i]);
                    if (path[0] != '/') path = '/' + path;
                    if (path == "key.bin") continue;
                    Files[path] = new FsFile(file, i);
                    //Files.Add(path, new FsFile(file, i));
                }
            }
			Options options = new Options
			{
				ArdFilename = Path.Combine(baseDir, "bf2.ard"),
				ArhFilename = Path.Combine(baseDir, "bf2.arh")
			};

			if (File.Exists(options.ArhFilename) && File.Exists(options.ArdFilename))
            {
                Archive = new FileArchive(options);
                foreach (var file in Archive.FileInfo.Where(x => x.Filename != null)) //Todo: Investigate
                {
                    Files.Add(file.Filename, new FsFile(file.Filename, -1));
                }
            }
        }

        public byte[] ReadFile(string filename)
        {
            if (!Files.TryGetValue(filename, out var file))
            {
                throw new FileNotFoundException("File does not exist", filename);
            }

            if (file.Directory == -1)
            {
                return Archive.ReadFile(file.Path);
            }

            return File.ReadAllBytes(file.Path);
        }

        public IEnumerable<string> FindFiles(string pattern)
        {
            //pattern = pattern.TrimStart('/');
            //var files = Enumerable.Empty<string>();
            Glob glob = Glob.Parse(pattern,
                new GlobOptions { Evaluation = new EvaluationOptions { CaseInsensitive = true } });

            var matches = Files.Keys.Where(x => glob.IsMatch(x));

            return matches;
        }

        public bool Exists(string filename)
        {
            return Files.ContainsKey(filename);
        }

        public void Dispose()
        {
            Archive?.Dispose();
        }

        private class FsFile
        {
            public string Path { get; }
            public int Directory { get; }

            public FsFile(string path, int directory)
            {
                Path = path;
                Directory = directory;
            }
        }
    }
}

