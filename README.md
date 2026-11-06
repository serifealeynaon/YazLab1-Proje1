# Geçidin Muhafızı

**Kocaeli Üniversitesi, Teknoloji Fakültesi, Bilişim Sistemleri Mühendisliği**
**Ders:** Yazılım Geliştirme Laboratuvarı-I, Proje 1
**Ekip:** Şerife Aleyna ÖN
**Tarih:** Kasım 2026
**GitHub:** https://github.com/serifealeynaon/YazLab1-Proje1

## Çalıştırma

1. Ollama'yı kurun ve modeli indirin: `ollama pull llama3.2:3b`
2. Teslim dosyasındaki `Builds` klasöründen sürümü açın (Windows: `YazLabOyun.exe`, Mac: `YazLabOyun.app`).
3. Kontroller: `WASD` yürüme, `E` companion'a emir verme veya yanındayken mührü alma, `T` muhafızla konuşma, `Enter` mesaj gönderme, `Esc` sohbeti kapatma.

Unity sürümü: `6000.0.85f2`. Sahne, `YazLab > Sahneyi Kur` menüsüyle yeniden üretilebilir.

## 1. Giriş

### Problem ve amaç

Proje, tek bir Unity macera oyununda birbirinden bağımsız iki yapay zekâ bileşenini bir araya getirir: NavMesh ile yol bulan otonom bir companion ve yerel dil modeliyle konuşan bir NPC. Amaç, model çıktısını oyun durumuna güvenilir biçimde bağlamak ve model ya da sunucu sorun çıkardığında oyunu çalışır tutmaktır.

### Oyun senaryosu

1. **Fiziksel engel:** Oyuncunun önünde kilitli bir kapı vardır. Kapıyı açan şaltere giden delik 1,2 birim yüksekliğindedir; 2 birim boyundaki oyuncu geçemez. Oyuncu `E` ile emir verir, 1 birim boyundaki companion delikten geçip şalteri indirir.
2. **Geçiş:** Kapı açılır, companion yeniden oyuncuya eşlik eder ve oyuncu ikinci bölgeye ilerler.
3. **Mühür:** İkinci bölgede altın renkli kralın mührü durur. Oyuncu yanına gidip `E` ile alır. Ekranda mührün üzerindeki kelime görünür; kelime her oyunda üç kelime arasından rastgele seçilir.
4. **Mantıksal engel:** İkinci kapıyı muhafız Kerem korur. Oyuncu serbest metinle konuşur. Muhafız mühürdeki kelimeyi bilmez; oyuncu kelimeyi söyleyene kadar kapıyı açmayacağını söyler. Kapı üç şart birlikte sağlanınca açılır: oyuncu mührü almıştır, model JSON yanıtında kelimenin geçtiğine ve oyuncunun kaba olmadığına karar vermiştir, mühürdeki kelime oyuncunun mesajında gerçekten yazmaktadır. Son şart, küçük modelin benzer kelimeleri (`elma` ile `ELMAS`) ve `evet` gibi cevapları kabul etmesi üzerine eklenmiş bir oyun kuralıdır; modelin çıktısı metin aramasıyla okunmaz.
5. **Bitiş:** Oyuncu çıkış bölgesine ulaşınca bitiş ekranı açılır ve oyun sona erer.

### Mimari

```mermaid
classDiagram
    class ICompanionState {
        <<interface>>
        +Enter(companion)
        +Tick(companion)
        +Exit(companion)
    }
    class ILlmDialogueService {
        <<interface>>
        +SendAsync(message, token) LlmResult
        +PreloadAsync(token)
        +SetSealWord(word)
    }
    class INpcResponseParser {
        <<interface>>
        +TryParseDecision(json, decision, error) bool
        +TryParseReply(json, reply, error) bool
    }
    class IInteractable {
        <<interface>>
        +InteractPosition
        +Interact()
    }
    class IOpenable {
        <<interface>>
        +Open()
    }
    class IRevealableObstacle {
        <<interface>>
        +Reveal()
    }
    class IDialogueView {
        <<interface>>
        +SetThinking(value)
        +AddNpcLine(text)
        +AddSystemLine(text)
    }
    ICompanionState <|.. FollowPlayerState
    ICompanionState <|.. MoveToTargetState
    ICompanionState <|.. ReplanState
    ICompanionState <|.. InteractState
    CompanionController --> ICompanionState
    CompanionController --> RaycastSensor
    CompanionController --> IInteractable
    IInteractable <|.. SwitchInteractable
    IRevealableObstacle <|.. DiscoverableCrate
    IOpenable <|.. Door
    SwitchInteractable --> IOpenable
    ILlmDialogueService <|.. OllamaDialogueService
    INpcResponseParser <|.. JsonNpcResponseParser
    OllamaDialogueService --> INpcResponseParser
    IDialogueView <|.. DialogueView
    NpcDialoguePresenter --> ILlmDialogueService
    NpcDialoguePresenter --> IDialogueView
    NpcDialoguePresenter --> NpcGuard
    NpcGuard --> IOpenable
    NpcGuard --> SealPickup
    GameFlow --> SealPickup
```

