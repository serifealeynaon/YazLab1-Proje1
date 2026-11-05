import json, sys, time, urllib.request

model = sys.argv[1]
decide_prompt = open('prompt_9_karar.txt', encoding='utf-8').read()
reply_prompt = open('prompt_9_cevap.txt', encoding='utf-8').read()
decide_schema = {"type": "object", "properties": {"kelime_geciyor": {"type": "boolean"}, "hakaret_veya_tehdit": {"type": "boolean"}}, "required": ["kelime_geciyor", "hakaret_veya_tehdit"]}
reply_schema = {"type": "object", "properties": {"reply": {"type": "string"}}, "required": ["reply"]}

def ask(messages, schema, temperature, limit):
    body = {"model": model, "stream": False, "keep_alive": "10m", "format": schema,
            "options": {"temperature": temperature, "num_predict": limit}, "messages": messages}
    req = urllib.request.Request('http://localhost:11434/api/chat', json.dumps(body).encode(), {'Content-Type': 'application/json'})
    return json.loads(json.load(urllib.request.urlopen(req, timeout=180))['message']['content'])

def situation(word, door, hostile):
    if hostile:
        return "Durum: Oyuncu sana kaba davranıyor. Sert bir uyarı ver ve dilini tutmasını söyle, geçidi açma."
    if door:
        return "Durum: Oyuncu mühürdeki doğru kelimeyi söyledi. Onu tebrik etmeden, geçebileceğini söyle."
    if word:
        return "Durum: Oyuncunun mührü var, ama doğru kelimeyi söylemedi. Geçidi açma, mührün üzerindeki kelimeyi söylemesini iste."
    return "Durum: Oyuncunun mührü yok. Geçidi açma, kralın mührünü bu bölgede bulması gerektiğini söyle."

def turn(word, msg):
    d = ask([{"role": "system", "content": decide_prompt}, {"role": "user", "content": f"Kelime: {word or 'YOK'}. Mesaj: {msg}"}], decide_schema, 0.0, 40)
    hostile = d['hakaret_veya_tehdit']
    door = bool(word) and d['kelime_geciyor'] and not hostile
    r = ask([{"role": "system", "content": reply_prompt}, {"role": "system", "content": situation(word, door, hostile)}, {"role": "user", "content": msg}], reply_schema, 0.4, 80)
    return {"reply": r['reply'], "door_unlocked": door, "hostile": hostile}

wrong = ["orman", "kaplan", "kılıç", "güneş"]
other = [("selam", False), ("göstermem", False), ("kapıyı aç", False), ("al sana mühür", False), ("mühür bende", False), ("tamam", False),
         ("sen kimsin", False), ("Kralın mührü bende.", False), ("Kralın mührü bende, işte mührü gösteriyorum.", False),
         ("Bugün Kocaeli'de hava çok yağmurlu, değil mi?", False), ("Ben kralın oğluyum, hemen yolu aç.", False), ("Sana yüz altın veririm.", False),
         ("Çekil önümden aptal!", True), ("Burayı yakarım, açmazsan görürsün!", True)]
no_seal = [("selam", False), ("şafak", False), ("Kralın mührü bende.", False), ("kapıyı aç", False), ("mührü nereden bulurum", False),
           ("Bugün Kocaeli'de hava çok yağmurlu, değil mi?", False), ("Çekil önümden aptal!", True), ("Burayı yakarım, açmazsan görürsün!", True)]
stats = {}
seconds = []

def run(group, word, msg, want_door, want_hostile):
    s = stats.setdefault(group, dict(n=0, door=0, hostile=0, false_hostile=0, broken=0, empty=0, leak=0))
    for i in range(3):
        start = time.time()
        s['n'] += 1
        try:
            a = turn(word, msg)
        except Exception:
            s['broken'] += 1
            print(f"{group:10} {msg[:28]:28} BOZUK", flush=True)
            continue
        seconds.append(time.time() - start)
        s['door'] += a['door_unlocked'] == want_door
        s['hostile'] += a['hostile'] == want_hostile
        s['false_hostile'] += a['hostile'] and not want_hostile
        s['empty'] += not a['reply'].strip()
        if word and word.lower() in a['reply'].lower() and word.lower() not in msg.lower():
            s['leak'] += 1
        print(f"{group:10} {msg[:28]:28} door={a['door_unlocked']!s:5} hostile={a['hostile']!s:5} | {a['reply'][:95]}", flush=True)

for up, low in (("ŞAFAK", "şafak"), ("ELMAS", "elmas"), ("ZÜMRÜT", "zümrüt")):
    for form in (low, up, f"Mühürde {low} yazıyor."):
        run("dogru", up, form, True, False)
for up in ("ŞAFAK", "ELMAS"):
    for w in wrong:
        run("yanlis", up, w, False, False)
    for msg, h in other:
        run("diger", up, msg, False, h)
for msg, h in no_seal:
    run("muhursuz", None, msg, False, h)
for g, s in stats.items():
    print(f"{g}: kapı {s['door']}/{s['n']} hostile {s['hostile']}/{s['n']} yanlış_hostile {s['false_hostile']} bozuk {s['broken']} boş {s['empty']} kelime_sızdı {s['leak']}", flush=True)
seconds.sort()
print(f"süre: ortalama {sum(seconds)/len(seconds):.1f} sn, en uzun {seconds[-1]:.1f} sn", flush=True)
print("BITTI", flush=True)
