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
    # Match for exports with names
    match_named = re.match(r"\s*(\d+)\s+[0-9A-F]+\s+[0-9A-F]+\s+(\S+)", line)
    # Match for exports by ordinal only (no name)
    match_unnamed = re.match(r"\s*(\d+)\s+[0-9A-F]+\s+[0-9A-F]+\s+\[NONAME\]", line)
    
    if match_named:
        ordinal = match_named.group(1)
        name = match_named.group(2)
        exports.append((ordinal, name, True))
    elif match_unnamed:
        ordinal = match_unnamed.group(1)
        exports.append((ordinal, None, False))


with open(def_file, "w", encoding="utf-8") as f:
    f.write("EXPORTS\n")
    f.write("    SteamAPI_ISteamUserStats_SetAchievement\n")
    for ordinal, name, has_name in exports:
        if has_name:
            if name != "SteamAPI_ISteamUserStats_SetAchievement":
                f.write(f"    {name} = {dll_name}.{name} @{ordinal}\n")
        else:
            f.write(f"    NONAME_{ordinal} = {dll_name}.#{ordinal} @{ordinal} NONAME\n")