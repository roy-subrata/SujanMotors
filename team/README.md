# Team Knowledge Base — AI-Augmented Development

This folder is the knowledge base for developing SujanMotors with AI agents
(Claude Code and similar). Everything here exists so that a human engineer —
or an LLM acting on their behalf — starts every task with the same context a
senior team member would have.

## Folder Map

```
team/
  README.md                      ← you are here
  process/
    development-workflow.md      # How we plan, build, and verify (human + AI)
  templates/
    feature-spec.md              # Fill-in template: one file per feature
    feature-spec-example.md      # A completed example spec
  standards/                     # How code must be written
    architecture.md              # Clean architecture rules (layers, dependencies)
    api.md                       # REST API design standards
    coding.md                    # General C#/code style
    database.md                  # EF Core, migrations, SQL Server rules
    angular.md                   # Frontend standards
  agents/                        # Role definitions for specialized AI agents
    architect.md  backend.md  frontend.md  devops.md  qa.md  security.md
```

## How to use this with an AI agent

1. **Starting a feature?** Copy `templates/feature-spec.md`, fill it in, and
   give the spec to the agent as the task description. The spec — not the chat
   prompt — is the source of truth for scope.
2. **During development**, both humans and agents follow
   `process/development-workflow.md`. Point the agent at the relevant
   `standards/*.md` files for the layers it touches.
3. **Repo-wide conventions** (build commands, project layout) live in
   `/CLAUDE.md` at the repository root — agents load it automatically;
   humans should read it once.
4. Keep these documents **current-state only**. When a rule changes, edit the
   document — never append "UPDATE:" notes or keep outdated sections. Stale
   guidance is worse for an LLM than no guidance, because it is followed
   literally.

## Writing docs that LLMs use well

- Be **prescriptive**, not descriptive: "Use X. Never do Y." beats a discussion.
- Show a **Good / Avoid** pair for every rule that has a common wrong version.
- Use **real paths and real names** from this repo, not generic placeholders.
- State the **why** in one line when a rule is non-obvious — an agent that
  understands intent generalizes correctly to cases the rule didn't foresee.
- Keep each file scoped to one topic so it can be loaded selectively.
