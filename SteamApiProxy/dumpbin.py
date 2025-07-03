import re
import sys

if len(sys.argv) < 4:
    print("Usage: python dumpbin.py <dumpbin_input.txt> <output_def_file.def> <original_dll_name>")
    sys.exit(1)

dumpbin_file = sys.argv[1]
def_file = sys.argv[2]
dll_name = sys.argv[3]

with open(dumpbin_file, "r", encoding="utf-8") as f:
    lines = f.readlines()

exports = []
for line in lines:
    match = re.match(r"\s*(\d+)\s+[0-9A-F]+\s+[0-9A-F]+\s+(\S+)", line)
    if match:
        ordinal = match.group(1)
        name = match.group(2)
        exports.append((ordinal, name))

with open(def_file, "w", encoding="utf-8") as f:
    f.write("EXPORTS\n")
    f.write("    SteamAPI_ISteamUserStats_SetAchievement\n")
    for ordinal, name in exports:
        if name != "SteamAPI_ISteamUserStats_SetAchievement":
            f.write(f"    {name} = {dll_name}.{name} @{ordinal}\n")