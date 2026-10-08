# Project Start Guide

## Purpose

This document explains how to start a new software project using the **ProgramingAssistedAI** development standard.

The process is technology-independent.

It can be used for projects involving:

* Backend
* Frontend
* Full-stack applications
* APIs
* Mobile applications
* Desktop applications
* CLI applications
* Automation
* Scripts
* Services
* Libraries
* Other software projects

The programming language, framework, database, cloud provider, and development tools are selected according to the requirements of each project.

---

# 1. Create the Project Workspace

Create a directory for the new project.

Example:

```bash
mkdir MyProject
cd MyProject
```

The project name should describe the actual software being developed.

Do not start implementing application features yet.

---

# 2. Obtain the ProgramingAssistedAI Standard

The standard can be obtained in one of two ways.

## Option A — Clone the Standard Repository

If the standard repository is available remotely:

```bash
git clone https://github.com/CristiamSanchez/ProgramingAssistedAI.git
```

Then use the required files and templates from that repository.

## Option B — Use a Local Copy

If the standard already exists on the computer, copy the required standard files into the new project.

The important idea is that the new project receives the standard's methodology, templates, and instructions.

---

# 3. Create the Project Structure

A new project should normally start with a structure similar to:

```text
MyProject/
├── README.md
├── AGENTS.md
├── PROJECT-START-GUIDE.md
│
├── docs/
│   ├── project-state.md
│   ├── architecture.md
│   ├── domain-model.md
│   └── decision-log.md
│
└── prompts/
```

Not every project will require every file.

The project definition determines which files are necessary.

The standard should not force unnecessary architecture or documentation.

---

# 4. Open the Project in the Development Environment

Open the project using the editor or IDE selected for the project.

Examples include:

* VS Code
* Cursor
* JetBrains IDEs
* Visual Studio
* Other suitable development environments

The editor does not change the development methodology.

---

# 5. Define the Project Before Writing Code

Do not immediately ask the AI to build the application.

First define what is being built.

The project definition should answer:

* What problem does the project solve?
* Who will use it?
* What are the main objectives?
* What are the main features?
* What is explicitly out of scope?
* What technical constraints exist?
* What technologies are required or available?
* What security requirements exist?
* What deployment requirements exist?
* What testing requirements exist?

The result becomes the project's initial source of truth.

This information should be recorded in:

```text
README.md
```

---

# 6. Configure AGENTS.md

Create or update:

```text
AGENTS.md
```

This file defines how AI coding agents should work inside the project.

It should contain rules such as:

* Inspect before modifying.
* Reuse existing code before creating new code.
* Follow the project's architecture.
* Make small, controlled changes.
* Do not perform unrelated refactoring.
* Do not recreate existing functionality.
* Work one phase at a time.
* Verify changes before considering a phase complete.
* Update project documentation when required.
* Do not continue automatically to the next phase.
* Respect explicit project constraints.
* Ask for clarification when requirements are genuinely ambiguous.

`AGENTS.md` describes **how the AI should work**.

It does not replace the project requirements.

---

# 7. Create the Project State

Create:

```text
docs/project-state.md
```

This file records the current state of the project.

At minimum it should contain:

```text
Current Phase
Completed Phases
Current Work
Next Phase
Known Issues
Verification Status
Important Notes
```

The purpose is to prevent the AI from repeatedly analyzing the entire project.

The AI should read the current state before starting work.

After completing a phase, the state must be updated.

---

# 8. Define the Architecture

Create:

```text
docs/architecture.md
```

The architecture document should describe:

* Main components
* Responsibilities
* Dependencies
* Communication between components
* Important boundaries
* Technical constraints
* Architectural decisions

The architecture must be appropriate for the actual project.

Do not apply a specific architecture simply because it is popular.

The project requirements determine the appropriate architecture.

---

# 9. Define the Domain or Core Concepts

If the project contains meaningful business rules, create:

