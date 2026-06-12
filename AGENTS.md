# **🛠 Universal AI Agent Standards & Repository Health**

This document serves as the mandatory operational framework for all AI Agents (GitHub Copilot, Cursor, Codex, etc.) interacting with this repository. Adherence to these standards is required for all generated code, tests, scripts, and commits.

## **1. Git Commit Standards: Conventional Commits**

To maintain a clean, machine-readable, and professional history, you must strictly adhere to the [Conventional Commits](https://www.conventionalcommits.org/) specification.

* **Format:** <type>(<scope>): <description>
* **The Imperative Mood:** Always use the imperative, present tense. Use "Add feature" instead of "Added feature" or "Adds feature."
* **Case Sensitivity:** The type and scope must be strictly **lowercase**.
* **Granularity:** If a task involves both a refactor and a new feature, you are required to split them into two distinct commits.
* **Scope:** This is mandatory and must represent the specific module or component affected (e.g., repo, agents, routing, github).
* **Authors:** Ensure that commits are authored by the authenticated user only; do not add co-authors.
* **Branch Names:** Agent-created branches must use lowercase prefixes such as `feature/`, `fix/`, `chore/`, `docs/`, `refactor/`, `test/`, `perf/`, or `research/`.
* **Issue Branches:** When a GitHub issue number exists, include it in the branch name, for example `feature/issue-123-add-oauth-provider`.
* **PR Re-iteration:** Review, verification, and remediation passes must stay on the current PR head branch. Do not create a second branch for the same PR.

| Type | Use Case | Example |
| :---- | :---- | :---- |
| **feat** | A new feature for the user. | feat(auth): add OAuth2 provider |
| **fix** | A bug fix for the user. | fix(api): resolve null pointer in user-lookup |
| **docs** | Documentation-only changes. | docs(automation): explain assigned issue prerequisites |
| **refactor** | Code change that neither fixes a bug nor adds a feature. | refactor(db): flatten repository hierarchy |
| **test** | Adding missing tests or correcting existing tests. | test(vault): add boundary checks for encryption |
| **chore** | Updating build tasks, package manager configs, etc. | chore(deps): bump Newtonsoft.Json to 13.0.3 |
| **ci** | CI workflow or automation pipeline changes. | ci(github): add release workflow validation |
| **perf** | Performance improvements. | perf(parser): reduce tokenization allocations |
| **tool** | Automation scripts or internal dev-tooling. | tool(automation): add C# script for log rotation |

## **2. Testing: The "Anti-Pollution" Mandate**

We prioritize **quality and logical failure paths** over coverage metrics. You are forbidden from generating "shallow" or "ritualistic" tests.

* **Ban on Mock-Only Tests:** Do not write tests that only verify if a mock was called (e.g., _mock.Verify(x => x.Save(), Times.Once)). This tests implementation details (how the code is written), not business behavior (what the code does).
* **The "Mutation" Requirement:** Every test must be designed so that if the underlying logic is changed or deleted, the test **fails**. If a test passes after the logic it is supposedly testing is removed because everything is mocked, the test is pollution and must be deleted.
* **Behavioral Focus:** Focus on state changes, return values, and edge cases. If a method is a simple "Pass-Through" (calling another service with no internal logic), **do not unit test it.**
* **Dependency Limit:** If a unit test requires more than 3 Mock<T> objects, the code is too highly coupled. Stop and suggest a refactor or write an **Integration Test** instead.

## **3. TUnit: Microsoft Testing Platform Rules**

This repository uses **TUnit on Microsoft.Testing.Platform (MTP)**, not VSTest. Agents must use TUnit-compatible filters, timeouts, and hang-debugging options.

* **Never use VSTest filters:** Do not run `dotnet test --filter ...` with TUnit. Use `--treenode-filter` instead. If platform args are rejected, pass them after `--`.
* **Filter shape:** TUnit filters use `/Assembly/Namespace/Class/TestName[Property=Value]`. Common patterns: class `/*/*/ClassName/*`, method `/*/*/*/MethodName`, category `/*/*/*/*[Category=Integration]`, exclude category `/*/*/*/*[Category!=Slow]`, and combined properties `/*/*/*/*[(Category=Smoke)&(Priority=High)]`.
* **Tag tests explicitly:** Prefer `[Property("Category", "Integration")]`, `[Property("Priority", "High")]`, and other explicit metadata so filters stay stable and self-documenting.
* **Timeout defaults are mandatory:** Add an assembly-level `[assembly: Timeout(...)]` and override slower classes or methods with more specific `[Timeout(...)]` attributes.
* **Cancellation is mandatory for async and IO tests:** Every async, integration, or polling-heavy test must accept `CancellationToken cancellationToken` and pass it to HTTP calls, DB calls, delays, process waits, and application startup/shutdown.
* **No sync-over-async or uncancellable waits:** Do not use `.Wait()`, `.Result`, `.GetAwaiter().GetResult()`, uncancellable `Task.Delay(...)`, or polling loops that ignore the token.
* **Parallelism can look like hangs:** TUnit runs tests in parallel by default. If a full-suite run hangs but a filtered run passes, suspect shared ports, shared databases, static locks/state, or non-thread-safe fixtures. Use `[NotInParallel]`, `[ParallelGroup]`, `[ParallelLimiter<T>]`, or lower global parallelism.
* **Use MTP run-level watchdogs for CI:** Prefer `dotnet test -c Release --timeout 10m` for suite timeouts, or `dotnet test -c Release -- --timeout 10m` when the SDK requires explicit forwarding.
* **Use hang dumps for true deadlocks:** When cooperative cancellation is not enough, add `Microsoft.Testing.Extensions.HangDump` and use `--hangdump --hangdump-timeout <duration>` instead of VSTest-only `--blame-hang`.

```text
TUnit quick rules:
- Preferred filter run: dotnet test -c Release --treenode-filter "<filter>"
- Forwarded filter run: dotnet test -c Release -- --treenode-filter "<filter>"
- Preferred timeout run: dotnet test -c Release --timeout 10m
- Filter shape: /Assembly/Namespace/Class/TestName[Property=Value]
```

## **4. Automation: C# 10+ File-Based Apps**

**Bash and PowerShell are deprecated in this repository.** All automation, maintenance, and tooling must be written as **C# 10 File-Based Apps**.

* **Standalone Execution:** Use the single-file format that runs via dotnet run <filename>.cs.
* **NuGet Integration:** Use the `#:package` directive at the top of the file to manage dependencies.
* **No Boilerplate:** Do not use namespace, class Program, or static void Main. Write logic directly using Top-Level Statements.
* **Portability:** Use Path.Combine or forward slashes. Scripts must be execution-ready on Windows, macOS, and Linux without modification.
* **Option Forwarding:** When invoking a file-based app and forwarding option-style args to the script, insert `--` after the script file. *Correct:* `dotnet run Build.cs -- test -c Release`. *Incorrect:* `dotnet run Build.cs test -c Release` because the outer `dotnet run` consumes `-c`.
* **Example Structure:**
  `#!/usr/bin/env dotnet`
  `#:package Newtonsoft.Json@13.0.3`
  `#:package Spectre.Console@0.49.1`

  `using Newtonsoft.Json;`
  `using Spectre.Console;`

  `// Logic starts here...`
  `AnsiConsole.MarkupLine("[bold green]Executing repo automation...[/]");`

## **5. Modern C# 14 Idioms**

Always favor the most concise, high-performance syntax available in C# 14. Do not generate legacy C# code styles.

* **The field Keyword:** For properties with logic, use the field keyword instead of declaring explicit backing fields.
  * *Correct:* `public int Quality { get; set => field = Math.Clamp(value, 0, 100); }`
* **Collection Expressions:** Use the `[]` syntax for all collection initializations and the spread operator `..` for concatenations.
  * *Correct:* `string[] items = ["alpha", "beta", "gamma"];`
  * *Correct:* `var combined = [..existingItems, newItem];`
* **Primary Constructors:** Use primary constructors for all classes and structs, particularly for dependency injection.
  * *Correct:* `public class OrderService(IDbContext db, ILogger log) { ... }`
* **Terseness:** If a method or property can be expressed in a single line, use the expression-bodied member syntax (`=>`).
* **Null-State Safety:** Use `is not null` and the null-coalescing assignment operator `??=`. Avoid redundant manual null checks where the compiler's static analysis already provides safety.

## **6. Agent Self-Correction Protocol**

Before finalizing any output, the Agent must perform an internal "Pre-Flight Check":

1. **Logic Check:** Does the generated test actually catch a logic error, or is it just mocking a call?
2. **TUnit Check:** If I touched tests or test docs, did I use TUnit/MTP-compatible filters, timeouts, cancellation tokens, and parallelism controls instead of VSTest-only flags?
3. **Script Check:** Is this automation a `.cs` file? If it is `.ps1` or `.sh`, it must be converted.
4. **Syntax Check:** Am I using the `field` keyword, `[]` collections, and Primary Constructors?
5. **Build.cs Arg Check:** If I documented or invoked `Build.cs` with forwarded option-style args, did I include `--` after `Build.cs`?
6. **Commit Check:** Is my proposed commit message formatted as `type(scope): description`?

**Failure to comply:** If an Agent is informed it has violated these rules, it must immediately revert the offending code and provide a compliant correction.
