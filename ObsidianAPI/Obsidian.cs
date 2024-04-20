﻿using System.Text.RegularExpressions;
using System.Text;

namespace Obsidian
{
    public class ObsidianAPI
    {
        private readonly string vaultPath;
        private readonly Dictionary<string, string> noteCache = new Dictionary<string, string>();

        public ObsidianAPI(string path)
        {
            vaultPath = path;
            Directory.CreateDirectory(vaultPath);
        }

        public void RenameNote(string originalName, string newName)
        {
            string originalPath = Path.Combine(vaultPath, originalName + ".md");
            string newPath = Path.Combine(vaultPath, newName + ".md");
            if (!File.Exists(originalPath))
                throw new FileNotFoundException("The original note does not exist.");
            File.Move(originalPath, newPath);
            if (noteCache.ContainsKey(originalName))
            {
                noteCache[newName] = noteCache[originalName];
                noteCache.Remove(originalName);
            }
        }

        public void DeleteNote(string noteName)
        {
            string filePath = Path.Combine(vaultPath, noteName + ".md");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The note to delete does not exist.");
            File.Delete(filePath);
            noteCache.Remove(noteName);
        }

        public async Task CreateNoteAsync(string noteName, string content)
        {
            string filePath = Path.Combine(vaultPath, noteName + ".md");
            await File.WriteAllTextAsync(filePath, content);
            noteCache[noteName] = content;
        }

        public async Task AppendContentAsync(string noteName, string content)
        {
            string filePath = Path.Combine(vaultPath, noteName + ".md");
            await File.AppendAllTextAsync(filePath, content);
            if (noteCache.ContainsKey(noteName))
                noteCache[noteName] += content;
        }

        public async Task<string> ReadNoteAsync(string noteName)
        {
            if (noteCache.TryGetValue(noteName, out var content))
                return content;

            string filePath = Path.Combine(vaultPath, noteName + ".md");
            content = await File.ReadAllTextAsync(filePath);
            noteCache[noteName] = content;
            return content;
        }

        public async Task LinkNotesAsync(string noteName1, string listName, List<string> notes)
        {
            StringBuilder append = new StringBuilder();
            append.AppendLine();
            append.AppendLine($" - {listName}");
            foreach (var item in notes)
            {
                append.AppendLine($"\t - [[{item}]]");
            }
            await AppendContentAsync(noteName1, append.ToString());
        }

        public async Task<List<string>> SearchNotesAsync(string searchTerm)
        {
            if (noteCache.Count == 0)
            {
                await CacheAllNotesAsync();
            }
            return noteCache.Where(kv => kv.Value.Contains(searchTerm))
                            .Select(kv => kv.Key)
                            .ToList();
        }

        private async Task CacheAllNotesAsync()
        {
            var files = Directory.GetFiles(vaultPath, "*.md");
            foreach (var file in files)
            {
                string noteName = Path.GetFileNameWithoutExtension(file);
                string content = await File.ReadAllTextAsync(file);
                noteCache[noteName] = content;
            }
        }

        public async Task AddTagsAsync(string noteName, List<string> tags)
        {
            StringBuilder tagsLine = new StringBuilder();
            foreach (var tag in tags)
            {
                tagsLine.Append($"- #{tag}\n");
            }
            await AppendContentAsync(noteName, "\n- Tags\n\t" + tagsLine + "\n");
        }

        public async Task<List<string>> GetTagsAsync(string noteName)
        {
            string content = await ReadNoteAsync(noteName);
            var matches = Regex.Matches(content, @"#\w+");
            return matches.Cast<Match>().Select(match => match.Value).ToList();
        }

        public async Task ApplyTableAsync(string noteName, string[,] data)
        {
            StringBuilder sb = new StringBuilder();
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            for (int col = 0; col < cols; col++)
            {
                sb.Append($"| {data[0, col]} ");
            }
            sb.AppendLine("|");

            for (int col = 0; col < cols; col++)
            {
                sb.Append("| --- ");
            }
            sb.AppendLine("|");

            for (int row = 1; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    sb.Append($"| {data[row, col]} ");
                }
                sb.AppendLine("|");
            }

            await AppendContentAsync(noteName, sb.ToString());
        }

        public async Task BulkDeleteNotesAsync(Func<string, bool> filter)
        {
            var files = Directory.GetFiles(vaultPath, "*.md");
            foreach (var file in files)
            {
                var noteName = Path.GetFileNameWithoutExtension(file);
                if (filter(noteName))
                {
                    File.Delete(file);
                    noteCache.Remove(noteName);
                }
            }
        }
    }
}