<a href="Ω"><img src="http://readme-typing-svg.herokuapp.com?font=VT323&size=90&duration=2000&pause=1000&color=39008a&center=true&random=false&width=1100&height=140&lines=%E2%98%A6++ObsidianAPI++%E2%98%A6;%E2%98%A6++By+xyeizo++%E2%98%A6;" alt="Ω" /></a>


The Obsidian API is a .NET tool for managing markdown notes in an Obsidian vault, allowing you to create, edit, and search notes easily.

## Features

- Create, read, update, and delete markdown notes.
- Append content to existing notes.
- Link multiple notes.
- Search for notes containing specific terms.
- Add and retrieve tags from notes.
- Implement tables and bulk operations on notes.


## Usage

### Initialize the API

First, you need to initialize the API with the path to your Obsidian vault:

```csharp
var api = new ObsidianAPI("path_to_your_vault");
```

### Creating a Note

To create a new note with initial content:

```csharp
await api.CreateNoteAsync("SampleNote", "This is the initial content of the note.");
```

### Reading a Note

To read the content of an existing note:

```csharp
string content = await api.ReadNoteAsync("SampleNote");
Console.WriteLine("Note Content: " + content);
```

### Appending Content to a Note

To append content to an existing note:

```csharp
await api.AppendContentAsync("SampleNote", "\nThis is additional content appended to the note.");
```

### Deleting a Note

To delete a note:

```csharp
api.DeleteNote("SampleNote");
```
