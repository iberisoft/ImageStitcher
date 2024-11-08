# Image Stitcher

This tool stitches images based on a Python script.

## Installation

Open https://dotnet.microsoft.com/en-us/download/dotnet/8.0, download and install the following package:
* .NET Desktop Runtime

Copy the latest release to a folder and run `ImageStitcher`.

## Configuration

File `ImageStitcher.dll.config`:

Setting         | Description
----------------|--------------------------
`PythonPath`    | Path to a Python DLL
`ScriptPath`    | Path to the Python script
`PythonModules` | Python module names separated by commas which are used by the script

For example, `PythonPath` can be `C:\Users\<user>\AppData\Local\Programs\Python\Python312\python312.dll`.

**The tool supports Python v3.7-3.12 only.**

## Python Script

The script stitches two images by knowing an ROI that is visible on both. Then the script should save a resulting image.

The script is called with the following local parameters:

Parameter   | Description
------------|--------------------------
`src_path1` | Path to the first source image
`roi1`      | ROI on the first source image
`src_path2` | Path to the second source image
`roi2`      | ROI on the second source image
`res_path`  | Path to the resulting image

The script is called with the following global parameters:

Parameter   | Description
------------|--------------------------
`console`   | The script can print texts by calling `console.print(text)`; those texts will be displayed by the tool
