namespace SalesManagement.Core.Models;
public sealed class QuoteNumberSequence { public int Year { get; set; } public int Month { get; set; } public string Kind { get; set; } = "quote"; public int NextValue { get; set; } }
