# Agent Skill Requirement

When validating or testing C# and Godot code in this project, the agent must use:

`dotnet build`

This is the required baseline build/test command before reporting code as verified.

When reading project files, the agent must read files directly using file tools, not through PowerShell (pwsh).