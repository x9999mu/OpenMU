# Hướng dẫn chơi OpenMU x9999

Tài liệu này dành cho người mới tham gia server. Mục tiêu của server là **lên cấp nhanh, mua đủ đồ khởi đầu, săn box và boss cùng nhóm khoảng 3 người**, sau đó thử build nhân vật hoặc PvP mà không phải cày cuốc kéo dài.

## 1. Thông tin nhanh

| Nội dung | Cấu hình hiện tại |
| --- | --- |
| Phiên bản | OpenMU Season 6 |
| Cấp thường tối đa | 400 |
| EXP | x9999 |
| Điểm mỗi lần lên cấp | 500; nhân vật đã hoàn thành Hero Status nhận 501 |
| Giới hạn mỗi stat | 32.767 |
| Zen nhặt được | x1000 |
| Quái tại spot | Thông thường tối thiểu 10 con; mỗi spot Icarus có 3 con; Kalima 7 có 60 quái thường phân bố ngẫu nhiên trong vùng X 28–121, Y 6–109; hồi sinh khoảng 5 giây; toàn bộ chỉ số quái thường ×1,47, Kundun 7 giữ nguyên; quái thường không rơi Zen/Box Kundun +1–+3, Box Kundun +4 tỷ lệ 50%, Box Kundun +5 tỷ lệ 40% |
| PvP/PK | Đang bật; kỹ năng diện rộng có thể đánh trúng người chơi |
| Nhịp boss | Golden → Red Dragon → White Wizard, đổi event mỗi 10 phút |

Với 500 điểm mỗi cấp, một nhân vật đi từ level 1 đến 400 nhận khoảng **199.500 điểm**, đủ để đưa các stat cần thiết lên gần hoặc tới giới hạn. Vì vậy server không yêu cầu reset nhiều lần mới có một build hoàn chỉnh.

## 2. Kết nối server

### Trong mạng LAN

```text
Địa chỉ: 192.168.1.28
Connect Server: 44405
```

### Qua mạng riêng/Tailscale

```text
Địa chỉ: 100.108.169.118
Connect Server: 44405
```

Một số bộ client có thể được cấu hình dùng cổng `44406`. Nếu client được phát kèm file cấu hình thì nên giữ nguyên cổng ghi trong file đó.

## 3. Nên bắt đầu như thế nào?

1. Tạo nhân vật thuộc class muốn chơi.
2. Tới NPC trang bị và NPC skill tương ứng với class.
3. Mua bộ giáp và vũ khí Excellent +9, Luck và normal option tối đa.
4. Mua đầy đủ sách, ngọc hoặc orb học skill có thể sử dụng.
5. Mua Large Healing Potion và Large Mana Potion; mỗi bình có thể chứa tối đa 255 lần dùng.
6. Lập party rồi luyện tại các spot đông quái cho tới level 400; quái luôn cho Zen và chỉ quay thêm ngọc hoặc box, không rơi trang bị trực tiếp.
7. Nhặt Box of Kundun +1 đến +3 từ quái thường để nâng dần trang bị.
8. Mua item change class tại Potion Girl Amy khi cần làm quest chuyển class.
9. Farm Icarus để săn Box +4; theo thông báo invasion và tập trung nhóm săn boss để lấy Box +4, +5 hoặc GM Gift Full Option.
10. Sau khi hoàn thiện build, thử đấu PvP hoặc đổi cách phân bổ stat.

## 4. Shop trang bị và skill

### Dark Wizard / Soul Master

- **Pasi the Mage (254), Lorencia**
  - Pad Set Excellent +9, Luck và normal option tối đa.
  - Skull Staff và Serpent Staff Excellent +9, Luck và normal option tối đa.
  - Sách/ngọc skill dành cho Dark Wizard và Magic Gladiator.
- **Izabel the Wizard (245), Devias**
  - Bản shop thuận tiện tại Devias, bán lại trang bị Dark Wizard và skill Dark Wizard/Magic Gladiator.

### Dark Knight / Blade Knight

