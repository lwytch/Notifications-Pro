# Contributing to Notifications Pro

Thank you for your interest in contributing!

## Getting Started

1. Fork the repository
2. Clone your fork and create a feature branch
3. Build and test:
   ```bash
   dotnet restore src/NotificationsPro/NotificationsPro.csproj
   dotnet restore tests/NotificationsPro.Tests/NotificationsPro.Tests.csproj
   dotnet build src/NotificationsPro/NotificationsPro.csproj
   dotnet test tests/NotificationsPro.Tests/NotificationsPro.Tests.csproj
   ```
4. Make your changes
5. Submit a pull request with a clear description

Release packaging and signing are maintainer-only local workflows and are not required for normal contributions.

## Guidelines

### Code Conventions
- Follow MVVM pattern — Views bind to ViewModels; no business logic in code-behind
- PascalCase for public members, `_camelCase` for private fields
- One class per file, named after the class
- Use `INotifyPropertyChanged` for data binding, never manipulate UI directly from services

### Privacy Rules (Hard Constraints)
- **Never persist notification content** — no files, database, cache, logs, or telemetry
- Notification text exists only in RAM for rendering
- Overflow stores only a count, never content
- See `AGENTS.md` for the full privacy policy

### Before Submitting
- [ ] `dotnet build src/NotificationsPro/NotificationsPro.csproj` succeeds with no errors or warnings
- [ ] `dotnet test tests/NotificationsPro.Tests/NotificationsPro.Tests.csproj` passes all tests
- [ ] No notification content in any file I/O or serialization
- [ ] No `settings.json` (real user settings) committed
- [ ] Update `docs/PLAN.md` and `docs/STATUS.md` if scope changed
- [ ] No `.github/workflows/` files, paid services, or cloud dependencies added

### What We Accept
- Bug fixes with clear reproduction steps
- New features that align with the project roadmap (see `docs/PLAN.md`)
- Accessibility improvements
- Performance optimizations
- Documentation improvements

### What We Don't Accept
- Features that persist notification content
- External API dependencies or telemetry
- CI/CD workflow files
- Changes that require paid services

## Reporting Issues

Use [GitHub Issues](../../issues) to report bugs or request features. Include:
- Windows version (10/11, build number)
- Steps to reproduce
- Expected vs. actual behavior
- Screenshots if applicable

## License

By contributing, you agree that your contributions will be licensed under the GNU General Public License v3.0.