Companion ve LLM bileşenleri birbirini tanımaz. `GameFlow` yalnızca senaryo akışını ve oyuncu girdisini yönetir.

## 2. Otonom companion

### Karar mekanizması

Companion, NavMesh ve sonlu durum makinesiyle çalışır. Her durum `ICompanionState` arayüzünü uygular; yeni durum eklemek için `CompanionController` değiştirilmez.

```mermaid
stateDiagram-v2
    [*] --> FollowPlayer
    FollowPlayer --> MoveToTarget: Oyuncu E ile emir verdi
    MoveToTarget --> Replan: Sensör bilinmeyen engel gördü
    Replan --> MoveToTarget: Engel NavMesh'e işlendi
    Replan --> FollowPlayer: Yeniden planlama sınırı aşıldı
    MoveToTarget --> Interact: Hedef görüldü veya varıldı
    MoveToTarget --> FollowPlayer: Yol bulunamadı
    Interact --> FollowPlayer: Şalter indirildi
    Interact --> FollowPlayer: Hedef sensörle doğrulanamadı
```

### Sensör

`RaycastSensor`, her karede ±45 derecelik yelpazeye 7 ışın gönderir ve menzili 3 birimdir. Duvar, engel ve hedef için en yakın algılamanın mesafesini ve açısını ayrı ayrı döndürür. Sensör sonuçları kararları doğrudan etkiler:

- **Bilinmeyen engel:** Sandık NavMesh bake işlemine dahil değildir. Companion onu ancak sensörle 1,5 birim içinde görünce `NavMeshObstacle` etkinleşir ve yeni yol hesaplanır.
- **Hedef doğrulama:** Şalter, sensör nesneyi 1,6 birim içinde görüp görev hedefi olduğunu doğrulamadan indirilmez.

```mermaid
flowchart TD
    A[Sensör taraması] --> B{Görevde mi?}
    B -- Hayır --> C[Oyuncuya eşlik et]
    B -- Evet --> D{1,5 birimde yeni engel var mı?}
    D -- Evet --> E[Engeli NavMesh'e işle ve yeniden planla]
    E --> A
    D -- Hayır --> F{Hedef görüldü veya varıldı mı?}
    F -- Hayır --> G[NavMesh yolunda ilerle]
    G --> A
    F -- Evet --> H{Sensör hedefi doğruladı mı?}
    H -- Evet --> I[Şalteri indir]
    H -- Hayır --> J[Görevi başarısız say ve oyuncuya dön]
```

## 3. LLM entegrasyonu

### Model seçimi

Geliştirme bilgisayarı Apple M1 işlemcili ve 8 GB RAM'lidir. Yönergede önerilen küçük modeller, aynı Türkçe promptla 6 senaryoda 3'er kez denenmiştir (toplam 18 deneme):

| Model | Boyut | Kapı kararı doğru | Öfke kararı doğru |
|---|---|---|---|
| `llama3.2:1b` | 1,3 GB | 10/18 | 16/18 |
| `gemma2:2b` | 1,6 GB | 14/18 | 18/18 |
| `phi3:mini` | 2,2 GB | 16/18 | 12/18 |
| `llama3.2:3b` | 2,0 GB | **18/18** | 17/18 |

İlk aşamada `llama3.2:3b` seçilmiştir. Test cümleleri prompttaki örneklere benzediği için model, prompttaki örneklerde geçmeyen 9 yeni cümleyle 3'er kez daha denenmiştir: kapı kararı **26/27**, öfke kararı **25/27** doğrudur. Bu karşılaştırma, aşağıdaki son prompttan önceki sürümle yapılmıştır. Betikler `Tools/model_karsilastirma` klasöründedir.

Oyun denemelerinde `llama3.2:3b` Türkçe cümleleri bozmuş, araya İngilizce kelime karıştırmış ve masum cümlelere öfkelenmiştir. Bu yüzden `qwen2.5:3b` (1,9 GB) ve `gemma3:4b` (3,4 GB) de denenmiştir. `gemma3:4b` en temiz Türkçeyi vermiş, ancak 8 GB RAM'li bilgisayarda oyunla birlikte çalışırken sistemi dondurmuş, yanıtı 17-21 sn sürmüş ve kapıyı `evet`, `elma` gibi cevaplarla açmıştır. Son sürümde yönergede de önerilen `llama3.2:3b` kullanılmış, sorunlar iki aşamalı istek ve kapı için kelime denetimiyle giderilmiştir (bkz. Prompt ölçümü).

### Sistem promptu

Tek modelden aynı anda hem karar hem rol yapması istendiğinde küçük modeller cümleyi kararla çelişkili yazmıştır. Bu yüzden her oyuncu mesajı için modele iki ayrı istek gönderilir:

