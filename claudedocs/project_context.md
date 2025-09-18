# TextDiff.Sharp Project Context

## Project Overview
**TextDiff.Sharp** is a C# library for applying diff files to original text documents. The library processes unified diff format and patches changes onto source documents to produce updated versions.

## Project Structure
```
D:\data\TextDiff\
├── .git/                           # Git repository
├── .github/                        # GitHub workflows and templates
├── nupkg/                          # NuGet package artifacts
├── src/
│   ├── TextDiff.Sharp.sln          # Solution file
│   ├── TextDiff.Sharp/             # Main library project
│   │   ├── TextDiff.Sharp.csproj   # Project file (.NET Standard 2.0)
│   │   ├── TextDiffer.cs           # Main API entry point
│   │   ├── Core/                   # Core processing components
│   │   │   ├── DocumentProcessor.cs # Main diff application logic
│   │   │   ├── ChangeTracker.cs    # Change tracking
│   │   │   ├── ContextMatcher.cs   # Context line matching
│   │   │   └── DiffBlockParser.cs  # Diff parsing
│   │   ├── Helpers/                # Utility classes
│   │   ├── Models/                 # Data models
│   │   └── GlobalUsings.cs         # Global using directives
│   └── TextDiff.Tests/             # Unit tests project
│       ├── TextDiff.Tests.csproj   # Test project file
│       ├── DiffProcessorTests.cs   # Core functionality tests
│       ├── FileTests.cs            # File handling tests
│       └── TextComparisonHelper.cs # Test utilities
├── README.md                       # Project documentation
├── .gitignore                      # Git ignore rules
└── .gitattributes                  # Git attributes
```

## Key Components

### Main API
- **TextDiffer**: Primary entry point class with `Process(string document, string diff)` method
- Dependency injection support for core components via constructor

### Core Architecture
- **DocumentProcessor**: Handles diff application logic with indentation preservation
- **DiffBlockParser**: Parses unified diff format into structured blocks
- **ContextMatcher**: Finds matching positions in original document
- **ChangeTracker**: Tracks statistics about applied changes

### Processing Pipeline
1. Split document and diff into lines
2. Parse diff into structured blocks
3. Apply each block to document using DocumentProcessor
4. Return ProcessResult with updated text and change statistics

## Key Features
- **Diff Application**: Apply unified diff format to original documents
- **Indentation Preservation**: Smart handling of whitespace and indentation
- **Context Matching**: Accurate positioning using before/after context lines
- **Change Tracking**: Statistics on additions, deletions, modifications
- **Performance Optimized**: Efficient processing for large documents

## Technical Details
- **Target Framework**: .NET Standard 2.0 (broad compatibility)
- **Language**: C# with Preview language features
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled
- **Root Namespace**: TextDiff

## Development Environment
- Git repository (not initialized in this directory)
- Visual Studio solution structure
- xUnit test framework
- NuGet package configuration

## Testing
- Comprehensive unit tests in TextDiff.Tests project
- Tests cover various diff scenarios: simple replacements, insertions, deletions
- Test helper utilities for text comparison

## Recent Notes (from DocumentProcessor comments)
- Korean comments indicate specific requirements for indentation handling
- Changed lines preserve original line indentation
- Added lines use diff indentation as-is
- Focus on accurate line change/addition detection