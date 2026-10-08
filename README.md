# ProgramingAssistedAI

## AI-Assisted Software Development Standard

ProgramingAssistedAI is a reusable methodology for developing software projects with the assistance of AI.

The goal is not to replace the developer, but to provide a consistent way to plan, implement, verify, document, and maintain software projects while keeping the developer in control.

The methodology is designed to be independent of:

* Programming language
* Framework
* Database
* Operating system
* IDE or code editor
* Cloud provider
* Application type

It can be adapted to backend, frontend, full-stack, mobile, desktop, API, automation, CLI, and other software projects.

---

## Core Principles

The methodology is based on these principles:

1. **Define before implementing**
2. **Inspect before modifying**
3. **Reuse before creating**
4. **Work in small sequential phases**
5. **Verify before continuing**
6. **Keep persistent project context**
7. **Avoid unnecessary changes**
8. **Document important decisions**
9. **Stop after completing the assigned phase**
10. **Keep the developer in control**

---

## Project Documentation

A project using this methodology should maintain clear and persistent context.

Typical files include:

```text
README.md
AGENTS.md

docs/
├── project-state.md
├── architecture.md
├── domain-model.md
└── decision-log.md
```

Not every project requires every document.

Documentation should exist when it provides useful project context.

### README.md

Defines what the project is and what it should accomplish.

### AGENTS.md

Defines how AI agents should work inside the project.

### docs/project-state.md

Defines the current state of the project.

### docs/architecture.md

Defines the project's architecture and important boundaries.

### docs/domain-model.md

Defines important domain concepts and business rules when applicable.

### docs/decision-log.md

Records important technical and architectural decisions.

---

## Project Development Cycle

Projects follow a controlled development cycle:

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
UPDATE STATE
  ↓
STOP
  ↓
HUMAN APPROVAL
  ↓
NEXT PHASE
```

The cycle is repeated until the project is complete.

---

## AI's Role

AI is used as a development assistant.

AI may help with:

* Analysis
* Planning
* Implementation
* Testing
* Debugging
* Documentation
* Refactoring
* Research
* Repetitive development tasks

The developer remains responsible for:

* Requirements
* Scope
* Priorities
* Important technical decisions
* Architecture approval
* Verification
* Final acceptance

---

## Starting a New Project

Use:

```text
PROJECT-START-GUIDE.md
```

This document provides the step-by-step process for applying the methodology to a new project.

---

## Phase-Based Development

Projects should be divided into manageable phases.

Each phase should define:

* Objective
* Scope
* Constraints
* Expected changes
* Verification
* Stop condition

A phase is not complete simply because the AI reports that it is finished.

It must be verified according to the project's requirements.

---

## Repository

The methodology itself is maintained in this repository:

```text
ProgramingAssistedAI
```

The standard should evolve through real project experience.

When improvements are discovered during actual development, they can be incorporated into the methodology.

---

## Philosophy

The main objective is simple:

> Use AI to accelerate software development without losing understanding, control, verification, or maintainability.
