# Development Rules

Version: 1.0

---

# Mission

Every project must be built as if it were intended for production.

Learning is important, but production quality is the objective.

---

# Development Workflow

Every new feature follows this workflow:

1. Understand the business problem.
2. Clarify requirements.
3. Identify constraints.
4. Design the architecture.
5. Design the folder structure.
6. Define implementation tasks.
7. Implement incrementally.
8. Test.
9. Review.
10. Document.

Never skip architecture.

---

# Before Writing Code

Always answer:

- What problem are we solving?
- Why is this the best solution?
- What alternatives exist?
- What are the trade-offs?
- How will this scale?

---

# Project Structure

Every project should contain, when applicable:

- README.md
- LICENSE
- .gitignore
- .editorconfig
- Docker support
- Docker Compose
- Configuration files
- Environment variables
- Documentation
- Unit Tests
- Integration Tests (when justified)

---

# Source Control

Commit often.

Each commit should represent one logical change.

Preferred commit style:

- feat:
- fix:
- refactor:
- docs:
- test:
- chore:

Example:

feat: add customer registration endpoint

---

# Documentation

Every important decision must be documented.

Architecture is part of the project.

Documentation is never optional.

---

# Code Review Checklist

Before considering a feature complete:

✔ Code compiles

✔ No warnings

✔ No duplicated code

✔ Correct naming

✔ SOLID respected

✔ Logging added

✔ Exceptions handled

✔ Validation implemented

✔ Configuration externalized

✔ Documentation updated

---

# Error Handling

Never ignore exceptions.

Always:

- Log important failures.
- Return meaningful messages.
- Avoid exposing internal details.
- Fail gracefully whenever possible.

---

# Logging

Every application should include logging.

Log:

- Startup
- Shutdown
- Errors
- Warnings
- Important business events

Avoid excessive logging.

---

# Configuration

Never hardcode:

- API Keys
- Connection Strings
- Secrets
- Passwords

Use:

- Environment Variables
- appsettings
- Secret Managers when applicable

---

# Testing

Testing should provide confidence.

Prioritize:

- Business Rules
- Domain Logic
- Critical Services

Avoid testing framework internals.

---

# Refactoring

Refactor continuously.

Never wait until the end.

Small improvements are preferred over massive rewrites.

---

# AI Assisted Development

AI is a development accelerator.

Before accepting AI generated code:

- Understand it.
- Review it.
- Improve it.
- Adapt it to project standards.

Never merge code you do not understand.

---

# Portfolio Quality

Every completed project should be good enough to:

- Publish on GitHub.
- Explain during an interview.
- Demonstrate engineering skills.
- Demonstrate architectural reasoning.
- Showcase AI integration.

---

# Continuous Improvement

Every project should improve the previous one.

The objective is not only to finish projects.

The objective is to become a better engineer.