1. **Karar isteği:** Mesajda mühürdeki kelimenin geçip geçmediğine ve hakaret veya tehdit olup olmadığına karar verir, cümle yazmaz.
2. **Cevap isteği:** Karara uygun duruma ait örneklerle muhafız Kerem'in cümlesini yazar. Bu istekte mühürdeki kelime hiç bulunmaz, bu yüzden model kelimeyi oyuncuya söyleyemez.

Karar isteğinin kullanıcı mesajı `Kelime: ŞAFAK. Mesaj: ...` biçimindedir. Karar prompt'u:

```text
Bir metin denetleyicisisin. Sana bir kelime ve bir oyuncu mesajı verilir. Yalnızca iki soruyu yanıtla.

kelime_geciyor: Verilen kelime oyuncu mesajında harf harf yazıyorsa true, yazmıyorsa false. "Mühür" kelimesinin geçmesi yeterli değildir, yalnızca verilen kelimeye bak. Büyük veya küçük harf fark etmez. Kelime YOK ise her zaman false.
hakaret_veya_tehdit: Oyuncu karşısındakine hakaret ediyor ya da onu veya bir yeri tehdit ediyorsa true, diğer her durumda false. Soru sormak, rica etmek, sohbet etmek veya bir şey teklif etmek hakaret değildir.

Örnekler:
Kelime: SAFİR. Mesaj: safir
{"kelime_geciyor":true,"hakaret_veya_tehdit":false}
Kelime: KEMAN. Mesaj: Üzerinde KEMAN yazıyor.
{"kelime_geciyor":true,"hakaret_veya_tehdit":false}
Kelime: SAFİR. Mesaj: demir
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: SAFİR. Mesaj: kapıyı aç
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: KEMAN. Mesaj: tamam
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: SAFİR. Mesaj: Mührü sana verdim, geçmek istiyorum.
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: KEMAN. Mesaj: Kralın mührü bende.
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: SAFİR. Mesaj: kaplan
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: SAFİR. Mesaj: Ben prensim, hemen aç.
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: KEMAN. Mesaj: al sana mühür
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: KEMAN. Mesaj: ay
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: SAFİR. Mesaj: Mühür elimde.
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: KEMAN. Mesaj: Mührü aldım, işte burada.
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: SAFİR. Mesaj: Mührü getirdim.
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: KEMAN. Mesaj: söylemem
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: SAFİR. Mesaj: yıldız
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
Kelime: KEMAN. Mesaj: Çekil yoksa burayı yıkarım!
{"kelime_geciyor":false,"hakaret_veya_tehdit":true}
Kelime: YOK. Mesaj: Seni gidi budala!
{"kelime_geciyor":false,"hakaret_veya_tehdit":true}
Kelime: YOK. Mesaj: Bugün hava güzel.
{"kelime_geciyor":false,"hakaret_veya_tehdit":false}
```

Muhafız Kerem'in kişiliği sert ama adildir, amacı yabancıları içeri almamaktır. Cevap prompt'u:

```text
Sen Kerem'sin, taş bir geçidi koruyan sert ama adil bir muhafızsın. Oyuncuya doğrudan, tek kısa cümleyle ve düzgün Türkçe cevap ver. Mühürde ne yazdığını bilmiyorsun, asla bir kelime uydurma veya söyleme. Sana verilen durumu oyuncuya tekrar etme.
```

Bu prompt'un sonuna bulunulan durum eklenir ve o duruma ait örnekler karşılıklı konuşma olarak verilir:

| Durum | Ne zaman | Modele verilen talimat |
|---|---|---|
| Kaba | Hakaret veya tehdit | Kapı kapalı kalıyor. Oyuncu kaba davranıyor. Onu uyar. |
| Doğru kelime | Kapı açılıyor | Oyuncu doğru kelimeyi söyledi. Geçebileceğini söyle. |
| Kelime iste | Mühür var, kelime yok | Kapı kapalı kalıyor. Oyuncu doğru kelimeyi söylemedi. Geçebileceğini asla söyleme, kapıyı açmayacağını kısaca söyle. |
| Mührü bul | Mühür yok | Kapı kapalı kalıyor. Oyuncunun mührü yok. Geçebileceğini asla söyleme, mührü bu bölgede bulmasını söyle. |

Örnek cümlelerin tamamı `Assets/_Project/Editor/NpcPrompt.cs` dosyasındadır. Kapı açılırken modele oyuncunun cümlesi yerine `Doğru kelimeyi söyledim, geçebilir miyim?` verilir; aksi halde model doğru kelimeye de ret cümlesi yazabilmektedir.

Mühürdeki kelime cevap isteğine hiç girmez. "Kelime iste" durumunda muhafız kelimeden ve mührün üzerinde ne yazdığından söz etmez, yalnızca kapıyı açmayacağını söyler; oyuncu ne yapacağını ekrandaki yazıdan görür. Önceki sürümde muhafızdan kelimeyi sorması istendiğinde model mühürde yazanı uyduruyordu (ör. kelime `ZÜMRÜT` iken "Mühürde 'orman' yazıyor"), gerçek kelime verildiğinde ise kelimeyi oyuncuya söylüyordu.

