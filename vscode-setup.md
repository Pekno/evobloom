# VS Code Configuration Templates

This directory contains template files for VS Code configuration.
Copy these to `.vscode/` and customize the paths for your local setup.

## Files to create in .vscode/:

### settings.json
```json
{
    "godotTools.editorPath.godot4": "PATH_TO_YOUR_GODOT_EXECUTABLE"
}
```

### launch.json  
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Play",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "PATH_TO_YOUR_GODOT_EXECUTABLE",
            "args": [],
            "cwd": "${workspaceFolder}",
            "stopAtEntry": false,
        }
    ]
}
```

### tasks.json
```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build"
            ],
            "problemMatcher": "$msCompile"
        }
    ]
}
```

## Setup Instructions:
1. Create `.vscode/` directory in project root (it's gitignored)
2. Copy the JSON files above into `.vscode/`
3. Replace `PATH_TO_YOUR_GODOT_EXECUTABLE` with your actual Godot installation path
4. Customize as needed for your development environment