// Assets/Editor/HP55_FileTreeScan.cs
// Exports a complete file tree of the project to FileTree.md (project root).
// - Shows hierarchical directory/file structure.
// - Adds a section with file count by extension.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace hp55games.Editor.Tools
{

public static class HP55_FileTreeScan
{
    // Folders to ignore at the project root
    private static readonly string[] IgnoredFolders =
    {
        "Library",
        "Logs",
        "Temp",
        "Obj",
        ".git",
        ".idea",
        ".vs",
        "UserSettings"
    };

    [MenuItem("hp55games Tools/File Tree/Export")]
    public static void Run()
    {
        // Project root = folder containing "Assets"
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var outPath = Path.Combine(projectRoot, "FileTree.md");

        var sb = new StringBuilder();
        sb.AppendLine("# hp55games – Project File Tree");
        sb.AppendLine($"Generated: {DateTime.Now}");
        sb.AppendLine();
        sb.AppendLine($"Project root: `{projectRoot}`");
        sb.AppendLine();

        // Extension -> count map
        var extCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        sb.AppendLine("## Directory Tree");
        sb.AppendLine();

        // Recursive scan starting from the project root
        var rootDir = new DirectoryInfo(projectRoot);
        AppendDirectoryTree(sb, rootDir, "", extCounts);

        sb.AppendLine();
        sb.AppendLine("## File count by extension");
        sb.AppendLine();

        if (extCounts.Count == 0)
        {
            sb.AppendLine("- (no files found)");
        }
        else
        {
            foreach (var kvp in extCounts.OrderBy(k => k.Key))
            {
                var ext = string.IsNullOrEmpty(kvp.Key) ? "(no extension)" : kvp.Key;
                sb.AppendLine($"- `{ext}` : **{kvp.Value}** file(s)");
            }
        }

        File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
        EditorUtility.RevealInFinder(outPath);
        Debug.Log($"[hp55games] FileTree exported → {outPath}");
    }

    private static void AppendDirectoryTree(
        StringBuilder sb,
        DirectoryInfo dir,
        string indent,
        Dictionary<string, int> extCounts)
    {
        // Skip system folders at root level
        if (IsIgnored(dir))
            return;

        // Directory name
        sb.AppendLine($"{indent}- 📁 **{dir.Name}**");

        string childIndent = indent + "  ";

        FileInfo[] files = Array.Empty<FileInfo>();
        DirectoryInfo[] subDirs = Array.Empty<DirectoryInfo>();

        try
        {
            files = dir.GetFiles();
        }
        catch (Exception e)
        {
            sb.AppendLine($"{childIndent}- ⚠️ <error reading files> ({e.Message})");
        }

        try
        {
            subDirs = dir.GetDirectories();
        }
        catch (Exception e)
        {
            sb.AppendLine($"{childIndent}- ⚠️ <error reading subdirs> ({e.Message})");
        }

        // Files in current directory
        foreach (var file in files.OrderBy(f => f.Name))
        {
            var ext = file.Extension ?? "";
            if (!extCounts.TryGetValue(ext, out int count))
                count = 0;
            extCounts[ext] = count + 1;

            sb.AppendLine($"{childIndent}- 📄 {file.Name}");
        }

        // Subdirectories
        foreach (var sub in subDirs.OrderBy(d => d.Name))
        {
            AppendDirectoryTree(sb, sub, childIndent, extCounts);
        }
    }

    private static bool IsIgnored(DirectoryInfo dir)
    {
        var name = dir.Name;
        return IgnoredFolders.Any(f => string.Equals(f, name, StringComparison.OrdinalIgnoreCase));
    }
}

}