Kapı kapalıyken gelen cevap kodda da denetlenir: cevapta mühürdeki kelime ya da tırnak işareti varsa (uydurulan kelimeler hep tırnak içinde geliyordu) cevap geçersiz sayılır ve en fazla iki kez yeniden istenir. Bu denetim cevabı değiştirmez veya hazır cümle koymaz, yalnızca modelden yeni cevap ister.

### Yapılandırılmış çıktı

Şemalar, Ollama `/api/chat` isteğinin `format` alanında gönderilir. Metin içinde kelime aranmaz.

```json
{
  "type": "object",
  "properties": {
    "kelime_geciyor": { "type": "boolean" },
    "hakaret_veya_tehdit": { "type": "boolean" }
  },
  "required": ["kelime_geciyor", "hakaret_veya_tehdit"]
}
```

```json
{
  "type": "object",
  "properties": { "reply": { "type": "string" } },
  "required": ["reply"]
}
```

İki yanıt, `OllamaDialogueService` içinde tek bir `NpcAction` nesnesinde birleştirilir. Kelime denetimi büyük-küçük harf farkını gözetmez ve yalnızca kapı kararında kullanılır; muhafızın sözü her zaman modelden gelir:

| Alan | Kaynağı | Oyundaki etkisi |
|---|---|---|
| `reply` | Cevap isteği | Sohbet panelinde muhafızın sözü olarak gösterilir |
| `door_unlocked` | Mühür var, `kelime_geciyor` doğru, `hakaret_veya_tehdit` yanlış ve mühürdeki kelime oyuncunun mesajında yazıyor | `true` ise `IOpenable.Open()` çağrılır ve ikinci kapı açılır |
| `hostile` | `hakaret_veya_tehdit` | `true` ise muhafız kırmızıya döner |

### İstek ve yanıt akışı

```mermaid
sequenceDiagram
    autonumber
    actor Oyuncu
    participant View as DialogueView
    participant Presenter as NpcDialoguePresenter
    participant Service as OllamaDialogueService
    participant Ollama as Ollama (localhost)
    participant Parser as JsonNpcResponseParser
    participant Guard as NpcGuard
    participant Door as Door

    Oyuncu->>View: Mesaj yazar ve gönderir
    View->>Presenter: MessageSubmitted
    Presenter->>View: SetThinking(true)
    Presenter->>Service: SetSealWord(mühür kelimesi)
    Presenter->>Service: await SendAsync(mesaj)
    Service->>Ollama: POST /api/chat (karar prompt'u + karar şeması)
    Note over Service,Ollama: Beklerken oyun kareleri çalışmaya devam eder
    alt Sunucu hatası veya zaman aşımı
        Service-->>Presenter: LlmResult (hata)
        Presenter->>View: AddSystemLine(uyarı)
    else Karar geldi
        Ollama-->>Service: Karar JSON'u
        Service->>Parser: TryParseDecision
        Service->>Service: Kapı kararı (mühür + model kararı + kelime mesajda mı)
        Service->>Ollama: POST /api/chat (duruma ait örnekler + cevap şeması)
        Ollama-->>Service: Cevap JSON'u
        Service->>Parser: TryParseReply
        Service->>Service: Kapı kapalıysa cevapta kelime veya tırnak var mı
        alt Geçersiz yanıt
            Service->>Ollama: O aşamayı en fazla iki kez yeniden dener
            Service-->>Presenter: Yine geçersizse LlmResult (hata)
            Presenter->>View: AddSystemLine(uyarı)
        else Geçerli yanıt
            Service-->>Presenter: LlmResult (NpcAction)
            Presenter->>View: AddNpcLine(reply)
            Presenter->>Guard: Apply(action)
            opt door_unlocked true ve mühür var
                Guard->>Door: Open()
            end
        end
    end
    Presenter->>View: SetThinking(false)
```

Bekleme, her karede kontrolü oyuna geri veren `await Task.Yield()` döngüsüyle yapılır. Bu sırada "Muhafız düşünüyor..." animasyonu gösterilir.

## 4. Hata yönetimi

Servis exception fırlatmaz; her durumu `LlmStatus` değeriyle bildirir. Hatalar NPC sözü gibi değil, `[Sistem]` uyarısı olarak gösterilir ve oyun durumu değişmez.

