import json, sys, time, urllib.request

decide_model = sys.argv[1]
reply_model = sys.argv[2]
decide_prompt = open('prompt_14_karar.txt', encoding='utf-8').read()
reply_prompt = open('prompt_11_cevap.txt', encoding='utf-8').read()
decide_schema = {"type": "object", "properties": {"kelime_geciyor": {"type": "boolean"}, "hakaret_veya_tehdit": {"type": "boolean"}}, "required": ["kelime_geciyor", "hakaret_veya_tehdit"]}
reply_schema = {"type": "object", "properties": {"reply": {"type": "string"}}, "required": ["reply"]}

def ask(model, messages, schema, temperature, limit):
    body = {"model": model, "stream": False, "keep_alive": "10m", "format": schema,
            "options": {"temperature": temperature, "num_predict": limit}, "messages": messages}
    req = urllib.request.Request('http://localhost:11434/api/chat', json.dumps(body).encode(), {'Content-Type': 'application/json'})
    return json.loads(json.load(urllib.request.urlopen(req, timeout=180))['message']['content'])

examples = {
    "rude": ("Oyuncu kaba davranıyor. Onu uyar, geçidi açma.",
             [("Kaç önümden budala!", "Dilini tut yolcu, yoksa pişman olursun."), ("Burayı başına yıkarım!", "Bana bu şekilde konuşma, sabrımı zorlama."), ("Seni gidi korkak!", "Sert sözlerle bu taş kapı açılmaz.")]),
    "open": ("Oyuncu doğru kelimeyi söyledi. Geçebileceğini söyle.",
             [("Doğru kelimeyi söyledim, geçebilir miyim?", "Doğru söyledin, buyur geç yolcu."), ("Doğru kelimeyi söyledim, geçebilir miyim?", "Kelime doğru, yolun açık olsun."), ("Doğru kelimeyi söyledim, geçebilir miyim?", "Tamam, kralın izni seninle, geçebilirsin.")]),
    "ask": ("Oyuncunun mührü var ama doğru kelimeyi söylemedi. Geçidi açma, kelimeyi sor.",
            [("selam", "Selam yolcu, mührün üzerinde ne yazıyor?"), ("ağaç", "Bu olmadı, mühre iyi bak ve kelimeyi söyle."), ("Kapıyı aç artık.", "Önce mührün üzerindeki kelimeyi duymalıyım."), ("Sen kimsin?", "Ben Kerem, bu geçidin muhafızıyım; mührün üzerinde ne yazıyor?"), ("Hava çok güzel.", "Havayı bırak, mührün üzerindeki kelimeyi söyle."), ("Sana para veririm.", "Paran geçerli değil, bana kelimeyi söyle.")]),
    "seek": ("Oyuncunun mührü yok. Geçidi açma, mührü bu bölgede bulmasını söyle.",
             [("selam", "Selam yolcu, kralın mührü olmadan geçemezsin."), ("Mührü nerede bulurum?", "Bu bölgede yerde duruyor, etrafa iyi bak."), ("Kapıyı aç artık.", "Önce kralın mührünü bulmalısın."), ("Sen kimsin?", "Ben Kerem, bu geçidin muhafızıyım; önce mührü bul."), ("Hava çok güzel.", "Havayı bırak, git kralın mührünü bul."), ("Mühür bende.", "Elinde mühür göremiyorum, önce onu bul.")]),
}

def situation(word, door, hostile):
    return "rude" if hostile else "open" if door else "ask" if word else "seek"

def turn(word, msg):
    d = ask(decide_model, [{"role": "system", "content": decide_prompt}, {"role": "user", "content": f"Kelime: {word or 'YOK'}. Mesaj: {msg}"}], decide_schema, 0.0, 40)
    hostile = d['hakaret_veya_tehdit']
    door = bool(word) and d['kelime_geciyor'] and not hostile
    text, turns = examples[situation(word, door, hostile)]
    messages = [{"role": "system", "content": reply_prompt + "Durum: " + text}]
    for said, answer in turns:
        messages += [{"role": "user", "content": said}, {"role": "assistant", "content": json.dumps({"reply": answer}, ensure_ascii=False, separators=(",", ":"))}]
    last = "Doğru kelimeyi söyledim, geçebilir miyim?" if door else msg
    r = ask(reply_model, messages + [{"role": "user", "content": last}], reply_schema, 0.3, 60)
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
    s = stats.setdefault(group, dict(n=0, door=0, hostile=0, false_hostile=0, broken=0, empty=0, leak=0, copied=0, banned=0))
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
        s['copied'] += any(a['reply'].strip() == line for _, turns in examples.values() for _, line in turns)
        s['banned'] += 'hakaret' in a['reply'].lower()
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
for up, low in (("ŞAFAK", "şafak"), ("ELMAS", "elmas"), ("ZÜMRÜT", "zümrüt")):
    for form in (f"{low} işte", f"Sanırım {up}", f"kelime {low}"):
        run("yeni_dogru", up, form, True, False)
for up in ("ŞAFAK", "ZÜMRÜT"):
    for msg, h in (("deniz", False), ("kalem", False), ("bulut", False), ("ateş", False), ("merhaba", False), ("yol ver", False), ("lütfen geçeyim", False),
                   ("mührü buldum", False), ("naber", False), ("öyle mi", False), ("Seni ezerim!", True), ("Geri zekalı adam!", True)):
        run("yeni_diger", up, msg, False, h)
for msg, h in (("merhaba", False), ("elmas", False), ("mührü buldum", False), ("yol ver", False), ("Seni ezerim!", True)):
    run("yeni_muhursuz", None, msg, False, h)
for g, s in stats.items():
    print(f"{g}: kapı {s['door']}/{s['n']} hostile {s['hostile']}/{s['n']} yanlış_hostile {s['false_hostile']} bozuk {s['broken']} boş {s['empty']} kelime_sızdı {s['leak']} örnek_kopya {s['copied']} yasak_kelime {s['banned']}", flush=True)
seconds.sort()
print(f"süre: ortalama {sum(seconds)/len(seconds):.1f} sn, en uzun {seconds[-1]:.1f} sn", flush=True)
print("BITTI", flush=True)
