"""Read-only Unity project inventory; writes evidence JSON under Reports."""
from pathlib import Path
import re, json, hashlib, collections

root = Path(__file__).resolve().parents[1]
assets = root / 'Assets'
guid_map = {}
for base in (assets, root / 'Packages', root / 'Library/PackageCache'):
    for p in base.rglob('*.meta'):
        m = re.search(r'^guid: ([0-9a-f]{32})', p.read_text(errors='replace'), re.M)
        if m:
            guid_map[m[1]] = str(p.with_suffix('').relative_to(root)).replace('\\', '/')

def blocks(p):
    text = p.read_text(encoding='utf-8-sig', errors='replace')
    return [(int(m[1]), m[2], m[3]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M|re.S)]

def field(body, name):
    m = re.search(r'^  '+re.escape(name)+r': (.*)$', body, re.M)
    return m[1] if m else None

scenes = {}
missing = []
for p in assets.rglob('*'):
    if p.suffix not in ('.unity', '.prefab'):
        continue
    bs = blocks(p)
    rel = str(p.relative_to(root)).replace('\\', '/')
    objs = {bid: {'name': field(b, 'm_Name'), 'active':field(b,'m_IsActive')} for t,bid,b in bs if t==1}
    custom = []
    for t,bid,b in bs:
        if t != 114:
            continue
        m = re.search(r'm_Script: \{fileID: (-?\d+)(?:, guid: ([0-9a-f]{32}), type: \d+)?\}', b)
        if not m:
            continue
        script = guid_map.get(m[2], 'UNRESOLVED:'+str(m[2])) if m[2] else 'NULL_SCRIPT'
        if script.startswith(('UNRESOLVED:', 'NULL_SCRIPT')) and m[2] != '0000000000000000e000000000000000':
            missing.append({'asset':rel,'component':bid,'script':script})
        if script.startswith(('Assets/Script/', 'Assets/Stage2/script/', 'Assets/LJR/Scripts/')):
            go = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', b)
            settings = {k:v for k,v in re.findall(r'^  ([A-Za-z]\w*): (.+)$',b,re.M) if not k.startswith('m_')}
            custom.append({'script':script,'component':bid,'object':objs.get(go[1],{}) if go else {},'enabled':field(b,'m_Enabled'),'settings':settings})
    if p.parent == assets/'Scenes':
        scenes[rel]={'objects':len(objs),'components':custom,'mono_behaviours':sum(t==114 for t,_,_ in bs)}

charts = []
for p in list(assets.rglob('*notedata*.json')) + list((root/'Build').rglob('*notedata*.json')):
    data=json.loads(p.read_text(encoding='utf-8-sig'))
    times=[n['createTime'] for n in data]
    issues=[]
    for i,n in enumerate(data):
        if n.get('noteType') not in (0,1) or n.get('noteKind') not in (0,1,2): issues.append({'index':i,'issue':'invalid_type'})
        if n.get('noteKind')==1 and n.get('holdTime',0)<=0: issues.append({'index':i,'issue':'invalid_hold'})
        if n.get('noteKind')==2 and (n.get('needHitCount',0)<=0 or n.get('mashLimitTime',0)<=0): issues.append({'index':i,'issue':'invalid_mash'})
    overlaps=[]
    for i,n in enumerate(data):
        if n.get('noteKind')!=1: continue
        for j,q in enumerate(data):
            if i!=j and q['noteType']==n['noteType'] and n['createTime']<q['createTime']<n['createTime']+n['holdTime']:
                overlaps.append([i,j])
    charts.append({'path':str(p.relative_to(root)).replace('\\','/'),'count':len(data),'kinds':dict(collections.Counter(n['noteKind'] for n in data)),'first':min(times) if times else None,'last':max(times) if times else None,'sorted':times==sorted(times),'issues':issues,'same_lane_hold_overlaps':overlaps,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})

music=[]
for p in assets.rglob('*.asset'):
    for t,bid,b in blocks(p):
        if t==114 and '  sceneName:' in b and '  musicName:' in b:
            music.append({'asset':str(p.relative_to(root)).replace('\\','/'),'name':field(b,'musicName'),'scene':field(b,'sceneName'),'preview':field(b,'previewClip'),'game':field(b,'gameClip')})

counts=collections.Counter(p.suffix.lower() for p in assets.rglob('*') if p.is_file() and p.suffix!='.meta')
sizes=collections.Counter()
for p in assets.rglob('*'):
    if p.is_file() and p.suffix!='.meta': sizes[p.relative_to(assets).parts[0]]+=p.stat().st_size
report={'scenes':scenes,'unresolved_script_guids':missing,'charts':charts,'music':music,'asset_counts':dict(counts),'asset_folder_bytes':dict(sizes)}
out=root/'Reports/audit_evidence.json'
out.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'scenes':{k:{'objects':v['objects'],'custom_components':len(v['components'])} for k,v in scenes.items()},'unresolved_count':len(missing),'unresolved':missing[:20],'charts':charts,'music':music,'asset_counts':dict(counts),'largest_folders':sizes.most_common(8)},ensure_ascii=False,indent=2))