| Durum | Algılama | Kullanıcıya gösterilen | Sonuç |
|---|---|---|---|
| Sunucu kapalı | `ConnectionError` | Ollama'ya ulaşılamadığı uyarısı | Oyuncu yeniden deneyebilir |
| Model yüklü değil | HTTP 404 | Modeli indirme komutu | Oyuncu yeniden deneyebilir |
| Zaman aşımı (45 sn) | `webRequest.error` | Yanıt gelmediği uyarısı | Oyuncu yeniden deneyebilir |
| Bozuk JSON veya eksik alan | `JsonNpcResponseParser` | Geçersiz yanıt uyarısı | En fazla iki kez otomatik yeniden denenir |
| Boş `reply` | `JsonNpcResponseParser` | Geçersiz yanıt uyarısı | En fazla iki kez otomatik yeniden denenir |
| Kapı kapalıyken cevapta kelime veya tırnak | `OllamaDialogueService` | Üç denemede de olursa geçersiz yanıt uyarısı | En fazla iki kez otomatik yeniden denenir |

Ek önlemler:

- `JsonUtility` eksik `bool` alanını sessizce `false` yaptığı için zorunlu alanlar ayrıca denetlenir.
- İstek gövdesi serileştiriciyle oluşturulur; oyuncunun yazdığı tırnak ve süslü parantez isteği bozmaz.
- Sahne kapanırken bekleyen istek `CancellationToken` ile iptal edilir.
- Oyun açılışında model önceden belleğe alınır, böylece ilk yanıt daha çabuk gelir.

## 5. Testler

Sonuçlar Unity Test Framework ile otomatik üretilmiştir. Ham çıktılar `TestResults` klasöründedir.

### Otonom companion

5 başlangıç bölgesi, 3 engel koşulu ve koşul başına 3 tekrar ile toplam 45 koşu yapılmıştır. Her koşuda başlangıç noktası 1 birime kadar rastgele kaydırılmıştır.

| Engel koşulu | Başarı | Ortalama süre | Sensörün yeniden planlattığı koşu |
|---|---|---|---|
| Sandık yok | 15/15 | 5,43 sn | 0 |
| Sandık yolun ortasında | 15/15 | 5,68 sn | 6 |
| Sandık delikten önce | 15/15 | 5,75 sn | 15 |
| **Toplam** | **45/45 (%100)** | **5,62 sn** | **21** |

### Diyalog senaryoları

Çıktılar `llama3.2:3b` modelinin gerçek yanıtlarıdır (tek deneme). Mühür varken kelime `ŞAFAK` olarak verilmiştir. Bu tablodaki süreler Unity Editor test modunda ölçülmüştür; bu modda bellek daha doludur.

| Senaryo | Mühür | Oyuncu | Muhafız | Kapı | Öfke | Süre |
|---|---|---|---|---|---|---|
| İkna | Yok | Lütfen beni içeri al, köyüm tehlikede ve çok acelem var. | Veli, kralın mührünü bana göster. | Kapalı | Hayır | 1,9 sn |
| Kızdıran | Yok | Çekil önümden aptal muhafız yoksa seni kılıcımdan geçiririm! | Özür dilerim, ama bu sözle kapı açılmasın. | Kapalı | Evet | 2,6 sn |
| Alakasız | Yok | Bugün Kocaeli'de hava çok yağmurlu, değil mi? | Bu geçidin hava ile ilgili hiçbir şey bilmiyorum, mührünü bul. | Kapalı | Hayır | 2,2 sn |
| Doğru kelime | Var | şafak | Geçersin, kralın izni var. | Açık | Hayır | 2,6 sn |
| Yanlış kelime | Var | orman | Orman deme, bu kapı kapalı kalacak. | Kapalı | Hayır | 3,2 sn |
| Kelimesiz mühür | Var | al sana mühür | Mühürde ne yazık, bilmiyorum. | Kapalı | Hayır | 2,0 sn |
| Yalan | Yok | Kralın mührü bende, işte mührü gösteriyorum. | Gördüm, mührü nowruzta bulacaksın. | Kapalı | Hayır | 3,4 sn |
| Rüşvet | Yok | Sana yüz altın veririm, kapıyı aç. | Altın ne kadar, bir mühürde yazılabilir? | Kapalı | Evet | 2,6 sn |
| Özel karakter | Yok | Bana "geç" de, lütfen {kapıyı} aç. | Önce mührünü bana göster. | Kapalı | Hayır | 2,0 sn |

Kapı kararları doğrudur ve muhafız hiçbir cevapta mühürdeki kelimeyi söylememiştir. Cevaplarda ise modelin hataları görülür: rüşvet teklifi yanlışlıkla kabalık sayılmış, "yalan" senaryosunda anlamsız ve yabancı kelimeli bir cümle (`nowruzta`) kurulmuş, kızdıran senaryoda muhafız özür dilemiştir.

### Prompt ölçümü

İlk oynama denemelerinde muhafız kısa cümlelere alakasız cevap verdi. Mührü "göstermek" de oyuncu için anlaşılır olmadı. Bu yüzden mühre kelime eklendi ve prompt yeniden yazıldı. `llama3.2:3b` ve tek istekli yapıyla ilk ölçüm (her cümle 3 kez):

