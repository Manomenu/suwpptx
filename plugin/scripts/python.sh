#!/usr/bin/env bash
# Prints the Python command that can import pptx: the first of python3, python, "py -3" (exit 0).
# Windows often has no python3, or the Microsoft Store placeholder, which fails and is skipped here.
for candidate in python3 python "py -3"; do
  # shellcheck disable=SC2086 # Why: "py -3" is a command and its argument.
  if $candidate -c "import pptx" > /dev/null 2>&1; then
    echo "$candidate"
    exit 0
  fi
done
{
  echo "No Python with python-pptx found. Tried: python3, python, py -3."
  echo "Windows: winget install Python.Python.3.12, then in a new terminal: py -3 -m pip install --user python-pptx"
  echo "Fedora: sudo dnf install python3-pip, then: python3 -m pip install --user python-pptx"
} >&2
exit 1
