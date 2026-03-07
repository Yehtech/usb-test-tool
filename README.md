USB Suspend/Resume Tester v1.0.0
A desktop tool for continuous USB device suspend/resume cycle testing, designed to verify system and hardware stability after repeated suspend and resume operations.

Features
Auto-detection and enumeration of connected USB devices

Customizable suspend/resume delays and test intervals

Supports infinite loop or custom cycle stress testing

Real-time colored logs and status validation (PASS / FAIL)

Test log export (.txt format)

Safety mechanism (auto-enables device upon test interruption)

Platform
Windows 10 / 11

Python 3.10+

tkinter (GUI)

pnputil (Device control)

Use Case
This tool is designed for:

USB hardware stability verification

Driver and firmware development debugging

QA automated stress testing

Simulating repeated plugging/unplugging (for non-critical system devices)

Future Roadmap
Multi-USB device testing support

Automatic test analysis report generation

CLI mode for automated script integration