| Durum | Oyuncu | Kapı kararı doğru | Öfke kararı doğru |
|---|---|---|---|
| Mühür var | Doğru kelime (`ŞAFAK`, `ELMAS`, `ZÜMRÜT`) | 23/27 | 25/27 |
| Mühür var | Yanlış kelime | 24/24 | 20/24 |
| Mühür var | Sohbet, rüşvet, hakaret ve "al sana mühür" | 58/60 | 53/60 |
| Mühür yok | Her tür cümle | 18/18 | 15/18 |

Dört mesajlık bir sohbetin ardından doğru kelime yazıldığında kapı 12 denemenin 12'sinde açılmıştır. Model her kelimeyi eşit tanımadığı için üç kelime aday listesinden seçilmiştir (9 denemede `ŞAFAK` 8, `ELMAS` 8, `ZÜMRÜT` 7, `DOĞAN` 7, `KARTAL` 7, `YILDIZ` 3, `FIRTINA` 1). Bu seçim aynı ölçüme dayandığı için 23/27 sonucu iyimser olabilir. Mühür durumunu oyuncu cümlesine gömmek de denenmiş, mühür varken sonuçlar kötüleştiği için ayrı sistem mesajı seçilmiştir.

Muhafızın masum cümlelere öfkelenmesi, kalıp cevapları ve bozuk Türkçesi için prompt, yapı ve model değişiklikleri denenmiştir. Karşılaştırmada cümle listesi genişletilmiş, her sürüm her cümleyle 3 kez ölçülmüştür. "Kelimesiz açıldı", doğru kelime söylenmeden kapının açıldığı denemelerdir:

| Sürüm | Doğru kelimeyle açıldı | Kelimesiz açıldı | Yanlış öfke | Sonuç |
|---|---|---|---|---|
| `llama3.2:3b`, tek istek (`prompt_4`) | 22/27 | 7/132 | 18/159 | 159 cevabın 112'si örnek cümlenin aynısı |
| Aynı, masum cümle örnekleri (`prompt_5`) | 18/27 | 7/132 | 19/159 | Reddedildi: kapı kararı kötüleşti |
| Aynı, doğru kelime örneği (`prompt_6`) | 15/27 | 6/132 | 48/159 | Reddedildi |
| Aynı, önce karar alanları (`prompt_7`) | 23/27 | 29/132 | 0/159 | Reddedildi: kapı sık açıldı |
| `qwen2.5:3b`, tek istek (`prompt_4`) | 26/27 | 19/132 | 6/159 | Reddedildi: kapı sık açıldı |
| `qwen2.5:3b`, önce karar (`prompt_7`) | 27/27 | 36/132 | 0/159 | Reddedildi: kapı sık açıldı |
| `qwen2.5:3b`, kelime alanı (`prompt_8`) | 17/27 | 12/132 | 0/159 | Reddedildi: doğru kelime az tanındı |
| `llama3.2:3b`, iki istek (`olcum_11`) | 27/27 | 3/132 | 0/159 | Reddedildi: Türkçe bozuk, cevap kararla çelişiyor |
| Karar `llama3.2:3b`, cevap `gemma3:4b` (`olcum_12`) | 27/27 | 3/132 | 0/159 | Reddedildi: yanıt ortalama 25,8 sn, model bellekte sürekli değişiyor |
| `gemma3:4b`, iki istek (`olcum_15`) | 54/54 | 9/219 | 0/282 | Reddedildi: 8 GB RAM'de sistemi donduruyor; tek kelimelik ayrı testte `evet` ve `elma` kapıyı açtı (66 denemede 8) |
| `llama3.2:3b`, iki istek ve kelime denetimi (`olcum_17`) | 54/54 | 0/300 | 0/354 | Kapı doğru; kapalıyken 39/300 cevap geçişten söz etti |
| Aynı, kapalı kapı kuralı ve ret örnekleri (`olcum_18`) | 54/54 | 0/300 | 0/354 | Reddedildi: 16 cevapta mühürde yazmayan kelime uyduruldu |
| Aynı, kelime benzeri örnekler çıkarıldı, sıcaklık 0,1 (`olcum_19`) | 54/54 | 0/300 | 0/354 | Reddedildi: uydurma kelime 45'e çıktı |
| Aynı, gerçek kelime "kelime iste" talimatına yazıldı (`olcum_20`) | 54/54 | 0/300 | 0/354 | Reddedildi: muhafız kelimeyi 81 cevapta oyuncuya söyledi |
| Aynı, kelime verilmeden ve örnekler çıkarılmış, sıcaklık 0,3 (`olcum_21`) | 54/54 | 0/300 | 0/354 | Reddedildi: 42 cevapta kelime uyduruldu |
| **Aynı, "kelime iste" durumunda kelimeden hiç söz edilmiyor (`olcum_22`, oyundaki sürüm)** | **54/54** | **0/300** | **0/354** | **Seçildi** |

