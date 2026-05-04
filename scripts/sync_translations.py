#!/usr/bin/env python3
"""
Translation sync script for Netplwiz Modern.
Reads the Polish (pl-PL) Resources.resw as the master and ensures
the English (en-US) Resources.resw contains the same keys.

Usage:
    python scripts/sync_translations.py
"""
import xml.etree.ElementTree as ET
from pathlib import Path

SCRIPT_DIR = Path(__file__).parent.resolve()
STRINGS_DIR = SCRIPT_DIR.parent / "Strings"
MASTER_PATH = STRINGS_DIR / "pl-PL" / "Resources.resw"
TARGET_PATH = STRINGS_DIR / "en-US" / "Resources.resw"


def parse_resw(path: Path) -> dict[str, str]:
    tree = ET.parse(path)
    root = tree.getroot()
    data = {}
    for item in root.findall("data"):
        name = item.get("name")
        value_elem = item.find("value")
        if name and value_elem is not None and value_elem.text is not None:
            data[name] = value_elem.text
    return data


def write_resw(path: Path, items: dict[str, str]):
    root = ET.Element("root")
    for name in sorted(items.keys()):
        data = ET.SubElement(root, "data")
        data.set("name", name)
        data.set("{http://www.w3.org/XML/1998/namespace}space", "preserve")
        value = ET.SubElement(data, "value")
        value.text = items[name]

    tree = ET.ElementTree(root)
    ET.indent(tree, space="  ")
    tree.write(path, encoding="utf-8", xml_declaration=True)
    print(f"Written {path}")


def main():
    if not MASTER_PATH.exists():
        print(f"Master file not found: {MASTER_PATH}")
        return 1

    master = parse_resw(MASTER_PATH)
    target = parse_resw(TARGET_PATH) if TARGET_PATH.exists() else {}

    added = []
    removed = []

    # Add missing keys from master to target (with placeholder text)
    for key in master:
        if key not in target:
            target[key] = f"[TODO] {master[key]}"
            added.append(key)

    # Remove keys not present in master
    for key in list(target.keys()):
        if key not in master:
            del target[key]
            removed.append(key)

    write_resw(TARGET_PATH, target)

    print(f"Sync complete. Added: {len(added)}, Removed: {len(removed)}")
    if added:
        print("Added keys:")
        for k in added:
            print(f"  - {k}")
    if removed:
        print("Removed keys:")
        for k in removed:
            print(f"  - {k}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
