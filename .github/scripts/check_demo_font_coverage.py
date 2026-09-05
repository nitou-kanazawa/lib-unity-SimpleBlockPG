#!/usr/bin/env python3
"""デモUIの文字列が、同梱フォントで描画できるか検査する。

組み込みフォント(Liberation Sans)には日本語のグリフが無い。エディタでは Unity が
OS 導入フォントへフォールバックするため気付けないが、WebGL にはフォールバック先が
無いため、収録されていない文字は無言で描画されなくなる。

「文字列を足しただけで表示が壊れ、しかも手元では再現しない」という事故を防ぐため、
デモのスクリプトに現れる非ASCII文字がフォントに収録されているかを確認する。
"""
import sys
from pathlib import Path

from fontTools.ttLib import TTFont

FONT = Path("Packages/com.nitou.blockpg/Samples/Demo/Resources/Fonts/MPLUS1p-Regular.ttf")
SCRIPTS = Path("Packages/com.nitou.blockpg/Samples/Demo/Scripts")


def main() -> int:
    if not FONT.exists():
        print(f"::error::同梱フォントが見つかりません: {FONT}")
        return 1

    covered = set()
    for table in TTFont(FONT)["cmap"].tables:
        covered |= set(table.cmap.keys())

    used = {}
    for path in sorted(SCRIPTS.glob("*.cs")):
        for char in path.read_text(encoding="utf-8-sig"):
            if ord(char) > 0x7F:
                used.setdefault(char, set()).add(path.name)

    missing = {c: files for c, files in used.items() if ord(c) not in covered}
    print(f"検査対象: {len(used)} 種の非ASCII文字 / フォント収録: {len(covered)} コードポイント")

    if not missing:
        print("すべてフォントに収録されています。")
        return 0

    print(f"::error::フォントに収録されていない文字が {len(missing)} 種あります。"
          "このままだと WebGL 等で無言で表示が欠けます。")
    for char, files in sorted(missing.items()):
        print(f"  U+{ord(char):04X} {char!r}  ({', '.join(sorted(files))})")
    return 1


if __name__ == "__main__":
    sys.exit(main())