- **Hanzo the Blacksmith (251), Lorencia**
  - Leather Set Excellent +9, Luck và normal option tối đa.
  - Blade và Gladius Excellent +9, Luck và normal option tối đa.
- **Alex (230), Lorencia**
  - Ngọc/orb skill của Dark Knight.

### Fairy Elf / Muse Elf

- **Eo the Craftsman (243), Noria**
  - Vine Set Excellent +9, Luck và normal option tối đa.
  - Short Bow và Battle Bow Excellent +9, Luck và normal option tối đa.
- **Elf Lala (242), Noria**
  - Toàn bộ skill tiêu hao dành cho Fairy Elf.
  - Summoning Orb level 0–6; mỗi level triệu hồi một loại quái khác nhau.

### Magic Gladiator

- **Hanzo the Blacksmith (251), Lorencia**
  - Storm Crow Set Excellent +9, Luck và normal option tối đa.
  - Blade và Skull Staff Excellent +9, Luck và normal option tối đa.
- Skill dùng chung với Dark Wizard được bán tại **Pasi** hoặc **Izabel**.

### Dark Lord

- **Hanzo the Blacksmith (251), Lorencia**
  - Light Plate Set Excellent +9, Luck và normal option tối đa.
  - Battle Scepter và Master Scepter Excellent +9, Luck và normal option tối đa.
- **Alex (230), Lorencia**
  - Skill tiêu hao dành cho Dark Lord.

### Summoner

- **Rhea (416), Elvenland**
  - Red Wing Set Excellent +9, Luck và normal option tối đa.
  - Violent Wind Stick, Book of Sahamutt và Book of Neil Excellent +9, Luck và normal option tối đa.
- **Marce (417), Elvenland**
  - Toàn bộ sách skill dành cho Summoner.

### Rage Fighter

- **Hanzo the Blacksmith (251), Lorencia**
  - Sacred Set Excellent +9, Luck và normal option tối đa.
  - Sacred Glove và Storm Hard Glove Excellent +9, Luck và normal option tối đa.
- **Alex (230), Lorencia**
  - Skill tiêu hao dành cho Rage Fighter.

### Shop vũ khí chung tại Devias

**Zienna the Weapons Merchant (246)** bán các vũ khí khởi đầu Excellent +9, Luck và normal option tối đa cho Dark Knight, Fairy Elf, Magic Gladiator, Dark Lord và Rage Fighter. Skill đầy đủ vẫn nằm tại shop quê nhà của từng class.

## 5. Shop vật phẩm thiết yếu

Các NPC general-goods ở nhiều thành đều bán:

- Large Healing Potion +1, 255 lần dùng.
- Large Mana Potion +1, 255 lần dùng.
- Antidote.
- Bolt và Arrow.
- Town Portal Scroll.
- Armor of Guardsman.

Riêng **Potion Girl Amy (253)** còn bán:

