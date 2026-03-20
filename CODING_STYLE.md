# CODING_STYLE

本檔列出本專案採用的詳細 C# 程式碼風格與工具建議，旨在維持可讀性與一致性。

1. General
  - File encoding: UTF-8 without BOM.
  - Line length: 120 characters maximum (soft limit).
  - Use `file-scoped namespaces` where appropriate.

2. Naming
  - Types (classes, structs, enums, interfaces): PascalCase.
  - Methods and properties: PascalCase.
  - Local variables and parameters: camelCase.
  - Private fields: `_camelCase` (leading underscore).
  - Constants: PascalCase for `const` and `static readonly`.
  - Async methods: suffix with `Async`.

3. Language features & style
  - Prefer explicit types; use `var` only when the type is obvious from the right-hand side.
  - Enable nullable reference types (`#nullable enable` or project-level setting).
  - Use expression-bodied members for short single-line methods/properties.
  - Prefer auto-properties (`public int Score { get; private set; }`).
  - Use pattern matching and modern C# constructs where they improve clarity.

4. Braces & formatting
  - Use braces for all control blocks (if/for/while) even for single statements.
  - Place opening brace on same line as declaration (K&R style).
  - Indent with 4 spaces; do not use tabs.

5. Comments & XML docs
  - All inline code comments and `//` comments must be written in English.
  - Public API XML documentation comments should be in English.
  - Use Chinese for high-level design notes in repository docs (e.g., README), not inline code comments.

6. Exceptions & logging
  - Catch specific exception types. Avoid empty catch blocks.
  - Use logging for recoverable errors; rethrow or wrap exceptions for fatal errors.

7. JSON beatmap format
  - Beatmap files are human-readable JSON. Use lowercase keys: `time` (seconds, float), `column` (int).
  - Example:

```json
{
  "notes": [ { "time": 0.5, "column": 0 }, { "time": 1.0, "column": 2 } ]
}
```

8. Tooling
  - Provide an `.editorconfig` and use `dotnet format` or IDE formatting to enforce style.
  - Project uses `nullable` and modern C#; keep project `LangVersion` up to date.

9. Commits & PRs
  - Commit message: short summary (<=50 chars), blank line, detailed body.
  - Open PRs against `main` with description of changes and testing instructions.

10. Example `.editorconfig` (recommended)

```
root = true

[*.cs]
charset = utf-8
indent_style = space
indent_size = 4
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
max_line_length = 120
```

遵守上述規範將使專案更易維護；如需調整規範，請在 PR 中討論並更新此檔。

