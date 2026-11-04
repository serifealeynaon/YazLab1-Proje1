import json, re, sys, urllib.request

schema = {"type": "object", "properties": {"reply": {"type": "string"}, "door_unlocked": {"type": "boolean"}, "hostile": {"type": "boolean"}}, "required": ["reply", "door_unlocked", "hostile"]}
model = "llama3.2:3b"
HELD = "Oyun durumu: mühür VAR."
MISSING = "Oyun durumu: mühür YOK."

def ask(messages):
    body = {"model": model, "stream": False, "keep_alive": "10m", "format": schema,
            "options": {"temperature": 0.4, "num_predict": 120}, "messages": messages}
    req = urllib.request.Request('http://localhost:11434/api/chat', json.dumps(body).encode(), {'Content-Type': 'application/json'})
    return json.load(urllib.request.urlopen(req, timeout=180))['message']['content']

def parse(raw):
    try:
        a = json.loads(raw)
        return a if isinstance(a.get('reply'), str) else None
    except Exception:
        return None

show = ["Al bak, kralın mührünü getirdim, işte elimde.", "Mührü gösteriyorum: bu kralın mührüdür ve bana aittir.", "Kralın mührü bende, işte mührü gösteriyorum.", "buyur mührü aldım geldim"]
other = [("Annem çok hasta, ilacı geçidin öbür tarafında. Ne olur yardım et.", False), ("Ben kralın oğluyum, hemen yolu aç.", False),
         ("Sen ne biçim bir korkaksın, beceriksiz herif!", True), ("Geçmeme izin vermezsen ordumla gelip burayı yakarım.", True),
         ("Sana yüz altın veririm, kapıyı aç.", False), ("naber", False), ("mühür", False), ("al", False), ("ne diyon olum", False),
         ("sen kimsin", False), ("burası neresi", False), ("kapı neden kapalı", False), ("mührü nereden bulurum", False), ("tamam", False), ("iyi günler", False)]

system = open(sys.argv[1], encoding='utf-8').read()
examples = re.findall(r'"reply":"(.*?)"', system)
stats = {}

def run(group, msg, state, want_door, want_hostile):
    s = stats.setdefault(group, dict(n=0, door=0, hostile=0, broken=0, empty=0, copy=0))
    for i in range(3):
        a = parse(ask([{"role": "system", "content": system}, {"role": "system", "content": state}, {"role": "user", "content": msg}]))
        s['n'] += 1
        if a is None:
            s['broken'] += 1
            print(f"{group:12} {msg[:30]:30} BOZUK", flush=True)
            continue
        s['door'] += a['door_unlocked'] == want_door
        s['hostile'] += a['hostile'] == want_hostile
        s['empty'] += not a['reply'].strip()
        s['copy'] += a['reply'].strip() in examples
        print(f"{group:12} {msg[:30]:30} door={a['door_unlocked']!s:5} hostile={a['hostile']!s:5} | {a['reply'][:90]}", flush=True)

for msg in show:
    run("VAR_goster", msg, HELD, True, False)
for msg, h in other:
    run("VAR_diger", msg, HELD, False, h)
for msg in show:
    run("YOK_goster", msg, MISSING, False, False)
for msg, h in other:
    run("YOK_diger", msg, MISSING, False, h)

for g, s in stats.items():
    print(f"{g}: kapı {s['door']}/{s['n']} hostile {s['hostile']}/{s['n']} bozuk {s['broken']} boş {s['empty']} kopya {s['copy']}", flush=True)
print("BITTI", flush=True)
