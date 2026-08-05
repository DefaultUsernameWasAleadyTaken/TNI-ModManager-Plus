# День 1 и дальше — пошаговый гайд с нуля

Tower Networking Inc. Гайд для человека, **который никогда не играл**.  
Стартовая ситуация: **ЦОД (floor 0) + 3 этажа**.  
Схема: блоки по 3 этажа; на каждом этаже **роутер** в цепочке вверх/вниз (у последнего этажа блока — только вниз); edge/DNS/money в ЦОД.  
Пресет: без авто-DNS/авто-DHCP, **нужны сетевые адреса**, **реальная ПС**, без автозапуска программ.

Связанные файлы:

- полный справочник: [`tni-floor-connectivity.md`](./tni-floor-connectivity.md)
- только алиасы: [`alias-pack.txt`](./alias-pack.txt)

---

## Оглавление

1. [Что это за игра (30 секунд)](#что-это-за-игра-30-секунд)
2. [Как устроена сеть в двух словах](#как-устроена-сеть-в-двух-словах)
3. [Анатомия блока: цепочка этажных роутеров](#анатомия-блока-цепочка-этажных-роутеров)
4. [Имена (заучи сразу)](#имена-заучи-сразу)
5. [Цвета кабелей](#цвета-кабелей)
6. [Список покупок на старт](#список-покупок-на-старт)
7. [Порядок подключений: что к чему](#порядок-подключений-что-к-чему)
8. [ЧАСТЬ I — День 1 (пошагово)](#часть-i--день-1-пошагово)
9. [Что ещё легко забыть](#что-ещё-легко-забыть-чеклист-дырок)
10. [ЧАСТЬ II — Расширение (что зачем)](#часть-ii--расширение-что-зачем)
11. [Алиасы (скопируй в netshell)](#алиасы-скопируй-в-netshell)
12. [Чеклисты](#чеклисты)

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
Клиенты → Blade → этажный роутер
                ↕ вверх/вниз (Tower Link между этажами)
Низ блока → ЦОД: edge блока → ядро → svc → money (DNS/VOIP/Git)
```

**Блок** = ~3 этажа, связанные **цепочкой этажных роутеров**. Сейчас `b1` = этажи 1–3. Этажи 4–6 = новый блок `b2`, своя цепочка — **не** продолжай линк «вверх» из этажа 3 в этаж 4 как будто это всё ещё b1.

---

## Анатомия блока: цепочка этажных роутеров

### Идея

На **каждом** этаже блока свой роутер:

| Этаж внутри блока | Куда смотрят линки роутера |
|-------------------|----------------------------|
| **Нижний** (f1, ближе к ЦОД) | **Вниз** → в ЦОД (на edge `@c1/b1`) **и вверх** → на роутер следующего этажа |
| **Средний** (f2) | **Вниз** → этаж ниже **и вверх** → этаж выше |
| **Верхний / последний** (f3) | **Только вниз** → на этаж ниже. Вверх **не** ведём (конец блока) |

Клиенты **не** висят напрямую на Tower Link: `клиенты → Blade → этажный роутер → линк вниз/вверх`.

Money (VOIP/Git) и обычно DNS/DHCP блока остаются **в ЦОД**. На жилых этажах — Blade + этажный роутер, не voip-сервер.

```mermaid
flowchart TB
  subgraph dc [ЦОД]
    edge["@c1/b1 edge"]
    fw["@c1/b1/fw"]
    core["@c1"]
    svc["@c1/svc + dns/voip/git"]
    edge --> fw --> core --> svc
  end
  subgraph f1 [Этаж 1 · низ блока]
    sw1["Blade + клиенты"] --> r1["роутер @c1/b1/f1"]
  end
  subgraph f2 [Этаж 2]
    sw2["Blade + клиенты"] --> r2["роутер @c1/b1/f2"]
  end
  subgraph f3 [Этаж 3 · последний блока]
    sw3["Blade + клиенты"] --> r3["роутер @c1/b1/f3"]
  end
  r1 -->|вниз Tower Link| edge
  r1 <-->|вверх / вниз| r2
  r2 <-->|вверх / вниз| r3
```

```text
ЦОД:     @c1/b1 ←——(ствол блока)——┐
           ↓ FW                    │
         @c1 → svc → money         │
Этаж 1:  Blade → @c1/b1/f1  down↑  │
                      │ up         │
Этаж 2:  Blade → @c1/b1/f2  ↕      │
Этаж 3:  Blade → @c1/b1/f3  down only
```

### Зачем роутер на каждом этаже

- Префикс `@c1/b1/fN` остаётся на этаже: меньше broadcast на чужих Blade.
- Цепочка: в ЦОД уходит **один** Tower Link от низа блока, а не три параллельных с каждого этажа.
- Граница блока явная: у f3 нет «вверх» → случайно не склеишь b1 с b2.
- Потом проще VLAN/FW на этаже.

### Комплект ролей

| Роль | Имя | Где физически |
|------|-----|----------------|
| Edge блока | `@c1/b1` | **ЦОД** — сюда «вниз» с этажа 1 |
| **Firewall блока** | `@c1/b1/fw` | **ЦОД**, **в разрыв** edge → ядро (режет Morris до `@c1`/`svc`) |
| DNS / DHCP блока | `@c1/b1/dns`, `…/dhcp` | **ЦОД** (рядом с edge или svc) |
| Money | `@c1/svc/voip`, `…/git` | **ЦОД** |
| Ядро | `@c1` | **ЦОД** |
| Этажный роутер | `@c1/b1/f1`, `f2`, `f3` | **На своём этаже** |
| Switch | `@c1/b1/fN/s1` | На своём этаже |
| Клиенты | `@c1/b1/fN/c…` | Патч в Blade |

FW без правил почти прозрачен (default allow) — **от Morris почти не спасает**, пока не повесишь deny/whitelist. Держи **Datawiper USB** на случай, если сам себя отрежешь (tcp/23).

Опционально позже: второй FW перед `@c1/svc` (`@c1/svc/fw`).

### Что НЕ делать

- Вешать этаж 4 «вверх» с `@c1/b1/f3` — это уже блок `b2`, своя цепочка и свой edge.
- Ставить voip/git на этаж 1 «раз уж роутер есть».
- Клиентов в розетку стены минуя Blade и этажный роутер.
- Три линка этаж→ЦОД плюс цепочка одновременно без нужды — выбери **цепочку** (этот гайд).

### Упрощение «очень мало денег» (не основной путь)

Только Blade на этаже и три линка прямо в `@c1/b1` — быстрее старт, хуже ПС и границы блоков. Дальше в гайде везде **цепочка с этажными роутерами**.

---

## Имена (заучи сразу)

```text
@c1                 ядро ЦОД
@c1/b1              edge блока в ЦОД (сюда down с этажа 1)
@c1/b1/fw           firewall блока (между b1 и c1)
@c1/b1/dns          DNS блока
@c1/svc             сервисный роутер money
@c1/svc/voip        VOIP
@c1/svc/git         GitCoffee
@c1/b1/f1           этажный роутер этажа 1 (down→ЦОД, up→f2)
@c1/b1/f2           этажный роутер этажа 2 (down→f1, up→f3)
@c1/b1/f3           этажный роутер этажа 3 (только down→f2)
@c1/b1/f1/s1        switch этажа 1
@c1/b1/f1/c1        клиент
@c1/b1/f2/p1        продюсер
```

Позже: `@c1/b2/f1…f3` для этажей 4–6. **Не** называй DNS просто `@dns`.

---

## Цвета кабелей

Цвет **не влияет на скорость**. Один цвет = один заказ на длину.

| Цвет | Куда |
|------|------|
| **Синий** | Клиент → Blade |
| **Зелёный** | Продюсер / телефон / камера → Blade |
| **Белый** (или серый) | Blade → **этажный роутер** (патч на этаже) |
| **Жёлтый** | Роутер **вниз** (к нижнему этажу или в ЦОД) → розетка riser |
| **Оранжевый** | Роутер **вверх** / ствол в ЦОД (`b1`↔**fw**↔`c1`↔`svc`) |
| **Красный** | В ЦОД к серверам (DNS, VOIP, Git) |
| **Фиолетовый** | Debugger |

Если мало цветов: минимум синий (клиенты), белый (blade↔router), жёлтый (все вертикальные Tower Link), красный (серверы).

Длину меряй **T**.

---

## Список покупок на старт

### Железо в ЦОД

| Кол-во | Что | Имя | Зачем |
|--------|-----|-----|--------|
| 1 | Disco **Micro** | `@c1` | Ядро |
| 1 | Disco **Milli** | `@c1/b1` | Edge блока: принимает down с этажа 1 |
| 1 | **Firewall** (любой стартовый FW) | `@c1/b1/fw` | В разрыв b1→c1; Morris/scraper |
| 1 | Disco **Milli** | `@c1/svc` | Перед money |
| 1 | Boulder+ | `@c1/b1/dns` | DNS |
| 1 | Boulder+ | `@c1/svc/voip` | voip-server |
| 1 | Boulder+ | `@c1/svc/git` | gitcoffee |
| 1 | Debugger | `@me` | netshell |
| 1 | **Datawiper USB** | — | Сброс FW, если залочил tcp/23 |

Отложить: DHCP, второй FW перед svc, NAS, отдельный Padu.  
**Не откладывай надолго:** хотя бы `fcmal` на `@c1/b1/fw` в тот же день, как пошла выручка — вирус иначе сожрёт ПС роутеров.

### Железо на каждый из 3 этажей

| Кол-во | Что | Имя пример | Зачем |
|--------|-----|------------|--------|
| 1 | Blade5 | `@c1/b1/fN/s1` | Клиенты |
| 1 | Роутер (Disco Micro/Milli — по бюджету) | `@c1/b1/fN` | Цепочка up/down |
| питание | на Blade и роутер | — | Иначе тишина |
| патчи | синий / зелёный / белый | — | клиенты, phone, blade↔router |
| вертикаль | жёлтый (down) + оранжевый (up; на f3 up не нужен) | — | в розетки riser |

На этаже 3 (последний блока) **не** покупай второй вертикальный линк «вверх».

### Кабели (ориентир)

| Цвет | Длина | Куда |
|------|-------|------|
| Оранжевый | 200 | ЦОД: b1↔**fw**↔c1↔svc; этажи: **up**-линки f1→f2, f2→f3 |
| Жёлтый | 500–2000 | **down**-линки: f1→ЦОД, f2→f1, f3→f2 |
| Красный | 200–500 | dns/voip/git |
| Белый | 200–500 | Blade → этажный роутер |
| Синий / зелёный | по T | клиенты / phone |
| Фиолетовый | 1000–1500 | debugger |

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

## Порядок подключений: что к чему

Схема дня 1: **цепочка этажных роутеров** + edge/DNS/money в ЦОД. Номера портов — пример; **запиши свои**.

### Общая картина

```mermaid
flowchart TB
  subgraph dc [ЦОД]
    B1["@c1/b1"]
    FW["@c1/b1/fw"]
    CORE["@c1"]
    SVC["@c1/svc"]
    DNS["dns"]
    VOIP["voip"]
    GIT["git"]
    B1 --> FW --> CORE --> SVC
    SVC --> DNS
    SVC --> VOIP
    SVC --> GIT
  end
  R1["@c1/b1/f1 + Blade"] -->|down жёлтый TL| B1
  R1 -->|up оранжевый TL| R2["@c1/b1/f2 + Blade"]
  R2 -->|up оранжевый TL| R3["@c1/b1/f3 + Blade"]
  R3 -->|down only| R2
```

Вертикальные линки = кабель в розетку + **Tower Link** в телефоне.  
FW стоит **на пути** пакетов (кабель через него), не «рядом для красоты».

---

### ЦОД — порядок патчинга

#### 0. Питание

`@c1`, `@c1/b1`, `@c1/b1/fw`, `@c1/svc`, dns, voip, git → розетки ЦОД.

#### 1. Ствол с firewall (оранжевый ~200)

| # | От | Port | К | Port |
|---|----|------|---|------|
| 1 | `@c1/b1` | **7** | `@c1/b1/fw` | **0** (вход со стороны блока) |
| 2 | `@c1/b1/fw` | **1** | `@c1` | **0** (выход к ядру) |
| 3 | `@c1` | **1** | `@c1/svc` | **7** |

```text
@c1/b1 port7 ===== FW ===== @c1 port0 / port1 ===== @c1/svc port7
                 (в разрыве)
```

Магазинный FW ведёт себя как «фильтрующий switch»: отдельные `route` на нём обычно не нужны. Важно только: трафик **реально проходит** через него.

#### 2. Серверы (красный)

| # | От `@c1/svc` | К |
|---|--------------|---|
| 4 | port**0** | `@c1/b1/dns` |
| 5 | port**1** | `@c1/svc/voip` |
| 6 | port**2** | `@c1/svc/git` |

#### 3. Одна розетка под ствол блока (не три!)

| # | Розетка ЦОД | К | Port edge |
|---|-------------|---|-----------|
| 7 | под линк с этажа 1 | `@c1/b1` | **0** |

#### 4. Debugger

Фиолетовый → свободный порт `@c1` (не 0/1). Пока настраиваешь FW — удобнее debugger **со стороны ядра** (за FW), иначе при ошибке deny можешь отрезать себе доступ с этажа.

#### Схема портов ЦОД (заполни)

```text
@c1/b1      0   ← TL down с @c1/b1/f1     serial ____
@c1/b1      7   → оранжевый → FW port0
@c1/b1/fw   0   ← от b1
@c1/b1/fw   1   → оранжевый → @c1 port0
@c1         0   ← от FW
@c1         1   → @c1/svc port7
@c1/svc     7   ← @c1
@c1/svc     0/1/2 → dns / voip / git
```

---

### Этаж — общий шаблон патчинга

На каждом этаже сначала локаль, потом вертикаль.

| # | Действие | От | К | Цвет |
|---|----------|----|---|------|
| 1 | Питание | розетка | Blade + этажный роутер | питание |
| 2 | Клиенты | consumer | Blade | синий |
| 3 | Phone / cam / producer | устройство | Blade | зелёный |
| 4 | Агрегация | Blade | **этажный роутер** portL (local) | **белый** |
| 5 | Down | этажный роутер portD | розетка «вниз» | **жёлтый** |
| 6 | Up (если не последний этаж блока) | этажный роутер portU | розетка «вверх» | **оранжевый** |
| 7 | Tower Link | serial своей розетки | serial парной на соседнем этаже / ЦОД | — |

```text
Клиенты ──синий──┐
Phone ──зелёный──┼→ Blade ──белый──→ Роутер этажа
Producer ────────┘                    │
                         portD down ──жёлтый──→ розетка вниз → TL
                         portU up ────оранж.──→ розетка вверх → TL
                                      (на последнем этаже блока portU нет)
```

---

### Этаж 1 (низ блока) — down в ЦОД + up на этаж 2

| Порт роутера `@c1/b1/f1` (пример) | Куда |
|-----------------------------------|------|
| port**0** (local) | белый ← Blade f1 |
| port**7** (down) | жёлтый → розетка → **TL на `@c1/b1` port0 в ЦОД** |
| port**1** (up) | оранжевый → розетка → **TL на `@c1/b1/f2` down** |

Маршруты на f1 (идея):

```text
rca @c1/b1/f1/s1 0 @c1/b1/f1
rca @c1/b1/f1 0 @c1/b1/f1
rcd 7 @c1/b1/f1
rca @c1/b1/f2 1 @c1/b1/f1
rca @c1/b1/f3 1 @c1/b1/f1
```

На `@c1/b1` в ЦОД: `rca @c1/b1/f1 0 @c1/b1` (весь блок приходит через f1).

---

### Этаж 2 (середина) — down + up

| Порт `@c1/b1/f2` | Куда |
|------------------|------|
| port**0** local | Blade f2 |
| port**7** down | TL → up-порт `@c1/b1/f1` |
| port**1** up | TL → down-порт `@c1/b1/f3` |

```text
rcd 7 @c1/b1/f2
rca @c1/b1/f3 1 @c1/b1/f2
rca @c1/b1/f2/s1 0 @c1/b1/f2
```

---

### Этаж 3 (последний блока) — только down

| Порт `@c1/b1/f3` | Куда |
|------------------|------|
| port**0** local | Blade f3 |
| port**7** down | TL → up-порт `@c1/b1/f2` |
| up | **нет** — конец блока |

```text
rcd 7 @c1/b1/f3
rca @c1/b1/f3/s1 0 @c1/b1/f3
```

Не делай Tower Link с этажа 3 на этаж 4 «на вырост» — для этажей 4–6 будет блок `b2` со своей цепочкой и своим down в `@c1/b2`.

---

### Таблица Tower Link блока b1

| Линк | From | To | Зачем |
|------|------|----|-------|
| A | floor 0, serial к `@c1/b1` port0 | floor 1, serial down `@c1/b1/f1` | ствол блока в ЦОД |
| B | floor 1, serial up f1 | floor 2, serial down f2 | вверх/вниз |
| C | floor 2, serial up f2 | floor 3, serial down f3 | вверх/вниз |
| — | этаж 3 вверх | — | **не создавать** |

На старт везде **cat1**; апгрейд по View Links.

---

### Чего не делать

| Ошибка | Почему |
|--------|--------|
| Три линка этаж→ЦОД вместо цепочки | Ломает идею блока; лишняя ПС/дневка |
| Up с f3 на этаж 4 | Склеишь b1 и b2 |
| Клиент → розетка стены | Минуя Blade и этажный роутер |
| Blade → ЦОД минуя этажный роутер | Нет границы этажа / prefix |
| voip в порт этажного роутера | Money остаётся в ЦОД |
| FW воткнут «сбоку», трафик мимо | Вирус и scraper идут в ядро как раньше |
| `fcall` / default deny без allow **tcp/23** | Локаут; нужен Datawiper |

---

### Мини-чеклист патчинга

**ЦОД**

- [ ] Питание c1, b1, **fw**, svc, dns, voip, git
- [ ] b1 → **fw** → c1 → svc оранжевый
- [ ] svc → dns/voip/git красный
- [ ] Одна розетка: b1 port0 ← будущий down с этажа 1
- [ ] Debugger (лучше со стороны ядра)
- [ ] Datawiper под рукой до жёсткого whitelist

**Этажи**

- [ ] На 1–3: питание Blade + роутер
- [ ] Клиенты → Blade → белый → роутер
- [ ] f1: down→ЦОД, up→f2 (+ TL)
- [ ] f2: down→f1, up→f3 (+ TL)
- [ ] f3: только down→f2 (+ TL)
- [ ] Link lights на A/B/C

---

# ЧАСТЬ I — День 1 (пошагово)

Делай строго по порядку. Не прыгай к DNS, пока не горят линки.

## Шаг 0. Осмотрись

1. Ты в **ЦОД (floor 0)** — стойки, розетки сети и питания.  
2. На телефоне открой **Surveyor** — посмотри этажи 1–3: кто consumer, кто producer, часы активности.  
3. Запиши в блокнот (или clipboard игры) serial’ы розеток riser, которые будешь использовать.

## Шаг 1. Расставь железо в ЦОД и запатчь по схеме

Полная таблица: [Порядок подключений](#порядок-подключений-что-к-чему). Кратко по ЦОД:

1. Micro → `@c1`, Milli → `@c1/b1`, **FW** → `@c1/b1/fw`, Milli → `@c1/svc`, Boulder+ ×3 → dns/voip/git.  
2. Питание.  
3. Оранжевый: `b1 port7` → `fw` → `c1 port0`; `c1 port1` ↔ `svc port7`.  
4. Красный: svc → dns / voip / git.  
5. **Одна** розетка ЦОД → `b1 port0` (down с этажа 1).  
6. Debugger в `@c1`. Datawiper в инвентаре.  

Этажные роутеры — шаг 7–8. Правила FW — шаг 5b (после того как ping/деньги живы).

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

```text
ncall @c1 @c1/b1/dns HARDWARE_ID_ЯДРА
ncall @c1/b1 @c1/b1/dns HARDWARE_ID_EDGE
ncall @c1/b1/fw @c1/b1/dns HARDWARE_ID_FW
ncall @c1/svc @c1/b1/dns HARDWARE_ID_SVC
ncall @c1/b1/dns @c1/b1/dns HARDWARE_ID_DNS
ncall @c1/svc/voip @c1/b1/dns HARDWARE_ID_VOIP
ncall @c1/svc/git @c1/b1/dns HARDWARE_ID_GIT
```

Этажные роутеры назовёшь на этажах (`@c1/b1/f1` … `f3`).

## Шаг 4. Программы на серверах

```text
pidns2 @c1/b1/dns
pivoip @c1/svc/voip
pigitc @c1/svc/git
pip1 @c1/svc/git
```

Автозапуска нет — убедись, что программы реально работают (`watch @c1/b1/dns`, и т.д.). Если в билде нужен явный start — смотри `man program` / `program list`.

## Шаг 5. Маршруты в ЦОД

Пока только ядро↔edge↔svc. Этажную цепочку добавишь после патчинга этажей.

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

После линка с этажа 1:

```text
rca @c1/b1/f1 0 @c1/b1
```

(весь блок `@c1/b1/f…` приходит на edge через f1.)

Проверка: `rsh @c1` · `ping @c1/b1/dns`.

## Шаг 5b. Firewall против Morris (не откладывай на неделю)

Железо уже в разрыве b1→c1. Без правил FW ≈ прозрачный — **вирус проходит**.

1. Имя уже есть: `ncall @c1/b1/fw …` (шаг 3).  
2. **Сначала мягкий режим** (default allow + deny мусора) — почти не ломает выручку:

```text
fcmal @c1/b1/fw
fsh @c1/b1/fw
```

Режет scraper `tcp/8034` и Morris `tcp/510–519`.

3. Когда `ping`/`trace`/деньги стабильны — можно ужесточить whitelist:

```text
fcsafe @c1/b1/fw
```

или `fcall` (то же по сути). **tcp/23 всегда первым в allow**, иначе Datawiper.

4. Проверка после правил: `ping @c1/b1/f1` с ядра, `trc @c1/svc/voip from @c1/b1/f1/c1`, управление netshell на FW живо.

5. Если отрезал себя → Datawiper USB на FW = factory reset → снова с `fcmal`.

Костыль без железа FW (хуже): blackhole на edge `rcbh tcp/8034 EMPTY_PORT @c1/b1` (+ пачка на 510–519) — не замена нормальному FW.

Позже: второй FW перед `@c1/svc` тем же рецептом.

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

## Шаг 7. Этаж 1 (низ блока)

Не «тот же рецепт, что 2 и 3»: здесь **down в ЦОД** и **up на этаж 2**. Подробная таблица портов — в [порядке подключений](#этаж-1-низ-блока--down-в-цод--up-на-этаж-2).

1. Blade + этажный роутер, питание на оба.  
2. Клиенты/phone → Blade (синий/зелёный).  
3. Blade → роутер белым (local port).  
4. Жёлтый down-порт роутера → розетка → **Tower Link** на `@c1/b1` port0 в ЦОД.  
5. Оранжевый up-порт → розетка → (пока можно не активировать TL, пока не готов этаж 2).  
6. Имена:

```text
ncall @c1/b1/f1 @c1/b1/dns ROUTER_HW
ncall @c1/b1/f1/s1 @c1/b1/dns SWITCH_HW
ncall @c1/b1/f1/c1 @c1/b1/dns CLIENT_HW
```

7. Маршруты на этажном роутере + на edge:

```text
rca @c1/b1/f1/s1 0 @c1/b1/f1
rcd 7 @c1/b1/f1
rca @c1/b1/f1 0 @c1/b1
```

8. Проверка: `ping @c1/b1/f1` · `trc @c1/svc/voip from @c1/b1/f1/c1`.

## Шаг 8. Этажи 2 и 3 (цепочка)

**Этаж 2:** Blade + роутер `@c1/b1/f2`; down TL → up этажа 1; up TL → down этажа 3.

```text
ncall @c1/b1/f2 @c1/b1/dns ROUTER2_HW
ncall @c1/b1/f2/s1 @c1/b1/dns SWITCH2_HW
rcd 7 @c1/b1/f2
rca @c1/b1/f2/s1 0 @c1/b1/f2
rca @c1/b1/f2 1 @c1/b1/f1
rca @c1/b1/f3 1 @c1/b1/f1
```

**Этаж 3 (последний блока):** то же, но **только down** на f2 — без up и без TL на этаж 4.

```text
ncall @c1/b1/f3 @c1/b1/dns ROUTER3_HW
ncall @c1/b1/f3/s1 @c1/b1/dns SWITCH3_HW
rcd 7 @c1/b1/f3
rca @c1/b1/f3/s1 0 @c1/b1/f3
rca @c1/b1/f3 1 @c1/b1/f2
```

Продюсера: `ncall @c1/b1/f2/p1 …` + `dmap имя.xxx @c1/b1/f2/p1 @c1/b1/dns`.

Активируй Tower Link B и C из [таблицы](#таблица-tower-link-блока-b1); link lights на всех трёх вертикалях.

## Шаг 9. Телефон (VOIP-выручка)

1. Телефон → Blade (зелёный) → уже в этажном роутере.  
2. `ncall @c1/b1/f2/phone @c1/b1/dns HW`.  
3. Traffic к VOIP вверх по цепочке / default:

```text
rcat udp/5060 7 @c1/b1/f2
rcat udp/5060 7 @c1/b1/f1
rcat udp/5060 0 @c1/b1
rcat udp/5060 PORT_VOIP @c1/svc
```

(или опирайся на default down, если он уже ведёт в ЦОД — проверь `trace`.)  
4. `watch @c1/svc/voip`.

## Шаг 10. День 1 готов, если

- [ ] В ЦОД: edge + **fw** + svc + dns/voip/git  
- [ ] `fcmal` (или `fcsafe`) на `@c1/b1/fw`; Datawiper есть  
- [ ] На этажах 1–3: Blade **и** этажный роутер  
- [ ] TL: ЦОД↔f1, f1↔f2, f2↔f3; у f3 **нет** up  
- [ ] `ping` / `trace` с клиента до voip через цепочку  
- [ ] Registry + `dmap`  
- [ ] Имена `@c1/b1/fN/…`  

**Не делай:** up с этажа 3 на 4; три параллельных линка этаж→ЦОД; voip на жилом этаже; FW мимо трафика; whitelist без tcp/23.

---

## Что ещё легко забыть (чеклист «дырок»)

| Тема | Зачем | Когда |
|------|-------|--------|
| **Firewall + Datawiper** | Morris/scraper; иначе жрут ПС и роутеры | День 1, шаг 5b |
| **Автозапуск программ выкл** | После `program install` проверь `watch` / список — сервис может «лежать» | После install |
| **Ручной `dns map`** | Авто-DNS выкл; Registry без map = пустой домен | Шаг 6 + каждый продюсер |
| **Публичный телефон** | Без него Accept-VOIP часто не капает | Шаг 9 |
| **Карта продюсера в DNS** | Surveyor → имя → `dmap` на `@…/p1` | Когда producer онлайн |
| **Запись портов/serial/цветов** | Авария / замена железа без фото = ад | С первого патча |
| **Питание этажа (лимит)** | На жилых этажах бывает лимит Вт — Secretariat снимает; ЦОД обычно без лимита | Если «не хватает мощности» |
| **Счёт за электричество** | Бесплатного света нет — не плоди железо «на склад» включённым | Всегда |
| **View Links / cat** | 100% traversals → апгрейд cat (краткий outage) | Как только краснеет |
| **Warranty** | После гарантии железо дохнет вместе с routes/FW | Документируй; ASAP Remote Backups |
| **Не втыкать заражённое в чистое ядро** | Сначала отрежь uplink блока, чисти снизу вверх | При Morris |
| **Чистка Morris** | Сервер: `program uninstall`; роутер: `sftp rm` (`morrt`) — нужен sftp | После unlock бэкапов |
| **Камеры udp/554** | Как телефон: зелёный патч + `rcat udp/554` к CCTV/серверу | Если есть cam usage |
| **Socketeer** | Мало розеток в ЦОД под FW/NAS/второй блок | По мере спагетти |
| **Свободный порт под второй FW / NAS** | Не забить все порты `@c1` в день 1 | Раскладка стоек |
| **Этаж-4 ≠ up с f3** | Новый блок `b2`, своя цепочка | Рост башни |
| **Blackhole scraper** | Временная защита, если FW ещё нет | `rcbh` на edge |
| **Алиасы в userdata** | Потеряются при смене ПК/профиля — бэкапь `settings.json` | После набора алиасов |

---

# ЧАСТЬ II — Расширение (что зачем)

Ниже — **по пунктам**: зачем нужно → когда брать → что купить → как настроить → алиасы.  
Деньги и **базовый FW (`fcmal`)** — в части I. Дальше: жёсткий whitelist, бэкапы, DHCP, рост.

## Обзор приоритетов

| Порядок | Тема | Зачем | Unlock / цена (ориентир) |
|---------|------|-------|---------------------------|
| 0 | **FW блока (`fcmal`)** | Morris — уже в дне 1 | Железо FW + Datawiper |
| 1 | Блок b2 (этажи 4–6) | Не убить ПС и b1 | Магазин + бюджет |
| 2 | FW whitelist / FW перед svc | Жёстче политика | `fcsafe` / второй FW |
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

### Куда ставить серверы блока b2

| | Edge + DNS блока | Этажные роутеры |
|--|------------------|-----------------|
| Как в b1 | `@c1/b2` (+ dns) в **ЦОД** | `@c1/b2/f1…f3` на этажах 4–6, цепочка; down с f1 → `@c1/b2` |
| Хаб наверху | Edge+DNS можно на этаже 4 | Цепочка 4↔5↔6; down хаба → `@c1` |

Money (`@c1/svc`) не переезжает. **Не** линкуй up с `@c1/b1/f3` на этаж 4.

### Что купить
- Milli → `@c1/b2` + Boulder DNS  
- Роутер ×3 + Blade ×3 на этажи 4–6  
- Жёлтый/оранжевый под down/up цепочки + один TL ствол b2→ЦОД  

### Настройка

```text
ncall @c1/b2 @c1/b2/dns EDGE_HW
ncall @c1/b2/dns @c1/b2/dns DNS_HW
pidns2 @c1/b2/dns
dmap voip.none @c1/svc/voip @c1/b2/dns
dmap git.none @c1/svc/git @c1/b2/dns
rca @c1/b2 PORT @c1
rca @c1/b2/f1 0 @c1/b2
```

Дальше — та же цепочка f1↔f2↔f3 внутри b2, у f3 только down.

---

## 2. Firewall (углубление)

Базовый `@c1/b1/fw` + `fcmal` уже в **части I**. Здесь — ужесточение и второй контур.

### Зачем ещё
Whitelist вместо «всё кроме Morris»; отдельный FW перед money, чтобы заражённый блок не стучался в voip/git лишним.

### Что купить
Второй FW → `@c1/svc/fw` в разрыв `c1` → `svc` (или `svc` → серверы). Datawiper уже должен быть.

### Настройка
На блочном FW, когда стабильно: `fcsafe @c1/b1/fw`.  
На money: `fcsafe @c1/svc/fw` или `fcall`.  
Клон правил после sftp: `cpfw @c1/b1/fw @c1/svc/fw`.

Симптомы Morris: роутер «тупит», ПС проседает, в `program list` / `sftp ls` появляется morris. **Отрежь uplink блока** от ядра → чисти → только потом возвращай линк.

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

- [ ] ЦОД: Micro + Milli edge + **FW** + Milli svc + 3× Boulder  
- [ ] Datawiper; `fcmal` на `@c1/b1/fw`  
- [ ] Этажи 1–3: Blade **+ этажный роутер** каждый  
- [ ] Цепочка TL: ЦОД↔f1, f1↔f2, f2↔f3; у f3 нет up  
- [ ] `always using` / алиасы  
- [ ] Имена включая `@c1/b1/fw`, `@c1/b1/f1…f3`  
- [ ] Программы dns + voip + git (и они реально запущены)  
- [ ] Маршруты ствола + цепочки  
- [ ] Registry + `dmap` + телефон  
- [ ] Порты/serial записаны  
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