```text
docs/domain-model.md
```

Document:

* Main entities or concepts
* Relationships
* Important rules
* Valid states
* Restrictions
* Important workflows
* Business terminology

For projects without meaningful business/domain rules, this document may be simplified or omitted.

---

# 10. Record Important Decisions

Use:

```text
docs/decision-log.md
```

for important technical or architectural decisions.

Each decision should explain:

```text
Decision
Reason
Alternatives considered
Consequences
```

Example:

```text
Decision:
Use technology X.

Reason:
It satisfies requirement Y and is already supported by the target environment.

Alternatives:
Technology A
Technology B

Consequences:
The project will require...
```

The purpose is to preserve the reasoning behind important decisions.

---

# 11. Create the Development Phases

Before significant implementation begins, divide the project into manageable phases.

Example:

```text
Phase 1 — Project Foundation
Phase 2 — Core Model
Phase 3 — Persistence
Phase 4 — Application Logic
Phase 5 — API
Phase 6 — Authentication
Phase 7 — Authorization
Phase 8 — Testing
Phase 9 — Integration
Phase 10 — Deployment
Phase 11 — Documentation
```

These are examples only.

The phases must be adapted to the actual project.

Each phase should define:

### Objective

What the phase must accomplish.

### Scope

What can be changed.

### Constraints

What must not be changed.

### Verification

How completion will be verified.

### Stop Condition

When the AI must stop.

---

# 12. Start the First Phase

Use a phase-specific prompt.

The prompt should instruct the AI to:

1. Read the project documentation.
2. Read `AGENTS.md`.
3. Read `docs/project-state.md`.
4. Inspect the existing project.
5. Determine what already exists.
6. Determine what is missing.
7. Implement only the current phase.
8. Reuse existing functionality where possible.
9. Run the appropriate verification.
10. Fix problems related to the current phase.
11. Update the project state.
12. Stop.

The AI must not automatically implement future phases.

---

# 13. Inspect Before Implement

This is a fundamental rule of the standard.

Before creating or modifying anything, the AI should determine:

```text
What exists?
What is already implemented?
What can be reused?
What is missing?
What is incorrect?
What is required for the current phase?
```

The AI should not assume that the project is empty.

---

# 14. Implement Small Changes

A phase should solve a specific group of related problems.

Avoid prompts such as:

```text
Build the entire application.
```

Prefer:

```text
Implement the authentication foundation.
```

or:

```text
Implement the persistence layer for the existing model.
```

Small changes make failures easier to identify and corrections easier to perform.

---

# 15. Verify the Phase

A phase is not complete because the AI reports:

```text
Done.
```

The phase is complete only when the appropriate verification succeeds.

Depending on the project, verification may include:

* Build
* Compilation
* Unit tests
* Integration tests
* Static analysis
* Linting
* Type checking
* Security checks
* Manual verification
* Runtime verification
* Database verification
* API verification

Only use the verification methods relevant to the project.

---

# 16. Update the Project State

After successful verification, update:

```text
docs/project-state.md
```

Record:

```text
Completed phase
Changes made
Verification performed
Results
Known limitations
Next phase
```

This becomes the persistent memory of the project.

---

# 17. Stop After the Phase

After completing the assigned phase, the AI must stop.

It should not automatically continue with:

```text
Next phase
Additional refactoring
New features
Architecture changes
"Improvements"
```

unless explicitly requested.

The human decides when to continue.

---

# 18. Start the Next Phase

When the previous phase has been verified, execute the next phase.

The AI should again:

```text
Read context
↓
Inspect current state
↓
Implement only current phase
↓
Verify
↓
Update state
↓
STOP
```

The process repeats until the project is complete.

---

# 19. Handle Problems Without Losing Project State

If a phase fails:

```text
Phase
   ↓
Verification
   ↓
FAIL
```

Do not simply continue to the next phase.

Instead:

1. Record the problem.
2. Identify the cause.
3. Correct the issue.
4. Re-run verification.
5. Update project state.
6. Continue only when the phase is valid.

A failed phase remains incomplete.

---

# 20. Review the Project Before Completion

Before considering the project complete, review:

### Functionality

Does the implemented functionality satisfy the requirements?

### Architecture

Are the defined boundaries still respected?

### Tests

Are important behaviors verified?

### Security

Are secrets, credentials, authentication, authorization, and sensitive data handled appropriately?

### Documentation

Is the project understandable to another developer?

### Configuration

Are environment-specific values separated from source code?

### Repository

Is the repository clean and free of unnecessary files?

### Deployment

Can the project be built and deployed according to its requirements?

---

# 21. Prepare the Repository

Before publishing the project:

Check:

```text
Source code
Documentation
Tests
Configuration
Ignore files
Secrets
Generated files
Build artifacts
Dependencies
```

Never commit:

```text
Passwords
API keys
Private credentials
Production secrets
Private certificates
Local environment files containing secrets
Build artifacts
Temporary files
```

Use appropriate environment/configuration mechanisms for sensitive values.

---

# 22. Initialize Version Control

If the project is not already a Git repository:

```bash
git init
```

Review the files:

```bash
git status
```

Review ignored files and sensitive files before committing.

Then:

```bash
git add .
git commit -m "Initial project setup"
```

---

# 23. Connect the Remote Repository

Create the repository in the selected Git hosting service.

Then configure the remote:

```bash
git remote add origin <repository-url>
```

Rename the main branch if necessary:

```bash
git branch -M main
```

Push:

```bash
git push -u origin main
```

Never force-push unless there is a specific and understood reason.

---

# 24. Maintain the Project

The standard continues to apply after the initial implementation.

Future changes should follow the same process:

```text
Requirement
    ↓
Inspect
    ↓
Plan
    ↓
Small Phase
    ↓
Implement
    ↓
Verify
    ↓
Document
    ↓
Update State
    ↓
STOP
```

---

# 25. The Core Rule

The most important rule of ProgramingAssistedAI is:

> **The AI assists the developer; it does not replace the developer's control of the project.**

The developer remains responsible for:

* Requirements
* Decisions
* Priorities
* Scope
* Architecture approval
* Verification
* Acceptance of changes

The AI is responsible for assisting with:

* Analysis
* Implementation
* Testing
* Documentation
* Refactoring when requested
* Problem investigation
* Repetitive development tasks

---

# 26. Standard Development Cycle

The complete methodology can be summarized as:

```text
DEFINE
   ↓
DOCUMENT
   ↓
PLAN
   ↓
INSPECT
   ↓
IMPLEMENT
   ↓
VERIFY
   ↓
DOCUMENT STATE
   ↓
STOP
   ↓
HUMAN APPROVAL
   ↓
NEXT PHASE
```

Repeat until the project is complete.

---

# 27. Quick Start Checklist

When starting a new project:

```text
[ ] Create project directory
[ ] Obtain ProgramingAssistedAI standard
[ ] Create project README.md
[ ] Create AGENTS.md
[ ] Create docs/project-state.md
[ ] Create architecture documentation if needed
[ ] Create domain documentation if needed
[ ] Create decision log
[ ] Define project requirements
[ ] Define scope
[ ] Define constraints
[ ] Select technology
[ ] Define architecture
[ ] Divide project into phases
[ ] Create the first phase prompt
[ ] Inspect before implementing
[ ] Implement only the current phase
[ ] Verify the phase
[ ] Update project state
[ ] Stop
[ ] Approve next phase
[ ] Repeat
[ ] Perform final review
[ ] Check for secrets
[ ] Initialize Git
[ ] Commit
[ ] Connect remote repository
[ ] Push
```

This checklist is the starting point for every new project using the ProgramingAssistedAI standard.