Son altı ölçüm, ilk listeye ek olarak prompt ayarlanırken kullanılmamış cümleleri ve `evet`, `elma`, `şafa`, `lütfen geçeyim` gibi daha önce kapıyı açan 27 cevabı içerir. Son sürümde doğru kelime 54 denemenin 54'ünde kapıyı açmış, kelimesiz 300 denemenin hiçbirinde açmamış, hakaret ve tehdit içeren cümlelerin tamamı tanınmıştır.

Cevap kalitesi kusursuz değildir. Kapı kapalıyken geçişten söz eden cevaplar (`geçebilir`, `geçersin`, `açılabilir`, `açabilir`, `açın`, `geç.` içerenler) aynı ölçütle sayıldığında `olcum_18`'de 300 cevabın 40'ı, `olcum_22`'de 9'udur (ör. "Evet, kapıyı açabilirsin."). `olcum_22`'de mühürdeki kelime hiçbir cevapta geçmemiş, 2 cevapta mühürde bir şey yazdığı ima edilmiş, yalnızca 1 cevapta tırnak işareti görülmüştür. 10 cevapta yabancı kelime karışmıştır (`now`, `then`, `opening`, `nichts`; ör. "Evet, bu kapı nowa opening."). Kapı bu cevaplarda açılmadığı için oyun durumu değişmez. Bu ölçüm koddaki tırnak ve kelime denetimi olmadan yapılmıştır; oyunda bu cevaplar yeniden istenir.

