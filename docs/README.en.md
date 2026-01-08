# UTerminal

Default is Korean. Choose your language:
- [한국어](README.md)

This document Translated from Korean with AI.

---
## Introduction
UTerminal is a cross-platform UART communication monitoring application based on Avalonia UI. This program is designed to make serial port communication management easier by supporting various encoding types and custom settings. With UTerminal, users can select encoding types such as ASCII, HEX, and UTF-8, configure custom ComPort paths and Baudrates, and utilize macro functionality for quick transmission.

## Overview
The legacy `Terminal` program used on `Windows` was primarily utilized for hardware communication and data monitoring. However, it experienced performance degradation when processing high-speed data or accumulating large amounts of data, which highlighted the need for improvements.

As an actual user of this program, I started developing a new application based on the inconveniences I experienced and thoughts like "I wish this feature existed." In particular, I developed it as a cross-platform application that is not restricted by operating systems, so that all users can use it conveniently.

The current program was created by emulating the `Terminal` program.

## New Features
- Serial data parsing through user-defined presets
  - Update data information under the name and each data type
  - Data parsing success verification through indicators
  - Configurable user-defined presets
  - Variable length packet parsing feature added
  - Preset save and load functionality added

## Features
- Connection functionality with serial port devices
- Support for ASCII, HEX, UTF-8 encoding types
- Support for CR+LF, STX-ETX, Custom STX-ETX reading methods
  - Data reception based on packet size settings
  - If packet size is 0 or unset, data is returned immediately upon ETX detection
- Custom port configuration and custom baudrate settings
- Quick transmission through macro functionality
- Program status logging functionality
- Serial data logging functionality

---

## ***TODO*** List
- Transmit serial data via TCP/UDP

---

# Screenshot

### Main Screen
![main.png](Images/main.png "MacOS Running")

### Preset Mode Screen
![preset.png](Images/preset.png "Preset Mode")

---

# Usage

### Basic Mode

#### Data Reception
1. First, select a serial port and choose the appropriate Baudrate, Parity, Data bits, etc.
2. Press the Connect button to start.
3. Adjust the Encoding and ReadType according to the incoming data and verify the output on the screen.

※ If you can't find the port you're looking for, press the Rescan button to refresh. <br>
※ If using STX/ETX, you can set the packet size to receive and monitor data of that size.

#### Data Transmission
1. Enter the desired data in the Input field at the bottom of the screen.
2. Transmit the data by pressing Enter or the Send button.

#### Hex Data Transmission Method
You can create hex data by prefixing with the **$** symbol.

Examples:
- $01 → 0x01
- $FF → 0xFF

※ This applies to both the Input on the main screen and the Input in the Macro Window.


### User-Defined Preset Mode
1. First, connect the serial port.
2. Press `Preset Mode` to enter the new window.
3. Press `New Preset` at the top of the tab to add a new preset.
  - A preset must be selected at the bottom to add fields.
4. Add fields from the preset tab on the left.
  1. Set the field name
  2. Set the field's data type
5. Start parsing by pressing the `Start` button at the top.

※ Presets can be saved and loaded

<br>

#### Variable Data (Variable Length Data Parsing)
1. Activate by checking `Variable data`
2. Set the size of the length data for variable length in DataType
  - e.g., If the length data type is 8 bytes, set it to Uint8
3. Verify that a pair of data has been added

※ Data is parsed by Byte for that length. <br>
※ The order in which data length arrives can be changed <br>
※ You can identify which data field the data length is connected to by its name

---

# Build

## Requirements

- Dotnet 8 or higher
- [Avalonia UI](https://github.com/AvaloniaUI/Avalonia) 11.x.x
- [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) 11.1.0
- [Avalonia.Xaml.Behaviors](https://github.com/wieslawsoltes/Avalonia.Xaml.Behaviors) 11.2.0.x
- [log4net](https://github.com/apache/logging-log4net) 3.0.3


## Deployment

Deployment is possible for each platform.

You can proceed with the build as follows according to your desired platform.

**Note:**
Currently, for arm64, building is possible but execution has not been tested and cannot be confirmed.

#### Windows
```shell
dotnet publish -c Release -r win-x64
```

#### MacOS
```shell
# If it is the M series, you can publish it as osx-arm64 (not tested)
dotnet publish -c Release -r osx-x64
```

#### Linux
```shell
# If using arm, you can publish it as linux-arm (not tested)
dotnet publish -c Release -r linux-x64
```

---

## License

[LICENSE](LICENSE)
