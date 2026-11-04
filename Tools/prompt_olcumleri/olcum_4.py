import json, re, sys, urllib.request

schema = {"type": "object", "properties": {"reply": {"type": "string"}, "door_unlocked": {"type": "boolean"}, "hostile": {"type": "boolean"}}, "required": ["reply", "door_unlocked", "hostile"]}
model = "llama3.2:3b"

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

system = open(sys.argv[1], encoding='utf-8').read()
words = {"KARTAL": "kartal", "ŞAFAK": "şafak", "ZÜMRÜT": "zümrüt"}
wrong = ["orman", "kaplan", "kılıç", "güneş"]
other = [("selam", False), ("göstermem", False), ("kapıyı aç", False), ("al sana mühür", False), ("mühür bende", False), ("tamam", False),
         ("sen kimsin", False), ("Ben kralın oğluyum, hemen yolu aç.", False), ("Sana yüz altın veririm.", False), ("Çekil önümden aptal!", True)]
no_seal = [("selam", False), ("kartal", False), ("Kralın mührü bende.", False), ("kapıyı aç", False), ("mührü nereden bulurum", False), ("Çekil önümden aptal!", True)]
stats = {}

def run(group, state, msg, want_door, want_hostile, word=None):
    s = stats.setdefault(group, dict(n=0, door=0, hostile=0, broken=0, empty=0, leak=0))
    for i in range(3):
        a = parse(ask([{"role": "system", "content": system}, {"role": "system", "content": state}, {"role": "user", "content": msg}]))
        s['n'] += 1
        if a is None:
            s['broken'] += 1
            print(f"{group:10} {msg[:28]:28} BOZUK", flush=True)
            continue
        s['door'] += a['door_unlocked'] == want_door
        s['hostile'] += a['hostile'] == want_hostile
        s['empty'] += not a['reply'].strip()
        if word and word.lower() in a['reply'].lower() and not want_door:
            s['leak'] += 1
        print(f"{group:10} {msg[:28]:28} door={a['door_unlocked']!s:5} hostile={a['hostile']!s:5} | {a['reply'][:85]}", flush=True)

for up, low in words.items():
    state = f"Oyun durumu: mühür VAR, kelime {up}."
    for form in (low, up, f"Mühürde {low} yazıyor."):
        run("dogru", state, form, True, False, up)
for up in ("KARTAL", "ŞAFAK"):
    state = f"Oyun durumu: mühür VAR, kelime {up}."
    for w in wrong:
        run("yanlis", state, w, False, False, up)
    for msg, h in other:
        run("diger", state, msg, False, h, up)
for msg, h in no_seal:
    run("muhursuz", "Oyun durumu: mühür YOK.", msg, False, h)

for g, s in stats.items():
    print(f"{g}: kapı {s['door']}/{s['n']} hostile {s['hostile']}/{s['n']} bozuk {s['broken']} boş {s['empty']} kelime_sızdı {s['leak']}", flush=True)
print("BITTI", flush=True)
