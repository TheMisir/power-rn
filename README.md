# power-rn
Power rename tool. A prototype for [#4642](https://github.com/microsoft/PowerToys/issues/4642).

# How it works?

1. Launch with `-d direcotory -f *.jpg` arguments
2. Searches all files within `directory` that matches `*.jpg`
3. Creates a temporary file and puts each file name to a new line
4. Opens editor (you can customize using `editor.(bat|sh)` or `-e path/to/editor.exe` argument).
5. Waits for editor to close
6. Checks for modified lines and renames files with matching line

## How to build?

_The app requires .NET Core 3.1 development kit to be installed._

```sh
cd src

# Install dependencies
dotnet restore

# Build project
dotnet build --configuration Release --no-restore

# Launch app
dotnet PowerRename.dll --help
```

## Notice

Use it at own risk. This tool is not well tested and may damage your files!
