# День 1 и дальше — пошаговый гайд с нуля

Tower Networking Inc. Гайд для человека, **который никогда не играл**.  
Стартовая ситуация: **ЦОД (floor 0) + 3 этажа**.  
Схема: этажи режем на **блоки по 3** (`b1` = этажи 1–3, потом `b2` = 4–6…).  
Пресет: без авто-DNS/авто-DHCP, **нужны сетевые адреса**, **реальная ПС**, без автозапуска программ.

Связанные файлы:

- полный справочник: [`tni-floor-connectivity.md`](./tni-floor-connectivity.md)
- только алиасы: [`alias-pack.txt`](./alias-pack.txt)

---

## Оглавление

1. [Что это за игра (30 секунд)](#что-это-за-игра-30-секунд)
2. [Как устроена сеть в двух словах](#как-устроена-сеть-в-двух-словах)
3. [Имена (заучи сразу)](#имена-заучи-сразу)
4. [Цвета кабелей](#цвета-кабелей)
5. [Список покупок на старт](#список-покупок-на-старт)
6. [Карта соединений](#карта-соединений)
7. [ЧАСТЬ I — День 1 (пошагово)](#часть-i--день-1-пошагово)
8. [ЧАСТЬ II — Расширение (что зачем)](#часть-ii--расширение-что-зачем)
9. [Алиасы (скопируй в netshell)](#алиасы-скопируй-в-netshell)
10. [Чеклисты](#чеклисты)

---

## Что это за игра (30 секунд)

Ты ISP в башне. Жильцы и офисы хотят интернет и сервисы. Ты:

1. **Физически** соединяешь устройства кабелями и заказываешь **Tower Link** между этажами.  
2. **Логически** даёшь устройствам имена `@…`, прописываешь **маршруты** и **DNS**.  
3. **Зарабатываешь**, когда клиенты доходят до твоих сервисов (VOIP, Git…) или связываешь consumer↔producer.

Без кабеля — тишина. Без `@имени` (при твоём пресете) — тоже тишина. Без маршрута — пакет не знает, куда идти. Без `dns map` — жилец не найдёт `voip.none`.

---

## Как устроена сеть в двух словах

```text
Клиент этажа → Switch (Blade) → розетка riser
       ↕ Tower Link (приложение на телефоне)
ЦОД: edge-роутер блока → ядро → сервисный роутер → серверы (DNS/VOIP/Git)
```

**Блок** = группа из ~3 этажей с одним общим edge-роутером. Сейчас у тебя ровно один блок `b1` (этажи 1–3). Когда появятся этажи 4–6 — **не** вешай их на тот же edge: заведи блок `b2`.

---

## Имена (заучи сразу)

```text
@c1                 ядро ЦОД (Disco Micro)
@c1/b1              edge блока 1 — все 3 этажа сюда (Disco Milli)
@c1/b1/dns          DNS блока 1 (Boulder+)
@c1/svc             сервисный роутер money (Disco Milli)
@c1/svc/voip        VOIP-сервер
@c1/svc/git         GitCoffee
@c1/svc/padu        Padu (опционально на старте)
@c1/b1/f1/s1        switch этажа 1
@c1/b1/f1/c1        клиент на этаже 1
@c1/b1/f2/p1        продюсер на этаже 2
```

Позже: `@c1/b2` для этажей 4–6, `@c2` для второго ЦОД. **Не** называй DNS просто `@dns` — потом ад.

---

## Цвета кабелей

Цвет **не влияет на скорость** — только на порядок. Один цвет = один заказ на одну длину.

| Цвет | Куда |
|------|------|
| **Синий** | Клиент → switch |
| **Зелёный** | Продюсер / телефон / камера → switch |
| **Жёлтый** | Switch → riser (uplink этажа) |
| **Оранжевый** | Роутер ↔ роутер (ствол в ЦОД) |
| **Красный** | К серверам (DNS, VOIP, Git) |
| **Фиолетовый** | Debugger |

Длину меряй клавишей **T**.

---

## Список покупок на старт

Открой магазин / каталог и купи примерно так (имена железа могут чуть отличаться по билду — бери ближайший класс):

### Железо в ЦОД

| Кол-во | Что | Имя потом | Зачем |
|--------|-----|-----------|--------|
| 1 | Disco **Micro** (или аналог «ядро») | `@c1` | Склеивает блок и сервисы |
| 1 | Disco **Milli** | `@c1/b1` | Edge: все uplink’и этажей 1–3 |
| 1 | Disco **Milli** | `@c1/svc` | Роутер перед money-серверами |
| 1 | Boulder+ | `@c1/b1/dns` | DNS (авто-DNS у тебя выкл!) |
| 1 | Boulder+ | `@c1/svc/voip` | voip-server |
| 1 | Boulder+ | `@c1/svc/git` | gitcoffee (+ часто padu) |
| 1 | Debugger | `@me` (по желанию) | Терминал netshell |

На старте **можно отложить**: отдельный DHCP-сервер, firewall, Padu на третьем Boulder, NAS.

### Железо на каждый из 3 этажей

| Кол-во | Что | Зачем |
|--------|-----|--------|
| 1 | Blade5 (switch) | Клиенты и uplink |
| питание | UK plug / то, что даёт этаж | Без света switch мёртв |
| патчи | синий Ethernet | клиенты → switch |
| 1 uplink | жёлтый, длина по T | switch → розетка riser |

### Кабели в ЦОД (ориентир)

| Цвет | Длина | Сколько | Куда |
|------|-------|---------|------|
| Оранжевый | 200 | 3–4 | `@c1`↔`@c1/b1`, `@c1`↔`@c1/svc` |
| Красный | 200–500 | 4–6 | svc → dns/voip/git |
| Фиолетовый | 1000–1500 | 1 | debugger |
| Жёлтый | 500–2000 | 3 | этажи (по замерам) |
| Синий | 200–1000 | пачка | клиенты на этажах |
| Зелёный | 200–500 | пара | телефон/камера, если есть |

### Программы (что ставить)

| Программа | Куда | Зачем | Когда |
|-----------|------|-------|--------|
| `dns-server` | `@c1/b1/dns` | Резолв имён | **День 1, обязательно** |
| `voip-server` | `@c1/svc/voip` | Деньги STREAM-VOICE | День 1 |
| `gitcoffee` | `@c1/svc/git` | Деньги UPDATE-SOFTWARE | День 1 |
| `padu_v1` | на git и/или отдельный padu | Store / связка с updates | День 1–2 |
| `dnsmasq` или `kea` | `@c1/b1/dhcp` | Автораздача `@` | Позже (у тебя DHCP выкл) |

У тебя **автозапуск выкл** — после `program install` проверь, что сервис жив (`watch`, трафик).

### Приложения на телефоне (Rocket Store)

| Приложение | Зачем |
|------------|--------|
| **Tower Link** | Обычно уже есть — связь этаж↔ЦОД |
| **The Registry** | Домены voip.none / git.none + PPU |
| **Surveyor** | Кто producer/consumer, когда онлайн |
| **Socketeer** (~500$) | Лишние розетки в ЦОД — удобно, не обязательно в час 1 |

---

## Карта соединений

### В ЦОД (логика портов — запиши свои номера!)

Пример раскладки (подставь свои port0, port1…):

```text
[ rиser этажа 1 ] --жёлтый/Tower Link-- port0 @c1/b1
[ rиser этажа 2 ] ---------------------- port1 @c1/b1
[ rиser этажа 3 ] ---------------------- port2 @c1/b1

@c1/b1  --оранжевый--  portX @c1  --оранжевый--  @c1/svc
                              |
                         (ядро склеивает)

@c1/svc --красный-- @c1/b1/dns
@c1/svc --красный-- @c1/svc/voip
@c1/svc --красный-- @c1/svc/git

Debugger --фиолетовый-- в любой живой порт (часто в @c1 или @c1/b1)
```

DNS можно воткнуть в `@c1/b1` вместо svc — главное, чтобы был маршрут от клиентов до DNS и от DNS «наружу» не обязателен для простых map.

### На этаже

```text
Клиенты --синий--> Blade5 --жёлтый--> розетка на стене (riser)
Питание --> Blade5
Телефон/камера --зелёный--> Blade5   (если есть)
```

Потом в телефоне **Tower Link**: floor 0 + serial порта в ЦОД ↔ floor N + serial на этаже → скорость **cat1** на старт → **Request link**. Индикаторы link lights должны загореться.

---

# ЧАСТЬ I — День 1 (пошагово)

Делай строго по порядку. Не прыгай к DNS, пока не горят линки.

## Шаг 0. Осмотрись

1. Ты в **ЦОД (floor 0)** — стойки, розетки сети и питания.  
2. На телефоне открой **Surveyor** — посмотри этажи 1–3: кто consumer, кто producer, часы активности.  
3. Запиши в блокнот (или clipboard игры) serial’ы розеток riser, которые будешь использовать.

## Шаг 1. Расставь железо в ЦОД

1. Поставь Micro → это будет `@c1`.  
2. Поставь Milli рядом → `@c1/b1`.  
3. Поставь второй Milli → `@c1/svc`.  
4. Поставь Boulder+ ×3 → dns, voip, git.  
5. Подключи **питание** ко всему (электричество платное — не включай лишнее «на склад» без нужды).  
6. Соедини оранжевым: `b1`↔`c1`↔`svc`.  
7. Соедини красным: `svc`→ dns, voip, git.  
8. Воткни debugger фиолетовым.

## Шаг 2. Открой netshell и заведи алиасы

1. Debugger в сети → открой терминал / netshell.  
2. Узнай hardware id debugger’а (число на устройстве / в UI).  
3. Выполни (подставь число):

```text
always using 12345
```

или после создания алиасов: `setdbg 12345`.

4. Скопируй блок **«Старт: обязательные алиасы»** из [конца файла](#алиасы-скопируй-в-netshell) (или весь [`alias-pack.txt`](./alias-pack.txt)).

## Шаг 3. Имена устройств в ЦОД

Hardware ID смотри на каждом устройстве. DNS пока указываем на будущий `@c1/b1/dns` (даже если DNS ещё не поднят — имя уже правильное).

```text
ncall @c1 @c1/b1/dns HARDWARE_ID_ЯДРА
ncall @c1/b1 @c1/b1/dns HARDWARE_ID_EDGE
ncall @c1/svc @c1/b1/dns HARDWARE_ID_SVC
ncall @c1/b1/dns @c1/b1/dns HARDWARE_ID_DNS
ncall @c1/svc/voip @c1/b1/dns HARDWARE_ID_VOIP
ncall @c1/svc/git @c1/b1/dns HARDWARE_ID_GIT
```

Проверка: `ping @c1/b1` (когда маршруты появятся — см. ниже).

## Шаг 4. Программы на серверах

```text
pidns2 @c1/b1/dns
pivoip @c1/svc/voip
pigitc @c1/svc/git
pip1 @c1/svc/git
```

Автозапуска нет — убедись, что программы реально работают (`watch @c1/b1/dns`, и т.д.). Если в билде нужен явный start — смотри `man program` / `program list`.

## Шаг 5. Маршруты в ЦОД

Подставь **свои** номера портов (смотри, в какой port воткнут кабель).

```text
rca @c1/b1 0 @c1
rca @c1/svc 1 @c1
rca @c1 7 @c1/b1
rcd 7 @c1/b1
rca @c1 7 @c1/svc
rcd 7 @c1/svc
rca @c1/b1/dns 0 @c1/svc
rca @c1/svc/voip 1 @c1/svc
rca @c1/svc/git 2 @c1/svc
```

Идея: ядро знает, где блок и где svc; блок и svc знают путь в ядро; svc знает порты серверов.

Проверка:

```text
rsh @c1
rsh @c1/b1
ping @c1/b1/dns
```

## Шаг 6. Registry (деньги)

1. Rocket Store → **The Registry**.  
2. Домен `voip.none` → usage **STREAM-VOICE** → PPU **~1.1**.  
3. Домен `git.none` → usage **UPDATE-SOFTWARE** → PPU **~1.1**.  
4. В netshell:

```text
dmap voip.none @c1/svc/voip @c1/b1/dns
dmap git.none @c1/svc/git @c1/b1/dns
```

Без этого шага (авто-DNS выкл) домен в Registry пустой.

## Шаг 7. Этаж 1 (потом 2 и 3 — тот же рецепт)

На каждом этаже:

1. Поставь Blade5 у riser, питание.  
2. Синие патчи: все клиентские порты → switch.  
3. Жёлтый: свободный порт switch → розетка стены.  
4. Запиши serial розетки (4 буквы).  
5. Телефон → **Tower Link**:  
   - From: floor **0**, serial порта на `@c1/b1` (например port0)  
   - To: floor **1**, serial на этаже  
   - Speed: **cat1**  
   - Request link  
6. Debugger на этаж (или управляй удалённо, если уже есть путь).  
7. Имена (пример для этажа 1):

```text
ncall @c1/b1/f1/s1 @c1/b1/dns SWITCH_HW
ncall @c1/b1/f1/c1 @c1/b1/dns CLIENT1_HW
ncall @c1/b1/f1/c2 @c1/b1/dns CLIENT2_HW
```

Повтори для всех видимых клиентов. Спящих — когда проснутся (Surveyor).

8. На edge `@c1/b1` маршрут на этаж (port0 = этаж 1 в примере шага 1):

```text
rca @c1/b1/f1 0 @c1/b1
```

9. Проверка:

```text
ping @c1/b1/f1/s1
trc @c1/svc/voip from @c1/b1/f1/c1
trc @c1/b1/dns from @c1/b1/f1/c1
```

## Шаг 8. Этажи 2 и 3

То же: Blade → патчи → жёлтый uplink → Tower Link на **другие порты** `@c1/b1` (port1, port2) → `ncall` →

```text
rca @c1/b1/f2 1 @c1/b1
rca @c1/b1/f3 2 @c1/b1
```

Продюсера назови `@c1/b1/f2/p1` и замапь его домен из Surveyor:

```text
dmap имя_продюсера.xxx @c1/b1/f2/p1 @c1/b1/dns
```

## Шаг 9. Телефон (VOIP-выручка)

Без **публичного телефона** на сети VOIP с Accept-VOIP часто не капает.

1. Найди телефон на этаже.  
2. Зелёный патч → switch.  
3. `ncall @c1/b1/f2/phone @c1/b1/dns HW`.  
4. Traffic-route к VOIP:

```text
rcat udp/5060 PORT_К_ЯДРУ @c1/b1
rcat udp/5060 PORT_VOIP @c1/svc
```

5. Смотри `watch @c1/svc/voip` — есть ли трафик.

## Шаг 10. День 1 готов, если

- [ ] Link lights на всех 3 этажах  
- [ ] `ping` switch и клиентов  
- [ ] `trace` до voip/git с этажа  
- [ ] `dns map` voip.none и git.none  
- [ ] Хотя бы один money-сервис принимает трафик  
- [ ] Имена только в схеме `@c1/b1/…`

**Не делай сейчас:** вешать этаж 4 на `@c1/b1`, плоский `@dns`, firewall default deny без allow tcp/23.

---

# ЧАСТЬ II — Расширение (что зачем)

Ниже — **по пунктам**: зачем нужно → когда брать → что купить → как настроить → алиасы.  
Не бери всё сразу: сначала деньги стабильны, потом Remote Backups, потом FW, потом остальное.

## Обзор приоритетов

| Порядок | Тема | Зачем | Unlock / цена (ориентир) |
|---------|------|-------|---------------------------|
| 1 | Блок b2 (этажи 4–6) | Не убить ПС и b1 | Магазин + бюджет |
| 2 | Firewall | Morris / scraper | Железо FW |
| 3 | DHCP блока | Не `ncall` каждый порт | dnsmasq/kea |
| 4 | **Remote Backups** (`sftp`) | Конфиги после смерти warranty | Secretariat **~450$** |
| 5 | **Jailbreaker** | Достать ПО с железа (FW→сервер и т.п.) | Secretariat **~1000$** (часто нужен sftp) |
| 6 | Socketeer | Розетки где удобно | Rocket Store **~500$** |
| 7 | VLAN | Разделить phone/cam/клиентов на одном uplink | Managed switch |
| 8 | RIP | Не писать mid-hop routes руками | Secretariat **~1500$** |
| 9 | Padu / больше money | Больше usage | padu_v1/v2/v3 |
| 10 | Второй ЦОД | Место + короче путь наверх | New Data Center (**+10% admin**) |
| 11 | Cablers Union | Дешевле кабели / makers | Secretariat |
| 12 | VM / HA / NetOps | Поздняя оптимизация | Research proposals |

---

## 1. Новый блок `b2` (этажи 4–6)

### Зачем
Один edge на 6+ этажей = перегруз ПС и единая точка отказа. Блок изолирует сбой: умер `b2` — `b1` и money живы.

### Почему может потребоваться
Появились этажи 4+. View Links краснеет. Клиенты тормозят.

### Что купить
- Disco Milli → `@c1/b2`  
- Boulder+ → `@c1/b2/dns`  
- (скоро) DHCP-сервер  
- Blade5 ×3 + жёлтые uplink’и + Tower Link  
- Оранжевый линк `@c1/b2` → `@c1`

### Настройка

```text
ncall @c1/b2 @c1/b2/dns EDGE_HW
ncall @c1/b2/dns @c1/b2/dns DNS_HW
pidns2 @c1/b2/dns
dmap voip.none @c1/svc/voip @c1/b2/dns
dmap git.none @c1/svc/git @c1/b2/dns
rca @c1/b2 PORT @c1
rca @c1 PORT @c1/b2
rcd PORT_TO_CORE @c1/b2
```

Этажи: имена `@c1/b2/f1/…`, uplink’и в порты `b2`, **не** в `b1`.

---

## 2. Firewall

### Зачем
Morris и text-scraper жрут ПС и ломают роутеры/серверы. FW на uplink блока режет мусор **до** ядра.

### Почему может потребоваться
Первый Morris. Красные линки без явного трафика жильцов. Хочешь default deny перед money.

### Что купить
1× firewall на путь `@c1/b1` → `@c1` (и позже перед `@c1/svc`).  
**Datawiper USB** — если сам себя отрезал.

### Настройка (осторожно!)

Сначала **всегда** allow **tcp/23** (управление), иначе lockout.

Мягкий старт (blacklist при default allow):

```text
fcmal @c1/b1/fw
```

Жёсткий whitelist:

```text
fcall @c1/b1/fw
```

или комбо:

```text
fcsafe @c1/svc/fw
```

Клон правил после sftp: `cpfw @fw1 @fw2`.

---

## 3. DHCP в блоке

### Зачем
При выключенном Default DHCP каждый новый клиент = ручной `ncall`. DHCP раздаёт prefix и DNS сам.

### Почему может потребоваться
Много устройств на 3 этажах. Надоело бегать с debugger’ом.

### Что купить
Boulder (или второй) → `@c1/b1/dhcp`. Программа `dnsmasq` или `kea`.

### Настройка

```text
ncall @c1/b1/dhcp @c1/b1/dns DHCP_HW
pidhcp1 @c1/b1/dhcp
dhprefix @c1/b1/ @c1/b1/dhcp
dhdns @c1/b1/dns @c1/b1/dhcp
dhbind PRODUCER_HW @c1/b1/f2/p1 @c1/b1/dhcp
rcat udp/67 PORT_TO_DHCP @c1/b1
rcb @c1/b1
```

Авто-DNS доменов Registry **всё равно нет** — `dmap` руками.

---

## 4. Remote Backups (`sftp`) — обязательно для долгой башни

### Зачем
После **warranty** железо дохнет. Routes, firewall rules, программы живут на устройстве. Без бэкапа = настройка с нуля.

### Почему может потребоваться
Любой edge/ядро/DNS с конфигом. Особенно после первого «умер роутер».

### Unlock
Secretariat → **Remote Backups** (~450$, «3-2-1 let's back it up!») → команда `sftp`.

### Что купить
Устройство со **свободным storage** (NAS / запасной Boulder) → `@nas`.

### Настройка

```text
sfls @c1/b1
bkroutes @c1/b1 @nas
bkfw @c1/b1/fw @nas
```

Восстановление: `sftp cp` **с NAS на** новое железо с тем же `@`, потом те же кабели/порты.

Когда откроют **cron** + `try`:

```text
crdr routes @c1/b1 @nas
crping @c1
```

---

## 5. Jailbreaker (не забудь)

### Зачем
**Достать программы/прошивки с железа** и поставить на другое устройство. Примеры из сообщества:

- скопировать firewall-софт на обычный сервер (software-firewall);  
- переносить бинарники/конфиги между коробками вместе с sftp;  
- гибче кастомить, чем «только магазинный FW».

Без Jailbreak’а sftp часто копирует конфиги, но **извлекать и ставить «железные» программы** куда угодно — ограничено.

### Почему может потребоваться
Хочешь FW-логику на сервере; мало слотов под железные firewall; эксперименты с routing-firewall; копирование ПО после аварии.

### Unlock
Secretariat → **Jailbreaker** (~1000$, «Hardware Liberation Day»).  
В гайдах часто пишут: **сначала Remote Backups / sftp**, потом Jailbreak (или оба).

### Как пользоваться (общая схема)

1. Unlock Jailbreaker + рабочий `sftp`.  
2. `sftp ls on @firewall` — смотри `/bin/`, конфиги (`/etc/nftables.conf` и т.п.).  
3. Копируй нужные файлы на сервер с storage.  
4. На целевом устройстве — install/запуск по man текущей сборки (интерфейс ещё развивается).  
5. Не забудь allow’ы и tcp/23, если крутишь FW-правила на новом хосте.

```text
sfls @c1/b1/fw
sfcp /etc/nftables.conf @c1/b1/fw @nas /backups/fw/nftables.conf
cpfw @c1/b1/fw @spare_fw
```

Точные пути бинарников смотри `sftp ls` и `man` у себя — сборки отличаются.

### Связка sftp + Jailbreak + Morris

Чистка роутера после Morris часто: `sftp rm /bin/morris…` (`morrt`). Без sftp — боль. Jailbreak расширяет, что можно снимать/переносить с железа.

---

## 6. GitCoffee + Padu (money глубже)

### Зачем
`git.none` (UPDATE-SOFTWARE) + Padu (Store-Text и связки) — стабильный доход наряду с VOIP.

### Почему может потребоваться
Только VOIP мало; Surveyor показывает спрос на updates/store.

### Настройка

```text
pigitc @c1/svc/git
pip1 @c1/svc/git
pip1 @c1/svc/padu
dmap git.none @c1/svc/git @c1/b1/dns
rcat tcp/443 PORT_GIT @c1/svc
rcat tcp/80 PORT_PADU @c1/svc
```

Registry: git → UPDATE-SOFTWARE PPU ~1.1; store-домены → Store-Text ~1.1.  
Позже Secretariat: padu_v2/v3, poems-db.

---

## 7. Socketeer

### Зачем
Дополнительные **розетки** copper/fiber/power в мире — не тянуть кабель через весь ЦОД.

### Почему
Закончились порты на стене; второй ряд стоек; новый ЦОД.

### Как
Rocket Store → Socketeer (~500$) → place socket → pay. Remove — тоже платно.

---

## 8. VLAN

### Зачем
При **конечной ПС** switch ≈ хаб: телефоны, камеры и клиенты мешают друг другу. VLAN режет broadcast-домены на одном физическом uplink’е.

### Почему может потребоваться
Uplink этажа забит; udp/5060 и udp/554 тонут в клиентском шуме; хочешь phone «ближе» к VOIP.

### Что купить
Managed switch с VLAN (Blade12/15/88… — смотри фичи). Иногда VLAN-роутер (router-on-a-stick).

### Настройка (идея)

```text
vshow @c1/b1/f1/s1
vtag1 2 10 @c1/b1/f1/s1
vlan tag port0 with #10 #20 on @c1/b1/f1/s1
```

На роутере — subinterface `port0.1` с тем же tag и `rca` на префикс VLAN.  
Новичку: **сначала блоки без VLAN**; VLAN — когда упёрся в ПС.

---

## 9. RIP

### Зачем
Автораздача маршрутов между роутерами. Ты пишешь только endpoint’ы; середина подхватывается.

### Unlock
Secretariat → Route Discovery / RIP (~1500$).

```text
ripup @c1
ripup @c1/b1
ripup @c1/svc
ripsh @c1
```

Endpoint’ы (`rca` на voip/git) всё равно нужны.

---

## 10. Второй ЦОД

### Зачем
Место под стойки; edge верхних блоков ближе к этажам; разгрузка линков в подвал.

### Unlock
**New Data Center** — бесплатно, но **admin fees +10%**.

### Как
Ядро `@c2`, блоки `@c2/b1…`, Tower Link c2↔c1, money оставь на `@c1/svc`. Подробно: [`tni-floor-connectivity.md`](./tni-floor-connectivity.md) → «Несколько ЦОД».

---

## 11. Tower Link апгрейд / ПС

### Зачем
cat1 ≈ 15 traversals/tick. Много этажей → 100% в View Links.

### Как
View Links → Manage → deactivate → upgrade (cat5 / fiber…) → reactivate. Краткий outage.  
Лечение роста: блоки + толще ствол + второй ЦОД, не «все на один Blade».

---

## 12. Прочее (кратко)

| Тема | Зачем | Когда |
|------|-------|--------|
| **Cablers Union** | −30% кабели, потом cable makers | Много закупок Ethernet |
| **Power Management** | Счета за свет, wake/suspend NAS | После Remote Backups / cron |
| **VM Research** | Несколько ролей на одном сервере | Мало места в стойке |
| **HA Research** | Отказоустойчивость роутеров | Поздняя игра |
| **pcap / tap** | Диагностика перегруза | Красные линки без причины |
| **Blackhole route** | Глушить scraper без FW | Быстрый костыль: `rcbh tcp/8034 EMPTY_PORT @edge` |
| **Decentro** | crypto tcp/8333 | Отдельный money-stream |
| **Botnet / UBBT** | Подмена/раздувание трафика | Поздний/опциональный контент |

---

# Алиасы (скопируй в netshell)

Создание: вставь строку целиком. Удаление: `alias имя`.  
Полный пак: [`alias-pack.txt`](./alias-pack.txt).

## Старт: обязательные алиасы

```text
alias setdbg echo usage: setdbg DEBUGGER_ADDR; always using $1
alias ncall echo usage: ncall NETADDR DNS_ADDR DEVICE; net address set $1 on $3; net dns set $2 on $3; net dhcp disable on $3
alias nca echo usage: nca NETADDR DEVICE; net address set $1 on $2
alias rsh echo usage: rsh ROUTER; route show on $1
alias rca echo usage: rca DEST_OR_PREFIX PORTNUM ROUTER; route add $1 via port$2 on $3
alias rcat echo usage: rcat TRAFFIC PORTNUM ROUTER; route add traffic $1 via port$2 on $3
alias rcd echo usage: rcd PORTNUM ROUTER; route default via port$1 on $2
alias rcb echo usage: rcb ROUTER; route enable broadcast on $1
alias dmap echo usage: dmap DOMAIN TARGET DNS; dns map $1 as $2 on $3
alias dsh echo usage: dsh DNS; dns show on $1
alias pdev echo usage: pdev ADDR; ping $1
alias trc echo usage: trc DEST from SRC; trace $1 from $2
alias wdev echo usage: wdev ADDR; watch $1
alias pidns2 echo usage: pidns2 SERVER; program install dns-server on $1
alias pivoip echo usage: pivoip SERVER; program install voip-server on $1
alias pigitc echo usage: pigitc SERVER; program install gitcoffee on $1
alias pip1 echo usage: pip1 SERVER; program install padu_v1 on $1
alias q echo usage: q; quit
alias cls echo usage: cls; clear
```

## DHCP (когда поднимешь)

```text
alias pidhcp1 echo usage: pidhcp1 SERVER; program install dnsmasq on $1
alias pidhcp2 echo usage: pidhcp2 SERVER; program install kea on $1
alias dhs echo usage: dhs DHCP; dhcp show on $1
alias dhprefix echo usage: dhprefix PREFIX DHCP; dhcp option prefix $1 on $2
alias dhdns echo usage: dhdns DNS DHCP; dhcp option dns $1 on $2
alias dhbind echo usage: dhbind HWADDR NETADDR DHCP; dhcp option bind $1 as $2 on $3
```

## Firewall

```text
alias fsh echo usage: fsh FIREWALL; firewall show on $1
alias fcclear echo usage: fcclear FIREWALL; firewall clear on $1
alias fcadbug echo usage: fcadbug FIREWALL; firewall allow tcp/23 on $1
alias fcmal echo usage: fcmal FIREWALL; firewall deny tcp/8034 on $1; firewall deny tcp/510 on $1; firewall deny tcp/511 on $1; firewall deny tcp/512 on $1; firewall deny tcp/513 on $1; firewall deny tcp/514 on $1; firewall deny tcp/515 on $1; firewall deny tcp/516 on $1; firewall deny tcp/517 on $1; firewall deny tcp/518 on $1; firewall deny tcp/519 on $1
alias fcall echo usage: fcall FIREWALL; firewall allow tcp/23 on $1; firewall allow udp/53 on $1; firewall allow udp/67 on $1; firewall allow udp/5060 on $1; firewall allow udp/554 on $1; firewall allow icmp on $1; firewall allow tcp/80 on $1; firewall allow tcp/443 on $1; firewall allow udp/1194 on $1; firewall allow udp/8333 on $1; firewall default deny on $1
alias fcsafe echo usage: fcsafe FIREWALL; firewall deny tcp/8034 on $1; firewall deny tcp/510 on $1; firewall deny tcp/511 on $1; firewall deny tcp/512 on $1; firewall deny tcp/513 on $1; firewall deny tcp/514 on $1; firewall deny tcp/515 on $1; firewall deny tcp/516 on $1; firewall deny tcp/517 on $1; firewall deny tcp/518 on $1; firewall deny tcp/519 on $1; firewall allow tcp/23 on $1; firewall allow udp/53 on $1; firewall allow udp/67 on $1; firewall allow udp/5060 on $1; firewall allow udp/554 on $1; firewall allow icmp on $1; firewall allow tcp/80 on $1; firewall allow tcp/443 on $1; firewall allow udp/1194 on $1; firewall allow udp/8333 on $1; firewall default deny on $1
```

## SFTP / бэкапы / Jailbreak-помощники (нужен Remote Backups)

```text
alias sfls echo usage: sfls DEVICE; sftp ls on $1
alias sfcp echo usage: sfcp PATH SRC DST RENAMETO; sftp cp $1 on $2 to $3 rename $4
alias sfrm echo usage: sfrm PATH DEVICE; sftp rm $1 on $2
alias bkroutes echo usage: bkroutes ROUTER NAS; echo backup routes $1 to $2; sftp cp /etc/routes.conf on $1 to $2 rename /backups/$1/routes.conf
alias bkfw echo usage: bkfw FW NAS; echo backup fw $1 to $2; sftp cp /etc/nftables.conf on $1 to $2 rename /backups/$1/nftables.conf
alias cpfw echo usage: cpfw FW_SRC FW_DST; sftp cp /etc/nftables.conf from $1 to $2
alias morrt echo usage: morrt ROUTER PATH; sftp rm $2 on $1
alias morsr echo usage: morsr SERVER PROGNAME; program uninstall $2 on $1
```

Cron (когда откроют + `try`):

```text
alias drstart echo usage: drstart CONFNAME DEVICE NAS; power wake on $3; sftp cp /etc/$1.conf on $2 to $3 rename /backups/$2/$1.conf; power suspend on $3; echo Backup of $2 attempted
alias drtest echo usage: drtest CONFNAME DEVICE NAS; try drstart $1 $2 $3 else notify Backup for $2 failed
alias crdr echo usage: crdr CONFNAME DEVICE NAS; cron add hourly drtest $1 $2 $3
alias crping echo usage: crping ADDR; cron add */30 try ping $1 else notify Connection to $1 failed from Debugger
```

## VLAN / RIP / pcap / blackhole

```text
alias vshow echo usage: vshow SWITCH; vlan show on $1
alias vtag1 echo usage: vtag1 PORTNUM TAGNUM SWITCH; vlan tag port$1 with #$2 on $3
alias vuntag echo usage: vuntag PORTNUM TAGNUM SWITCH; vlan untag port$1 with #$2 on $3
alias ripup echo usage: ripup ROUTER; rip advertise on $1; rip listen on $1
alias ripsh echo usage: ripsh ROUTER; rip show on $1
alias pcapa echo usage: pcapa TAP; pcap on $1
alias pcape echo usage: pcape TAP; pcap exclude =tcp/23 =udp/67 =udp/53 on $1
alias rcbh echo usage: rcbh TRAFFIC EMPTY_PORTNUM ROUTER; route add traffic $1 via port$2 on $3
```

---

# Чеклисты

## День 1

- [ ] Куплено: Micro, 2× Milli, 3× Boulder+, Blade5×3, кабели по цветам, питание  
- [ ] `always using` / `setdbg`  
- [ ] Алиасы стартовые вставлены  
- [ ] Имена `@c1`, `@c1/b1`, `@c1/svc`, dns/voip/git  
- [ ] Программы dns + voip + git (+ padu)  
- [ ] Маршруты c1 ↔ b1 ↔ svc ↔ серверы  
- [ ] Registry + `dmap` voip/git  
- [ ] 3 этажа: switch, Tower Link cat1, `ncall`, `rca` на f1/f2/f3  
- [ ] Телефон + `rcat udp/5060`  
- [ ] `ping` / `trace` ок  

## Первая неделя расширения

- [ ] FW + `fcmal` или `fcsafe` (tcp/23 первым!)  
- [ ] Remote Backups → NAS → `bkroutes`  
- [ ] Jailbreaker (после/вместе с sftp)  
- [ ] DHCP блока или терпишь статику  
- [ ] План на `b2`, когда появятся этажи 4+  
- [ ] Socketeer, если мало розеток  

## Долгая башня

- [ ] VLAN только если ПС давит  
- [ ] RIP когда надоело писать mid-routes  
- [ ] Второй ЦОД осознанно (+10% admin)  
- [ ] Апгрейд Tower Link по View Links  
- [ ] Бэкапы edge + ядра + FW актуальные  

---

## Если что-то не работает

| Симптом | Что проверить |
|---------|----------------|
| Кабель есть, интернета нет | Есть ли `@` (`ncall`)? |
| Имя не открывается | `dns map` на том DNS, что у клиента |
| Install «тишина» | Автозапуск выкл — сервис запущен? |
| Link не горит | Tower Link Request + правильные serial |
| Trace обрывается | `rsh` на каждом роутере; обратный путь |
| Залочил FW | Datawiper USB |
| Всё тормозит | View Links 100% → апгрейд cat / блоки |

---

## Источники

- Steam: [Hitchhiker's Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3651464033)
- Steam: [Firewalls - Basics](https://steamcommunity.com/sharedfiles/filedetails/?id=3548511586)
- Локально: [`tni-floor-connectivity.md`](./tni-floor-connectivity.md)

Сверяй `man` на своей сборке: команды и пути файлов слегка плавают между патчами.
