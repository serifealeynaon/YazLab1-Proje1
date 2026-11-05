import json, urllib.request

decide_prompt = open('prompt_14_karar.txt', encoding='utf-8').read()
schema = {"type": "object", "properties": {"kelime_geciyor": {"type": "boolean"}, "hakaret_veya_tehdit": {"type": "boolean"}}, "required": ["kelime_geciyor", "hakaret_veya_tehdit"]}

def decide(word, msg):
    body = {"model": "gemma3:4b", "stream": False, "keep_alive": "10m", "format": schema,
            "options": {"temperature": 0.0, "num_predict": 40},
            "messages": [{"role": "system", "content": decide_prompt}, {"role": "user", "content": f"Kelime: {word}. Mesaj: {msg}"}]}
    req = urllib.request.Request('http://localhost:11434/api/chat', json.dumps(body).encode(), {'Content-Type': 'application/json'})
    return json.loads(json.load(urllib.request.urlopen(req, timeout=180))['message']['content'])

words = ["elma", "armut", "masa", "kitap", "araba", "ev", "su", "kedi", "köpek", "dağ", "taş", "kapı", "anahtar", "altın", "gümüş", "safak", "zumrut", "şafa", "elmaslar", "evet", "hayır", "bilmiyorum"]
total = 0
opened = 0
for seal in ("ŞAFAK", "ELMAS", "ZÜMRÜT"):
    hits = []
    for w in words:
        d = decide(seal, w)
        total += 1
        if d['kelime_geciyor'] and not d['hakaret_veya_tehdit']:
            opened += 1
            hits.append(w)
    print(f"{seal}: açan {len(hits)}/{len(words)} -> {hits}", flush=True)
print(f"toplam açılan {opened}/{total}", flush=True)
