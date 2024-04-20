using System.Text.RegularExpressions;
using System.Text;
using System.Linq.Expressions;

namespace Obsidian
{
    public class ObsidianAPI
    {
        private readonly string vaultPath;
        private readonly Dictionary<string, string> noteCache = new Dictionary<string, string>();
        private readonly object cacheLock = new object();

        public ObsidianAPI(string path)
        {
            vaultPath = path;
            Directory.CreateDirectory(vaultPath);
        }

        public void RenameNote(string originalName, string newName)
        {
            ValidateNoteName(originalName);
            ValidateNoteName(newName);

            string originalPath = Path.Combine(vaultPath, originalName + ".md");
            string newPath = Path.Combine(vaultPath, newName + ".md");

            if (!File.Exists(originalPath))
                throw new FileNotFoundException($"The note '{originalName}' does not exist.");

            if (File.Exists(newPath))
                throw new InvalidOperationException($"A note with the name '{newName}' already exists.");

            File.Move(originalPath, newPath);

            lock (cacheLock)
            {
                if (noteCache.ContainsKey(originalName))
                {
                    noteCache[newName] = noteCache[originalName];
                    noteCache.Remove(originalName);
                }
            }
        }

        public void DeleteNote(string noteName)
        {
            ValidateNoteName(noteName);

            string filePath = Path.Combine(vaultPath, noteName + ".md");
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The note '{noteName}' does not exist.");

            File.Delete(filePath);

            lock (cacheLock)
            {
                noteCache.Remove(noteName);
            }
        }

        private void ValidateNoteName(string noteName)
        {
            if (string.IsNullOrWhiteSpace(noteName))
                throw new ArgumentException("Note name cannot be null or whitespace.", nameof(noteName));
        }

        public async Task CreateNoteAsync(string noteName, string content)
        {
            ValidateNoteName(noteName);
            if (content == null) throw new ArgumentNullException(nameof(content), "Note content cannot be null.");

            string filePath = Path.Combine(vaultPath, noteName + ".md");
            await File.WriteAllTextAsync(filePath, content);

            lock (cacheLock)
            {
                noteCache[noteName] = content;
            }
        }

        public async Task AppendContentAsync(string noteName, string content)
        {
            ValidateNoteName(noteName);
            if (content == null) throw new ArgumentNullException(nameof(content), "Content to append cannot be null.");

            string filePath = Path.Combine(vaultPath, noteName + ".md");
            await File.AppendAllTextAsync(filePath, content);

            lock (cacheLock)
            {
                if (noteCache.ContainsKey(noteName))
                {
                    noteCache[noteName] += content;
                }
            }
        }

        public async Task<string> ReadNoteAsync(string noteName)
        {
            ValidateNoteName(noteName);

            lock (cacheLock)
            {
                if (noteCache.TryGetValue(noteName, out var cacheContent))
                    return cacheContent;
            }

            string filePath = Path.Combine(vaultPath, noteName + ".md");
            string content = await File.ReadAllTextAsync(filePath);

            lock (cacheLock)
            {
                noteCache[noteName] = content;
            }

            return content;
        }
        public async Task LinkNotesAsync(string noteName1, string listName, List<string> notes)
        {
            if (string.IsNullOrWhiteSpace(noteName1) || string.IsNullOrWhiteSpace(listName) || notes == null)
                throw new ArgumentException("Note names and list name cannot be null or empty.");

            StringBuilder append = new StringBuilder();
            append.AppendLine();
            append.AppendLine($" - {listName}");
            foreach (var item in notes)
            {
                if (string.IsNullOrWhiteSpace(item))
                    throw new ArgumentException("Note names in the list cannot be null or empty.");
                append.AppendLine($"\t - [[{item}]]");
            }
            await AppendContentAsync(noteName1, append.ToString());
        }

        public async Task<List<string>> SearchNotesAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                throw new ArgumentException("Search term cannot be null or empty.");

            if (noteCache.Count == 0)
            {
                await CacheAllNotesAsync();
            }
            return noteCache.Where(kv => kv.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
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
