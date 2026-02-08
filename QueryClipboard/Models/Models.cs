using System;
using System.Collections.Generic;

namespace QueryClipboard.Models
{
    public class QueryItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SqlQuery { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsed { get; set; }
        public int UsageCount { get; set; }
    }

    public class Category
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#2196F3";
    }

    public class AppSettings
    {
        public string HotkeyModifier { get; set; } = "Control+Alt";
        public string HotkeyKey { get; set; } = "Q";
        public StorageMode StorageMode { get; set; } = StorageMode.Json;
        public string? ConnectionString { get; set; }
        public List<Category> Categories { get; set; } = new();
    }

    public enum StorageMode
    {
        Json,
        SqlServer
    }
}
