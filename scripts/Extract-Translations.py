"""Validate central PL/EN resources without changing application code or projects."""
import json,pathlib,re,sys
folder=pathlib.Path(__file__).resolve().parent.parent
resources=folder/'src/Zetmar.Core/Resources'
pl=json.loads((resources/'pl.json').read_text(encoding='utf-8-sig'))
en=json.loads((resources/'en.json').read_text(encoding='utf-8-sig'))
missing=set(pl)^set(en)
for source in (folder/'src').rglob('*.cs'):
    if {'bin','obj'}&set(source.parts):continue
    for key in re.findall(r'\bT\(("(?:[^"\\]|\\.)*")\)',source.read_text(encoding='utf-8-sig')):
        value=json.loads(key)
        if value not in pl or value not in en:missing.add(value)
if missing:
    print('Missing translations:',sorted(missing));sys.exit(1)
print(f'PL/EN resources valid: {len(pl)} keys')
