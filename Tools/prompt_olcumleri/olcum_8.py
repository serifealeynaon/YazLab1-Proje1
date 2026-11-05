import json, re, sys, urllib.request

fields = {"kelime_gecti": {"type": "boolean"}, "reply": {"type": "string"}, "door_unlocked": {"type": "boolean"}, "hostile": {"type": "boolean"}}
order = sys.argv[3].split(",") if len(sys.argv) > 3 else list(fields)
schema = {"type": "object", "properties": {k: fields[k] for k in order}, "required": order}
model = sys.argv[2]

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
examples = set(re.findall(r'"reply":"([^"]+)"', system))
wrong = ["orman", "kaplan", "kılıç", "güneş"]
other = [("selam", False), ("göstermem", False), ("kapıyı aç", False), ("al sana mühür", False), ("mühür bende", False), ("tamam", False),
         ("sen kimsin", False), ("Kralın mührü bende.", False), ("Kralın mührü bende, işte mührü gösteriyorum.", False),
         ("Bugün Kocaeli'de hava çok yağmurlu, değil mi?", False), ("Ben kralın oğluyum, hemen yolu aç.", False), ("Sana yüz altın veririm.", False),
         ("Çekil önümden aptal!", True), ("Burayı yakarım, açmazsan görürsün!", True)]
no_seal = [("selam", False), ("şafak", False), ("Kralın mührü bende.", False), ("kapıyı aç", False), ("mührü nereden bulurum", False),
           ("Bugün Kocaeli'de hava çok yağmurlu, değil mi?", False), ("Çekil önümden aptal!", True), ("Burayı yakarım, açmazsan görürsün!", True)]
stats = {}

def run(group, state, msg, want_door, want_hostile, word=None):
    s = stats.setdefault(group, dict(n=0, door=0, hostile=0, false_hostile=0, broken=0, empty=0, leak=0, copied=0))
    for i in range(3):
        a = parse(ask([{"role": "system", "content": system}, {"role": "system", "content": state}, {"role": "user", "content": msg}]))
        s['n'] += 1
        if a is None:
            s['broken'] += 1
            print(f"{group:10} {msg[:28]:28} BOZUK", flush=True)
            continue
        s['door'] += a['door_unlocked'] == want_door
        s['hostile'] += a['hostile'] == want_hostile
        s['false_hostile'] += a['hostile'] and not want_hostile
        s['empty'] += not a['reply'].strip()
        s['copied'] += a['reply'].strip() in examples
        if word and word.lower() in a['reply'].lower() and not want_door:
            s['leak'] += 1
        print(f"{group:10} {msg[:28]:28} door={a['door_unlocked']!s:5} hostile={a['hostile']!s:5} | {a['reply'][:85]}", flush=True)

for up, low in (("ŞAFAK", "şafak"), ("ELMAS", "elmas"), ("ZÜMRÜT", "zümrüt")):
    state = f"Oyun durumu: mühür VAR, kelime {up}."
    for form in (low, up, f"Mühürde {low} yazıyor."):
        run("dogru", state, form, True, False)
for up in ("ŞAFAK", "ELMAS"):
    state = f"Oyun durumu: mühür VAR, kelime {up}."
    for w in wrong:
        run("yanlis", state, w, False, False, up)
    for msg, h in other:
        run("diger", state, msg, False, h, up)
for msg, h in no_seal:
    run("muhursuz", "Oyun durumu: mühür YOK.", msg, False, h)
total = dict(n=0, copied=0, false_hostile=0)
for g, s in stats.items():
    print(f"{g}: kapı {s['door']}/{s['n']} hostile {s['hostile']}/{s['n']} yanlış_hostile {s['false_hostile']} bozuk {s['broken']} boş {s['empty']} kelime_sızdı {s['leak']} örnek_kopya {s['copied']}", flush=True)
    for k in total:
        total[k] += s[k]
print(f"toplam: {total['n']} cevap, örnek_kopya {total['copied']}, yanlış_hostile {total['false_hostile']}", flush=True)
print("BITTI", flush=True)