- Toàn bộ item quest chuyển class: Scroll of Emperor, Ring of Honor, Broken Sword, Dark Stone, Tear of Elf, Soul Shard of Wizard, Flame of Death Beam Knight, Horn of Hell Maine, Feather of Dark Phoenix và Eye of Abyssal.
- Chaos Dragon Axe, Chaos Nature Bow và Chaos Lightning Staff **+4 +4 option** để đưa thẳng vào công thức tạo cánh cấp 1.
- Jewel of Bless, Soul, Chaos, Life và Creation dạng viên lẻ; đồng thời có Packed Jewel tương ứng loại 10, 20 và 30 viên.
- Loch's Feather thường, Loch's Feather +1 (Monarch's Crest), Flame of Condor và Feather of Condor cho các công thức cánh/cape cao hơn.
- **Golden Cherry Blossom Branch** — dùng để **mở thêm 4 hàng túi đồ**, mỗi lần dùng thêm 1 lần mở rộng, tối đa 4 lần. Giá **500.000.000 Zen**.

> **Cách mở rộng túi đồ:** mua Golden Cherry Blossom Branch ở Potion Girl Amy rồi **thả item xuống đất**. Server sẽ dùng item để mở rộng túi thay vì để nó rơi xuống đất. Một số client không gửi lệnh "dùng" cho item này, nên thao tác thả xuống đất là cách dùng chính thức; nếu client của bạn cho phép nhấn đúp thì cách đó cũng hoạt động.
> Nếu túi chưa thấy thêm hàng ngay, thoát ra vào lại nhân vật. Khi đã đủ 4 lần mở rộng, thả item sẽ rơi xuống đất bình thường.

Riêng **Lumen the Barmaid (255)** bán vé vào event hoàn chỉnh: Invisibility Cloak +1 đến +8 (Blood Castle), Devil's Invitation +1 đến +7 (Devil Square), Scroll of Blood +1 đến +6 (Illusion Temple), Armor of Guardsman (Chaos Castle), Lost Map +7 (Kalima 7), cùng Ale.

### Công thức chế tạo Wings

Thực hiện tại **Chaos Goblin/Chaos Machine**. Tất cả 29 công thức của máy Chaos có tỷ lệ thành công cố định **100%**, bao gồm Wings, cape, vé event và nâng cấp Fenrir. Wings/cape tạo thành luôn có **Luck** và normal option tối đa; nếu loại đó hỗ trợ Recover HP thì hệ thống luôn ưu tiên Recover HP, nếu không sẽ dùng option gốc của loại Wings. Mỗi special wing line vẫn được quay độc lập với tỷ lệ **90%**, vì vậy khả năng ra full dòng rất cao.

#### Wings cấp 1

| Nguyên liệu | Yêu cầu |
| --- | --- |
| Chaos Weapon | 1 Chaos Dragon Axe, Chaos Nature Bow hoặc Chaos Lightning Staff, tối thiểu **+4 +4 option** |
| Jewel of Chaos | 1 viên |
| Trang bị thường bổ sung | Không bắt buộc; tối thiểu **+4 +4 option** |
| Jewel of Bless/Soul | Không bắt buộc |

Potion Girl bán sẵn ba Chaos Weapon **+4 +4 option có Luck**, nên có thể mua một món rồi đưa thẳng vào Chaos Machine. Kết quả ngẫu nhiên: Wings of Elf, Wings of Heaven, Wings of Satan hoặc Wings of Curse.

#### Wings cấp 2

| Nguyên liệu | Yêu cầu |
| --- | --- |
| Wings cấp 1 | 1 cánh, từ +0 trở lên |
| Loch's Feather | 1 cái |
| Jewel of Chaos | 1 viên |
| Trang bị Excellent bổ sung | Không bắt buộc; tối thiểu +4 |

Chi phí cơ bản **5.000.000 Zen**. Tỷ lệ thành công cố định **100%**. Kết quả ngẫu nhiên: Wings of Spirit, Wings of Soul, Wings of Dragon, Wings of Darkness hoặc Wings of Despair.

#### Cape of Lord hoặc Cape of Fighter

| Nguyên liệu | Yêu cầu |
| --- | --- |
| Wings cấp 1 | 1 cánh, từ +0 trở lên |
| Monarch's Crest | 1 **Loch's Feather +1** |
| Jewel of Chaos | 1 viên |
| Trang bị Excellent bổ sung | Không bắt buộc; tối thiểu +4 |

Chi phí cơ bản **5.000.000 Zen**. Tỷ lệ thành công cố định **100%**. Chaos Machine chọn ngẫu nhiên Cape of Lord hoặc Cape of Fighter.

#### Wings cấp 3 — bước 1: tạo Feather of Condor

| Nguyên liệu | Yêu cầu |
| --- | --- |
| Wings cấp 2 hoặc cape | 1 món **+9 trở lên, có option** |
| Trang bị Ancient | 1 món **+7 trở lên, có option** |
| Jewel of Chaos | 1 viên |
| Jewel of Creation | 1 viên |
| Packed Jewel of Soul | 1 pack 10 viên, tức Packed Soul +0 |

Tỷ lệ thành công cố định **100%**. Kết quả là **Feather of Condor**. Vì Potion Girl cũng bán sẵn Feather of Condor, có thể bỏ qua bước này nếu chỉ muốn chế tạo nhanh.

#### Wings cấp 3 — bước 2

| Nguyên liệu | Yêu cầu |
| --- | --- |
| Trang bị Excellent | 1 món **+9 trở lên, có option** |
| Feather of Condor | 1 cái |
| Flame of Condor | 1 cái |
| Jewel of Chaos | 1 viên |
| Jewel of Creation | 1 viên |
| Packed Jewel of Bless | 1 pack 10 viên, tức Packed Bless +0 |
| Packed Jewel of Soul | 1 pack 10 viên, tức Packed Soul +0 |

Tỷ lệ thành công cố định **100%**. Kết quả ngẫu nhiên: Wings of Storm, Wings of Eternal, Wings of Illusion, Wings of Ruin, Cape of Emperor, Wings of Dimension hoặc Cape of Overrule.

> **Lưu ý:** Packed Jewel +0/+1/+2 lần lượt đại diện cho pack **10/20/30 viên**. Công thức Wings cấp 3 chỉ yêu cầu pack 10 viên, vì vậy hãy mua bản **+0**.

Với Wings cấp 2 và Cape of Fighter có ba special lines, xác suất nhận đủ cả ba là khoảng **72,9%**. Cape of Lord và Wings cấp 3 có bốn special lines, nên xác suất full bốn dòng là khoảng **65,6%** (`90%` cho từng dòng, quay độc lập).

## 6. Trang bị Excellent trong shop

Đồ shop là bộ khởi đầu mạnh, nhưng vẫn không bằng phần thưởng full-option từ Box +4/+5 hoặc GM Gift:

- Mọi trang bị hỗ trợ normal option đều được bán sẵn ở **+9**, có **Luck** và normal option cấp tối đa; không cần gắn thêm Jewel of Life.
- Vũ khí giữ skill nếu loại vũ khí đó hỗ trợ và giữ Excellent option tấn công có sẵn.
- Giáp giữ Excellent option phòng thủ/sinh tồn có sẵn.
- Muốn có toàn bộ Excellent lines và các set/vũ khí gần end-game, người chơi vẫn phải săn Box +4, +5 hoặc GM Gift.

## 7. Hướng dẫn săn Box of Kundun

### Lộ trình nhanh

1. Mua đồ shop **+9, Luck, normal option tối đa**, học đủ skill và mang Large Healing Potion 255 lần dùng.
2. Farm quái thường ở map phù hợp để lấy Box +1/+2/+3. Chọn spot giết nhanh thay vì cố đánh quái quá mạnh vì tỷ lệ được tính trên mỗi quái chết.
3. Khi đã đủ stat và đồ shop, chuyển sang **Icarus** để săn Box +4. Mỗi spot Icarus chỉ có 3 quái nhưng quái mạnh hơn rõ rệt; solo được nếu dùng potion đều, đi nhóm sẽ an toàn hơn.
4. Theo thông báo Golden, Red Dragon và White Wizard invasion. Nhóm khoảng 3 người nên tập trung cùng một boss; boss đã được tăng mạnh và đồ shop chưa đủ để giết trong một hit.
5. Dọn sẵn inventory trước khi săn. Box chỉ sinh một món; nếu pool chọn một set thì nhận **một mảnh ngẫu nhiên**, không nhận nguyên bộ.

### Quái thường: Box +1 đến +3

Quái thường không còn rơi giáp, vũ khí, đồ Excellent ngẫu nhiên, item quest hoặc nguyên liệu map. Mỗi lần chết luôn trả Zen, sau đó có đúng một lượt quay phần thưởng phụ:

| Phần thưởng | Tỷ lệ mỗi quái |
| --- | ---: |
| Ngọc ngẫu nhiên | 1% |
| Box of Kundun +1 | 2% |
| Box of Kundun +2 | 1,5% |
| Box of Kundun +3 | 1% |
| Không có phần thưởng phụ | 94,5% |

Ngọc và Box +1/+2/+3 dùng chung một lượt quay nên một quái không đồng thời rơi hai phần thưởng này.

### Icarus: thêm Box +4

- Mọi quái thường trên map **Icarus** có thêm tỷ lệ **5%** rơi Box of Kundun +4.
- Mỗi spot có 3 quái. Quái Icarus có HP, damage, defense và attack rate cao hơn quái luyện cấp thông thường; nên mang nhiều máu và tránh kéo nhiều spot cùng lúc khi solo.
- Box rơi tại Icarus là Box of Kundun **+4 thật** và mở đúng pool +4 bên dưới.

### Boss: Box +4, Box +5 và GM Gift

Mỗi boss hợp lệ có một lượt gacha với tổng xác suất 100%:

| Phần thưởng | Tỷ lệ |
| --- | ---: |
| Box of Kundun +4 | 47,5% |
| Box of Kundun +5 | 47,5% |
| GM Gift Full Option | 5% |

Các boss áp dụng gồm nhóm Golden Monster, Red Dragon, White Wizard, Illusion of Kundun, Erohim, Nightmare, Maya và hai tay Maya, Dark Elf và Selupan. Hộ vệ của White Wizard không được tính là boss. Golden → Red Dragon → White Wizard chạy luân phiên, đổi event mỗi 10 phút; hãy theo thông báo trong game để tìm đúng map.

### Boss Kalima 7: Illusion of Kundun 7

Illusion of Kundun 7 (map **Kalima 7**) không dùng bảng gacha chung ở trên. Mỗi lần hạ boss luôn rơi đủ **9 món** (boss hồi sinh sau 3 giờ):

| Phần thưởng | Số lượng |
| --- | ---: |
| GM Gift Full Option | 3 |
| Box of Kundun +5 | 3 |
| Jewel of Harmony | 1 |
| Jewel of Guardian | 1 |
| Trang bị full option ngẫu nhiên (+9, mọi dòng Excellent) | 1 |

Món full option ngẫu nhiên dùng chung pool với GM Gift (vũ khí và set giáp gần end-game). Boss không rơi Zen, không rơi Box +4 và không rơi Jewel of Bless/Soul/Chaos.

### Ép Box of Kundun (Nâng cấp)

Bạn có thể ghép 10 Box of Kundun cấp thấp thành 1 Box cấp cao hơn tại **Chaos Goblin** thông qua chức năng **Regular Combination** (Kết hợp bình thường). Yêu cầu thêm 1 Jewel of Chaos cho mỗi lần ép. Nếu thất bại, toàn bộ nguyên liệu sẽ biến mất.

| Công thức | Yêu cầu | Tỷ lệ thành công |
| --- | --- | ---: |
| Box of Kundun +2 | 10 Box of Kundun +1, 1 Jewel of Chaos | 100% |
| Box of Kundun +3 | 10 Box of Kundun +2, 1 Jewel of Chaos | 90% |
| Box of Kundun +4 | 10 Box of Kundun +3, 1 Jewel of Chaos | 80% |
| Box of Kundun +5 | 10 Box of Kundun +4, 1 Jewel of Chaos | 70% |

> **Lưu ý quan trọng cho Admin/Người chơi:** Server sử dụng ID (MixType) từ 101 đến 104 cho các công thức này. Do đó, người chơi cần cập nhật/thêm các công thức tương ứng vào file `Data\Local\Mix.bmd` ở Client (với ID 101, 102, 103, 104) để nút "Kết hợp" không bị mờ khi bỏ đủ 10 Box vào.

## 8. Danh sách item trong từng box

### Quy tắc chung

- Mỗi box mở ra đúng **một** item trong pool tương ứng; không còn kết quả Zen.
- Box +1/+2/+3: item Excellent ngẫu nhiên và có Luck nếu definition hỗ trợ; số Excellent lines vẫn được quay ngẫu nhiên.
- Box +4/+5: item cố định **+9, full Excellent, Luck và normal option tối đa** nếu item hỗ trợ; vũ khí có skill nếu definition hỗ trợ và durability được nạp đầy.
- `Full Excellent` nghĩa là lấy toàn bộ Excellent lines mà chính loại item đó hỗ trợ. Một item không có đủ sáu loại option trong definition sẽ không thể hiện đủ sáu dòng.
- Các box không tự thêm Harmony, Ancient/Set option hoặc socket option. Trang bị socket từ GM Gift vẫn có số socket ngẫu nhiên theo khả năng của item.
- Tên `Set` dưới đây đại diện cho các mảnh giáp hợp lệ của set. Khi trúng set, box chỉ trả một mảnh.

### Box of Kundun +1

- **Kiếm:** Kris, Short Sword, Rapier, Katana, Sword of Assassin.
- **Rìu:** Small Axe, Hand Axe, Double Axe, Tomahawk.
- **Chùy:** Mace, Morning Star, Flail.
- **Giáo:** Spear, Dragon Lance, Double Poleaxe, Halberd, Berdysh.
- **Cung/nỏ:** Short Bow, Bow, Elven Bow, Crossbow, Golden Crossbow.
- **Staff/Stick:** Skull Staff, Angelic Staff, Serpent Staff, Mistery Stick, Violent Wind Stick.
- **Khiên:** Small Shield, Horn Shield, Kite Shield, Elven Shield, Buckler.
- **Set:** Leather, Pad, Vine, Bronze, Silk, Violent Wind, Red Wing.

### Box of Kundun +2

- **Kiếm:** Blade, Gladius, Falchion, Serpent Sword, Sword of Salamander, Light Saber.
- **Rìu:** Elven Axe, Battle Axe, Nikea Axe, Larkan Axe.
- **Chùy/Scepter:** Great Hammer, Crystal Morning Star, Battle Scepter, Master Scepter.
- **Giáo/lưỡi hái:** Light Spear, Giant Trident, Serpent Spear, Great Scythe.
- **Cung/nỏ:** Battle Bow, Tiger Bow, Arquebus, Light Crossbow, Serpent Crossbow.
- **Staff/Stick:** Thunder Staff, Gorgon Staff, Red Wing Stick.
- **Khiên:** Dragon Slayer Shield, Skull Shield, Spiked Shield, Tower Shield.
- **Trang sức:** Ring of Ice, Ring of Poison, Pendant of Lighting, Pendant of Fire.
- **Set:** Scale, Brass, Bone, Sphinx, Wind, Spirit, Light Plate, Ancient.

### Box of Kundun +3

- **Kiếm/găng:** Legendary Sword, Heliacal Sword, Double Blade, Lightning Sword, Giant Sword, Sacred Glove.
- **Rìu/chùy/Scepter:** Crescent Axe, Crystal Sword, Chaos Dragon Axe, Elemental Mace, Great Scepter.
- **Giáo:** Bill of Balrog.
- **Cung/nỏ:** Silver Bow, Chaos Nature Bow, Bluewing Crossbow, Aquagold Crossbow.
- **Staff/Stick:** Staff of Resurrection, Legendary Staff, Chaos Lightning Staff, Ancient Stick, Black Rose Stick.
- **Khiên:** Plate Shield, Large Round Shield, Serpent Shield, Bronze Shield, Legendary Shield.
- **Trang sức:** Ring of Fire, Ring of Earth, Ring of Wind, Ring of Magic, Pendant of Ice, Pendant of Wind, Pendant of Water, Pendant of Ability.
- **Set:** Plate, Dragon, Legendary, Guardian, Storm Crow, Adamantine, Black Rose, Sacred.

### Box of Kundun +4 — near end-game

- **Kiếm/găng:** Sword of Destruction, Dark Breaker, Thunder Blade, Divine Sword of Archangel, Knight Blade, Dark Reign Blade, Rune Blade, Storm Hard Glove, Piercing Blade Glove.
- **Scepter:** Lord Scepter, Great Lord Scepter, Divine Scepter of Archangel, Shining Scepter.
- **Giáo:** Dragon Spear.
- **Cung/nỏ:** Saint Crossbow, Celestial Bow, Divine Crossbow of Archangel, Great Reign Crossbow, Arrow Viper Bow.
- **Staff/Stick:** Staff of Destruction, Dragon Soul Staff, Divine Staff of Archangel, Staff of Kundun, Platina Staff, Storm Blitz Stick.
- **Khiên:** Chaos Dragon Shield, Grand Soul Shield, Elemental Shield.
- **Set DK/BK:** Dark Phoenix, Great Dragon.
- **Set DW/SM:** Grand Soul, Dark Soul.
- **Set Elf:** Divine, Red Spirit.
- **Set MG:** Thunder Hawk, Hurricane.
- **Set DL:** Dark Steel, Dark Master.
- **Set Summoner:** Black Rose, Lilium.
- **Set RF:** Storm Hard, Piercing.

### Box of Kundun +5 — level 380

- **Kiếm/găng:** Bone Blade, Explosion Blade, Flamberge, Sword Breaker, Imperial Sword, Phoenix Soul Star.
- **Scepter:** Soleil Scepter.
- **Cung:** Sylph Wind Bow.
- **Staff/Stick:** Grand Viper Staff, Storm Blitz Stick, Eternal Wing Stick, Deadly Staff, Imperial Staff.
- **Set level 380:** Dragon Knight (DK/BK), Venom Mist (DW/SM), Sylphid Ray (Elf), Volcano (MG), Sunlight (DL), Aura (Summoner), Phoenix Soul (RF).

## 9. GM Gift Full Option

GM Gift là jackpot có tỷ lệ **5% từ boss**. Client gốc có thể vẫn hiển thị tên item là `GM Gift`; đây chính là box Full Option cao nhất của server.

GM Gift chứa **toàn bộ vũ khí và set của Box +5**, cộng thêm các set socket end-game:

- Titan.
- Brave.
- Destroy — một số client/data hiển thị tên `Destory`.
- Phantom.
- Seraphim.
- Faith.
- Paewang.
- Hades.
- Queen.

GM Gift có **1%** mở ra một Ring hoặc Pendant ngẫu nhiên: Ring of Ice/Poison/Fire/Earth/Wind/Magic và Pendant of Lightning/Fire/Ice/Wind/Water/Ability. Các trang sức này cũng nhận **+4** (cấp tối đa chúng hỗ trợ), full Excellent, Luck và normal option tối đa; **99%** còn lại giữ pool vũ khí/set nêu trên.

Item nhận được có:

- Level **+9**.
- Toàn bộ Excellent lines mà item hỗ trợ.
- Luck nếu item hỗ trợ.
- Skill nếu vũ khí hỗ trợ skill.
- Normal option cấp tối đa nếu item hỗ trợ.
- Durability tối đa.
- Với trang bị socket: số socket ngẫu nhiên từ 1 đến giới hạn của item; socket chưa được gắn seed option sẵn.

## 10. Lịch invasion boss

Ba invasion chạy thành một vòng liên tục 30 phút:

| Phút trong chu kỳ | Event |
| --- | --- |
| 00–10 | Golden Invasion |
| 10–20 | Red Dragon Invasion |
| 20–30 | White Wizard Invasion |

Sau phút 30, chu kỳ bắt đầu lại. Mỗi event kéo dài 10 phút và không chồng lên hai event còn lại. Hãy theo dõi thông báo trong game để biết event vừa bắt đầu và bản đồ liên quan.

## 11. Cách chơi đề xuất cho nhóm 3 người

### Chia vai trò

- Một nhân vật thiên về sát thương đơn mục tiêu để đánh boss.
- Một nhân vật có buff/hỗ trợ, thường là Fairy Elf.
- Một nhân vật dọn quái, hỗ trợ sát thương hoặc chịu đòn tùy đội hình.

Đây chỉ là gợi ý. Do tốc độ lên cấp và lượng point rất cao, nhóm có thể đổi chiến thuật mà không phải tạo lại toàn bộ tiến trình.

### Vòng chơi ngắn

1. Luyện level, nhặt Zen và ngọc.
2. Mua đồ Excellent +9 có Luck, normal option tối đa và toàn bộ skill cần thiết.
3. Mua item change class hoặc trang bị tạo cánh tại Potion Girl Amy khi cần.
4. Farm quái thường để lấy Kundun +1/+2/+3; quái không rơi trang bị trực tiếp.
5. Farm Icarus để săn Box +4 với tỷ lệ 5% mỗi quái.
6. Gom nhóm theo chu kỳ invasion 10 phút để săn Box +4/+5 và jackpot GM Gift.
7. So sánh build bằng duel hoặc PvP.

### Phân phối đồ trong party

Nên thống nhất trước một trong hai cách:

- Item phù hợp class nào thì ưu tiên class đó.
- Luân phiên quyền nhận box hoặc jackpot giữa các thành viên.

Server dành cho nhóm nhỏ nên chia đồ hợp lý sẽ giúp cả nhóm đạt ngưỡng săn boss nhanh hơn việc một người giữ toàn bộ vật phẩm.

## 12. Lưu ý về PK/PvP

- PvP đang bật trên game server.
- Kỹ năng diện rộng có thể đánh trúng người chơi khác.
- Khi đi invasion, nên lập party và thống nhất khu vực đánh để tránh vô tình PK nhau.
- Không nên đứng AFK tại khu vực boss hoặc spot đang tranh chấp.
- Các quy tắc Blood Castle, Chaos Castle, duel và chi phí `/pkclear` vẫn giữ theo cấu hình Season 6 hiện có.

## 13. Câu hỏi thường gặp

### Vì sao tôi chỉ nhận 5 point khi lên cấp?

Cấu hình hiện tại là 500 point mỗi level, hoặc 501 sau Hero Status. Hãy thoát hẳn nhân vật và đăng nhập lại để nạp cấu hình mới. Nếu vẫn nhận 5 point, báo tên nhân vật cho quản trị viên kiểm tra thuộc tính đã lưu.

### Vì sao quái không rơi trang bị hoặc item quest?

Đây là thiết kế mới của server. Quái chỉ trả Zen và quay thêm ngọc hoặc Box of Kundun. Trang bị lấy từ shop hoặc mở box; item change class mua tại Potion Girl Amy.

### Vì sao Kundun box không rơi Zen?

Kundun +1 đến +5 luôn trả về một item trang bị để giữ nhịp chơi nhanh; Box +4/+5 còn bảo đảm +9 và full Excellent theo khả năng của item.

### Vì sao item Full Excellent không luôn hiện đủ sáu option?

Box +1/+2/+3 vẫn quay số Excellent options ngẫu nhiên. Box +4/+5 và GM Gift lấy toàn bộ Excellent lines mà loại item hỗ trợ, nhưng một definition chỉ có ít hơn sáu loại option thì client chỉ hiện số dòng tương ứng.

### Tôi có cần reset nhiều lần không?

Không bắt buộc. Với 500 point mỗi level, lần lên level 400 đầu tiên đã đủ điểm cho một build rất mạnh và có thể đạt giới hạn các stat cần thiết.

### Skill đã mua nhưng chưa học được?

Shop bán đủ skill phù hợp với class, kể cả skill cấp cao. Yêu cầu level, class và quest của từng skill vẫn được kiểm tra khi sử dụng.

## 14. Tóm tắt cho người mới

```text
Mua đồ Excellent +9 có Luck, normal option tối đa và skill đúng class
→ mua potion 255, item change class hoặc đồ tạo cánh tại Amy
→ lập party lên level 400
→ farm Zen/ngọc/Kundun +1/+2/+3; quái không rơi trang bị
→ farm Icarus để săn Kundun +4
→ theo invasion mỗi 10 phút để săn Kundun +4/+5 và GM Gift
→ hoàn thiện build rồi PvP
```