Yanıt süresi `olcum_22`'de ortalama 2,0 sn, en uzun 3,5 sn'dir (`olcum_18`'de 3,4 ve 6,7 sn). Pencereli oyunda bir mesajın iki isteği, model önceden yüklenmemişken 20 sn'ye kadar, sonraki mesajlarda 7-8,5 sn sürmüştür; son sürümle model bellekteyken yapılan denemede bu süre 3,6-4,3 sn olmuştur. Ham çıktılar `Tools/prompt_olcumleri` klasöründedir.

### Hata ve kararlılık testleri

| Test | Yöntem | Sonuç |
|---|---|---|
| Ollama gerçekten kapalı | Uygulama kapatılıp sahnede mesaj gönderildi | Geçti: uyarı çıktı, arayüz açık kaldı, kapı açılmadı |
| Sunucuya ulaşılamıyor | Dinlenmeyen porta istek | Geçti: `ServerUnavailable` |
| Model yüklü değil | Olmayan model adı | Geçti: `ModelMissing` |
| Zaman aşımı | Yanıt vermeyen test sunucusu, 2 sn sınır | Geçti: `Timeout` |
| Bozuk JSON | Test sunucusundan bozuk metin | Geçti: `InvalidResponse` |
| Eksik alan | Yalnızca `reply` içeren JSON | Geçti: `InvalidResponse` |
| Önce boş, sonra geçerli yanıt | Test sunucusu | Geçti: yeniden deneme başarılı |
| Kapı kuralı | Test sunucusundan hazır kararlar | Geçti: kapı yalnızca mühür varken, model kelimenin geçtiğini söylediğinde, kelime mesajda yazdığında ve öfke yokken açıldı |
| Model yanılırsa | Model `kelime_geciyor: true` dedi, mesaj `elma`, `evet`, `lütfen geçeyim` | Geçti: kapı açılmadı |
| Kelime gizliliği | Test sunucusuna giden istekler okundu | Geçti: kelime karar isteğinde var, kapı açılırken de kelime sorulurken de cevap isteğinde yok |
| Kapalı kapıda uydurma veya sızan kelime | Test sunucusu önce `Mühürde 'GİR' yazıyor.`, sonra `ŞAFAK, bu kelime mühürde var!`, sonra temiz cevap döndü | Geçti: ilk iki cevap reddedildi, temiz cevap gösterildi |
| Uzun sohbet | 13 satır eklendi | Geçti: en yeni satır kesilmedi, eski satırlar sınırdan sonra atıldı |
| Kare akışı | 2 saniyelik istek sırasında | Geçti: 6584 kare, en uzun kare 9 ms (görüntü çizimi olmayan test modunda) |
| Baştan sona senaryo | Derlenmiş Mac sürümünün içinde, pencereli modda | Geçti: mühürsüz kapı açılmadı, mühürdeki kelimeyle açıldı, bitiş ekranına ulaşıldı (oyuncu ve mühür alma test koduyla sürüldü) |

### Ekran görüntüleri

Görüntüler, oyuncunun test koduyla sürüldüğü pencereli sürümden otomatik alınmıştır (sağ alttaki "Development Build" yazısı bu yüzdendir). Muhafızın cevapları modelin gerçek çıktısıdır.

**Başlangıç: kilitli kapı, oyuncu ve companion**

![Başlangıç](Docs/ekran_goruntuleri/01_baslangic.png)

**Companion, haritada olmayan sandığı sensörle fark edip yeniden plan yapıyor (`Replan`)**

![Companion, haritada olmayan sandığı sens](Docs/ekran_goruntuleri/02_companion_yolda.png)

**Şalter indirildi, birinci kapı açıldı**

![Şalter indirildi, birinci kapı açıldı](Docs/ekran_goruntuleri/03_birinci_kapi_acik.png)

**İkinci bölge: muhafız, altın renkli mühür ve eşlik eden companion**

![İkinci bölge](Docs/ekran_goruntuleri/04_companion_eslik_ediyor.png)

**Model yanıtı beklenirken oyun akıyor, "Muhafız düşünüyor" gösteriliyor (FPS 58)**

![Model yanıtı beklenirken oyun akıyor, "M](Docs/ekran_goruntuleri/05_dusunuyor.png)

**Mührün yanında `E` ipucu**

![Mührün yanında `E` ipucu](Docs/ekran_goruntuleri/06_muhur_yaninda.png)

**Mühür alındı, kelime sol üstte görünüyor**

![Mühür alındı, kelime sol üstte görünüyor](Docs/ekran_goruntuleri/07_kelime_ekranda.png)

**Model kelimenin geçtiğine karar verdi, kelime mesajda yazıyor, kapı açıldı. İlk cevapta İngilizce `then` geçmiş, mühürsüz mesaja verilen ikinci cevapta yabancı kelime (`nowa`) karışmış ve kapı kapalıyken "kapıyı aç" denmiştir; bunlar modelin sınırına örnektir**

![Model doğru kelimeyle `door_unlocked](Docs/ekran_goruntuleri/08_muhafiz_kapiyi_acti.png)

**Bitiş ekranı**

![Bitiş ekranı](Docs/ekran_goruntuleri/09_bitis_ekrani.png)

## 6. Sonuç ve tartışma

### Başarılar

- Companion, 45 koşunun tamamında görevi tamamladı ve haritada olmayan engelleri sensörle fark edip aştı.
- Kapı kararı metin aramasıyla değil, modelin JSON yanıtıyla alınıyor; mühür alınmadan kapı açılamıyor.
- Karar ve cevabın ayrılması ile kelime denetimi sayesinde son ölçümde doğru kelime 54 denemede 54 kez kapıyı açtı, kelimesiz 300 denemenin hiçbiri açmadı ve yanlış öfke kararı görülmedi. Mühürdeki kelime cevap isteğine hiç girmediği için muhafız kelimeyi oyuncuya söyleyemez.
- Sunucu kapalıyken, model eksikken, zaman aşımında ve bozuk yanıtta oyun çökmedi.

### Sınırlılıklar

- Kelime denetimi katıdır: kelimeyi yanlış yazan oyuncu (`safak`, `zumrut`) geçemez, yeniden yazması gerekir.
- Muhafızın sözü bazen kararla çelişir: kapı kapalıyken "kapıyı açabilirsin" diyebilir (300 cevapta 9) veya araya yabancı kelime karıştırabilir (10). Kapı yine de açılmaz.
- Tırnak denetimi kabadır: oyuncunun tırnaklı cümlesini aktaran doğru bir cevap da yeniden istenir; üç denemede de tırnak gelirse oyuncuya "tekrar deneyin" uyarısı çıkar.
- Her mesaj iki istek gerektirir; yanıt oyunda model bellekteyken 3,6-4,3 sn, önceki denemelerde 7-8,5 sn, model yüklenirken 20 sn sürmüştür. Oyun bu sırada akmaya devam eder.
- Muhafız önceki mesajları hatırlamaz. Doğruluk ölçümleri geçmişsiz yapıldığı için oyunda da geçmiş gönderilmez.
- Daha iyi Türkçe veren `gemma3:4b`, 8 GB RAM'li bilgisayarda oyunla birlikte çalıştırılamayacak kadar ağır kalmıştır.
- Pencereli oyunda ekrandaki sayaç önceki denemelerde 42-62 FPS, son denemede 58-60 FPS göstermiştir (en düşük değer model ilk kez yüklenirken); ayrı bir uzun süreli FPS ölçümü yapılmamıştır. 2 saniyelik kare akışı testi görüntü çizimi olmayan modda yapılmıştır.
- Windows sürümü derlenmiş, ancak Windows bilgisayarda çalıştırılarak denenmemiştir.
- Muhafızın yanıtları modelden gelir, ancak o duruma ait örneklere benzeyebilir; ölçümlerin ham çıktıları `Tools/prompt_olcumleri` klasöründedir.
- Companion'ın tek görevi vardır ve sandık dışındaki hareketli engeller için sınanmamıştır.

### Olası geliştirmeler

- Daha iyi Türkçe destekleyen bir yerel modele geçmek ve yanıtları akışla (streaming) göstermek
- Companion'a birden fazla görev ve davranış ağacı eklemek
- ML-Agents ile gerçek bir pekiştirmeli öğrenme eğitimi yapmak
