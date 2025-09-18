# TextDiff.Sharp Documentation

This directory contains the source files for generating comprehensive API documentation for TextDiff.Sharp.

## Prerequisites

1. Install DocFX:
   ```
   dotnet tool install -g docfx
   ```

## Generating Documentation

### Quick Build
```powershell
# From the repository root
.\scripts\generate-docs.ps1
```

### Development Mode (with live server)
```powershell
# Build and serve documentation locally
.\scripts\generate-docs.ps1 -Serve
```

### Clean Build
```powershell
# Clean previous builds and regenerate
.\scripts\generate-docs.ps1 -Clean
```

## Manual Generation

If you prefer to run DocFX commands manually:

```bash
# Navigate to docs directory
cd docs

# Generate metadata from source code
docfx metadata docfx.json

# Build documentation site
docfx build docfx.json

# Serve locally (optional)
docfx serve _site
```

## Documentation Structure

- `docfx.json` - DocFX configuration file
- `index.md` - Main documentation homepage
- `toc.yml` - Top-level table of contents
- `articles/` - Documentation articles and guides
- `api/` - Auto-generated API reference (created during build)
- `_site/` - Generated documentation site (created during build)

## Generated Output

The documentation generation process creates:

1. **API Reference**: Automatically generated from XML documentation comments in the source code
2. **Articles**: Hand-written guides and examples
3. **Searchable Site**: Complete documentation website with search functionality

## Customization

- Edit `docfx.json` to modify build settings
- Add new articles in the `articles/` directory
- Update `toc.yml` files to modify navigation structure
- Customize themes and styling as needed

## Deployment

The generated `_site` directory contains a complete static website that can be:
- Deployed to GitHub Pages
- Hosted on any static site hosting service
- Served locally for development

## Troubleshooting

If you encounter issues:

1. Ensure all projects build successfully: `dotnet build`
2. Verify DocFX is installed: `docfx --version`
3. Check that XML documentation is enabled in project files
4. Run with clean build: `.\scripts\generate-docs.ps1 -Clean`